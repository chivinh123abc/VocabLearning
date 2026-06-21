using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using VocabLearning.Data;

namespace VocabLearning.UI
{
    public partial class VocabStudyController
    {
        private AchievementJson _currentEditingAchievement = null;

        private void BindAdminAchievementEvents()
        {
            // Reset nếu null để tránh crash
            if (_jsonDb.achievements == null)
            {
                _jsonDb.achievements = new List<AchievementJson>();
            }

            // Nút Back - Quay lại trang Admin chính
            var btnBack = _root.Q<Button>("btn-back");
            if (btnBack != null)
            {
                btnBack.clicked += () =>
                {
                    Debug.Log("[Admin Achievement] Trở lại AdminScreen");
                    LoadScreen(AdminScreenAsset);
                };
            }

            // Nút Add (+) - Mở modal thêm mới
            var btnAdd = _root.Q<Button>("btn-add");
            if (btnAdd != null)
            {
                btnAdd.clicked += OpenAddAchievementModal;
            }

            // Ô tìm kiếm Search
            var searchField = _root.Q<TextField>("search-field");
            if (searchField != null)
            {
                searchField.RegisterValueChangedCallback(evt =>
                {
                    RefreshAchievementList();
                });
            }

            // Các nút điều khiển trong Modal
            var btnModalClose = _root.Q<Button>("btn-modal-close");
            if (btnModalClose != null) btnModalClose.clicked += CloseAchievementModal;

            var btnModalCancel = _root.Q<Button>("btn-modal-cancel");
            if (btnModalCancel != null) btnModalCancel.clicked += CloseAchievementModal;

            var btnModalSave = _root.Q<Button>("btn-modal-save");
            if (btnModalSave != null) btnModalSave.clicked += SaveAchievementFromModal;

            // Nạp và hiển thị danh sách thành tựu ban đầu
            RefreshAchievementList();
        }

        private void RefreshAchievementList()
        {
            var achList = _root.Q<ScrollView>("ach-list");
            if (achList == null) return;

            achList.Clear();

            // Đọc từ khóa tìm kiếm
            var searchField = _root.Q<TextField>("search-field");
            string filterQuery = (searchField != null) ? searchField.value.Trim().ToLower() : "";

            if (_jsonDb.achievements == null)
            {
                _jsonDb.achievements = new List<AchievementJson>();
            }

            int filteredCount = 0;
            int maxProgressValue = 0;

            foreach (var ach in _jsonDb.achievements)
            {
                // Kiểm tra bộ lọc
                bool matchesQuery = string.IsNullOrEmpty(filterQuery) ||
                                    ach.id.ToLower().Contains(filterQuery) ||
                                    ach.title.ToLower().Contains(filterQuery) ||
                                    ach.description.ToLower().Contains(filterQuery);

                if (!matchesQuery) continue;

                filteredCount++;
                if (ach.maxProgress > maxProgressValue)
                {
                    maxProgressValue = ach.maxProgress;
                }

                // Thiết kế Card cho Thành tựu
                var card = new VisualElement();
                card.AddToClassList("vocab-card");

                // Sự kiện nhấp chuột để chỉnh sửa (loại trừ nút Xóa)
                card.RegisterCallback<ClickEvent>(evt =>
                {
                    if (evt.target is Button btn && btn.ClassListContains("btn-delete")) return;
                    OpenEditAchievementModal(ach);
                });

                // Thumbnail biểu tượng (Icon emoji)
                var thumb = new VisualElement();
                thumb.AddToClassList("vocab-thumbnail");
                thumb.style.justifyContent = Justify.Center;
                thumb.style.alignItems = Align.Center;
                thumb.style.backgroundColor = new StyleColor(new Color(1f, 0.93f, 0.84f)); 

                var emojiLbl = new Label(string.IsNullOrEmpty(ach.icon) ? "🏆" : ach.icon);
                emojiLbl.style.fontSize = 38;
                emojiLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
                thumb.Add(emojiLbl);

                // Container thông tin chi tiết
                var info = new VisualElement();
                info.AddToClassList("vocab-info");

                var nameRow = new VisualElement();
                nameRow.AddToClassList("vocab-name-row");

                var lblTitle = new Label(ach.title);
                lblTitle.AddToClassList("vocab-word");
                lblTitle.style.fontSize = 15;

                nameRow.Add(lblTitle);

                var lblDesc = new Label(ach.description);
                lblDesc.AddToClassList("vocab-meaning");
                lblDesc.style.fontSize = 12;

                var lblSubInfo = new Label($"ID: {ach.id}  |  Yêu cầu tiến trình: {ach.maxProgress}");
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
                        "Xóa Thành Tựu?",
                        $"Bạn có chắc chắn muốn xóa thành tựu '{ach.title}' không?",
                        () => DeleteAchievement(ach),
                        null
                    );
                };

                card.Add(thumb);
                card.Add(info);
                card.Add(btnDelete);

                achList.Add(card);
            }

            // Cập nhật số liệu thống kê ở Stats Row đầu trang
            var statTotal = _root.Q<Label>("ach-stat-total");
            if (statTotal != null) statTotal.text = _jsonDb.achievements.Count.ToString();

            var statMax = _root.Q<Label>("ach-stat-max");
            if (statMax != null) statMax.text = maxProgressValue.ToString();

