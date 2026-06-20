using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using VocabLearning.Data;

namespace VocabLearning.UI
{
    public partial class VocabStudyController
    {
        private QuestJson _currentEditingQuest = null; // Quest đang được chỉnh sửa, null nếu thêm mới
        private string _questAdminCurrentTab = "Pool"; // Tab hiện tại: "Pool" hoặc "Active"

        private void BindAdminQuestEvents()
        {
            // Reset các giá trị khởi tạo nếu null
            if (_jsonDb.questPool == null) _jsonDb.questPool = new List<QuestJson>();
            if (_jsonDb.quests == null) _jsonDb.quests = new List<QuestJson>();

            // Nút Back - quay về màn hình admin chính
            var btnBack = _root.Q<Button>("btn-back");
            if (btnBack != null)
            {
                btnBack.clicked += () =>
                {
                    Debug.Log("[Admin Quest] Trở lại AdminScreen");
                    LoadScreen(AdminScreenAsset);
                };
            }

            // Nút Add (+) - Mở modal thêm nhiệm vụ mới
            var btnAdd = _root.Q<Button>("btn-add");
            if (btnAdd != null)
            {
                btnAdd.clicked += () => OpenAddQuestModal();
            }

            // Ô tìm kiếm Search
            var searchField = _root.Q<TextField>("search-field");
            if (searchField != null)
            {
                searchField.RegisterValueChangedCallback(evt =>
                {
                    RefreshQuestList();
                });
            }

            // Ô lọc theo loại nhiệm vụ ngoài danh sách
            var filterType = _root.Q<DropdownField>("filter-type");
            if (filterType != null)
            {
                filterType.choices = new List<string> { "Tất cả", "LearnWord", "WinBattle", "ScorePerfect", "WinRanked", "CollectCoin", "LoginDay", "PracticeSession" };
                filterType.value = "Tất cả";
                filterType.RegisterValueChangedCallback(evt =>
                {
                    RefreshQuestList();
                });
            }

            // Cấu hình các lựa chọn cho DropdownField trong Modal
            var inputType = _root.Q<DropdownField>("input-type");
            if (inputType != null)
            {
                inputType.choices = new List<string> { "LearnWord", "WinBattle", "ScorePerfect", "WinRanked", "CollectCoin", "LoginDay", "PracticeSession" };
            }

            var inputLocation = _root.Q<DropdownField>("input-location");
            if (inputLocation != null)
            {
                inputLocation.choices = new List<string> { "Pool", "Active" };
            }

            // Sự kiện Tab
            var tabPool = _root.Q<Button>("tab-pool");
            if (tabPool != null)
            {
                tabPool.clicked += () => SelectAdminQuestTab("Pool");
            }

            var tabActive = _root.Q<Button>("tab-active");
            if (tabActive != null)
            {
                tabActive.clicked += () => SelectAdminQuestTab("Active");
            }

            // Các nút trong Modal
            var btnModalClose = _root.Q<Button>("btn-modal-close");
            if (btnModalClose != null) btnModalClose.clicked += CloseQuestModal;

            var btnModalCancel = _root.Q<Button>("btn-modal-cancel");
            if (btnModalCancel != null) btnModalCancel.clicked += CloseQuestModal;

            var btnModalSave = _root.Q<Button>("btn-modal-save");
            if (btnModalSave != null) btnModalSave.clicked += SaveQuestFromModal;

            // Đồng bộ tab mặc định ban đầu và vẽ danh sách
            SelectAdminQuestTab("Pool");
        }

        // Thay đổi Tab lựa chọn
        private void SelectAdminQuestTab(string tab)
        {
            _questAdminCurrentTab = tab;
            var tabPool = _root.Q<Button>("tab-pool");
            var tabActive = _root.Q<Button>("tab-active");

            if (tab == "Pool")
            {
                if (tabPool != null)
                {
                    tabPool.style.backgroundColor = new StyleColor(new Color(1f, 0.67f, 0.12f)); // rgb(255, 170, 30)
                    tabPool.style.color = Color.white;
                }
                if (tabActive != null)
                {
                    tabActive.style.backgroundColor = new StyleColor(Color.clear);
                    tabActive.style.color = new StyleColor(new Color(0.39f, 0.37f, 0.33f)); // rgb(100, 95, 85)
                }
            }
            else
            {
                if (tabActive != null)
                {
                    tabActive.style.backgroundColor = new StyleColor(new Color(1f, 0.67f, 0.12f)); // rgb(255, 170, 30)
                    tabActive.style.color = Color.white;
                }
                if (tabPool != null)
                {
                    tabPool.style.backgroundColor = new StyleColor(Color.clear);
                    tabPool.style.color = new StyleColor(new Color(0.39f, 0.37f, 0.33f)); // rgb(100, 95, 85)
                }
            }

            RefreshQuestList();
        }

