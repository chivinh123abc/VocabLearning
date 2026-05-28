using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using VocabLearning.Data;

namespace VocabLearning.UI
{
    public partial class VocabStudyController
    {
        private WordJson _currentEditingWord = null; // Từ đang được sửa, nếu null tức là đang Thêm mới

        private void BindAdminVocabEvents()
        {
            // Nút Back - quay về màn hình admin chính
            var btnBack = _root.Q<Button>("btn-back");
            if (btnBack != null)
            {
                btnBack.clicked += () =>
                {
                    Debug.Log("[Admin Vocab] Trở lại AdminScreen");
                    LoadScreen(AdminScreenAsset);
                };
            }

            // Nút Add (+) - Mở modal thêm từ mới
            var btnAdd = _root.Q<Button>("btn-add");
            if (btnAdd != null)
            {
                btnAdd.clicked += () => OpenAddWordModal();
            }

            // Ô tìm kiếm Search
            var searchField = _root.Q<TextField>("search-field");
            if (searchField != null)
            {
                searchField.RegisterValueChangedCallback(evt =>
                {
                    RefreshVocabList();
                });
            }

            // Ô lọc theo Rank ngoài danh sách
            var filterRank = _root.Q<DropdownField>("filter-rank");
            if (filterRank != null)
            {
                filterRank.choices = new List<string> { "Tất cả", "Dong", "Bac", "Vang", "BachKim", "KimCuong", "SieuCap" };
                filterRank.value = "Tất cả";
                filterRank.RegisterValueChangedCallback(evt =>
                {
                    RefreshVocabList();
                });
            }

            // Cấu hình các lựa chọn cho DropdownField input-rank (trong Modal)
            var inputRank = _root.Q<DropdownField>("input-rank");
            if (inputRank != null)
            {
                inputRank.choices = new List<string> { "Dong", "Bac", "Vang", "BachKim", "KimCuong", "SieuCap" };
            }

            // Nút chọn ảnh từ máy tính (trong Modal)
            var btnChooseImage = _root.Q<Button>("btn-choose-image");
            if (btnChooseImage != null)
            {
                btnChooseImage.clicked += () =>
                {
#if UNITY_EDITOR
                    string selectedPath = UnityEditor.EditorUtility.OpenFilePanel("Chọn ảnh từ máy", "", "png,jpg,jpeg,webp");
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        // Tạo thư mục lưu trữ ảnh Vocab nếu chưa tồn tại
                        string vocabSpritesDir = System.IO.Path.Combine(Application.dataPath, "Sprites/Vocab");
                        if (!System.IO.Directory.Exists(vocabSpritesDir))
                        {
                            System.IO.Directory.CreateDirectory(vocabSpritesDir);
                        }

                        // Sao chép tệp tin vào trong thư mục Assets/Sprites/Vocab với tên tệp là duy nhất (tránh ghi đè)
                        string fileName = System.IO.Path.GetFileName(selectedPath);
                        string uniqueFileName = System.DateTime.Now.ToString("yyyyMMdd_HHmmss_") + fileName;
                        string destPath = System.IO.Path.Combine(vocabSpritesDir, uniqueFileName);

                        try
                        {
                            System.IO.File.Copy(selectedPath, destPath, true);
                            UnityEditor.AssetDatabase.Refresh();

                            string absoluteDestPath = System.IO.Path.GetFullPath(destPath);
                            string fileUrl = "file://" + absoluteDestPath.Replace("\\", "/");

                            SetInputValue("input-image", fileUrl);
                            Debug.Log($"[Admin Vocab] Đã nhập ảnh từ máy và lưu dạng file:// URL: {fileUrl}");
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"[Admin Vocab] Lỗi sao chép tệp ảnh: {ex.Message}");
                        }
                    }
#else
                    Debug.LogWarning("[Admin Vocab] Chọn tệp từ máy chỉ khả dụng khi chạy trong Unity Editor.");
#endif
                };
            }

            // Các nút trong Modal
            var btnModalClose = _root.Q<Button>("btn-modal-close");
            if (btnModalClose != null) btnModalClose.clicked += CloseModal;

            var btnModalCancel = _root.Q<Button>("btn-modal-cancel");
            if (btnModalCancel != null) btnModalCancel.clicked += CloseModal;

