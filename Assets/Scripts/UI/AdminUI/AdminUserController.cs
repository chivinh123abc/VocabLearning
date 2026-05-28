using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using VocabLearning.Data;

namespace VocabLearning.UI
{
    public partial class VocabStudyController
    {
        private List<UserJson> _adminUserList = new List<UserJson>();
        private UserJson _editingUser = null;

        private void BindAdminUserEvents()
        {
            // Nút Back - quay về màn hình admin chính
            var btnBack = _root.Q<Button>("btn-back");
            if (btnBack != null)
            {
                btnBack.clicked += () =>
                {
                    Debug.Log("[Admin Users] Trở lại AdminScreen");
                    LoadScreen(AdminScreenAsset);
                };
            }

            // Ô tìm kiếm Search
            var searchField = _root.Q<TextField>("search-field");
            if (searchField != null)
            {
                searchField.RegisterValueChangedCallback(evt => RefreshUserListUI());
            }

            // Bộ lọc Role
            var filterRole = _root.Q<DropdownField>("filter-role");
            if (filterRole != null)
            {
                filterRole.choices = new List<string> { "Tất cả", "user", "admin" };
                filterRole.value = "Tất cả";
                filterRole.RegisterValueChangedCallback(evt => RefreshUserListUI());
            }

            // Bộ lọc Status
            var filterStatus = _root.Q<DropdownField>("filter-status");
            if (filterStatus != null)
            {
                filterStatus.choices = new List<string> { "Tất cả", "active", "banned" };
                filterStatus.value = "Tất cả";
                filterStatus.RegisterValueChangedCallback(evt => RefreshUserListUI());
            }

            // Thiết lập giá trị dropdown trong modal
            var inputRole = _root.Q<DropdownField>("input-role");
            if (inputRole != null)
            {
                inputRole.choices = new List<string> { "user", "admin" };
            }

            var inputStatus = _root.Q<DropdownField>("input-status");
            if (inputStatus != null)
            {
                inputStatus.choices = new List<string> { "active", "banned" };
            }

            // Gán sự kiện cho các nút điều khiển trong modal
            var btnClose = _root.Q<Button>("btn-modal-close");
            if (btnClose != null) btnClose.clicked += CloseUserModal;

            var btnCancel = _root.Q<Button>("btn-modal-cancel");
            if (btnCancel != null) btnCancel.clicked += CloseUserModal;

            var btnSave = _root.Q<Button>("btn-modal-save");
            if (btnSave != null) btnSave.clicked += SaveUserFromModal;

            // Kéo danh sách người dùng từ backend Express
            FetchAllUsersFromServer();
        }

        private void FetchAllUsersFromServer()
        {
            VocabLearning.Network.NetworkClient.Instance.AdminGetUsers((success, msg, data) =>
            {
                if (success && data != null && data.users != null)
                {
                    _adminUserList = data.users;
                    Debug.Log($"[Admin Users] Tải thành công {_adminUserList.Count} người dùng.");
                    RefreshUserListUI();
                }
                else
                {
                    Debug.LogError($"[Admin Users] Lỗi tải người dùng: {msg}");
                    ShowAdminToast($"Không thể tải danh sách user: {msg}");
                }
            });
        }