        // Tải và vẽ danh sách nhiệm vụ lên UI ScrollView
        private void RefreshQuestList()
        {
            var questList = _root.Q<ScrollView>("quest-list");
            if (questList == null) return;

            questList.Clear();

            // Đọc từ khóa tìm kiếm và lọc loại
            var searchField = _root.Q<TextField>("search-field");
            string filterQuery = searchField != null ? searchField.value.Trim().ToLower() : "";

            var filterTypeField = _root.Q<DropdownField>("filter-type");
            string typeFilter = filterTypeField != null ? filterTypeField.value : "Tất cả";

            // Chọn nguồn dữ liệu tùy thuộc Tab
            List<QuestJson> sourceList = (_questAdminCurrentTab == "Pool") ? _jsonDb.questPool : _jsonDb.quests;

            int filteredCount = 0;

            foreach (var quest in sourceList)
            {
                // Bộ lọc tìm kiếm
                bool matchesQuery = string.IsNullOrEmpty(filterQuery) ||
                                    quest.id.ToLower().Contains(filterQuery) ||
                                    quest.title.ToLower().Contains(filterQuery) ||
                                    quest.description.ToLower().Contains(filterQuery);

                bool matchesType = string.IsNullOrEmpty(typeFilter) ||
                                   typeFilter == "Tất cả" ||
                                   quest.questType.ToLower() == typeFilter.ToLower();

                if (!matchesQuery || !matchesType) continue;

                filteredCount++;

                // Vẽ Card cho nhiệm vụ
                var card = new VisualElement();
                card.AddToClassList("vocab-card");

                // Đăng ký sự kiện nhấn để chỉnh sửa
                card.RegisterCallback<ClickEvent>(evt =>
                {
                    if (evt.target is Button btn && btn.ClassListContains("btn-delete")) return;
                    OpenEditQuestModal(quest);
                });

                // Thumbnail icon dựa trên loại nhiệm vụ
                var thumb = new VisualElement();
                thumb.AddToClassList("vocab-thumbnail");
                thumb.style.justifyContent = Justify.Center;
                thumb.style.alignItems = Align.Center;

                var thumbLabel = new Label();
                thumbLabel.style.fontSize = 26;
                thumbLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

                // Cấu hình Emoji và màu sắc dựa trên Quest Type
                if (quest.questType == "WinBattle")
                {
                    thumb.style.backgroundColor = new StyleColor(new Color(0.99f, 0.88f, 0.88f)); // đỏ nhạt
                    thumbLabel.text = "⚔";
                }
                else if (quest.questType == "LearnWord")
                {
                    thumb.style.backgroundColor = new StyleColor(new Color(0.86f, 0.99f, 0.9f)); // xanh lá nhạt
                    thumbLabel.text = "📘";
                }
                else if (quest.questType == "WinRanked")
                {
                    thumb.style.backgroundColor = new StyleColor(new Color(0.99f, 0.95f, 0.78f)); // vàng nhạt
                    thumbLabel.text = "🏆";
                }
                else if (quest.questType == "ScorePerfect")
                {
                    thumb.style.backgroundColor = new StyleColor(new Color(0.95f, 0.91f, 1f)); // tím nhạt
                    thumbLabel.text = "⭐";
                }
                else if (quest.questType == "CollectCoin")
                {
                    thumb.style.backgroundColor = new StyleColor(new Color(1f, 0.93f, 0.84f)); // cam nhạt
                    thumbLabel.text = "🪙";
                }
                else
                {
                    thumb.style.backgroundColor = new StyleColor(new Color(1f, 0.97f, 0.93f)); // kem nhạt
                    thumbLabel.text = "🎯";
                }

                thumb.Add(thumbLabel);

                // Info Container
                var info = new VisualElement();
                info.AddToClassList("vocab-info");

                // Hàng tiêu đề (Tiêu đề + Badge Type)
                var nameRow = new VisualElement();
                nameRow.AddToClassList("vocab-name-row");

                var lblTitle = new Label(quest.title);
                lblTitle.AddToClassList("vocab-word");
                lblTitle.style.fontSize = 15;

                var lblTypeBadge = new Label(quest.questType);
                lblTypeBadge.AddToClassList("rank-badge");
                // Style badge theo loại nhiệm vụ
                if (quest.questType == "WinBattle" || quest.questType == "WinRanked")
                    lblTypeBadge.AddToClassList("rank-badge--3"); // Màu cam/đỏ
                else if (quest.questType == "LearnWord" || quest.questType == "ScorePerfect")
                    lblTypeBadge.AddToClassList("rank-badge--2"); // Màu xanh lá
                
                nameRow.Add(lblTitle);
                nameRow.Add(lblTypeBadge);

                // Mô tả nhiệm vụ
                var lblDesc = new Label(quest.description);
                lblDesc.AddToClassList("vocab-meaning");
                lblDesc.style.fontSize = 12;

                // ID + Reward info
                var lblSubInfo = new Label($"ID: {quest.id} | 🪙 {quest.rewardCoins} xu | 🌟 {quest.rewardExp} EXP | Tiến độ tối đa: {quest.maxProgress}");
                lblSubInfo.AddToClassList("vocab-id");
                lblSubInfo.style.color = new Color(0.47f, 0.45f, 0.41f);
                lblSubInfo.style.unityFontStyleAndWeight = FontStyle.Bold;

                info.Add(nameRow);
                info.Add(lblDesc);
                info.Add(lblSubInfo);

                // Nút Xóa (thùng rác)
                var btnDelete = new Button();
                btnDelete.text = "🗑";
                btnDelete.AddToClassList("btn-delete");
                btnDelete.clicked += () =>
                {
                    ShowConfirmationDialog(
                        "Xóa Nhiệm Vụ?",
                        $"Bạn có chắc chắn muốn xóa nhiệm vụ '{quest.title}' không?",
                        () => DeleteQuest(quest),
                        null
                    );
                };

                card.Add(thumb);
                card.Add(info);
                card.Add(btnDelete);

                questList.Add(card);
            }

            // Làm mới các thông số Stats
            RefreshQuestStats(filteredCount);
        }

