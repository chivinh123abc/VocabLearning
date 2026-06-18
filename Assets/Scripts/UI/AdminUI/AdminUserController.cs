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

            // Tự động tính toán Level khi chỉnh sửa EXP trong Modal Admin
            var inputExp = _root.Q<TextField>("input-exp");
            var inputLevel = _root.Q<TextField>("input-level");
            if (inputExp != null && inputLevel != null)
            {
                inputExp.RegisterValueChangedCallback(evt =>
                {
                    if (int.TryParse(evt.newValue, out int parsedExp) && parsedExp >= 0)
                    {
                        int calculatedLevel = CalculateLevel(parsedExp);
                        inputLevel.value = calculatedLevel.ToString();
                    }
                });
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
                                     (!string.IsNullOrEmpty(user.displayName) && user.displayName.ToLower().Contains(searchQuery)) ||
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

                string shownName = (string.IsNullOrEmpty(user.displayName) || user.displayName == user.username) 
                    ? user.username 
                    : $"{user.displayName} ({user.username})";
                var lblUsername = new Label(shownName);
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
            if (!int.TryParse(expStr, out int exp) || exp < 0)
            {
                ShowModalError("EXP phải là số nguyên >= 0");
                return;
            }
            int level = CalculateLevel(exp); // Tự động tính toán Level từ EXP

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

            // 3. Cập nhật tạm thời vào object đang chỉnh sửa (để UI phản hồi nhanh)
            _editingUser.role = roleStr;
            _editingUser.status = statusStr;
            _editingUser.level = level;
            _editingUser.exp = exp;
            _editingUser.coins = coins;
            _editingUser.rankPoints = rp;

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

            // 4. Capture thông tin cần thiết TRƯỚC khi đóng modal
            // (CloseUserModal() sẽ set _editingUser = null, nên phải lưu lại trước)
            string editingUsername = _editingUser.username;
            var userToSave = _editingUser; // giữ reference trước khi null hoá

            // Đóng modal
            CloseUserModal();

            // 5. Đồng bộ lên SQL Server qua REST API, rồi RELOAD hoàn toàn từ DB
            // (Đây là nguồn sự thật duy nhất — đảm bảo mọi thống kê như tổng bị khóa luôn chính xác)
            VocabLearning.Network.NetworkClient.Instance.AdminUpdateUser(userToSave, (success, msg, res) =>
            {
                if (success)
                {
                    Debug.Log($"[Admin Users - Network] Cập nhật thành công {editingUsername} trên SQL Server.");
                    // Tải lại toàn bộ danh sách từ DB để UI luôn phản ánh đúng thực tế
                    FetchAllUsersFromServer();
                    ShowAdminToast($"✅ Đã cập nhật tài khoản {editingUsername} thành công!");
                }
                else
                {
                    Debug.LogError($"[Admin Users - Network] Lỗi cập nhật {editingUsername}: {msg}");
                    ShowAdminToast($"❌ Lỗi cập nhật: {msg}");
                    // Nếu lỗi, vẫn reload để hoàn nguyên dữ liệu đúng từ DB
                    FetchAllUsersFromServer();
                }
            });
        }

        private void DeleteUser(UserJson user)
        {
            if (_jsonDb == null) return;

            // 1. Kiểm tra an toàn: Không cho phép Admin tự tác động lên tài khoản của chính mình
            if (user.id == _jsonDb.currentUser.id)
            {
                ShowAdminToast("Bạn không thể tự tác động lên tài khoản của chính mình!");
                return;
            }

            ShowUserActionDialog(user);
        }

        private void ShowUserActionDialog(UserJson user)
        {
            // 1. Overlay (Màn phủ mờ)
            VisualElement overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.width = Length.Percent(100);
            overlay.style.height = Length.Percent(100);
            overlay.style.backgroundColor = new Color(0, 0, 0, 0.75f);
            overlay.style.justifyContent = Justify.Center;
            overlay.style.alignItems = Align.Center;

            // 2. Dialog Box
            VisualElement dialog = new VisualElement();
            dialog.style.width = 460; // Increased from 340 to 460 to prevent text clipping and look spacious
            dialog.style.backgroundColor = new Color(0.08f, 0.12f, 0.22f); // Deep premium dark blue-gray
            dialog.style.borderTopLeftRadius = 24;
            dialog.style.borderTopRightRadius = 24;
            dialog.style.borderBottomLeftRadius = 24;
            dialog.style.borderBottomRightRadius = 24;
            dialog.style.paddingTop = 32;
            dialog.style.paddingBottom = 32;
            dialog.style.paddingLeft = 32;
            dialog.style.paddingRight = 32;
            dialog.style.borderTopWidth = 1.5f;
            dialog.style.borderBottomWidth = 1.5f;
            dialog.style.borderLeftWidth = 1.5f;
            dialog.style.borderRightWidth = 1.5f;
            dialog.style.borderTopColor = new Color(0.18f, 0.35f, 0.64f, 0.6f); // Elegant glow outline
            dialog.style.borderBottomColor = new Color(0.18f, 0.35f, 0.64f, 0.6f);
            dialog.style.borderLeftColor = new Color(0.18f, 0.35f, 0.64f, 0.6f);
            dialog.style.borderRightColor = new Color(0.18f, 0.35f, 0.64f, 0.6f);

            // 3. Content
            Label titleLbl = new Label("Xử lý tài khoản");
            titleLbl.style.fontSize = 24;
            titleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            titleLbl.style.color = Color.white;
            titleLbl.style.marginBottom = 16;
            titleLbl.style.whiteSpace = WhiteSpace.Normal;
            dialog.Add(titleLbl);

            Label msgLbl = new Label($"Bạn muốn Khóa (Ban) tài khoản '{user.username}' để giữ lại lịch sử chơi/học tập, hay muốn Xóa vĩnh viễn khỏi hệ thống?");
            msgLbl.style.fontSize = 15;
            msgLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            msgLbl.style.color = new Color(0.70f, 0.76f, 0.85f);
            msgLbl.style.marginBottom = 28;
            msgLbl.style.whiteSpace = WhiteSpace.Normal;
            dialog.Add(msgLbl);

            // 4. Buttons Stack
            // Khóa (Ban)
            Button btnBan = new Button();
            btnBan.text = "🔒 KHÓA TÀI KHOẢN (KHUYÊN DÙNG)";
            btnBan.style.height = 48;
            btnBan.style.fontSize = 14;
            btnBan.style.backgroundColor = new StyleColor(new Color(0.95f, 0.57f, 0.06f)); // Premium Gold/Orange
            btnBan.style.color = Color.white;
            btnBan.style.unityFontStyleAndWeight = FontStyle.Bold;
            btnBan.style.marginBottom = 12;
            btnBan.style.borderTopLeftRadius = 12;
            btnBan.style.borderTopRightRadius = 12;
            btnBan.style.borderBottomLeftRadius = 12;
            btnBan.style.borderBottomRightRadius = 12;
            btnBan.style.borderTopWidth = 0;
            btnBan.style.borderBottomWidth = 0;
            btnBan.style.borderLeftWidth = 0;
            btnBan.style.borderRightWidth = 0;
            btnBan.clicked += () =>
            {
                _root.Remove(overlay);
                BanUser(user);
            };
            dialog.Add(btnBan);

            // Xóa vĩnh viễn
            Button btnDelete = new Button();
            btnDelete.text = "🗑 XÓA VĨNH VIỄN";
            btnDelete.style.height = 48;
            btnDelete.style.fontSize = 14;
            btnDelete.style.backgroundColor = new StyleColor(new Color(0.88f, 0.24f, 0.24f)); // Premium Crimson Red
            btnDelete.style.color = Color.white;
            btnDelete.style.unityFontStyleAndWeight = FontStyle.Bold;
            btnDelete.style.marginBottom = 12;
            btnDelete.style.borderTopLeftRadius = 12;
            btnDelete.style.borderTopRightRadius = 12;
            btnDelete.style.borderBottomLeftRadius = 12;
            btnDelete.style.borderBottomRightRadius = 12;
            btnDelete.style.borderTopWidth = 0;
            btnDelete.style.borderBottomWidth = 0;
            btnDelete.style.borderLeftWidth = 0;
            btnDelete.style.borderRightWidth = 0;
            btnDelete.clicked += () =>
            {
                _root.Remove(overlay);
                ConfirmDeleteUserPermanently(user);
            };
            dialog.Add(btnDelete);

            // Hủy bỏ
            Button btnCancel = new Button();
            btnCancel.text = "HỦY";
            btnCancel.style.height = 44;
            btnCancel.style.fontSize = 14;
            btnCancel.style.backgroundColor = new StyleColor(new Color(0.15f, 0.20f, 0.30f)); // Premium Slate Blue
            btnCancel.style.color = new Color(0.70f, 0.76f, 0.85f);
            btnCancel.style.unityFontStyleAndWeight = FontStyle.Bold;
            btnCancel.style.borderTopLeftRadius = 12;
            btnCancel.style.borderTopRightRadius = 12;
            btnCancel.style.borderBottomLeftRadius = 12;
            btnCancel.style.borderBottomRightRadius = 12;
            btnCancel.style.borderTopWidth = 0;
            btnCancel.style.borderBottomWidth = 0;
            btnCancel.style.borderLeftWidth = 0;
            btnCancel.style.borderRightWidth = 0;
            btnCancel.clicked += () =>
            {
                _root.Remove(overlay);
            };
            dialog.Add(btnCancel);

            overlay.Add(dialog);
            _root.Add(overlay);
        }

        private void BanUser(UserJson user)
        {
            user.status = "banned";
            
            // Cập nhật lên SQL Server
            VocabLearning.Network.NetworkClient.Instance.AdminUpdateUser(user, (success, msg, res) =>
            {
                if (success)
                {
                    Debug.Log($"[Admin Users - Network] Đã khóa tài khoản {user.username} thành công.");
                    ShowAdminToast($"🔒 Đã khóa tài khoản {user.username}!");
                }
                else
                {
                    Debug.LogError($"[Admin Users - Network] Lỗi khi khóa tài khoản {user.username}: {msg}");
                    ShowAdminToast($"❌ Lỗi: {msg}");
                }
                FetchAllUsersFromServer();
            });
        }

        private void ConfirmDeleteUserPermanently(UserJson user)
        {
            // Hỏi lại lần nữa trước khi xóa vĩnh viễn (double check)
            ShowConfirmationDialog(
                "Xác nhận xóa vĩnh viễn",
                $"Bạn có THỰC SỰ chắc chắn muốn xóa vĩnh viễn tài khoản '{user.username}'? Mọi tiến trình học và tiền vàng của tài khoản này sẽ BỊ XÓA HOÀN TOÀN và không thể khôi phục!",
                () => { ExecuteDeleteUser(user); },
                null
            );
        }

        private void ExecuteDeleteUser(UserJson user)
        {
            string deletingUsername = user.username;
            VocabLearning.Network.NetworkClient.Instance.AdminDeleteUser(user.id, (success, msg, res) =>
            {
                if (success)
                {
                    Debug.Log($"[Admin Users - Network] Đã xóa thành công {deletingUsername} khỏi SQL Server.");
                    ShowAdminToast($"🗑 Đã xóa tài khoản {deletingUsername} vĩnh viễn.");
                }
                else
                {
                    Debug.LogError($"[Admin Users - Network] Thất bại khi xóa {deletingUsername}: {msg}");
                    ShowAdminToast($"❌ Lỗi xóa tài khoản: {msg}");
                }
                FetchAllUsersFromServer();
            });
        }
    }
}
