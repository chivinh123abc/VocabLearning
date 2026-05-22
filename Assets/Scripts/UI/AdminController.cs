using UnityEngine;
using UnityEngine.UIElements;

namespace VocabLearning.UI
{
    public partial class VocabStudyController
    {
        private void BindAdminEvents()
        {
            // Header subtitle: hiển thị tên admin
            var lblSubtitle = _root.Q<Label>(className: "header-subtitle");
            if (lblSubtitle != null && _jsonDb?.currentUser != null)
                lblSubtitle.text = $"Xin chào, {_jsonDb.currentUser.username}";

            // Nút Setting (⚙) = Đăng xuất, tên trong UXML: "setting-btn"
            var settingBtn = _root.Q<Button>("setting-btn");
            if (settingBtn != null)
            {
                settingBtn.clicked += () =>
                {
                    _jsonDb.currentUser = null;
                    Debug.Log("[Admin] Đăng xuất → AuthScreen");
                    LoadScreen(AuthScreenAsset);
                };
            }

            // btn-VocabList → Quản lý Bộ Từ Vựng
            var vocabListBtn = _root.Q<Button>("btn-VocabList");
            if (vocabListBtn != null)
            {
                vocabListBtn.clicked += () =>
                {
                    if (VocabListAdminScreenAsset != null)
                    {
                        Debug.Log("[Admin] Mở màn hình Quản lý Bộ Từ Vựng");
                        LoadScreen(VocabListAdminScreenAsset);
                    }
                    else
                    {
                        ShowAdminToast("VocabListAdminScreenAsset chưa được gán!");
                    }
                };
            }


            var userBtn = _root.Q<Button>("btn-users");
            if (userBtn != null)
            {
                userBtn.style.display = DisplayStyle.None;
            }

            // btn-vocab
            var vocabBtn = _root.Q<Button>("btn-vocab");
            if (vocabBtn != null)
            {
                vocabBtn.clicked += () =>
                {
                    if (VocabAdminScreenAsset != null)
                    {
                        Debug.Log("[Admin] Mở màn hình Quản lý Từ vựng");
                        LoadScreen(VocabAdminScreenAsset);
                    }
                    else
                    {
                        ShowAdminToast("VocabAdminScreenAsset chưa được gán!");
                    }
                };
            }


            // btn-quest (Nhiệm vụ)
            var questBtn = _root.Q<Button>("btn-quest");
            if (questBtn != null)
            {
                questBtn.clicked += () =>
                {
                    if (QuestAdminScreenAsset != null)
                    {
                        Debug.Log("[Admin] Mở màn hình Quản lý Nhiệm vụ");
                        LoadScreen(QuestAdminScreenAsset);
                    }
                    else
                    {
                        ShowAdminToast("QuestAdminScreenAsset chưa được gán!");
                    }
                };
            }

            // btn-achievement (Thành tựu)
            var achBtn = _root.Q<Button>("btn-achievement");
            if (achBtn != null)
            {
                achBtn.clicked += () =>
                {
                    if (AchievementAdminScreenAsset != null)
                    {
                        Debug.Log("[Admin] Mở màn hình Quản lý Thành tựu");
                        LoadScreen(AchievementAdminScreenAsset);
                    }
                    else
                    {
                        ShowAdminToast("AchievementAdminScreenAsset chưa được gán!");
                    }
                };
            }

            // btn-item
            var itemBtn = _root.Q<Button>("btn-item");
            if (itemBtn != null)
            {
                itemBtn.style.display = DisplayStyle.None;
            }

            // btn-dashboard
            var dashboardBtn = _root.Q<Button>("btn-dashboard");
            if (dashboardBtn != null)
            {
                dashboardBtn.style.display = DisplayStyle.None;
            }

            // Cập nhật số liệu thống kê
            RefreshAdminStats();
        }

        private void RefreshAdminStats()
        {
            if (_jsonDb == null) return;

            int totalUsers = _jsonDb.registeredUsers?.Count ?? 0;
            SetAdminStatLabel("stat-users", totalUsers.ToString());

            int totalVocab = _jsonDb.vocabSets?.Count ?? 0;
            SetAdminStatLabel("stat-vocab", totalVocab.ToString());

            int totalWords = _jsonDb.words?.Count ?? 0;
            SetAdminStatLabel("stat-questions", totalWords.ToString());
        }

        private void SetAdminStatLabel(string labelName, string value)
        {
            var lbl = _root.Q<Label>(labelName);
            if (lbl != null) lbl.text = value;
        }

        // Toast nổi lên 2 giây khi bấm nút
        // Add vào "admin-root" (class của VisualElement ngoài cùng trong UXML)
        // để position:absolute hoạt động đúng (không bị ScrollView che)
        private void ShowAdminToast(string message)
        {
            const string toastName = "admin-toast";

            // Tìm container "admin-root" — VisualElement ngoài cùng trong adminScreen.uxml
            var container = _root.Q<VisualElement>(className: "admin-root");
            if (container == null)
            {
                // Fallback: lấy con đầu tiên của _root (chính là admin-root)
                container = _root.childCount > 0 ? _root[0] : _root;
                Debug.LogWarning("[AdminToast] Không tìm thấy .admin-root, dùng fallback: " + container?.name);
            }

            var toast = container.Q<Label>(toastName);
            if (toast == null)
            {
                toast = new Label();
                toast.name = toastName;
                toast.style.position = Position.Absolute;
                toast.style.bottom = 50;
                toast.style.left = 20;
                toast.style.right = 20;
                toast.style.height = 60;
                toast.style.backgroundColor = new StyleColor(new Color(0.06f, 0.09f, 0.16f, 0.95f));
                toast.style.color = Color.white;
                toast.style.fontSize = 18;
                toast.style.borderTopLeftRadius = 14;
                toast.style.borderTopRightRadius = 14;
                toast.style.borderBottomLeftRadius = 14;
                toast.style.borderBottomRightRadius = 14;
                toast.style.paddingLeft = 16;
                toast.style.paddingRight = 16;
                toast.style.unityTextAlign = TextAnchor.MiddleCenter;
                toast.style.display = DisplayStyle.None;
                container.Add(toast);
            }

            toast.text = message;
            toast.style.display = DisplayStyle.Flex;
            toast.BringToFront();

            toast.schedule.Execute(() =>
            {
                toast.style.display = DisplayStyle.None;
            }).StartingIn(2000);
        }
    }
}