        // Cập nhật số liệu thống kê lên các thẻ Card Stats
        private void RefreshQuestStats(int filteredCount)
        {
            int totalInPool = _jsonDb.questPool.Count;
            int totalActive = _jsonDb.quests.Count;

            // Tìm Coins và EXP lớn nhất
            int maxCoins = 0;
            int maxExp = 0;

            foreach (var q in _jsonDb.questPool)
            {
                if (q.rewardCoins > maxCoins) maxCoins = q.rewardCoins;
                if (q.rewardExp > maxExp) maxExp = q.rewardExp;
            }
            foreach (var q in _jsonDb.quests)
            {
                if (q.rewardCoins > maxCoins) maxCoins = q.rewardCoins;
                if (q.rewardExp > maxExp) maxExp = q.rewardExp;
            }

            SetTextValue("quest-stat-total", totalInPool.ToString());
            SetTextValue("quest-stat-active", totalActive.ToString());
            SetTextValue("quest-stat-coins", maxCoins.ToString());
            SetTextValue("quest-stat-exp", maxExp.ToString());
            SetTextValue("quest-count-label", $"Tìm thấy {filteredCount} nhiệm vụ trong tab này");
        }

        // Mở Modal để Thêm nhiệm vụ mới
        private void OpenAddQuestModal()
        {
            _currentEditingQuest = null;

            var modalOverlay = _root.Q<VisualElement>("modal-overlay");
            if (modalOverlay == null) return;

            // Cập nhật tiêu đề modal
            var lblTitle = modalOverlay.Q<Label>("modal-title");
            if (lblTitle != null) lblTitle.text = "Thêm Nhiệm Vụ";

            // Xóa rỗng các trường Form
            SetInputValue("input-title", "");
            SetInputValue("input-desc", "");
            SetInputValue("input-type", "LearnWord");
            SetInputValue("input-max-progress", "10");
            SetInputValue("input-reward-coins", "50");
            SetInputValue("input-reward-exp", "100");
            SetInputValue("input-location", _questAdminCurrentTab); // Mặc định khớp với tab đang mở

            // Ẩn lỗi cũ
            HideQuestModalError();

            // Hiện modal
            modalOverlay.style.display = DisplayStyle.Flex;
        }