            // Cập nhật nhãn số lượng thành tựu tìm thấy
            var countLbl = _root.Q<Label>("ach-count-label");
            if (countLbl != null) countLbl.text = $"{filteredCount} thành tựu";
        }

        private void OpenAddAchievementModal()
        {
            _currentEditingAchievement = null;

            var modalTitle = _root.Q<Label>("modal-title");
            if (modalTitle != null) modalTitle.text = "Thêm Thành Tựu";

            ClearAchievementForm();
            ShowAchievementModal(true);
        }

        private void OpenEditAchievementModal(AchievementJson ach)
        {
            _currentEditingAchievement = ach;

            var modalTitle = _root.Q<Label>("modal-title");
            if (modalTitle != null) modalTitle.text = "Sửa Thành Tựu";

            var inputIcon = _root.Q<TextField>("input-icon");
            if (inputIcon != null) inputIcon.value = ach.icon;

            var inputTitle = _root.Q<TextField>("input-title");
            if (inputTitle != null) inputTitle.value = ach.title;

            var inputDesc = _root.Q<TextField>("input-desc");
            if (inputDesc != null) inputDesc.value = ach.description;

            var inputMax = _root.Q<TextField>("input-max-progress");
            if (inputMax != null) inputMax.value = ach.maxProgress.ToString();

            ShowAchievementModal(true);
        }

        private void ShowAchievementModal(bool show)
        {
            var overlay = _root.Q<VisualElement>("modal-overlay");
            if (overlay != null)
            {
                overlay.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            }

            var errMsg = _root.Q<Label>("modal-error-msg");
            if (errMsg != null) errMsg.style.display = DisplayStyle.None;
        }

        private void CloseAchievementModal()
        {
            ShowAchievementModal(false);
        }

        private void ClearAchievementForm()
        {
            var inputIcon = _root.Q<TextField>("input-icon");
            if (inputIcon != null) inputIcon.value = "🏆";

            var inputTitle = _root.Q<TextField>("input-title");
            if (inputTitle != null) inputTitle.value = "";

            var inputDesc = _root.Q<TextField>("input-desc");
            if (inputDesc != null) inputDesc.value = "";

            var inputMax = _root.Q<TextField>("input-max-progress");
            if (inputMax != null) inputMax.value = "10";
        }

        private void SaveAchievementFromModal()
        {
            var inputIcon = _root.Q<TextField>("input-icon");
            var inputTitle = _root.Q<TextField>("input-title");
            var inputDesc = _root.Q<TextField>("input-desc");
            var inputMax = _root.Q<TextField>("input-max-progress");

            string icon = (inputIcon != null) ? inputIcon.value.Trim() : "🏆";
            string title = (inputTitle != null) ? inputTitle.value.Trim() : "";
            string desc = (inputDesc != null) ? inputDesc.value.Trim() : "";
            string maxStr = (inputMax != null) ? inputMax.value.Trim() : "";

            if (string.IsNullOrEmpty(icon)) icon = "🏆";

            // Kiểm chứng biểu mẫu đầu vào
            if (string.IsNullOrEmpty(title))
            {
                ShowModalError("⚠️ Vui lòng nhập tên thành tựu!");
                return;
            }

            if (string.IsNullOrEmpty(desc))
            {
                ShowModalError("⚠️ Vui lòng nhập mô tả thành tựu!");
                return;
            }

            if (!int.TryParse(maxStr, out int maxProgress) || maxProgress <= 0)
            {
                ShowModalError("⚠️ Mục tiêu tối đa phải là một số nguyên dương!");
                return;
            }

            if (_currentEditingAchievement == null)
            {
                // THÊM MỚI (ADD NEW)
                // Tìm ID nguyên lớn nhất để tự tăng tăng tiến an toàn
                int maxId = 0;
                foreach (var ach in _jsonDb.achievements)
                {
                    if (int.TryParse(ach.id, out int parseId))
                    {
                        if (parseId > maxId) maxId = parseId;
                    }
                }
                string newId = (maxId + 1).ToString();

                var newAch = new AchievementJson
                {
                    id = newId,
                    icon = icon,
                    title = title,
                    description = desc,
                    maxProgress = maxProgress,
                    currentProgress = 0,
                    isUnlocked = false,
                    unlockDate = ""
                };

                _jsonDb.achievements.Add(newAch);
                SaveJsonDatabase();
                ShowAdminToast($"Đã tạo thành tựu '{title}' thành công!");
            }
            else
            {
                // CẬP NHẬT (EDIT EXISTING)
                _currentEditingAchievement.icon = icon;
                _currentEditingAchievement.title = title;
                _currentEditingAchievement.description = desc;
                _currentEditingAchievement.maxProgress = maxProgress;

                // Tự động kiểm chứng lại trạng thái mở khóa nếu tiến trình đã hoàn thành
                if (_currentEditingAchievement.currentProgress >= maxProgress && !_currentEditingAchievement.isUnlocked)
                {
                    _currentEditingAchievement.isUnlocked = true;
                    _currentEditingAchievement.unlockDate = System.DateTime.Now.ToString("MMM dd, yyyy");
                }
                else if (_currentEditingAchievement.currentProgress < maxProgress)
                {
                    _currentEditingAchievement.isUnlocked = false;
                    _currentEditingAchievement.unlockDate = "";
                }

                SaveJsonDatabase();
                ShowAdminToast($"Đã cập nhật thành tựu '{title}' thành công!");
            }

            RefreshAchievementList();
            CloseAchievementModal();
        }

        private void DeleteAchievement(AchievementJson ach)
        {
            if (_jsonDb.achievements.Contains(ach))
            {
                string title = ach.title;
                _jsonDb.achievements.Remove(ach);
                SaveJsonDatabase();
                ShowAdminToast($"Đã xóa thành tựu '{title}'!");
                RefreshAchievementList();
            }
        }
    }
}