        private void RefreshUserListUI()
        {
            var userListContainer = _root.Q<ScrollView>("user-list");
            if (userListContainer == null) return;

            userListContainer.Clear();

            // 1. Tính toán thống kê toàn cục
            int totalUsers = _adminUserList.Count;
            int totalBanned = _adminUserList.Count(u => u.status == "banned");
            int totalAdmins = _adminUserList.Count(u => u.role == "admin");
            float avgLevel = totalUsers > 0 ? (float)_adminUserList.Average(u => u.level) : 0f;

            // Đưa thống kê lên UI
            SetTextValue("user-stat-total", totalUsers.ToString());
            SetTextValue("user-stat-banned", totalBanned.ToString());
            SetTextValue("user-stat-admin", totalAdmins.ToString());
            SetTextValue("user-stat-avg-level", avgLevel.ToString("F1"));

            // 2. Lấy bộ lọc từ UI
            var searchField = _root.Q<TextField>("search-field");
            string searchQuery = searchField != null ? searchField.value.Trim().ToLower() : "";

            var filterRoleField = _root.Q<DropdownField>("filter-role");
            string roleFilter = filterRoleField != null ? filterRoleField.value : "Tất cả";

            var filterStatusField = _root.Q<DropdownField>("filter-status");
            string statusFilter = filterStatusField != null ? filterStatusField.value : "Tất cả";

            int filteredCount = 0;

            foreach (var user in _adminUserList)
            {
                // Kiểm tra tìm kiếm theo từ khóa
                bool matchesSearch = string.IsNullOrEmpty(searchQuery) ||
                                     user.id.ToLower().Contains(searchQuery) ||
                                     user.username.ToLower().Contains(searchQuery) ||
                                     (!string.IsNullOrEmpty(user.email) && user.email.ToLower().Contains(searchQuery));

                // Kiểm tra theo quyền hạn
                bool matchesRole = roleFilter == "Tất cả" || user.role.ToLower() == roleFilter.ToLower();

                // Kiểm tra theo trạng thái
                bool matchesStatus = statusFilter == "Tất cả" || user.status.ToLower() == statusFilter.ToLower();

                if (!matchesSearch || !matchesRole || !matchesStatus) continue;

                filteredCount++;

                // Tạo User Card VisualElement
                var card = new VisualElement();
                card.AddToClassList("vocab-card");

                // Thêm sự kiện click đúp/click card để sửa
                card.RegisterCallback<ClickEvent>(evt =>
                {
                    if (evt.target is Button btn && btn.ClassListContains("btn-delete")) return;
                    OpenEditUserModal(user);
                });

                // Avatar tròn hiển thị ký tự đầu tiên
                var avatar = new VisualElement();
                avatar.AddToClassList("vocab-thumbnail");
                avatar.style.backgroundColor = GetThumbnailColor(user.username);
                avatar.style.justifyContent = Justify.Center;
                avatar.style.alignItems = Align.Center;
                avatar.style.borderTopLeftRadius = 42;
                avatar.style.borderTopRightRadius = 42;
                avatar.style.borderBottomLeftRadius = 42;
                avatar.style.borderBottomRightRadius = 42;

                var avatarChar = new Label(string.IsNullOrEmpty(user.username) ? "?" : user.username[0].ToString().ToUpper());
                avatarChar.style.fontSize = 34;
                avatarChar.style.unityFontStyleAndWeight = FontStyle.Bold;
                avatarChar.style.color = Color.white;
                avatar.Add(avatarChar);

                // Container chứa thông tin người chơi
                var info = new VisualElement();
                info.AddToClassList("vocab-info");

                // Hàng tên tài khoản + badge quyền hạn + badge trạng thái
                var nameRow = new VisualElement();
                nameRow.AddToClassList("vocab-name-row");

                var lblUsername = new Label(user.username);
                lblUsername.AddToClassList("vocab-word");
                nameRow.Add(lblUsername);

                // Badge Quyền hạn (Admin = Gold/Blue, User = Light)
                var lblRole = new Label(user.role == "admin" ? "Quản Trị" : "Người Chơi");
                lblRole.AddToClassList("rank-badge");
                if (user.role == "admin")
                {
                    // Royal blue badge cho Admin
                    lblRole.style.backgroundColor = new StyleColor(new Color(0.88f, 0.92f, 0.99f));
                    lblRole.style.color = new StyleColor(new Color(0.18f, 0.36f, 0.77f));
                    lblRole.style.borderTopColor = new StyleColor(new Color(0.78f, 0.85f, 0.98f));
                    lblRole.style.borderRightColor = new StyleColor(new Color(0.78f, 0.85f, 0.98f));
                    lblRole.style.borderBottomColor = new StyleColor(new Color(0.78f, 0.85f, 0.98f));
                    lblRole.style.borderLeftColor = new StyleColor(new Color(0.78f, 0.85f, 0.98f));
                }
                else
                {
                    // Light gray cho User
                    lblRole.style.backgroundColor = new StyleColor(new Color(0.95f, 0.95f, 0.94f));
                    lblRole.style.color = new StyleColor(new Color(0.45f, 0.44f, 0.42f));
                    lblRole.style.borderTopColor = new StyleColor(new Color(0.88f, 0.87f, 0.85f));
                    lblRole.style.borderRightColor = new StyleColor(new Color(0.88f, 0.87f, 0.85f));
                    lblRole.style.borderBottomColor = new StyleColor(new Color(0.88f, 0.87f, 0.85f));
                    lblRole.style.borderLeftColor = new StyleColor(new Color(0.88f, 0.87f, 0.85f));
                }
                nameRow.Add(lblRole);

                // Badge Trạng thái (Hoạt động = Green, Khóa = Red)
                var lblStatus = new Label(user.status == "active" ? "Hoạt Động" : "Đã Khóa");
                lblStatus.AddToClassList("rank-badge");
                lblStatus.style.marginLeft = 10;
                if (user.status == "active")
                {
                    lblStatus.style.backgroundColor = new StyleColor(new Color(0.86f, 0.96f, 0.9f));
                    lblStatus.style.color = new StyleColor(new Color(0.15f, 0.6f, 0.38f));
                    lblStatus.style.borderTopColor = new StyleColor(new Color(0.72f, 0.91f, 0.8f));
                    lblStatus.style.borderRightColor = new StyleColor(new Color(0.72f, 0.91f, 0.8f));
                    lblStatus.style.borderBottomColor = new StyleColor(new Color(0.72f, 0.91f, 0.8f));
                    lblStatus.style.borderLeftColor = new StyleColor(new Color(0.72f, 0.91f, 0.8f));
                }
                else
                {
                    // Premium Soft Red cho Banned
                    lblStatus.style.backgroundColor = new StyleColor(new Color(0.98f, 0.88f, 0.88f));
                    lblStatus.style.color = new StyleColor(new Color(0.85f, 0.23f, 0.23f));
                    lblStatus.style.borderTopColor = new StyleColor(new Color(0.96f, 0.76f, 0.76f));
                    lblStatus.style.borderRightColor = new StyleColor(new Color(0.96f, 0.76f, 0.76f));
                    lblStatus.style.borderBottomColor = new StyleColor(new Color(0.96f, 0.76f, 0.76f));
                    lblStatus.style.borderLeftColor = new StyleColor(new Color(0.96f, 0.76f, 0.76f));
                }
                nameRow.Add(lblStatus);

                // Email
                var lblEmail = new Label(string.IsNullOrEmpty(user.email) ? "Không có Email" : user.email);
                lblEmail.AddToClassList("vocab-meaning");

                // Chỉ số chi tiết
                var rankInfo = GetRankTier(user.rankPoints); // returns (name, icon, color)
                var lblDetails = new Label($"ID: {user.id}  |  🪙 {user.coins:N0} xu  |  🌟 Cấp {user.level}  |  🏆 {rankInfo.name} ({user.rankPoints} RP)");
                lblDetails.AddToClassList("vocab-id");

                info.Add(nameRow);
                info.Add(lblEmail);
                info.Add(lblDetails);

                // Nút chỉnh sửa (✏)
                var btnEdit = new Button();
                btnEdit.text = "✏";
                btnEdit.AddToClassList("btn-delete");
                btnEdit.style.color = new StyleColor(new Color(0.23f, 0.51f, 0.96f)); // Royal Blue
                btnEdit.style.backgroundColor = new StyleColor(new Color(0.9f, 0.93f, 0.98f));
                btnEdit.clicked += () => OpenEditUserModal(user);

                // Nút xóa tài khoản (🗑)
                var btnDelete = new Button();
                btnDelete.text = "🗑";
                btnDelete.AddToClassList("btn-delete");
                btnDelete.style.marginLeft = 8;
                btnDelete.clicked += () => DeleteUser(user);

                // Gom cụm card
                card.Add(avatar);
                card.Add(info);
                card.Add(btnEdit);
                card.Add(btnDelete);

                userListContainer.Add(card);
            }

            SetTextValue("user-count-label", $"{filteredCount} người dùng");
        }