        // Mở Modal ở trạng thái chỉnh sửa
        private void OpenEditQuestModal(QuestJson quest)
        {
            _currentEditingQuest = quest;

            var modalOverlay = _root.Q<VisualElement>("modal-overlay");
            if (modalOverlay == null) return;

            // Đổi tiêu đề modal
            var lblTitle = modalOverlay.Q<Label>("modal-title");
            if (lblTitle != null) lblTitle.text = $"Sửa Nhiệm Vụ ({quest.id})";

            // Điền thông tin cũ vào các trường Form
            SetInputValue("input-title", quest.title);
            SetInputValue("input-desc", quest.description);
            SetInputValue("input-type", quest.questType);
            SetInputValue("input-max-progress", quest.maxProgress.ToString());
            SetInputValue("input-reward-coins", quest.rewardCoins.ToString());
            SetInputValue("input-reward-exp", quest.rewardExp.ToString());
            SetInputValue("input-location", _questAdminCurrentTab);

            // Ẩn lỗi cũ
            HideQuestModalError();

            // Hiện modal
            modalOverlay.style.display = DisplayStyle.Flex;
        }

        // Đóng Modal nhiệm vụ
        private void CloseQuestModal()
        {
            var modalOverlay = _root.Q<VisualElement>("modal-overlay");
            if (modalOverlay != null)
            {
                modalOverlay.style.display = DisplayStyle.None;
            }
            _currentEditingQuest = null;
        }