            var btnModalSave = _root.Q<Button>("btn-modal-save");
            if (btnModalSave != null) btnModalSave.clicked += SaveWordFromModal;

            // Load và hiển thị danh sách từ vựng ban đầu
            RefreshVocabList();
        }

        // Tải và vẽ danh sách từ vựng lên UI ScrollView với bộ lọc tìm kiếm và Rank
        private void RefreshVocabList()
        {
            if (_jsonDb == null || _jsonDb.words == null) return;

            var vocabList = _root.Q<ScrollView>("vocab-list");
            if (vocabList == null) return;

            vocabList.Clear();

            // Đọc từ khóa tìm kiếm
            var searchField = _root.Q<TextField>("search-field");
            string filterQuery = searchField != null ? searchField.value.Trim().ToLower() : "";

            // Đọc Rank cần lọc
            var filterRankField = _root.Q<DropdownField>("filter-rank");
            string rankFilter = filterRankField != null ? filterRankField.value : "Tất cả";

            int totalCount = _jsonDb.words.Count;
            int filteredCount = 0;
            string maxRank = "Dong";

            foreach (var word in _jsonDb.words)
            {
                // Kiểm tra điều kiện tìm kiếm bằng từ khóa
                bool matchesQuery = string.IsNullOrEmpty(filterQuery) ||
                                    word.id.ToLower().Contains(filterQuery) ||
                                    word.word.ToLower().Contains(filterQuery) ||
                                    word.meaning.ToLower().Contains(filterQuery);

                // Kiểm tra điều kiện tìm kiếm bằng Rank
                bool matchesRank = string.IsNullOrEmpty(rankFilter) ||
                                   rankFilter == "Tất cả" ||
                                   word.rankRequired.ToLower() == rankFilter.ToLower();

                if (!matchesQuery || !matchesRank) continue;

                filteredCount++;

                // Theo dõi Max Rank
                if (IsRankHigher(word.rankRequired, maxRank))
                {
                    maxRank = word.rankRequired;
                }

                // Vẽ Card cho từ này
                var card = new VisualElement();
                card.AddToClassList("vocab-card");

                // Khi click vào card, mở modal chỉnh sửa từ vựng
                card.RegisterCallback<ClickEvent>(evt =>
                {
                    // Tránh mở sửa nếu click nhầm nút Xóa
                    if (evt.target is Button btn && btn.ClassListContains("btn-delete")) return;
                    OpenEditWordModal(word);
                });

                // Thumbnail tròn với màu nền ngẫu nhiên giả lập hoặc theo từ
                var thumb = new VisualElement();
                thumb.AddToClassList("vocab-thumbnail");
                thumb.style.backgroundColor = GetThumbnailColor(word.word);

                // Info Container
                var info = new VisualElement();
                info.AddToClassList("vocab-info");

                // Hàng tiêu đề (Từ tiếng Anh + Badge Rank)
                var nameRow = new VisualElement();
                nameRow.AddToClassList("vocab-name-row");

                var lblWord = new Label(word.word);
                lblWord.AddToClassList("vocab-word");

                var lblRank = new Label(word.rankRequired);
                lblRank.AddToClassList("rank-badge");
                // Đổi style badge theo rank
                if (word.rankRequired == "Bac")
                    lblRank.AddToClassList("rank-badge--2");
                else if (word.rankRequired != "Dong")
                    lblRank.AddToClassList("rank-badge--3");

                nameRow.Add(lblWord);
                nameRow.Add(lblRank);

                // Nghĩa tiếng Việt
                var lblMeaning = new Label(word.meaning);
                lblMeaning.AddToClassList("vocab-meaning");

                // ID của từ
                var lblId = new Label($"ID: {word.id}");
                lblId.AddToClassList("vocab-id");

                info.Add(nameRow);
                info.Add(lblMeaning);
                info.Add(lblId);

                // Nút Xóa (Thùng rác)
                var btnDelete = new Button();
                btnDelete.text = "🗑";
                btnDelete.AddToClassList("btn-delete");
                btnDelete.clicked += () =>
                {
                    DeleteWord(word);
                };

                card.Add(thumb);
                card.Add(info);
                card.Add(btnDelete);

                vocabList.Add(card);
            }

            // Cập nhật thông số Stats lên UI
            SetTextValue("vocab-stat-total", totalCount.ToString());
            SetTextValue("vocab-stat-filtered", filteredCount.ToString());
            SetTextValue("vocab-stat-max-rank", maxRank);
            SetTextValue("vocab-count-label", $"{filteredCount} từ vựng");
        }

        // Mở Modal ở trạng thái Thêm từ mới
        private void OpenAddWordModal()
        {
            _currentEditingWord = null;

            var modalOverlay = _root.Q<VisualElement>("modal-overlay");
            if (modalOverlay == null) return;

            // Đổi tiêu đề modal
            var lblTitle = modalOverlay.Q<Label>("modal-title");
            if (lblTitle != null) lblTitle.text = "Thêm Từ Vựng";

            // Xóa rỗng các trường Form
            SetInputValue("input-word", "");
            SetInputValue("input-meaning", "");
            SetInputValue("input-rank", "Dong");
            SetInputValue("input-image", "https://i.pinimg.com/1200x/3e/e3/6f/3ee36faa81060dd362a50ca1ef49ac6e.jpg"); // default placeholder image
            SetInputValue("input-sub", "");

            // Ẩn lỗi cũ
            HideModalError();

            // Hiện modal
            modalOverlay.style.display = DisplayStyle.Flex;
        }

        // Mở Modal ở trạng thái Sửa từ có sẵn
        private void OpenEditWordModal(WordJson word)
        {
            _currentEditingWord = word;

            var modalOverlay = _root.Q<VisualElement>("modal-overlay");
            if (modalOverlay == null) return;

            // Đổi tiêu đề modal
            var lblTitle = modalOverlay.Q<Label>("modal-title");
            if (lblTitle != null) lblTitle.text = $"Sửa Từ Vựng ({word.id})";

            // Điền thông tin cũ vào các trường Form
            SetInputValue("input-word", word.word);
            SetInputValue("input-meaning", word.meaning);
            SetInputValue("input-rank", word.rankRequired);
            SetInputValue("input-image", word.imageUrl);
            SetInputValue("input-sub", word.imageSub);

            // Ẩn lỗi cũ
            HideModalError();

            // Hiện modal
            modalOverlay.style.display = DisplayStyle.Flex;
        }

        // Đóng Modal
        private void CloseModal()
        {
            var modalOverlay = _root.Q<VisualElement>("modal-overlay");
            if (modalOverlay != null)
            {
                modalOverlay.style.display = DisplayStyle.None;
            }
            _currentEditingWord = null;
        }

        // Lưu dữ liệu từ Modal (Thêm hoặc Cập nhật)
        private void SaveWordFromModal()
        {
            var wordStr = GetInputValue("input-word").Trim();
            var meaningStr = GetInputValue("input-meaning").Trim();
            var rankStr = GetInputValue("input-rank").Trim();
            var imageStr = GetInputValue("input-image").Trim();
            var subStr = GetInputValue("input-sub").Trim();

            // Validation cơ bản
            if (string.IsNullOrEmpty(wordStr) || string.IsNullOrEmpty(meaningStr) || string.IsNullOrEmpty(rankStr))
            {
                ShowModalError("Vui lòng điền đầy đủ các trường bắt buộc (*)");
                return;
            }

            // Chuẩn hóa tên rank hợp lệ
            string[] validRanks = { "Dong", "Bac", "Vang", "BachKim", "KimCuong", "SieuCap" };
            bool isValidRank = false;
            foreach (var r in validRanks)
            {
                if (r.ToLower() == rankStr.ToLower())
                {
                    rankStr = r; // Lấy đúng ký tự hoa/thường chuẩn
                    isValidRank = true;
                    break;
                }
            }

            if (!isValidRank)
            {
                ShowModalError("Rank phải thuộc: Dong, Bac, Vang, BachKim, KimCuong, SieuCap");
                return;
            }

            if (_currentEditingWord == null)
            {
                // ---- THÊM MỚI (CREATE) ----
                // Tự động sinh ID mới dạng wX (ví dụ: w73)
                int maxIdNum = 0;
                foreach (var w in _jsonDb.words)
                {
                    if (w.id.StartsWith("w") && int.TryParse(w.id.Substring(1), out int num))
                    {
                        if (num > maxIdNum) maxIdNum = num;
                    }
                }
                string newId = "w" + (maxIdNum + 1);

                WordJson newWord = new WordJson
                {
                    id = newId,
                    word = wordStr,
                    meaning = meaningStr,
                    rankRequired = rankStr,
                    imageUrl = imageStr,
                    imageSub = string.IsNullOrEmpty(subStr) ? $"Hint: {meaningStr}" : subStr
                };

                _jsonDb.words.Add(newWord);
                Debug.Log($"[Admin Vocab] Đã thêm từ mới: {newWord.word} (ID: {newWord.id})");

                // Đồng bộ lên SQL Server qua Express Backend
                VocabLearning.Network.NetworkClient.Instance.AdminAddWord(newWord, (success, msg, res) =>
                {
                    if (success) Debug.Log($"[Admin Vocab - Network] Đã chèn thành công từ '{newWord.word}' vào SQL Server.");
                    else Debug.LogError($"[Admin Vocab - Network] Thất bại khi chèn từ vựng vào SQL Server: {msg}");
                });
            }
            else
            {
                // ---- CẬP NHẬT (UPDATE) ----
                _currentEditingWord.word = wordStr;
                _currentEditingWord.meaning = meaningStr;
                _currentEditingWord.rankRequired = rankStr;
                _currentEditingWord.imageUrl = imageStr;
                _currentEditingWord.imageSub = string.IsNullOrEmpty(subStr) ? $"Hint: {meaningStr}" : subStr;
                Debug.Log($"[Admin Vocab] Đã cập nhật từ vựng ID: {_currentEditingWord.id}");

                string capturedId = _currentEditingWord.id;
                // Đồng bộ cập nhật lên SQL Server qua Express Backend
                VocabLearning.Network.NetworkClient.Instance.AdminUpdateWord(_currentEditingWord, (success, msg, res) =>
                {
                    if (success) Debug.Log($"[Admin Vocab - Network] Đã cập nhật thành công từ ID {capturedId} trong SQL Server.");
                    else Debug.LogError($"[Admin Vocab - Network] Thất bại khi sửa từ vựng trong SQL Server: {msg}");
                });
            }

            // Đồng bộ dữ liệu
            SaveJsonDatabase();

            // Đóng Modal và làm tươi lại danh sách hiển thị
            CloseModal();
            RefreshVocabList();
        }

        // Xóa từ vựng (DELETE)
        private void DeleteWord(WordJson word)
        {
            if (_jsonDb == null || _jsonDb.words == null) return;

            // Xóa trong kho tổng
            _jsonDb.words.Remove(word);
            Debug.Log($"[Admin Vocab] Đã xóa từ vựng: {word.word} (ID: {word.id})");

            // Tự động dọn dẹp các tham chiếu trong các bộ từ vựng (vocabSets) để tránh lỗi mồ côi
            if (_jsonDb.vocabSets != null)
            {
                int removedRefs = 0;
                foreach (var set in _jsonDb.vocabSets)
                {
                    if (set.wordIds != null && set.wordIds.Contains(word.id))
                    {
                        set.wordIds.Remove(word.id);
                        removedRefs++;
                    }
                    if (set.levels != null)
                    {
                        foreach (var lvl in set.levels)
                        {
                            if (lvl.wordIds != null && lvl.wordIds.Contains(word.id))
                            {
                                lvl.wordIds.Remove(word.id);
                            }
                        }
                    }
                }
                if (removedRefs > 0)
                {
                    Debug.Log($"[Admin Vocab] Đã gỡ bỏ liên kết từ ID {word.id} khỏi {removedRefs} Bộ từ vựng.");
                }
            }

            // Gọi API mạng xóa trên SQL Server qua Express Backend
            VocabLearning.Network.NetworkClient.Instance.AdminDeleteWord(word.id, (success, msg, res) =>
            {
                if (success) Debug.Log($"[Admin Vocab - Network] Đã xóa thành công từ ID {word.id} khỏi SQL Server.");
                else Debug.LogError($"[Admin Vocab - Network] Thất bại khi xóa từ vựng khỏi SQL Server: {msg}");
            });

            // Ghi thay đổi xuống đĩa
            SaveJsonDatabase();

            // Refresh lại giao diện
            RefreshVocabList();
        }

        // Lưu thay đổi MockDatabase xuống file db.json và đồng bộ lên Node.js Backend SQL Server
        private void SaveJsonDatabase()
        {


            // Đồng bộ dữ liệu tiến trình người dùng lên SQL Server thông qua Node.js Backend API
            if (_jsonDb != null && _jsonDb.currentUser != null && !string.IsNullOrEmpty(_jsonDb.currentUser.id))
            {
                Debug.Log($"[JSON DB - Network] Bắt đầu đồng bộ tiến trình game của người chơi '{_jsonDb.currentUser.username}' lên SQL Server...");
                VocabLearning.Network.NetworkClient.Instance.SyncUserData(_jsonDb.currentUser, (success, message, responseJson) =>
                {
                    if (success)
                    {
                        Debug.Log($"[JSON DB - Network] Đồng bộ thành công tiến trình game của '{_jsonDb.currentUser.username}' lên SQL Server.");
                    }
                    else
                    {
                        Debug.LogWarning($"[JSON DB - Network] Thất bại khi đồng bộ dữ liệu game lên server: {message}");
                    }
                });
            }
        }

        // --- CÁC HÀM TIỆN ÍCH HỖ TRỢ (HELPERS) ---

        private string GetInputValue(string inputName)
        {
            var tf = _root.Q<TextField>(inputName);
            if (tf != null) return tf.value;

            var df = _root.Q<DropdownField>(inputName);
            if (df != null) return df.value;

            return "";
        }

        private void SetInputValue(string inputName, string value)
        {
            var tf = _root.Q<TextField>(inputName);
            if (tf != null)
            {
                tf.value = value;
                return;
            }

            var df = _root.Q<DropdownField>(inputName);
            if (df != null)
            {
                df.value = value;
            }
        }

        private void SetTextValue(string labelName, string value)
        {
            var lbl = _root.Q<Label>(labelName);
            if (lbl != null) lbl.text = value;
        }

        private void ShowModalError(string errorMsg)
        {
            var lblError = _root.Q<Label>("modal-error-msg");
            if (lblError != null)
            {
                lblError.text = errorMsg;
                lblError.style.display = DisplayStyle.Flex;
            }
        }

        private void HideModalError()
        {
            var lblError = _root.Q<Label>("modal-error-msg");
            if (lblError != null)
            {
                lblError.style.display = DisplayStyle.None;
            }
        }

        // Lấy màu ngẫu nhiên hài hòa cho Thumbnail dựa trên chữ cái đầu của từ
        private StyleColor GetThumbnailColor(string word)
        {
            if (string.IsNullOrEmpty(word)) return new StyleColor(new Color(0.7f, 0.7f, 0.7f));
            char first = char.ToLower(word[0]);
            int index = first - 'a';

            Color[] colors = {
                new Color(0.86f, 0.71f, 0.63f), // happy
                new Color(0.71f, 0.82f, 0.69f), // book
                new Color(0.63f, 0.73f, 0.82f), // friend
                new Color(0.78f, 0.69f, 0.78f), // beautiful
                new Color(0.85f, 0.65f, 0.65f), // rose
                new Color(0.65f, 0.83f, 0.85f), // cyan
                new Color(0.83f, 0.85f, 0.65f), // yellow-green
                new Color(0.80f, 0.75f, 0.85f)  // purple
            };

            int colorIndex = Mathf.Clamp(index % colors.Length, 0, colors.Length - 1);
            return new StyleColor(colors[colorIndex]);
        }

        // So sánh thứ tự Rank để tìm Max Rank hiển thị
        private bool IsRankHigher(string rankA, string rankB)
        {
            List<string> rankOrder = new List<string> { "Dong", "Bac", "Vang", "BachKim", "KimCuong", "SieuCap" };
            int indexA = rankOrder.IndexOf(rankA);
            int indexB = rankOrder.IndexOf(rankB);
            return indexA > indexB;
        }
    }
}