        private void OpenEditUserModal(UserJson user)
        {
            _editingUser = user;

            var modalOverlay = _root.Q<VisualElement>("modal-overlay");
            if (modalOverlay == null) return;

            var lblTitle = modalOverlay.Q<Label>("modal-title");
            if (lblTitle != null) lblTitle.text = $"Sửa Tài Khoản ({user.username})";

            // Điền thông tin cũ vào form
            SetInputValue("input-username", user.username);
            SetInputValue("input-email", string.IsNullOrEmpty(user.email) ? "" : user.email);
            SetInputValue("input-role", user.role);
            SetInputValue("input-status", user.status);
            SetInputValue("input-level", user.level.ToString());
            SetInputValue("input-exp", user.exp.ToString());
            SetInputValue("input-coins", user.coins.ToString());
            SetInputValue("input-rankPoints", user.rankPoints.ToString());

            HideModalError();

            modalOverlay.style.display = DisplayStyle.Flex;
        }

        private void CloseUserModal()
        {
            var modalOverlay = _root.Q<VisualElement>("modal-overlay");
            if (modalOverlay != null)
            {
                modalOverlay.style.display = DisplayStyle.None;
            }
            _editingUser = null;
        }

        private void SaveUserFromModal()
        {
            if (_editingUser == null) return;

            string roleStr = GetInputValue("input-role");
            string statusStr = GetInputValue("input-status");
            string levelStr = GetInputValue("input-level").Trim();
            string expStr = GetInputValue("input-exp").Trim();
            string coinsStr = GetInputValue("input-coins").Trim();
            string rpStr = GetInputValue("input-rankPoints").Trim();

            // 1. Kiểm tra an toàn: Không cho phép Admin tự khóa tài khoản của chính mình
            if (_editingUser.id == _jsonDb.currentUser.id && statusStr == "banned")
            {
                ShowModalError("Bạn không thể tự khóa (ban) chính mình!");
                return;
            }

            // 2. Kiểm tra định dạng số hợp lệ
            if (!int.TryParse(levelStr, out int level) || level < 1)
            {
                ShowModalError("Cấp độ phải là số nguyên dương >= 1");
                return;
            }
            if (!int.TryParse(expStr, out int exp) || exp < 0)
            {
                ShowModalError("EXP phải là số nguyên >= 0");
                return;
            }
            if (!int.TryParse(coinsStr, out int coins) || coins < 0)
            {
                ShowModalError("Coins phải là số nguyên >= 0");
                return;
            }
            if (!int.TryParse(rpStr, out int rp) || rp < 0)
            {
                ShowModalError("Rank Points phải là số nguyên >= 0");
                return;
            }

            // 3. Cập nhật offline cục bộ
            _editingUser.role = roleStr;
            _editingUser.status = statusStr;
            _editingUser.level = level;
            _editingUser.exp = exp;
            _editingUser.coins = coins;
            _editingUser.rankPoints = rp;

            Debug.Log($"[Admin Users] Cập nhật offline user {_editingUser.username}");

            // Nếu Admin tự chỉnh sửa chính mình, đồng bộ vào phiên đăng nhập hiện tại
            if (_editingUser.id == _jsonDb.currentUser.id)
            {
                _jsonDb.currentUser.role = roleStr;
                _jsonDb.currentUser.status = statusStr;
                _jsonDb.currentUser.level = level;
                _jsonDb.currentUser.exp = exp;
                _jsonDb.currentUser.coins = coins;
                _jsonDb.currentUser.rankPoints = rp;
            }

            // 4. Đồng bộ lên SQL Server qua REST API
            VocabLearning.Network.NetworkClient.Instance.AdminUpdateUser(_editingUser, (success, msg, res) =>
            {
                if (success)
                {
                    Debug.Log($"[Admin Users - Network] Cập nhật thành công {_editingUser.username} trên SQL Server.");
                }
                else
                {
                    Debug.LogError($"[Admin Users - Network] Lỗi cập nhật {_editingUser.username}: {msg}");
                }
            });

            // Ghi thay đổi cục bộ bền vững
            SaveJsonDatabase();

            // Đóng Modal và làm tươi danh sách
            CloseUserModal();
            RefreshUserListUI();
        }

        private void DeleteUser(UserJson user)
        {
            if (_jsonDb == null) return;

            // 1. Kiểm tra an toàn: Không cho phép Admin tự xóa chính mình
            if (user.id == _jsonDb.currentUser.id)
            {
                ShowAdminToast("Bạn không thể tự xóa tài khoản của chính mình!");
                return;
            }

            // 2. Xóa khỏi danh sách bộ nhớ cục bộ
            _adminUserList.Remove(user);
            Debug.Log($"[Admin Users] Đã xóa offline người chơi: {user.username}");

            // 3. Gọi API xóa trực tiếp lên backend SQL Server
            VocabLearning.Network.NetworkClient.Instance.AdminDeleteUser(user.id, (success, msg, res) =>
            {
                if (success)
                {
                    Debug.Log($"[Admin Users - Network] Đã xóa thành công {user.username} khỏi SQL Server.");
                }
                else
                {
                    Debug.LogError($"[Admin Users - Network] Thất bại khi xóa {user.username}: {msg}");
                }
            });

            // Ghi thay đổi offline bền vững
            SaveJsonDatabase();

            // Làm mới lại UI danh sách
            RefreshUserListUI();
        }
    }
}