        // Lưu thông tin từ Modal (Thêm mới hoặc Cập nhật)
        private void SaveQuestFromModal()
        {
            var titleStr = GetInputValue("input-title").Trim();
            var descStr = GetInputValue("input-desc").Trim();
            var typeStr = GetInputValue("input-type").Trim();
            var maxProgressStr = GetInputValue("input-max-progress").Trim();
            var rewardCoinsStr = GetInputValue("input-reward-coins").Trim();
            var rewardExpStr = GetInputValue("input-reward-exp").Trim();
            var locationStr = GetInputValue("input-location").Trim();

            // Kiểm tra điều kiện nhập trống
            if (string.IsNullOrEmpty(titleStr) || string.IsNullOrEmpty(descStr) || 
                string.IsNullOrEmpty(maxProgressStr) || string.IsNullOrEmpty(rewardCoinsStr) || 
                string.IsNullOrEmpty(rewardExpStr))
            {
                ShowQuestModalError("Vui lòng điền đầy đủ các thông tin bắt buộc (*)");
                return;
            }

            // Kiểm tra kiểu số hợp lệ
            if (!int.TryParse(maxProgressStr, out int maxProgress) || maxProgress <= 0 ||
                !int.TryParse(rewardCoinsStr, out int rewardCoins) || rewardCoins < 0 ||
                !int.TryParse(rewardExpStr, out int rewardExp) || rewardExp < 0)
            {
                ShowQuestModalError("Vui lòng nhập tiến độ và phần thưởng là các số nguyên hợp lệ!");
                return;
            }

            if (_currentEditingQuest == null)
            {
                // ---- THÊM MỚI (CREATE) ----
                // Tự động tính ID mới dạng qX (Ví dụ: q12)
                int maxIdNum = 0;
                
                // Quét qua cả 2 danh sách để lấy số ID cao nhất
                foreach (var q in _jsonDb.questPool)
                {
                    if (q.id.StartsWith("q") && int.TryParse(q.id.Substring(1), out int num))
                    {
                        if (num > maxIdNum) maxIdNum = num;
                    }
                }
                foreach (var q in _jsonDb.quests)
                {
                    if (q.id.StartsWith("q") && int.TryParse(q.id.Substring(1), out int num))
                    {
                        if (num > maxIdNum) maxIdNum = num;
                    }
                }

                string newId = "q" + (maxIdNum + 1);

                QuestJson newQuest = new QuestJson
                {
                    id = newId,
                    title = titleStr,
                    description = descStr,
                    currentProgress = 0,
                    maxProgress = maxProgress,
                    rewardCoins = rewardCoins,
                    rewardExp = rewardExp,
                    isClaimed = false,
                    questType = typeStr
                };

                if (locationStr == "Pool")
                {
                    _jsonDb.questPool.Add(newQuest);
                    Debug.Log($"[Admin Quest] Đã thêm mới vào Pool: {newQuest.title} (ID: {newQuest.id})");
                }
                else
                {
                    _jsonDb.quests.Add(newQuest);
                    Debug.Log($"[Admin Quest] Đã thêm mới vào Active: {newQuest.title} (ID: {newQuest.id})");
                }
            }
            else
            {
                // ---- CẬP NHẬT (UPDATE) ----
                string oldLocation = _jsonDb.questPool.Contains(_currentEditingQuest) ? "Pool" : "Active";

                _currentEditingQuest.title = titleStr;
                _currentEditingQuest.description = descStr;
                _currentEditingQuest.questType = typeStr;
                _currentEditingQuest.maxProgress = maxProgress;
                _currentEditingQuest.rewardCoins = rewardCoins;
                _currentEditingQuest.rewardExp = rewardExp;

                // Nếu người dùng thay đổi lưu trữ từ Pool sang Active hoặc ngược lại
                if (oldLocation != locationStr)
                {
                    if (oldLocation == "Pool" && locationStr == "Active")
                    {
                        _jsonDb.questPool.Remove(_currentEditingQuest);
                        _jsonDb.quests.Add(_currentEditingQuest);
                        Debug.Log($"[Admin Quest] Đã di chuyển ID {_currentEditingQuest.id} từ Pool sang Active");
                    }
                    else if (oldLocation == "Active" && locationStr == "Pool")
                    {
                        _jsonDb.quests.Remove(_currentEditingQuest);
                        _jsonDb.questPool.Add(_currentEditingQuest);
                        Debug.Log($"[Admin Quest] Đã di chuyển ID {_currentEditingQuest.id} từ Active sang Pool");
                    }
                }
                else
                {
                    Debug.Log($"[Admin Quest] Đã cập nhật thành công nhiệm vụ ID: {_currentEditingQuest.id}");
                }
            }

            // Ghi dữ liệu xuống đĩa và RAM
            SaveJsonDatabase();

            // Đóng Modal và vẽ lại giao diện
            CloseQuestModal();
            RefreshQuestList();
        }

        // Xóa nhiệm vụ (DELETE)
        private void DeleteQuest(QuestJson quest)
        {
            if (_jsonDb.questPool.Contains(quest))
            {
                _jsonDb.questPool.Remove(quest);
                Debug.Log($"[Admin Quest] Đã xóa nhiệm vụ trong Pool: {quest.title} (ID: {quest.id})");
            }
            else if (_jsonDb.quests.Contains(quest))
            {
                _jsonDb.quests.Remove(quest);
                Debug.Log($"[Admin Quest] Đã xóa nhiệm vụ đang hoạt động: {quest.title} (ID: {quest.id})");
            }

            // Ghi dữ liệu xuống đĩa
            SaveJsonDatabase();

            // Vẽ lại giao diện
            RefreshQuestList();
        }

        // --- CÁC HÀM TIỆN ÍCH HỖ TRỢ TRONG MODAL ---

        private void ShowQuestModalError(string errorMsg)
        {
            var lblError = _root.Q<Label>("modal-error-msg");
            if (lblError != null)
            {
                lblError.text = errorMsg;
                lblError.style.display = DisplayStyle.Flex;
            }
        }

        private void HideQuestModalError()
        {
            var lblError = _root.Q<Label>("modal-error-msg");
            if (lblError != null)
            {
                lblError.style.display = DisplayStyle.None;
            }
        }
    }
}
