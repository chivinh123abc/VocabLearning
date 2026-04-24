using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Linq;

namespace VocabLearning.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class VocabStudyController : MonoBehaviour
    {
        [Header("UI Templates (UXML)")]
        public VisualTreeAsset HomeScreenAsset;
        public VisualTreeAsset VocabDetailScreenAsset;
        public VisualTreeAsset PracticeModeScreenAsset;
        [Header("New Features")]
        public VisualTreeAsset QuestScreenAsset;
        public VisualTreeAsset BattleScreenAsset;
        public VisualTreeAsset BattleLoadoutScreenAsset;
        public VisualTreeAsset BattleGameplayScreenAsset;
        public VisualTreeAsset SoloQuizScreenAsset;
        public VisualTreeAsset FriendScreenAsset;

        [Header("Remaining Features")]
        public VisualTreeAsset ShopScreenAsset;
        public VisualTreeAsset RankingScreenAsset;
        public VisualTreeAsset ProfileScreenAsset;
        public VisualTreeAsset ResultScreenAsset;
        public VisualTreeAsset AchievementScreenAsset;
        public VisualTreeAsset InventoryScreenAsset;

        [Header("Databases (JSON)")]
        public TextAsset OverrideJsonDb; // Cửa hậu nếu muốn kéo thủ công từ Inspector (tùy chọn)

        private UIDocument _doc;
        private VisualElement _root;

        private VocabLearning.Data.MockDatabase _jsonDb;
        private VocabLearning.Data.VocabSetJson _currentVocabSet;

        // --- STATE DÀNH CHO PRACTICE SCREEN ---
        private int _practiceCurrentIndex = 0;
        private bool _practiceShowMeaning = false;

        // --- STATE DÀNH CHO INVENTORY SCREEN ---
        private string _inventoryCurrentTab = "Consumable";

        // --- STATE DÀNH CHO QUEST SCREEN ---
        private string _questCurrentTab = "Active";

        // --- STATE DÀNH CHO SHOP SCREEN ---
        private string _shopCurrentTab = "Cosmetic";

        // --- STATE DÀNH CHO DETAIL SCREEN ---
        private string _currentSelectedLevel = "Easy";
        private List<string> _sessionNewlyMasteredWords = new List<string>(); // NEW: Track words mastered in CURRENT session
        private int _lastSessionCoins = 0;
        private int _lastSessionExp = 0;

        // --- STATE DÀNH CHO BATTLE HISTORY ---
        private List<VocabLearning.Data.BattleRoundRecord> _currentBattleRounds = new List<VocabLearning.Data.BattleRoundRecord>();
        private VocabLearning.Data.BattleHistoryRecord _lastBattleRecord;

        private void OnEnable()
        {
            _doc = GetComponent<UIDocument>();
            LoadJsonDatabase();

            // Khởi chạy HomeScreen mặc định nếu chưa có VisualTree
            if (_doc.visualTreeAsset == null && HomeScreenAsset != null)
            {
                _doc.visualTreeAsset = HomeScreenAsset;
            }

            if (HomeScreenAsset == null || VocabDetailScreenAsset == null)
            {
                Debug.LogError("🚨 LỖI: Bạn CẦN kéo thả các file UXML vào 3 ô trống (HomeScreenAsset, VocabDetailScreenAsset...) trong Script VocabStudyController ở cửa sổ Inspector!");
            }

            _root = _doc.rootVisualElement;
            _root.Clear();
            _doc.visualTreeAsset.CloneTree(_root);
            BindCurrentEvents();
        }

        // --- Hàm Quản Lý Chuyển Màn Hình ---
        private void LoadScreen(VisualTreeAsset newScreenAsset)
        {
            if (newScreenAsset == null) return;

            _root.Clear();
            newScreenAsset.CloneTree(_root);
            BindCurrentEvents(newScreenAsset);
        }

        private void BindCurrentEvents(VisualTreeAsset targetAsset = null)
        {
            if (_root == null) return;

            // Nếu không được truyền vào, thử lấy từ assigned UIDocument (lúc vừa mở lên)
            if (targetAsset == null) targetAsset = _doc.visualTreeAsset;

            if (targetAsset == HomeScreenAsset) BindHomeEvents();
            else if (targetAsset == VocabDetailScreenAsset) BindDetailEvents();
            else if (targetAsset == PracticeModeScreenAsset) BindPracticeEvents();
            else if (targetAsset == QuestScreenAsset) BindQuestEvents();
            else if (targetAsset == BattleScreenAsset) BindBattleEvents();
            else if (targetAsset == BattleLoadoutScreenAsset) BindBattleLoadoutEvents();
            else if (targetAsset == BattleGameplayScreenAsset) BindBattleGameplayEvents();
            else if (targetAsset == FriendScreenAsset) BindFriendEvents();
            else if (targetAsset == SoloQuizScreenAsset) BindSoloQuizEvents();
            else if (targetAsset == ShopScreenAsset) BindShopEvents();
            else if (targetAsset == RankingScreenAsset) BindRankingEvents();
            else if (targetAsset == ProfileScreenAsset) BindProfileEvents();
            else if (targetAsset == ResultScreenAsset) BindResultEvents();
            else if (targetAsset == AchievementScreenAsset) BindAchievementEvents();
            else if (targetAsset == InventoryScreenAsset) BindInventoryEvents();
            else BindOtherScreens();
        }

        // --- QUẢN LÝ DỮ LIỆU JSON ---
        private void LoadJsonDatabase()
        {
            TextAsset jsonAsset = OverrideJsonDb != null ? OverrideJsonDb : Resources.Load<TextAsset>("Mockdata/db");
            if (jsonAsset != null)
            {
                // Dùng JsonUtility gốc của Unity để đọc thẳng file db.json thành Object siêu tốc
                _jsonDb = JsonUtility.FromJson<VocabLearning.Data.MockDatabase>(jsonAsset.text);
                Debug.Log($"[JSON DB] Đã tải Database. Số lượng bộ từ vựng: {_jsonDb.vocabSets.Count}");

                // Tự động kiểm tra và làm mới nhiệm vụ hàng ngày
                CheckDailyQuests();

                // Kiểm tra điểm danh tuần
                CheckWeeklyLogin();
            }
            else
            {
                Debug.LogError("🚨 LỖI: Không tìm thấy file JSON trong thư mục Resources/Mockdata/db.json");
            }
        }

        private void CheckDailyQuests()
        {
            if (_jsonDb == null || _jsonDb.currentUser == null || _jsonDb.questPool == null) return;

            string today = System.DateTime.Now.ToString("yyyy-MM-dd");
            if (_jsonDb.currentUser.lastQuestRefreshDate != today)
            {
                Debug.Log("🌞 New day detected! Refreshing daily quests...");
                RefreshQuests();
                _jsonDb.currentUser.lastQuestRefreshDate = today;
            }
            // Trường hợp file JSON trống quests (lần đầu), cũng cần refresh
            else if (_jsonDb.quests == null || _jsonDb.quests.Count == 0)
            {
                RefreshQuests();
            }
        }

        private void RefreshQuests()
        {
            if (_jsonDb.questPool == null || _jsonDb.questPool.Count == 0) return;

            // Copy pool để tránh làm hỏng dữ liệu gốc
            List<VocabLearning.Data.QuestJson> pool = new List<VocabLearning.Data.QuestJson>(_jsonDb.questPool);
            List<VocabLearning.Data.QuestJson> selected = new List<VocabLearning.Data.QuestJson>();

            int countToPick = Mathf.Min(5, pool.Count);
            for (int i = 0; i < countToPick; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, pool.Count);
                var q = pool[randomIndex];

                // Quan trọng: Phải reset trạng thái khi chọn nhiệm vụ mới cho ngày hôm nay
                q.currentProgress = 0;
                q.isClaimed = false;

                selected.Add(q);
                pool.RemoveAt(randomIndex);
            }

            _jsonDb.quests = selected;
            Debug.Log($"✅ Đã làm mới 5 nhiệm vụ ngẫu nhiên cho ngày hôm nay.");
        }

        private void CheckWeeklyLogin()
        {
            if (_jsonDb == null || _jsonDb.currentUser == null) return;

            var user = _jsonDb.currentUser;
            if (user.weeklyLogin == null) user.weeklyLogin = new VocabLearning.Data.WeeklyLoginData();

            System.DateTime now = System.DateTime.Now;

            // Tính ngày Thứ 2 của tuần hiện tại làm mốc reset tuần
            int diff = (7 + (now.DayOfWeek - System.DayOfWeek.Monday)) % 7;
            System.DateTime monday = now.AddDays(-1 * diff).Date;
            string weekStartStr = monday.ToString("yyyy-MM-dd");

            // Nếu sang tuần mới -> Reset toàn bộ điểm danh
            if (user.weeklyLogin.weekStartDate != weekStartStr)
            {
                Debug.Log("📅 New week detected! Resetting weekly login rewards...");
                user.weeklyLogin.weekStartDate = weekStartStr;
                user.weeklyLogin.loginDates.Clear();
                user.weeklyLogin.isRewardClaimed = false;
            }

            // Ghi nhận ngày hôm nay nếu chưa có trong danh sách
            string todayStr = now.ToString("yyyy-MM-dd");
            if (!user.weeklyLogin.loginDates.Contains(todayStr))
            {
                user.weeklyLogin.loginDates.Add(todayStr);
                Debug.Log($"📝 Đã ghi nhận điểm danh ngày {user.weeklyLogin.loginDates.Count}/5 trong tuần này.");
            }

            // Nếu đủ 5 ngày và chưa nhận quà -> Thưởng nóng
            if (user.weeklyLogin.loginDates.Count >= 5 && !user.weeklyLogin.isRewardClaimed)
            {
                user.weeklyLogin.isRewardClaimed = true;
                user.coins += 1000; // Thưởng lớn cho việc chuyên cần
                user.exp += 2000;
                Debug.Log("🎁 SIÊU CẤP CHUYÊN CẦN: Bạn đã nhận được 1000 Coins và 2000 EXP cho việc đăng nhập 5 ngày trong tuần!");
            }
        }

        // --- BINDING CHO HOME SCREEN ---
        private void BindHomeEvents()
        {
            // -- Đổ dữ liệu Username và Vàng từ JSON --
            if (_jsonDb != null && _jsonDb.currentUser != null)
            {
                Label lblNameFind = _root.Q<Label>("LblUserName");
                if (lblNameFind != null) lblNameFind.text = _jsonDb.currentUser.username;

                Label lblLevelFind = _root.Q<Label>("LblUserLevel");
                if (lblLevelFind != null) lblLevelFind.text = $"★ Level {_jsonDb.currentUser.level}";

                Label lblCoinsFind = _root.Q<Label>("LblUserCoins");
                if (lblCoinsFind != null) lblCoinsFind.text = _jsonDb.currentUser.coins.ToString();

                if (_jsonDb.inventory != null)
                {
                    var equippedAvatar = _jsonDb.inventory.Find(i => i.equipType == "Avatar" && i.isEquipped);
                    var equippedBorder = _jsonDb.inventory.Find(i => i.equipType == "Border" && i.isEquipped);
                    var equippedEffect = _jsonDb.inventory.Find(i => i.equipType == "Effect" && i.isEquipped);

                    Label avatarIcon = _root.Q<Label>("HomeAvatarIcon");
                    if (avatarIcon != null && equippedAvatar != null) avatarIcon.text = equippedAvatar.icon;

                    Label effectIcon = _root.Q<Label>("HomeAvatarEffect");
                    if (effectIcon != null)
                    {
                        effectIcon.text = equippedEffect != null ? equippedEffect.icon : "";
                        if (equippedEffect != null) AnimateAura(effectIcon);
                    }

                    VisualElement borderContainer = _root.Q<VisualElement>("HomeBorderContainer");
                    if (borderContainer != null && equippedBorder != null)
                    {
                        Color borderColor = Color.white;
                        if (equippedBorder.rarity == "Common") borderColor = new Color(0.23f, 0.51f, 0.96f);
                        else if (equippedBorder.rarity == "Rare") borderColor = new Color(0.96f, 0.62f, 0.04f);
                        else if (equippedBorder.rarity == "Epic") borderColor = new Color(0.55f, 0.36f, 0.96f);
                        else if (equippedBorder.rarity == "Legendary") borderColor = new Color(0.93f, 0.26f, 0.26f);

                        borderContainer.style.borderTopColor = new StyleColor(borderColor);
                        borderContainer.style.borderBottomColor = new StyleColor(borderColor);
                        borderContainer.style.borderLeftColor = new StyleColor(borderColor);
                        borderContainer.style.borderRightColor = new StyleColor(borderColor);
                    }
                }
            }

            // -- Các nút Bottom Nav --
            Button navHome = _root.Q<Button>("NavHome");
            if (navHome != null) navHome.clicked += () => LoadScreen(HomeScreenAsset);

            Button navQuest = _root.Q<Button>("NavQuest");
            if (navQuest != null) navQuest.clicked += () => LoadScreen(QuestScreenAsset);

            Button navShop = _root.Q<Button>("NavShop");
            if (navShop != null) navShop.clicked += () => LoadScreen(ShopScreenAsset);

            Button navRank = _root.Q<Button>("NavRank");
            if (navRank != null) navRank.clicked += () => LoadScreen(RankingScreenAsset);

            Button navProfile = _root.Q<Button>("NavProfile");
            if (navProfile != null) navProfile.clicked += () => LoadScreen(ProfileScreenAsset);

            // -- Các nút tính năng chính (LearnedSets, Battle, Friends) --
            Button actionLearnedSets = _root.Q<Button>("ActionLearnedSets");
            if (actionLearnedSets != null) actionLearnedSets.clicked += () => ShowLearnedSetsOverlay();

            Button btnCloseLearnedSets = _root.Q<Button>("BtnCloseLearnedSets");
            if (btnCloseLearnedSets != null) btnCloseLearnedSets.clicked += () =>
            {
                var overlay = _root.Q<VisualElement>("LearnedSetsOverlay");
                if (overlay != null) overlay.style.display = DisplayStyle.None;
            };

            Button actionBattle = _root.Q<Button>("ActionBattle");
            if (actionBattle != null) actionBattle.clicked += () => LoadScreen(BattleScreenAsset);

            Button actionFriends = _root.Q<Button>("ActionFriends");
            if (actionFriends != null) actionFriends.clicked += () => LoadScreen(FriendScreenAsset);

            Button actionSoloQuiz = _root.Q<Button>("ActionSoloQuiz");
            if (actionSoloQuiz != null) actionSoloQuiz.clicked += () => LoadScreen(SoloQuizScreenAsset);

            // -- Đổ dữ liệu Featured và Danh Sách Vocab Sets --
            if (_jsonDb != null && _jsonDb.vocabSets != null && _jsonDb.vocabSets.Count > 0)
            {
                // Featured Set (Lấy bộ đầu tiên)
                var featuredSet = _jsonDb.vocabSets[0];
                Label lblFeaturedTitle = _root.Q<Label>("LblFeaturedTitle");
                if (lblFeaturedTitle != null) lblFeaturedTitle.text = featuredSet.title;

                Label lblFeaturedCount = _root.Q<Label>("LblFeaturedCount");
                if (lblFeaturedCount != null) lblFeaturedCount.text = $"{featuredSet.wordCount} words";

                Button startFeatured = _root.Q<Button>("BtnStartFeatured");
                if (startFeatured != null) startFeatured.clicked += () =>
                {
                    _currentVocabSet = featuredSet;
                    LoadScreen(VocabDetailScreenAsset);
                };

                VisualElement featuredCard = _root.Q<VisualElement>("VocabSetFeaturedCard");
                if (featuredCard != null) featuredCard.RegisterCallback<ClickEvent>(evt =>
                {
                    // Tránh trigger 2 lần nếu click trúng nút Start Learning nằm bên trong
                    if (evt.target == startFeatured) return;
                    _currentVocabSet = featuredSet;
                    LoadScreen(VocabDetailScreenAsset);
                });

                // Đổ Data All Vocab Sets
                VisualElement listContainer = _root.Q<VisualElement>("VocabListContainer");
                if (listContainer != null)
                {
                    listContainer.Clear();
                    foreach (var set in _jsonDb.vocabSets)
                    {
                        VisualElement btnItem = new VisualElement();
                        btnItem.AddToClassList("card");
                        btnItem.style.marginRight = 24;
                        btnItem.style.marginLeft = 24;
                        btnItem.style.marginBottom = 12;
                        btnItem.style.flexDirection = FlexDirection.Row;
                        btnItem.style.paddingTop = 16;
                        btnItem.style.paddingBottom = 16;
                        btnItem.style.paddingLeft = 16;
                        btnItem.style.paddingRight = 16;
                        btnItem.style.alignItems = Align.FlexStart;
                        btnItem.style.justifyContent = Justify.FlexStart;
                        btnItem.style.borderBottomWidth = 1;
                        btnItem.style.borderTopWidth = 1;
                        btnItem.style.borderLeftWidth = 1;
                        btnItem.style.borderRightWidth = 1;

                        // Hành động khi nhấn vào list item (Thẻ)
                        var capturedSet = set; // Prevent closure issue
                        btnItem.RegisterCallback<ClickEvent>(evt =>
                        {
                            _currentVocabSet = capturedSet;
                            LoadScreen(VocabDetailScreenAsset);
                        });

                        // Icon màu sắc
                        VisualElement iconBox = new VisualElement();
                        iconBox.style.width = 64;
                        iconBox.style.height = 64;
                        iconBox.style.backgroundColor = new StyleColor(new Color(0.55f, 0.36f, 0.96f)); // #8B5CF6 random
                        iconBox.style.borderTopLeftRadius = 12;
                        iconBox.style.borderTopRightRadius = 12;
                        iconBox.style.borderBottomLeftRadius = 12;
                        iconBox.style.borderBottomRightRadius = 12;
                        iconBox.style.marginRight = 16;
                        iconBox.style.flexShrink = 0;
                        btnItem.Add(iconBox);

                        VisualElement textGroup = new VisualElement();
                        textGroup.style.flexGrow = 1;
                        textGroup.style.alignItems = Align.FlexStart;

                        Label titleLbl = new Label(set.title);
                        titleLbl.AddToClassList("font-bold");
                        titleLbl.AddToClassList("text-lg");
                        titleLbl.style.color = Color.white;
                        titleLbl.style.marginBottom = 4;
                        textGroup.Add(titleLbl);

                        Label descLbl = new Label(set.description);
                        descLbl.AddToClassList("text-muted");
                        descLbl.style.marginBottom = 8;
                        descLbl.style.whiteSpace = WhiteSpace.Normal;
                        textGroup.Add(descLbl);

                        VisualElement tagsGroup = new VisualElement();
                        tagsGroup.style.flexDirection = FlexDirection.Row;

                        // [MODIFIED] Logic hiển thị Label Cấp độ và Số từ phù hợp với hệ thống mới
                        string displayDiff = set.difficulty;
                        string displayCountText = $"{set.wordCount} words";

                        if (set.levels != null && set.levels.Count > 0)
                        {
                            displayDiff = "Multi-Level";
                            displayCountText = $"{set.levels.Count} Levels";

                            // Nếu có level đã chọn trước đó, hiển thị nó
                            if (_jsonDb.currentUser != null && _jsonDb.currentUser.savedSetLevels != null)
                            {
                                var saved = _jsonDb.currentUser.savedSetLevels.Find(s => s.setId == set.id);
                                if (saved != null)
                                {
                                    displayDiff = saved.level;
                                    // Cập nhật số từ của level đó luôn
                                    var levelData = set.levels.Find(l => l.difficulty == saved.level);
                                    if (levelData != null) displayCountText = $"{levelData.wordIds.Count} words";
                                }
                            }
                        }

                        Label countLbl = new Label(displayCountText);
                        countLbl.style.backgroundColor = new StyleColor(new Color(0.23f, 0.51f, 0.96f, 0.2f)); // rgba(59, 130, 246, 0.2)
                        countLbl.style.color = new StyleColor(new Color(0.23f, 0.51f, 0.96f, 1f));
                        countLbl.style.paddingTop = 4;
                        countLbl.style.paddingBottom = 4;
                        countLbl.style.paddingLeft = 8;
                        countLbl.style.paddingRight = 8;
                        countLbl.style.borderTopLeftRadius = 8;
                        countLbl.style.borderTopRightRadius = 8;
                        countLbl.style.borderBottomLeftRadius = 8;
                        countLbl.style.borderBottomRightRadius = 8;
                        countLbl.style.fontSize = 12;
                        countLbl.style.marginRight = 8;
                        tagsGroup.Add(countLbl);

                        Label diffLbl = new Label(displayDiff);
                        diffLbl.style.backgroundColor = new StyleColor(new Color(0.55f, 0.36f, 0.96f, 0.2f));
                        diffLbl.style.color = new StyleColor(new Color(0.55f, 0.36f, 0.96f, 1f));
                        diffLbl.style.paddingTop = 4;
                        diffLbl.style.paddingBottom = 4;
                        diffLbl.style.paddingLeft = 8;
                        diffLbl.style.paddingRight = 8;
                        diffLbl.style.borderTopLeftRadius = 8;
                        diffLbl.style.borderTopRightRadius = 8;
                        diffLbl.style.borderBottomLeftRadius = 8;
                        diffLbl.style.borderBottomRightRadius = 8;
                        diffLbl.style.fontSize = 12;
                        diffLbl.style.marginRight = 8; // Thêm margin right để đẹp hơn
                        tagsGroup.Add(diffLbl);

                        // [MODIFIED] Tính toán tiến độ dựa trên số từ "Đã nhớ" (Mastered)
                        if (_jsonDb.currentUser != null)
                        {
                            var user = _jsonDb.currentUser;
                            int masteredCount = 0;
                            int totalWords = (set.wordIds != null) ? set.wordIds.Count : 0;

                            // Đếm số từ trong bộ này đã đạt status = 2 (Mastered)
                            if (user.wordProgress != null && set.wordIds != null)
                            {
                                foreach (var wordId in set.wordIds)
                                {
                                    var p = user.wordProgress.Find(x => x.wordId == wordId);
                                    if (p != null && p.status == 2) masteredCount++;
                                }
                            }

                            if (masteredCount > 0)
                            {
                                bool isFullyMastered = (totalWords > 0 && masteredCount >= totalWords);
                                string progressText = isFullyMastered ? "✔ Hoàn thành" : $"✔ {masteredCount}/{totalWords} từ";
                                Color badgeColor = isFullyMastered ? new Color(0.06f, 0.73f, 0.51f) : new Color(0.96f, 0.62f, 0.04f); // Emerald or Gold

                                Label progressBadge = new Label(progressText);
                                progressBadge.style.backgroundColor = new StyleColor(new Color(badgeColor.r, badgeColor.g, badgeColor.b, 0.2f));
                                progressBadge.style.color = new StyleColor(badgeColor);
                                progressBadge.style.paddingTop = 4;
                                progressBadge.style.paddingBottom = 4;
                                progressBadge.style.paddingLeft = 8;
                                progressBadge.style.paddingRight = 8;
                                progressBadge.style.borderTopLeftRadius = 8;
                                progressBadge.style.borderTopRightRadius = 8;
                                progressBadge.style.borderBottomLeftRadius = 8;
                                progressBadge.style.borderBottomRightRadius = 8;
                                progressBadge.style.fontSize = 12;
                                tagsGroup.Add(progressBadge);
                            }
                            else if (totalWords > 0)
                            {
                                // Show Pending if zero mastered words
                                Label pendingBadge = new Label("Chưa học");
                                pendingBadge.style.backgroundColor = new StyleColor(new Color(0.58f, 0.64f, 0.72f, 0.2f)); // Slate-400 tint
                                pendingBadge.style.color = new StyleColor(new Color(0.58f, 0.64f, 0.72f, 1f));
                                pendingBadge.style.paddingTop = 4;
                                pendingBadge.style.paddingBottom = 4;
                                pendingBadge.style.paddingLeft = 8;
                                pendingBadge.style.paddingRight = 8;
                                pendingBadge.style.borderTopLeftRadius = 8;
                                pendingBadge.style.borderTopRightRadius = 8;
                                pendingBadge.style.borderBottomLeftRadius = 8;
                                pendingBadge.style.borderBottomRightRadius = 8;
                                pendingBadge.style.fontSize = 12;
                                tagsGroup.Add(pendingBadge);
                            }
                        }

                        textGroup.Add(tagsGroup);
                        btnItem.Add(textGroup);

                        listContainer.Add(btnItem);
                    }
                }
            }

            // -- Thanh Tìm kiếm --
            TextField searchBox = _root.Q<TextField>(className: "search-input");
            if (searchBox != null)
            {
                SetupPlaceholder(searchBox, "Search vocabulary sets...");

                searchBox.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue != "Search vocabulary sets..." && !string.IsNullOrEmpty(evt.newValue))
                    {
                        Debug.Log($"HomeScreen: Đang tìm kiếm -> {evt.newValue}");
                    }
                });
            }
        }

        private VocabLearning.Data.VocabSetJson FindVocabSetById(string id)
        {
            if (_jsonDb == null || _jsonDb.vocabSets == null) return null;
            return _jsonDb.vocabSets.Find(v => v.id == id);
        }

        private VocabLearning.Data.WordJson FindWordById(string wordId)
        {
            if (_jsonDb == null || _jsonDb.words == null) return null;
            return _jsonDb.words.Find(w => w.id == wordId);
        }

        private void ResolveSetWords(VocabLearning.Data.VocabSetJson set)
        {
            _currentVocabSetWords.Clear();
            if (set == null) return;

            List<string> wordIdsToUse = set.wordIds;

            // [NEW] If set has levels, filter by selected level
            if (set.levels != null && set.levels.Count > 0)
            {
                var levelData = set.levels.Find(l => l.difficulty == _currentSelectedLevel);
                if (levelData != null)
                {
                    wordIdsToUse = levelData.wordIds;
                }
            }

            if (wordIdsToUse == null) return;

            foreach (var id in wordIdsToUse)
            {
                var word = FindWordById(id);
                if (word != null) _currentVocabSetWords.Add(word);
            }
        }

        private void SelectLevel(string level)
        {
            _currentSelectedLevel = level;
            _sessionNewlyMasteredWords.Clear();

            // Save to database
            if (_jsonDb.currentUser != null)
            {
                if (_jsonDb.currentUser.savedSetLevels == null)
                    _jsonDb.currentUser.savedSetLevels = new List<VocabLearning.Data.UserSetLevelJson>();

                var saved = _jsonDb.currentUser.savedSetLevels.Find(s => s.setId == _currentVocabSet.id);
                if (saved != null)
                {
                    saved.level = level;
                }
                else
                {
                    _jsonDb.currentUser.savedSetLevels.Add(new VocabLearning.Data.UserSetLevelJson
                    {
                        setId = _currentVocabSet.id,
                        level = level
                    });
                }
            }

            // Refresh UI
            ResolveSetWords(_currentVocabSet);
            BindDetailEvents(); // Re-bind to refresh list and button states
        }

        private void CreateLevelButtons()
        {
            VisualElement container = _root.Q<VisualElement>("LevelSelectionContainer");
            if (container == null) return;

            container.Clear();

            // Nếu bộ này có danh sách Level cụ thể
            if (_currentVocabSet.levels != null && _currentVocabSet.levels.Count > 0)
            {
                foreach (var levelData in _currentVocabSet.levels)
                {
                    var levelName = levelData.difficulty;

                    // Tính tiến độ cho level này [NEW]
                    int masteredInLevel = 0;
                    int totalInLevel = levelData.wordIds.Count;
                    if (_jsonDb.currentUser != null && _jsonDb.currentUser.wordProgress != null)
                    {
                        foreach (var wId in levelData.wordIds)
                        {
                            var p = _jsonDb.currentUser.wordProgress.Find(x => x.wordId == wId);
                            if (p != null && p.status == 2) masteredInLevel++;
                        }
                    }

                    string btnText = $"{levelName.ToUpper()} ({masteredInLevel}/{totalInLevel})";
                    Button btn = CreateLevelBtn(levelName, btnText);
                    btn.clicked += () => SelectLevel(levelName);
                    container.Add(btn);
                }
            }
            else
            {
                // Nếu không có level, hiện 1 nút duy nhất với độ khó mặc định
                var defaultLevel = _currentVocabSet.difficulty;

                // Tính tiến độ cho level mặc định này [NEW FIX]
                int masteredCount = 0;
                int totalCount = _currentVocabSet.wordIds.Count;
                if (_jsonDb.currentUser != null && _jsonDb.currentUser.wordProgress != null)
                {
                    foreach (var wId in _currentVocabSet.wordIds)
                    {
                        var p = _jsonDb.currentUser.wordProgress.Find(x => x.wordId == wId);
                        if (p != null && p.status == 2) masteredCount++;
                    }
                }

                string btnText = $"{defaultLevel.ToUpper()} ({masteredCount}/{totalCount})";
                Button btn = CreateLevelBtn(defaultLevel, btnText);
                btn.clicked += () => SelectLevel(defaultLevel);
                container.Add(btn);
            }

            UpdateLevelButtonsUI();
        }

        private Button CreateLevelBtn(string levelName, string displayText)
        {
            Button btn = new Button();
            btn.text = displayText;
            btn.name = "BtnLevel_" + levelName;
            btn.AddToClassList("card");
            btn.style.flexGrow = 1;
            btn.style.flexShrink = 1;
            btn.style.minWidth = 80;
            btn.style.marginRight = 6;
            btn.style.marginBottom = 6;
            btn.style.paddingTop = 10;
            btn.style.paddingBottom = 10;
            btn.style.paddingLeft = 4;
            btn.style.paddingRight = 4;
            btn.style.fontSize = 11;
            btn.style.color = Color.white;
            btn.style.borderTopLeftRadius = 10;
            btn.style.borderTopRightRadius = 10;
            btn.style.borderBottomLeftRadius = 10;
            btn.style.borderBottomRightRadius = 10;
            btn.style.borderTopWidth = 0;
            btn.style.borderBottomWidth = 0;
            btn.style.borderLeftWidth = 0;
            btn.style.borderRightWidth = 0;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            return btn;
        }

        private void UpdateLevelButtonsUI()
        {
            VisualElement container = _root.Q<VisualElement>("LevelSelectionContainer");
            if (container == null) return;

            foreach (var child in container.Children())
            {
                if (child is Button btn)
                {
                    string levelName = btn.name.Replace("BtnLevel_", "");
                    bool isSelected = (levelName == _currentSelectedLevel);

                    Color activeColor = new Color(0.23f, 0.51f, 0.96f); // Default Blue
                    if (levelName == "Easy") activeColor = new Color(0.10f, 0.73f, 0.51f); // Emerald
                    else if (levelName == "Medium") activeColor = new Color(0.23f, 0.51f, 0.96f); // Blue
                    else if (levelName == "Hard") activeColor = new Color(0.93f, 0.26f, 0.26f); // Red

                    SetLevelButtonStyle(btn, isSelected, activeColor);
                }
            }
        }

        private void SetLevelButtonStyle(Button btn, bool isSelected, Color activeColor)
        {
            if (isSelected)
            {
                btn.style.backgroundColor = new StyleColor(activeColor);
                btn.style.borderBottomWidth = 4;
                btn.style.borderBottomColor = new StyleColor(new Color(0, 0, 0, 0.3f));
            }
            else
            {
                btn.style.backgroundColor = new StyleColor(new Color(0.2f, 0.25f, 0.33f)); // Slate-700
                btn.style.borderBottomWidth = 0;
            }
        }

        private void ShowLearnedSetsOverlay()
        {
            var overlay = _root.Q<VisualElement>("LearnedSetsOverlay");
            if (overlay == null) return;
            overlay.style.display = DisplayStyle.Flex;

            var container = _root.Q<VisualElement>("LearnedSetsListContainer");
            if (container == null) return;
            container.Clear();

            var user = _jsonDb?.currentUser;
            if (user == null || user.learnedSets == null || user.learnedSets.Count == 0)
            {
                Label emptyLbl = new Label("Bạn chưa hoàn thành bộ từ vựng nào cả!");
                emptyLbl.style.color = Color.white;
                emptyLbl.style.marginTop = 20;
                emptyLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
                container.Add(emptyLbl);
                return;
            }

            // Đảo ngược list để hiện bộ mới học lên trên
            List<string> reversedLearned = new List<string>(user.learnedSets);
            reversedLearned.Reverse();

            foreach (string setId in reversedLearned)
            {
                var vocabSet = _jsonDb.vocabSets.Find(s => s.id == setId);
                if (vocabSet == null) continue;

                VisualElement card = new VisualElement();
                card.AddToClassList("card");
                card.style.flexDirection = FlexDirection.Row;
                card.style.marginBottom = 16;
                card.style.paddingLeft = 16;
                card.style.paddingRight = 16;
                card.style.paddingTop = 16;
                card.style.paddingBottom = 16;

                VisualElement iconBox = new VisualElement();
                iconBox.style.width = 60;
                iconBox.style.height = 60;
                iconBox.style.backgroundColor = new StyleColor(new Color(0.06f, 0.72f, 0.5f));
                iconBox.style.borderTopLeftRadius = 12;
                iconBox.style.borderTopRightRadius = 12;
                iconBox.style.borderBottomLeftRadius = 12;
                iconBox.style.borderBottomRightRadius = 12;
                iconBox.style.marginRight = 16;
                iconBox.style.justifyContent = Justify.Center;
                iconBox.style.alignItems = Align.Center;

                Label iconLbl = new Label("✔");
                iconLbl.style.color = Color.white;
                iconLbl.style.fontSize = 24;
                iconLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                iconBox.Add(iconLbl);
                card.Add(iconBox);

                VisualElement infoBox = new VisualElement();
                infoBox.style.flexGrow = 1;
                infoBox.style.justifyContent = Justify.Center;

                Label titleLbl = new Label(vocabSet.title);
                titleLbl.style.color = Color.white;
                titleLbl.style.fontSize = 18;
                titleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                titleLbl.style.marginBottom = 4;
                infoBox.Add(titleLbl);

                Label countLbl = new Label($"{vocabSet.wordCount} words - Đã hoàn thành");
                countLbl.style.color = new StyleColor(new Color(0.6f, 0.64f, 0.71f));
                infoBox.Add(countLbl);

                card.Add(infoBox);

                Button btnReplay = new Button(() =>
                {
                    _currentVocabSet = vocabSet;
                    LoadScreen(VocabDetailScreenAsset);
                });
                btnReplay.text = "Review";
                btnReplay.AddToClassList("btn-secondary");
                btnReplay.style.paddingLeft = 12;
                btnReplay.style.paddingRight = 12;
                btnReplay.style.alignSelf = Align.Center;
                card.Add(btnReplay);

                container.Add(card);
            }
        }

        // --- BINDING CHO DETAIL SCREEN ---
        private void BindDetailEvents()
        {
            if (_currentVocabSet != null)
            {
                // [NEW] Load saved level for this set
                if (_jsonDb.currentUser != null && _jsonDb.currentUser.savedSetLevels != null)
                {
                    var saved = _jsonDb.currentUser.savedSetLevels.Find(s => s.setId == _currentVocabSet.id);
                    if (saved != null)
                    {
                        _currentSelectedLevel = saved.level;
                    }
                    else
                    {
                        // Default logic: Lấy từ levels[0] nếu có, nếu không lấy từ difficulty gốc
                        if (_currentVocabSet.levels != null && _currentVocabSet.levels.Count > 0)
                            _currentSelectedLevel = _currentVocabSet.levels[0].difficulty;
                        else
                            _currentSelectedLevel = _currentVocabSet.difficulty;
                    }
                }

                ResolveSetWords(_currentVocabSet);
                CreateLevelButtons();

                // Binding Dữ Liệu Động (Tiêu đề, Số từ, Level)
                Label lblTitle = _root.Q<Label>("LblVocabTitle");
                if (lblTitle != null) lblTitle.text = _currentVocabSet.title;

                Label lblDesc = _root.Q<Label>("LblVocabDesc");
                if (lblDesc != null) lblDesc.text = _currentVocabSet.description;

                Label lblCount = _root.Q<Label>("LblVocabCount");
                if (lblCount != null) lblCount.text = _currentVocabSetWords.Count.ToString();

                Label lblLevel = _root.Q<Label>("LblVocabLevel");
                if (lblLevel != null) lblLevel.text = _currentSelectedLevel;

                // Binding danh sách từ vựng động
                VisualElement wordsContainer = _root.Q<VisualElement>("WordsContainer");
                if (wordsContainer != null && _currentVocabSetWords != null)
                {
                    wordsContainer.Clear(); // Xóa sạch dữ liệu cũ

                    int count = 1;
                    foreach (var wordData in _currentVocabSetWords)
                    {
                        // Khởi tạo Card chứa từ
                        VisualElement wordCard = new VisualElement();
                        wordCard.AddToClassList("card");
                        wordCard.AddToClassList("word-item");

                        // Thứ tự (1., 2., 3.)
                        Label indexLbl = new Label($"{count}.");
                        indexLbl.AddToClassList("text-muted");
                        indexLbl.AddToClassList("font-bold");
                        indexLbl.style.width = 30;
                        indexLbl.style.fontSize = 18;
                        wordCard.Add(indexLbl);

                        // Cụm chứa từ vựng và nghĩa
                        VisualElement textContainer = new VisualElement();

                        Label enLbl = new Label(wordData.word);
                        enLbl.AddToClassList("font-bold");
                        enLbl.style.color = Color.white;
                        enLbl.style.fontSize = 18;
                        textContainer.Add(enLbl);

                        Label vnLbl = new Label(wordData.meaning);
                        vnLbl.AddToClassList("text-muted");
                        textContainer.Add(vnLbl);

                        // [NEW] Badge trạng thái từ vựng
                        int status = GetWordStatus(wordData.id);
                        if (status > 0)
                        {
                            Label statusBadge = new Label(status == 2 ? "★ Đã học" : "Đang học");
                            statusBadge.style.fontSize = 10;
                            statusBadge.style.marginLeft = 8;
                            statusBadge.style.paddingLeft = 4;
                            statusBadge.style.paddingRight = 4;
                            statusBadge.style.borderTopLeftRadius = 4;
                            statusBadge.style.borderTopRightRadius = 4;
                            statusBadge.style.borderBottomLeftRadius = 4;
                            statusBadge.style.borderBottomRightRadius = 4;
                            statusBadge.style.backgroundColor = status == 2 ? new Color(0.96f, 0.62f, 0.04f, 0.2f) : new Color(0.23f, 0.51f, 0.96f, 0.2f);
                            statusBadge.style.color = status == 2 ? new Color(0.96f, 0.62f, 0.04f) : new Color(0.23f, 0.51f, 0.96f);

                            // Thêm vào hàng chứa text (ngang với English word hoặc ngay dưới)
                            enLbl.parent.Insert(1, statusBadge);
                            statusBadge.style.alignSelf = Align.FlexStart;
                        }

                        wordCard.Add(textContainer);
                        wordsContainer.Add(wordCard);
                        count++;
                    }
                }
            }

            // Nút Practice
            Button practiceBtn = _root.Q<Button>(className: "mode-btn-practice");
            if (practiceBtn != null)
            {
                practiceBtn.clicked += () =>
                {
                    if (_currentVocabSet != null && _currentVocabSetWords != null && _currentVocabSetWords.Count > 0)
                    {
                        _practiceCurrentIndex = 0;
                        _practiceShowMeaning = false;
                        _sessionNewlyMasteredWords.Clear(); // [FIX] Reset danh sách mỗi khi vào Practice mới
                        LoadScreen(PracticeModeScreenAsset);
                    }
                    else
                    {
                        Debug.LogWarning("Không thể bắt đầu Practice vì bộ từ vựng rỗng!");
                    }
                };
            }

            // Nút Review Quiz 
            Button quizBtn = _root.Q<Button>(className: "mode-btn-quiz");
            if (quizBtn != null) quizBtn.clicked += () => StartReviewQuiz();

            // Nút Back
            Button backBtn = _root.Q<Button>("BtnBack");
            if (backBtn != null)
            {
                backBtn.clicked += () =>
                {
                    Debug.Log("DetailScreen: Returning to Home...");
                    LoadScreen(HomeScreenAsset);
                };
            }
        }

        // --- BINDING CHO CÁC MÀN HÌNH KHÁC ---
        private void BindOtherScreens()
        {
            // Các nút trở về chung chung (nếu có id là BtnBack)
            Button btnBack = _root.Q<Button>("BtnBack");
            if (btnBack != null) btnBack.clicked += () => LoadScreen(HomeScreenAsset);
        }

        // --- BINDING CHO PRACTICE SCREEN ---
        private void BindPracticeEvents()
        {
            if (_currentVocabSet == null || _currentVocabSetWords == null || _currentVocabSetWords.Count == 0) return;

            // Nút Đóng
            Button btnClose = _root.Q<Button>("BtnPracticeClose");
            if (btnClose != null)
            {
                Debug.Log("BindPracticeEvents: Found BtnPracticeClose");
                btnClose.clicked += () =>
                {
                    int newlyMastered = (_sessionNewlyMasteredWords != null) ? _sessionNewlyMasteredWords.Count : 0;

                    if (newlyMastered > 0)
                    {
                        ShowConfirmationDialog(
                            "Kết thúc bài học?",
                            $"Bạn đã thuộc được {newlyMastered} từ mới. Bạn có muốn lưu lại kết quả và nhận thưởng ngay bây giờ không?",
                            () => { RecordSetProgress(false); LoadScreen(ResultScreenAsset); },
                            null
                        );
                    }
                    else
                    {
                        ShowConfirmationDialog(
                            "Thoát luyện tập?",
                            "Bạn có chắc muốn thoát khỏi bài luyện tập này không?",
                            () => LoadScreen(VocabDetailScreenAsset),
                            null
                        );
                    }
                };
            }
            else
            {
                Debug.LogError("BindPracticeEvents: CANNOT find BtnPracticeClose!");
            }

            VisualElement flashcard = _root.Q<VisualElement>("FlashcardContainer");
            Button btnPrev = _root.Q<Button>("BtnPracticePrev");
            Button btnNext = _root.Q<Button>("BtnPracticeNext");

            // Logic khi bấm vào thẻ để mở đáp án
            if (flashcard != null)
            {
                flashcard.RegisterCallback<ClickEvent>(evt =>
                {
                    _practiceShowMeaning = !_practiceShowMeaning;
                    // Đọc từ tiếng Anh khi lật sang mặt sau
                    if (_practiceShowMeaning && _currentVocabSetWords != null)
                    {
                        var word = _currentVocabSetWords[_practiceCurrentIndex];
                        if (VocabLearning.Helpers.TextToSpeechHelper.Instance != null)
                            VocabLearning.Helpers.TextToSpeechHelper.Instance.Speak(word.word);
                    }
                    UpdatePracticeUI();
                });
            }

            // Nút Previous
            if (btnPrev != null)
            {
                btnPrev.clicked += () =>
                {
                    if (_practiceCurrentIndex > 0)
                    {
                        _practiceCurrentIndex--;
                        _practiceShowMeaning = false;
                        UpdatePracticeUI();
                    }
                };
            }

            // Nút Next
            if (btnNext != null)
            {
                btnNext.clicked += () => HandlePracticeNext();
            }

            // Nút Đã Nhớ (Mastered) [NEW]
            Button btnMastered = _root.Q<Button>("BtnPracticeMastered");
            if (btnMastered != null)
            {
                btnMastered.clicked += () =>
                {
                    MarkWordAsMastered();
                    HandlePracticeNext();
                };
            }

            // Lần đầu mở lên -> Vẽ UI
            UpdatePracticeUI();
        }

        private void MarkWordAsMastered()
        {
            if (_currentVocabSetWords == null || _practiceCurrentIndex >= _currentVocabSetWords.Count) return;
            var word = _currentVocabSetWords[_practiceCurrentIndex];

            if (_jsonDb != null && _jsonDb.currentUser != null)
            {
                var user = _jsonDb.currentUser;
                if (user.wordProgress == null) user.wordProgress = new List<VocabLearning.Data.UserWordProgressJson>();

                var progress = user.wordProgress.Find(p => p.wordId == word.id);

                // [NEW] Chỉ ghi nhận nếu từ này CHƯA từng được Mastered trước đó
                if (progress == null || progress.status != 2)
                {
                    if (!_sessionNewlyMasteredWords.Contains(word.id))
                    {
                        _sessionNewlyMasteredWords.Add(word.id);
                    }
                }

                if (progress == null)
                {
                    progress = new VocabLearning.Data.UserWordProgressJson { wordId = word.id, status = 2 };
                    user.wordProgress.Add(progress);
                }
                else
                {
                    progress.status = 2;
                }
            }
        }

        private void HandlePracticeNext()
        {
            // Tăng tiến độ nhiệm vụ "Học 10 từ mới"
            AddQuestProgressByType("LearnWord", 1);

            // Đánh dấu từ hiện tại là "Đã học" (status = 1) nếu chưa có status nào khác
            if (_jsonDb.currentUser != null)
            {
                var word = _currentVocabSetWords[_practiceCurrentIndex];
                var progress = _jsonDb.currentUser.wordProgress.Find(p => p.wordId == word.id);
                if (progress == null)
                {
                    _jsonDb.currentUser.wordProgress.Add(new VocabLearning.Data.UserWordProgressJson { wordId = word.id, status = 1 });
                }
                else if (progress.status == 0)
                {
                    progress.status = 1;
                }
            }

            if (_practiceCurrentIndex < _currentVocabSetWords.Count - 1)
            {
                _practiceCurrentIndex++;
                _practiceShowMeaning = false;
                UpdatePracticeUI();
            }
            else
            {
                // [NEW] Kiểm tra xem đã học hết (Mastered) tất cả các từ trong bài chưa
                int masteredInLevel = 0;
                foreach (var w in _currentVocabSetWords)
                {
                    if (GetWordStatus(w.id) == 2) masteredInLevel++;
                }

                if (masteredInLevel < _currentVocabSetWords.Count)
                {
                    ShowConfirmationDialog(
                        "Chưa thuộc hết bài!",
                        "Bạn vẫn còn một số từ chưa nhấn 'Đã nhớ'. Bạn có chắc muốn kết thúc bài học này ngay bây giờ không?",
                        () => { RecordSetProgress(false); LoadScreen(ResultScreenAsset); },
                        null
                    );
                }
                else
                {
                    // Hoàn thành bộ -> Tăng tiến độ nhiệm vụ "Đạt điểm tuyệt đối"
                    AddQuestProgressByType("PerfectPractice", 1);
                    RecordSetProgress(true);
                    LoadScreen(ResultScreenAsset);
                }
            }
        }

        private void RecordSetProgress(bool isLevelCompleted)
        {
            var user = _jsonDb.currentUser;
            if (user == null) return;

            // [NEW] Tính toán thưởng cuối bài dựa trên số từ mới học được
            _lastSessionCoins = _sessionNewlyMasteredWords.Count * 10; // 10 coin mỗi từ mới
            _lastSessionExp = _sessionNewlyMasteredWords.Count * 20;  // 20 exp mỗi từ mới

            user.coins += _lastSessionCoins;
            user.exp += _lastSessionExp;

            if (user.setProgress == null) user.setProgress = new List<VocabLearning.Data.UserSetProgressJson>();
            var progress = user.setProgress.Find(p => p.setId == _currentVocabSet.id);
            if (progress == null)
            {
                progress = new VocabLearning.Data.UserSetProgressJson { setId = _currentVocabSet.id };
                user.setProgress.Add(progress);
            }

            if (isLevelCompleted && !progress.completedLevels.Contains(_currentSelectedLevel))
            {
                progress.completedLevels.Add(_currentSelectedLevel);
                // Thưởng thêm khi hoàn thành cả level (Bonus)
                user.coins += 20;
                user.exp += 50;
                _lastSessionCoins += 20;
                _lastSessionExp += 50;
            }

            bool allDone = true;
            if (_currentVocabSet.levels != null && _currentVocabSet.levels.Count > 0)
            {
                foreach (var lv in _currentVocabSet.levels)
                {
                    if (!progress.completedLevels.Contains(lv.difficulty)) { allDone = false; break; }
                }
            }
            if (allDone)
            {
                if (user.learnedSets == null) user.learnedSets = new List<string>();
                if (!user.learnedSets.Contains(_currentVocabSet.id)) user.learnedSets.Add(_currentVocabSet.id);
            }
            // Chỉ cần học được ít nhất 1 từ thì cũng đưa vào Learned Sets
            else if (_sessionNewlyMasteredWords.Count > 0)
            {
                if (user.learnedSets == null) user.learnedSets = new List<string>();
                if (!user.learnedSets.Contains(_currentVocabSet.id)) user.learnedSets.Add(_currentVocabSet.id);
            }
        }

        private void UpdatePracticeUI()
        {
            if (_currentVocabSet == null || _currentVocabSetWords == null) return;
            var currentWord = _currentVocabSetWords[_practiceCurrentIndex];
            int totalWords = _currentVocabSetWords.Count;

            // Header Tiến trình
            Label lblCounter = _root.Q<Label>("LblPracticeCounter");
            if (lblCounter != null) lblCounter.text = $"{_practiceCurrentIndex + 1} / {totalWords}";

            VisualElement progressBar = _root.Q<VisualElement>("PracticeProgressBar");
            if (progressBar != null)
            {
                float progressPercent = ((float)(_practiceCurrentIndex + 1) / totalWords) * 100f;
                progressBar.style.width = new Length(progressPercent, LengthUnit.Percent);
            }

            // Nội dung Thẻ (Tiếng Anh)
            Label lblEnglish = _root.Q<Label>("LblEnglishWord");
            if (lblEnglish != null) lblEnglish.text = currentWord.word;

            // [NEW] Hiển thị trạng thái Word Mastery
            Label lblStatus = _root.Q<Label>("LblWordStatus");
            if (lblStatus != null)
            {
                int status = GetWordStatus(currentWord.id);
                switch (status)
                {
                    case 1:
                        lblStatus.text = "Learning";
                        lblStatus.style.color = new Color(0.23f, 0.51f, 0.96f); // Blue
                        break;
                    case 2:
                        lblStatus.text = "Mastered ✔";
                        lblStatus.style.color = new Color(0.10f, 0.73f, 0.51f); // Emerald
                        break;
                    default:
                        lblStatus.text = "New Word";
                        lblStatus.style.color = new Color(0.58f, 0.64f, 0.72f); // Slate
                        break;
                }
            }

            // Hình ảnh minh họa [NEW]
            VisualElement imgBox = _root.Q<VisualElement>("FlashcardImgBox");
            if (imgBox != null)
            {
                if (!string.IsNullOrEmpty(currentWord.imageUrl))
                {
                    imgBox.style.display = DisplayStyle.Flex;
                    imgBox.style.backgroundImage = null; // Clear old image
                    StartCoroutine(DownloadAndSetImage(currentWord.imageUrl, imgBox));
                }
                else
                {
                    imgBox.style.display = DisplayStyle.None;
                }
            }

            // Lật thẻ (Flip Card)
            VisualElement cardFront = _root.Q<VisualElement>("FlashcardFront");
            VisualElement cardBack = _root.Q<VisualElement>("FlashcardBack");

            if (cardFront != null && cardBack != null)
            {
                if (_practiceShowMeaning)
                {
                    cardFront.style.display = DisplayStyle.None;
                    cardBack.style.display = DisplayStyle.Flex;

                    Label lblVietnamese = _root.Q<Label>("LblVietnamese");
                    if (lblVietnamese != null) lblVietnamese.text = currentWord.meaning;
                }
                else
                {
                    cardFront.style.display = DisplayStyle.Flex;
                    cardBack.style.display = DisplayStyle.None;
                }
            }

            // Trạng thái nút Next/Prev
            Button btnPrev = _root.Q<Button>("BtnPracticePrev");
            if (btnPrev != null)
            {
                // Disable / Tương tác
                btnPrev.SetEnabled(_practiceCurrentIndex > 0);
            }

            Button btnNext = _root.Q<Button>("BtnPracticeNext");
            if (btnNext != null)
            {
                if (_practiceCurrentIndex == totalWords - 1)
                {
                    btnNext.text = "Finish";
                    btnNext.style.backgroundColor = new StyleColor(new Color(0.96f, 0.62f, 0.04f, 1f)); // Vàng Gold
                }
                else
                {
                    btnNext.text = "Next";
                    btnNext.style.backgroundColor = new StyleColor(new Color(0.12f, 0.16f, 0.23f)); // Trả về màu secondary tối
                }
            }

            // Nút Got It [NEW]
            Button btnMastered = _root.Q<Button>("BtnPracticeMastered");
            if (btnMastered != null)
            {
                int status = GetWordStatus(currentWord.id);
                if (status == 2)
                {
                    btnMastered.style.display = DisplayStyle.None;
                }
                else
                {
                    btnMastered.style.display = DisplayStyle.Flex;
                }
            }
        }

        private int GetWordStatus(string wordId)
        {
            if (_jsonDb == null || _jsonDb.currentUser == null || _jsonDb.currentUser.wordProgress == null) return 0;
            var p = _jsonDb.currentUser.wordProgress.Find(x => x.wordId == wordId);
            return (p != null) ? p.status : 0;
        }

        // --- BINDING CHO RESULT SCREEN ---
        private void BindResultEvents()
        {
            // [NEW] Hiển thị thưởng đã tính toán
            Label lblCoins = _root.Q<Label>("LblRewardCoins");
            Label lblExp = _root.Q<Label>("LblRewardExp");
            if (lblCoins != null) lblCoins.text = "+" + _lastSessionCoins;
            if (lblExp != null) lblExp.text = "+" + _lastSessionExp;

            Button btnHome = _root.Q<Button>("BtnResultHome");
            if (btnHome != null) btnHome.clicked += () => LoadScreen(HomeScreenAsset);

            Button btnReplay = _root.Q<Button>("BtnResultReplay");
            if (btnReplay != null) btnReplay.clicked += () =>
            {
                _practiceCurrentIndex = 0;
                _practiceShowMeaning = false;
                _sessionNewlyMasteredWords.Clear(); // [FIX] Reset danh sách khi chơi lại
                LoadScreen(PracticeModeScreenAsset);
            };

            Button btnContinue = _root.Q<Button>("BtnResultContinue");
            if (btnContinue != null)
            {
                btnContinue.clicked += () => HandleContinue();
            }

            Button btnBackToSet = _root.Q<Button>("BtnResultBackToSet");
            if (btnBackToSet != null)
            {
                btnBackToSet.clicked += () => LoadScreen(VocabDetailScreenAsset);
            }
        }

        private void HandleContinue()
        {
            if (_currentVocabSet == null) { LoadScreen(HomeScreenAsset); return; }

            // 1. Tìm level tiếp theo trong bộ hiện tại
            if (_currentVocabSet.levels != null && _currentVocabSet.levels.Count > 0)
            {
                int currentIndex = _currentVocabSet.levels.FindIndex(l => l.difficulty == _currentSelectedLevel);
                if (currentIndex >= 0 && currentIndex < _currentVocabSet.levels.Count - 1)
                {
                    // Chuyển sang level tiếp theo
                    _currentSelectedLevel = _currentVocabSet.levels[currentIndex + 1].difficulty;
                    _practiceCurrentIndex = 0;
                    _practiceShowMeaning = false;
                    _sessionNewlyMasteredWords.Clear(); // [FIX] Reset danh sách khi tiếp tục level mới
                    ResolveSetWords(_currentVocabSet);
                    LoadScreen(PracticeModeScreenAsset);
                    return;
                }
            }

            // 2. Nếu đã hết level, tìm bộ từ vựng tiếp theo
            int setIndex = _jsonDb.vocabSets.FindIndex(s => s.id == _currentVocabSet.id);
            if (setIndex >= 0 && setIndex < _jsonDb.vocabSets.Count - 1)
            {
                _currentVocabSet = _jsonDb.vocabSets[setIndex + 1];
                _currentSelectedLevel = "Easy"; // Reset về Easy cho bộ mới
                LoadScreen(VocabDetailScreenAsset);
            }
            else
            {
                // Hết sạch bộ -> Về home
                LoadScreen(HomeScreenAsset);
            }
        }

        private void BindQuestEvents()
        {
            BindBottomNav();

            Button tabActive = _root.Q<Button>("TabActive");
            Button tabCompleted = _root.Q<Button>("TabCompleted");

            if (tabActive != null)
            {
                tabActive.clicked += () =>
                {
                    _questCurrentTab = "Active";
                    tabActive.AddToClassList("tab-btn-active");
                    if (tabCompleted != null) tabCompleted.RemoveFromClassList("tab-btn-active");
                    RenderQuestList();
                };
            }

            if (tabCompleted != null)
            {
                tabCompleted.clicked += () =>
                {
                    _questCurrentTab = "Completed";
                    tabCompleted.AddToClassList("tab-btn-active");
                    if (tabActive != null) tabActive.RemoveFromClassList("tab-btn-active");
                    RenderQuestList();
                };
            }

            // Init state
            if (_questCurrentTab == "Active" && tabActive != null && tabCompleted != null)
            {
                tabActive.AddToClassList("tab-btn-active");
                tabCompleted.RemoveFromClassList("tab-btn-active");
            }
            else if (_questCurrentTab == "Completed" && tabActive != null && tabCompleted != null)
            {
                tabCompleted.AddToClassList("tab-btn-active");
                tabActive.RemoveFromClassList("tab-btn-active");
            }

            RenderQuestList();
        }

        private void RenderQuestList()
        {
            if (_jsonDb == null || _jsonDb.quests == null) return;

            VisualElement listContainer = _root.Q<VisualElement>("QuestListContainer");
            if (listContainer == null) return;

            listContainer.Clear();

            int activeCount = _jsonDb.quests.FindAll(q => !q.isClaimed).Count;
            int completedCount = _jsonDb.quests.FindAll(q => q.isClaimed).Count;

            Button tabActive = _root.Q<Button>("TabActive");
            Button tabCompleted = _root.Q<Button>("TabCompleted");
            if (tabActive != null) tabActive.text = $"Active ({activeCount})";
            if (tabCompleted != null) tabCompleted.text = $"Completed ({completedCount})";

            var filteredQuests = _questCurrentTab == "Active"
                ? _jsonDb.quests.FindAll(q => !q.isClaimed)
                : _jsonDb.quests.FindAll(q => q.isClaimed);

            // --- PHẦN 1: TIẾN ĐỘ TUẦN (Chỉ hiện ở tab Active) ---
            if (_questCurrentTab == "Active" && _jsonDb.currentUser.weeklyLogin != null)
            {
                var weekly = _jsonDb.currentUser.weeklyLogin;

                VisualElement weeklyCard = new VisualElement();
                weeklyCard.AddToClassList("card");
                weeklyCard.style.marginBottom = 24;
                weeklyCard.style.backgroundColor = new StyleColor(new Color(0.12f, 0.16f, 0.23f, 0.8f));
                weeklyCard.style.borderTopWidth = 2;
                weeklyCard.style.borderTopColor = new StyleColor(new Color(0.55f, 0.36f, 0.96f)); // Purple theme for weekly

                Label weeklyHeader = new Label("📅 WEEKLY LOGIN REWARD");
                weeklyHeader.AddToClassList("font-bold");
                weeklyHeader.style.color = new StyleColor(new Color(0.55f, 0.36f, 0.96f));
                weeklyHeader.style.fontSize = 14;
                weeklyHeader.style.marginBottom = 12;
                weeklyCard.Add(weeklyHeader);

                VisualElement dayContainer = new VisualElement();
                dayContainer.style.flexDirection = FlexDirection.Row;
                dayContainer.style.justifyContent = Justify.SpaceBetween;
                dayContainer.style.marginBottom = 16;

                int loginCount = weekly.loginDates.Count;
                for (int d = 1; d <= 5; d++)
                {
                    VisualElement dayCircle = new VisualElement();
                    dayCircle.style.width = 36;
                    dayCircle.style.height = 36;
                    dayCircle.style.borderTopLeftRadius = 18;
                    dayCircle.style.borderTopRightRadius = 18;
                    dayCircle.style.borderBottomLeftRadius = 18;
                    dayCircle.style.borderBottomRightRadius = 18;
                    dayCircle.style.justifyContent = Justify.Center;
                    dayCircle.style.alignItems = Align.Center;

                    if (d <= loginCount)
                    {
                        dayCircle.style.backgroundColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f)); // Green
                        Label check = new Label("✓");
                        check.style.color = Color.white;
                        check.style.unityFontStyleAndWeight = FontStyle.Bold;
                        dayCircle.Add(check);
                    }
                    else
                    {
                        dayCircle.style.backgroundColor = new StyleColor(new Color(0.2f, 0.25f, 0.33f)); // Dark
                        Label num = new Label(d.ToString());
                        num.style.color = new StyleColor(new Color(0.58f, 0.64f, 0.72f));
                        dayCircle.Add(num);
                    }
                    dayContainer.Add(dayCircle);
                }
                weeklyCard.Add(dayContainer);

                VisualElement weeklyInfo = new VisualElement();
                weeklyInfo.style.flexDirection = FlexDirection.Row;
                weeklyInfo.style.justifyContent = Justify.SpaceBetween;
                weeklyInfo.style.alignItems = Align.Center;

                VisualElement rewardInfo = new VisualElement();
                Label rewardTitle = new Label(weekly.isRewardClaimed ? "Weekly Reward Claimed!" : "Reward: 1000 Coins + 2000 EXP");
                rewardTitle.style.color = weekly.isRewardClaimed ? new StyleColor(new Color(0.06f, 0.73f, 0.51f)) : Color.white;
                rewardTitle.style.fontSize = 12;
                rewardInfo.Add(rewardTitle);

                Label progressText = new Label($"{loginCount}/5 Days");
                progressText.AddToClassList("text-muted");
                progressText.style.fontSize = 12;

                weeklyInfo.Add(rewardInfo);
                weeklyInfo.Add(progressText);
                weeklyCard.Add(weeklyInfo);

                listContainer.Add(weeklyCard);

                // --- PHẦN 2: TIÊU ĐỀ QUEST NGÀY ---
                Label dailyHeader = new Label("🌟 DAILY QUESTS");
                dailyHeader.AddToClassList("font-bold");
                dailyHeader.style.color = new StyleColor(new Color(0.58f, 0.64f, 0.72f));
                dailyHeader.style.fontSize = 12;
                dailyHeader.style.marginBottom = 12;
                listContainer.Add(dailyHeader);
            }

            foreach (var quest in filteredQuests)
            {
                VisualElement card = new VisualElement();
                card.AddToClassList("card");
                card.style.marginBottom = 16;
                card.style.borderTopColor = new StyleColor(new Color(0.2f, 0.25f, 0.33f)); // Default dark border
                card.style.borderBottomColor = new StyleColor(new Color(0.2f, 0.25f, 0.33f));
                card.style.borderLeftColor = new StyleColor(new Color(0.2f, 0.25f, 0.33f));
                card.style.borderRightColor = new StyleColor(new Color(0.2f, 0.25f, 0.33f));

                bool isReadyToClaim = quest.currentProgress >= quest.maxProgress && !quest.isClaimed;
                if (quest.isClaimed || isReadyToClaim)
                {
                    card.style.borderTopColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f)); // Green border
                    card.style.borderBottomColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f));
                    card.style.borderLeftColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f));
                    card.style.borderRightColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f));
                }

                VisualElement header = new VisualElement();
                header.style.flexDirection = FlexDirection.Row;
                header.style.justifyContent = Justify.SpaceBetween;
                header.style.alignItems = Align.FlexStart;
                header.style.marginBottom = 12;

                VisualElement textGroup = new VisualElement();
                textGroup.style.flexGrow = 1;
                textGroup.style.marginRight = 12;

                Label titleLbl = new Label(quest.title);
                titleLbl.AddToClassList("font-bold");
                titleLbl.AddToClassList("text-lg");
                titleLbl.style.color = Color.white;
                titleLbl.style.marginBottom = 4;
                textGroup.Add(titleLbl);

                Label descLbl = new Label(quest.description);
                descLbl.AddToClassList("text-muted");
                descLbl.style.whiteSpace = WhiteSpace.Normal;
                textGroup.Add(descLbl);
                header.Add(textGroup);

                if (quest.isClaimed || isReadyToClaim)
                {
                    VisualElement checkIconBox = new VisualElement();
                    checkIconBox.style.width = 32;
                    checkIconBox.style.height = 32;
                    checkIconBox.style.backgroundColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f));
                    checkIconBox.style.borderTopLeftRadius = 16;
                    checkIconBox.style.borderTopRightRadius = 16;
                    checkIconBox.style.borderBottomLeftRadius = 16;
                    checkIconBox.style.borderBottomRightRadius = 16;
                    checkIconBox.style.justifyContent = Justify.Center;
                    checkIconBox.style.alignItems = Align.Center;

                    Label checkLbl = new Label("V");
                    checkLbl.style.color = Color.white;
                    checkLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                    checkIconBox.Add(checkLbl);
                    header.Add(checkIconBox);
                }
                card.Add(header);

                // Progress Bar
                VisualElement progWrapper = new VisualElement();
                progWrapper.style.marginBottom = 12;

                VisualElement progTextRow = new VisualElement();
                progTextRow.style.flexDirection = FlexDirection.Row;
                progTextRow.style.justifyContent = Justify.SpaceBetween;
                progTextRow.style.marginBottom = 4;

                Label progTitle = new Label("Progress");
                progTitle.AddToClassList("text-muted");
                progTitle.style.fontSize = 12;
                progTextRow.Add(progTitle);

                Label progVal = new Label($"{quest.currentProgress} / {quest.maxProgress}");
                progVal.style.color = Color.white;
                progVal.style.unityFontStyleAndWeight = FontStyle.Bold;
                progVal.style.fontSize = 12;
                progTextRow.Add(progVal);
                progWrapper.Add(progTextRow);

                VisualElement progContainer = new VisualElement();
                progContainer.AddToClassList("progress-bar-container");
                progContainer.style.height = 8;
                progContainer.style.borderTopLeftRadius = 4;
                progContainer.style.borderTopRightRadius = 4;
                progContainer.style.borderBottomLeftRadius = 4;
                progContainer.style.borderBottomRightRadius = 4;

                VisualElement progFill = new VisualElement();
                float pct = quest.maxProgress > 0 ? Mathf.Clamp01((float)quest.currentProgress / quest.maxProgress) * 100f : 0;
                progFill.style.width = new Length(pct, LengthUnit.Percent);
                progFill.style.height = new Length(100, LengthUnit.Percent);
                progFill.style.backgroundColor = (quest.isClaimed || isReadyToClaim)
                    ? new StyleColor(new Color(0.06f, 0.73f, 0.51f))
                    : new StyleColor(new Color(0.23f, 0.51f, 0.96f)); // Green or Blue
                progFill.style.borderTopLeftRadius = 4;
                progFill.style.borderTopRightRadius = 4;
                progFill.style.borderBottomLeftRadius = 4;
                progFill.style.borderBottomRightRadius = 4;
                progContainer.Add(progFill);
                progWrapper.Add(progContainer);
                card.Add(progWrapper);

                // Rewards & Claim Button
                VisualElement bottomRow = new VisualElement();
                bottomRow.style.flexDirection = FlexDirection.Row;
                bottomRow.style.justifyContent = Justify.SpaceBetween;
                bottomRow.style.alignItems = Align.Center;
                bottomRow.style.borderTopWidth = 1;
                bottomRow.style.borderTopColor = new StyleColor(new Color(0.2f, 0.25f, 0.33f));
                bottomRow.style.paddingTop = 12;

                VisualElement rewardsRow = new VisualElement();
                rewardsRow.style.flexDirection = FlexDirection.Row;
                rewardsRow.style.alignItems = Align.Center;

                if (quest.rewardCoins > 0)
                {
                    Label coinReward = new Label($"O +{quest.rewardCoins}");
                    coinReward.style.color = new StyleColor(new Color(0.96f, 0.62f, 0.04f)); // #F59E0B
                    coinReward.style.unityFontStyleAndWeight = FontStyle.Bold;
                    coinReward.style.marginRight = 12;
                    rewardsRow.Add(coinReward);
                }

                if (quest.rewardExp > 0)
                {
                    Label expReward = new Label($"⚡ +{quest.rewardExp}");
                    expReward.style.color = new StyleColor(new Color(0.23f, 0.51f, 0.96f)); // #3B82F6
                    expReward.style.unityFontStyleAndWeight = FontStyle.Bold;
                    rewardsRow.Add(expReward);
                }

                bottomRow.Add(rewardsRow);

                if (isReadyToClaim)
                {
                    Button claimBtn = new Button();
                    claimBtn.text = "Claim";
                    claimBtn.AddToClassList("btn-primary");
                    claimBtn.style.backgroundColor = new StyleColor(new Color(0.96f, 0.62f, 0.04f));
                    claimBtn.style.paddingTop = 6;
                    claimBtn.style.paddingBottom = 6;
                    claimBtn.style.paddingLeft = 16;
                    claimBtn.style.paddingRight = 16;
                    claimBtn.style.marginTop = 0;
                    claimBtn.style.marginBottom = 0;
                    claimBtn.style.marginLeft = 0;
                    claimBtn.style.marginRight = 0;
                    claimBtn.style.minHeight = 30;

                    claimBtn.clicked += () =>
                    {
                        ClaimQuest(quest);
                    };

                    bottomRow.Add(claimBtn);
                }

                card.Add(bottomRow);
                listContainer.Add(card);
            }
        }

        private void ClaimQuest(VocabLearning.Data.QuestJson quest)
        {
            if (quest.isClaimed || quest.currentProgress < quest.maxProgress) return;

            // Update user balance
            if (_jsonDb != null && _jsonDb.currentUser != null)
            {
                _jsonDb.currentUser.coins += quest.rewardCoins;
                _jsonDb.currentUser.exp += quest.rewardExp;
            }

            quest.isClaimed = true;
            RenderQuestList();
        }

        private void BindBattleEvents()
        {
            Button backBtn = _root.Q<Button>("BtnBack");
            if (backBtn != null) backBtn.clicked += () => LoadScreen(HomeScreenAsset);

            if (_jsonDb != null && _jsonDb.currentUser != null)
            {
                var curUser = _jsonDb.currentUser;
                var tier = GetRankTier(curUser.rankPoints);

                Label lblName = _root.Q<Label>("BattleName");
                if (lblName != null) lblName.text = curUser.username;

                Label lblRank = _root.Q<Label>("BattleRank");
                if (lblRank != null)
                {
                    lblRank.text = $"{tier.icon} {tier.name}";
                    lblRank.style.color = new StyleColor(tier.color);
                }

                Label lblPoints = _root.Q<Label>("BattlePoints");
                if (lblPoints != null) lblPoints.text = curUser.rankPoints.ToString();

                Label lblWins = _root.Q<Label>("BattleWins");
                if (lblWins != null) lblWins.text = curUser.wins.ToString();

                Label lblGames = _root.Q<Label>("BattleGames");
                if (lblGames != null) lblGames.text = curUser.totalGames.ToString();

                Label lblWinRate = _root.Q<Label>("BattleWinRate");
                if (lblWinRate != null)
                {
                    float winrate = curUser.totalGames > 0 ? (float)curUser.wins / curUser.totalGames * 100f : 0f;
                    lblWinRate.text = $"{Mathf.RoundToInt(winrate)}%";
                }

                Label lblAvatar = _root.Q<Label>("BattleAvatar");
                if (lblAvatar != null) lblAvatar.text = GetUserAvatar(curUser.id);
            }

            Button btnRanked = _root.Q<Button>("BtnRankedMode");
            if (btnRanked != null) btnRanked.clicked += () => StartBattle(true);

            Button btnCasual = _root.Q<Button>("BtnCasualMode");
            if (btnCasual != null) btnCasual.clicked += () => StartBattle(false);

            // -- Lịch sử trận đấu --
            Button btnViewHistory = _root.Q<Button>("BtnViewHistory");
            if (btnViewHistory != null) btnViewHistory.clicked += () => ShowBattleHistoryOverlay();

            Button btnCloseHistory = _root.Q<Button>("BtnCloseHistory");
            if (btnCloseHistory != null) btnCloseHistory.clicked += () =>
            {
                var overlay = _root.Q<VisualElement>("BattleHistoryOverlay");
                if (overlay != null) overlay.style.display = DisplayStyle.None;
            };

            Button btnCloseDetail = _root.Q<Button>("BtnCloseDetail");
            if (btnCloseDetail != null) btnCloseDetail.clicked += () =>
            {
                var detail = _root.Q<VisualElement>("HistoryDetailOverlay");
                if (detail != null) detail.style.display = DisplayStyle.None;
            };
        }

        private void ShowBattleHistoryOverlay()
        {
            var overlay = _root.Q<VisualElement>("BattleHistoryOverlay");
            if (overlay == null) return;
            overlay.style.display = DisplayStyle.Flex;

            var container = _root.Q<VisualElement>("HistoryListContainer");
            if (container == null) return;
            container.Clear();

            var history = _jsonDb?.currentUser?.battleHistory;
            if (history == null || history.Count == 0)
            {
                Label empty = new Label("Chưa có trận đấu nào trong lịch sử.");
                empty.style.color = new StyleColor(new Color(0.6f, 0.64f, 0.71f));
                empty.style.marginTop = 24;
                empty.style.unityTextAlign = TextAnchor.MiddleCenter;
                container.Add(empty);
                return;
            }

            foreach (var record in history)
            {
                VisualElement card = new VisualElement();
                card.AddToClassList("card");
                card.style.flexDirection = FlexDirection.Row;
                card.style.alignItems = Align.Center;
                card.style.marginBottom = 12;
                card.style.paddingLeft = 16;
                card.style.paddingRight = 16;
                card.style.paddingTop = 12;
                card.style.paddingBottom = 12;

                // Badge WIN / LOSE
                VisualElement badge = new VisualElement();
                badge.style.width = 52;
                badge.style.height = 52;
                badge.style.borderTopLeftRadius = 12;
                badge.style.borderTopRightRadius = 12;
                badge.style.borderBottomLeftRadius = 12;
                badge.style.borderBottomRightRadius = 12;
                badge.style.justifyContent = Justify.Center;
                badge.style.alignItems = Align.Center;
                badge.style.marginRight = 16;
                badge.style.backgroundColor = record.isWin
                    ? new StyleColor(new Color(0.06f, 0.44f, 0.31f))
                    : new StyleColor(new Color(0.56f, 0.16f, 0.16f));

                Label badgeLbl = new Label(record.isWin ? "W" : "L");
                badgeLbl.style.color = Color.white;
                badgeLbl.style.fontSize = 22;
                badgeLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                badge.Add(badgeLbl);
                card.Add(badge);

                // Info
                VisualElement info = new VisualElement();
                info.style.flexGrow = 1;

                Label nameLbl = new Label($"vs {record.opponentName}");
                nameLbl.style.color = Color.white;
                nameLbl.style.fontSize = 15;
                nameLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                info.Add(nameLbl);

                Label modeLbl = new Label($"{(record.isRanked ? "Ranked" : "Casual")}  •  {record.correctCount}/{record.totalRounds} đúng  •  {record.date}");
                modeLbl.style.color = new StyleColor(new Color(0.6f, 0.64f, 0.71f));
                modeLbl.style.fontSize = 11;
                modeLbl.style.whiteSpace = WhiteSpace.Normal;
                info.Add(modeLbl);

                card.Add(info);

                // Nút xem chi tiết
                var capturedRecord = record;
                Label arrowLbl = new Label("→");
                arrowLbl.style.color = new StyleColor(new Color(0.38f, 0.65f, 0.98f));
                arrowLbl.style.fontSize = 20;
                card.Add(arrowLbl);

                card.RegisterCallback<ClickEvent>(_ => ShowBattleHistoryDetail(capturedRecord));
                container.Add(card);
            }
        }

        private void ShowBattleHistoryDetail(VocabLearning.Data.BattleHistoryRecord record)
        {
            var detailOverlay = _root.Q<VisualElement>("HistoryDetailOverlay");
            if (detailOverlay == null) return;
            detailOverlay.style.display = DisplayStyle.Flex;

            Label titleLbl = detailOverlay.Q<Label>("DetailTitle");
            if (titleLbl != null) titleLbl.text = $"vs {record.opponentName} — {(record.isWin ? "Victory" : "Defeat")}";

            Label statsLbl = detailOverlay.Q<Label>("DetailStats");
            if (statsLbl != null) statsLbl.text = $"{record.correctCount} / {record.totalRounds} đúng";

            VisualElement detailList = detailOverlay.Q<VisualElement>("DetailList");
            if (detailList == null) return;
            detailList.Clear();

            foreach (var round in record.rounds)
            {
                VisualElement row = CreateBattleRoundRow(round);
                detailList.Add(row);
            }
        }

        private VisualElement CreateBattleRoundRow(VocabLearning.Data.BattleRoundRecord round)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row; // Changed to Row to fit image
            row.style.marginBottom = 12;
            row.style.paddingLeft = 12;
            row.style.paddingRight = 12;
            row.style.paddingTop = 10;
            row.style.paddingBottom = 10;
            row.style.borderTopLeftRadius = 10;
            row.style.borderTopRightRadius = 10;
            row.style.borderBottomLeftRadius = 10;
            row.style.borderBottomRightRadius = 10;

            if (round.isTimeout)
                row.style.backgroundColor = new StyleColor(new Color(0.4f, 0.4f, 0.4f, 0.3f));
            else if (round.isCorrect)
                row.style.backgroundColor = new StyleColor(new Color(0.06f, 0.44f, 0.31f, 0.4f));
            else
                row.style.backgroundColor = new StyleColor(new Color(0.56f, 0.16f, 0.16f, 0.4f));

            // Image Thumbnail (if exists)
            if (!string.IsNullOrEmpty(round.imageUrl))
            {
                VisualElement img = new VisualElement();
                img.style.width = 40;
                img.style.height = 40;
                img.style.marginRight = 12;
                img.style.borderTopLeftRadius = 6;
                img.style.borderTopRightRadius = 6;
                img.style.borderBottomLeftRadius = 6;
                img.style.borderBottomRightRadius = 6;
                img.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0.2f));
                img.style.flexShrink = 0;
                StartCoroutine(DownloadAndSetImage(round.imageUrl, img));
                row.Add(img);
            }

            // Info Column
            VisualElement info = new VisualElement();
            info.style.flexGrow = 1;

            string icon = round.isTimeout ? "⏱" : round.isCorrect ? "✅" : "❌";
            Label qLbl = new Label($"{icon}  {round.question}");
            qLbl.style.color = Color.white;
            qLbl.style.fontSize = 15;
            qLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            qLbl.style.whiteSpace = WhiteSpace.Normal;
            info.Add(qLbl);

            // Always show correct answer
            Label ansLbl = new Label($"Đáp án đúng: {round.correctAnswer}");
            ansLbl.style.color = round.isCorrect ? new StyleColor(new Color(0.40f, 0.93f, 0.60f)) : new StyleColor(new Color(0.93f, 0.40f, 0.40f));
            ansLbl.style.fontSize = 12;
            ansLbl.style.whiteSpace = WhiteSpace.Normal;
            info.Add(ansLbl);

            if (!round.isCorrect && !round.isTimeout)
            {
                Label yourLbl = new Label($"Bạn chọn: {(string.IsNullOrEmpty(round.playerAnswer) ? "..." : round.playerAnswer)}");
                yourLbl.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
                yourLbl.style.fontSize = 11;
                yourLbl.style.whiteSpace = WhiteSpace.Normal;
                info.Add(yourLbl);
            }

            row.Add(info);
            return row;
        }

        private bool _isRankedBattle = false;
        private int _battlePlayerHP = 100;
        private int _battleEnemyHP = 100;
        private VocabLearning.Data.UserJson _battleEnemyData;
        private VocabLearning.Data.WordJson _battleCurrentWord; // Dùng WordJson từ bảng words trung tâm

        // --- Battle State ---
        private System.Collections.Generic.List<VocabLearning.Data.InventoryItemJson> _selectedBattleItems = new System.Collections.Generic.List<VocabLearning.Data.InventoryItemJson>();
        private float _battleTimer = 15f;
        private bool _playerAnswered = false;
        private bool _aiAnswered = false;
        private bool _aiCorrect = false;
        private string _playerAnswerText = "";
        private Coroutine _timerCoroutine;

        // --- Solo Quiz State ---
        private enum SoloMode { Survivor, Quick10, TimeRush }
        private SoloMode _soloMode;
        private int _soloHearts = 3;
        private int _soloScore = 0;
        private int _soloTimer = 60;
        private int _soloQuestionCount = 0;
        private Coroutine _soloTimerCoroutine;
        private System.Collections.Generic.List<VocabLearning.Data.BattleRoundRecord> _soloRoundRecords = new System.Collections.Generic.List<VocabLearning.Data.BattleRoundRecord>();
        private System.Collections.Generic.HashSet<string> _soloUsedWordIds = new System.Collections.Generic.HashSet<string>(); // NEW: Prevent duplicates
        
        // --- Review Mode State ---
        private bool _isReviewMode = false;
        private System.Collections.Generic.List<VocabLearning.Data.WordJson> _reviewPool = new System.Collections.Generic.List<VocabLearning.Data.WordJson>();
        private int _reviewCurrentIndex = 0;

        // --- Battle Word Pool (Rank-based, No Repeat) ---
        // Dùng bảng words trung tâm, lọc theo rankRequired của từng từ
        private System.Collections.Generic.List<VocabLearning.Data.WordJson> _battleWordPool = new System.Collections.Generic.List<VocabLearning.Data.WordJson>();
        private System.Collections.Generic.List<string> _battleAllPoolMeanings = new System.Collections.Generic.List<string>();
        private System.Collections.Generic.List<string> _battleAllPoolWords = new System.Collections.Generic.List<string>();
        private int _battlePoolIndex = 0;
        private bool _isImageMode = false;

        // --- Current Study Set Words ---
        private System.Collections.Generic.List<VocabLearning.Data.WordJson> _currentVocabSetWords = new System.Collections.Generic.List<VocabLearning.Data.WordJson>();

        private void StartBattle(bool isRanked)
        {
            _isRankedBattle = isRanked;
            _selectedBattleItems.Clear();

            if (_isRankedBattle)
            {
                // Ranked mode: NO Items allowed
                LoadScreen(BattleGameplayScreenAsset);
            }
            else
            {
                // Casual mode: Choose items
                if (BattleLoadoutScreenAsset == null)
                {
                    Debug.LogError("🚨 LỖI: BattleLoadoutScreenAsset chưa được gán!");
                    return;
                }
                LoadScreen(BattleLoadoutScreenAsset);
            }
        }

        private void BindBattleLoadoutEvents()
        {
            Button backBtn = _root.Q<Button>("BtnBack");
            if (backBtn != null) backBtn.clicked += () => LoadScreen(BattleScreenAsset);

            RenderLoadoutItems();
            UpdateLoadoutStatus();
        }

        private void RenderLoadoutItems()
        {
            ScrollView list = _root.Q<ScrollView>("LoadoutItemsList");
            if (list == null || _jsonDb == null) return;
            list.Clear();

            System.Collections.Generic.List<VocabLearning.Data.InventoryItemJson> consumables = _jsonDb.inventory.FindAll(i => i.category == "Consumable" && i.isCombatItem && i.quantity > 0);

            foreach (var item in consumables)
            {
                VisualElement card = new VisualElement();
                card.AddToClassList("loadout-item");
                if (_selectedBattleItems.Contains(item)) card.AddToClassList("loadout-item-selected");

                Label icon = new Label(item.icon);
                icon.style.fontSize = 24;
                icon.style.marginRight = 12;
                card.Add(icon);

                VisualElement info = new VisualElement();
                info.style.flexGrow = 1;
                Label name = new Label(item.name);
                name.AddToClassList("font-bold");
                name.style.color = Color.white;
                Label desc = new Label(item.description);
                desc.AddToClassList("text-muted");
                desc.style.fontSize = 11;
                info.Add(name);
                info.Add(desc);
                card.Add(info);

                Label qty = new Label($"x{item.quantity}");
                qty.AddToClassList("font-bold");
                qty.style.color = new StyleColor(new Color(0.23f, 0.51f, 0.96f));
                card.Add(qty);

                card.RegisterCallback<ClickEvent>(evt =>
                {
                    if (_selectedBattleItems.Contains(item))
                    {
                        _selectedBattleItems.Remove(item);
                        card.RemoveFromClassList("loadout-item-selected");
                    }
                    else if (_selectedBattleItems.Count < 3)
                    {
                        _selectedBattleItems.Add(item);
                        card.AddToClassList("loadout-item-selected");
                    }
                    UpdateLoadoutStatus();
                });

                list.Add(card);
            }
        }

        private void UpdateLoadoutStatus()
        {
            Label count = _root.Q<Label>("SelectionCount");
            if (count != null) count.text = $"Selected: {_selectedBattleItems.Count}/3";

            Button btnStart = _root.Q<Button>("BtnConfirmStart");
            if (btnStart != null)
            {
                // In UI Toolkit, we can't easily "disable" visually without class
                if (_selectedBattleItems.Count > 0)
                {
                    btnStart.style.backgroundColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f)); // Success green
                    btnStart.style.color = Color.white;
                    btnStart.clicked -= LoadBattleFromLoadout; // Clean up
                    btnStart.clicked += LoadBattleFromLoadout;
                }
                else
                {
                    btnStart.style.backgroundColor = new StyleColor(new Color(0.2f, 0.25f, 0.35f));
                    btnStart.style.color = new StyleColor(new Color(1f, 1f, 1f, 0.3f));
                    btnStart.clicked -= LoadBattleFromLoadout;
                }
            }
        }

        private void LoadBattleFromLoadout()
        {
            if (_selectedBattleItems.Count == 0) return;
            if (BattleGameplayScreenAsset == null) return;
            LoadScreen(BattleGameplayScreenAsset);
        }

        private bool _battleShieldActive = false;

        private void BindBattleGameplayEvents()
        {
            Button btnFlee = _root.Q<Button>("BtnFlee");
            if (btnFlee != null)
            {
                btnFlee.clicked += () =>
                {
                    ShowConfirmationDialog(
                        "Thoát trận đấu?",
                        "Bạn có chắc chắn muốn bỏ chạy? Bạn sẽ bị tính là thua cuộc!",
                        () =>
                        {
                            StopBattleTimer();
                            _battlePlayerHP = 0;
                            FinishBattle(false);
                        },
                        null
                    );
                };
            }

            Label lblMode = _root.Q<Label>("LblBattleMode");
            if (lblMode != null) lblMode.text = _isRankedBattle ? "RANKED MATCH" : "CASUAL MATCH";

            // Initialize Stats
            _battlePlayerHP = 100;
            _battleEnemyHP = 100;
            _battleShieldActive = false;

            // Pick Random Enemy
            if (_jsonDb.leaderboardUsers != null && _jsonDb.leaderboardUsers.Count > 0)
            {
                _battleEnemyData = _jsonDb.leaderboardUsers[UnityEngine.Random.Range(0, _jsonDb.leaderboardUsers.Count)];
            }

            // [RANK] Build word pool theo rank người chơi
            _currentBattleRounds.Clear(); // Reset lịch sử câu hỏi cho trận mới
            BuildBattleWordPool();

            // [RANK] Hiển thị rank pool info lên UI
            if (_jsonDb != null && _jsonDb.currentUser != null)
            {
                string playerRankKey = GetRankKey(_jsonDb.currentUser.rankPoints);
                int playerRankOrder = GetRankOrder(playerRankKey);
                string[] rankNames = { "Đồng", "Bạc", "Vàng", "Bạch Kim", "Kim Cương", "Siêu Cấp" };
                System.Text.StringBuilder poolLabel = new System.Text.StringBuilder();
                for (int r = 0; r <= playerRankOrder && r < rankNames.Length; r++)
                {
                    if (poolLabel.Length > 0) poolLabel.Append(" + ");
                    poolLabel.Append(rankNames[r]);
                }
                Label lblPool = _root.Q<Label>("LblRankPool");
                if (lblPool != null) lblPool.text = $"📚 Pool: {poolLabel}";
                Debug.Log($"[Battle] Rank Pool built for {playerRankKey}: {_battleWordPool.Count} words total.");
            }

            RenderBattleSkills();
            UpdateBattleUI();

            // Initial Binding for answer buttons
            for (int i = 0; i < 4; i++)
            {
                Button btn = _root.Q<Button>($"BtnAns{i}");
                if (btn != null)
                {
                    btn.clicked += () => OnBattleAnswer(btn.text);
                }
            }

            NextBattleQuestion();
        }

        private void StopBattleTimer()
        {
            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
        }

        private System.Collections.IEnumerator BattleTimerRoutine()
        {
            _battleTimer = 15f;
            while (_battleTimer > 0)
            {
                _battleTimer -= Time.deltaTime;
                VisualElement fill = _root.Q<VisualElement>("TimerFill");
                if (fill != null) fill.style.width = Length.Percent((_battleTimer / 15f) * 100f);

                // Random AI choice during the timer
                if (!_aiAnswered && _battleTimer < UnityEngine.Random.Range(4f, 13f))
                {
                    _aiAnswered = true;
                    // AI Accuracy
                    float accuracy = _isRankedBattle ? 0.85f : 0.7f;
                    _aiCorrect = UnityEngine.Random.value < accuracy;

                    if (_aiCorrect)
                    {
                        Debug.Log("🤖 Opponent got it RIGHT first!");
                        ResolveRound("AI");
                        yield break;
                    }
                    else
                    {
                        Debug.Log("🤖 Opponent guessed WRONG!");
                        if (_playerAnswered)
                        {
                            ResolveRound("None");
                            yield break;
                        }
                    }
                }

                yield return null;
            }

            ResolveRound("None");
        }

        private void ResolveRound(string winner)
        {
            StopBattleTimer();

            // Disable all buttons immediately
            for (int i = 0; i < 4; i++)
            {
                Button btn = _root.Q<Button>($"BtnAns{i}");
                if (btn != null) { btn.SetEnabled(false); btn.style.opacity = 0.5f; }
            }

            if (winner == "Player")
            {
                _battleEnemyHP -= 10;
                Debug.Log("✅ You were FAST and CORRECT! Enemy -10 HP");
            }
            else if (winner == "AI")
            {
                if (_battleShieldActive)
                {
                    _battleShieldActive = false;
                    Debug.Log("🛡️ Shield blocked AI damage!");
                }
                else
                {
                    _battlePlayerHP -= 10;
                }

                if (_playerAnswered)
                {
                    Debug.Log("❌ Opponent was CORRECT! You -10 HP");
                }
                else
                {
                    Debug.Log("❌ Opponent was FASTER! You -10 HP");
                }
            }
            else
            {
                // Timeout or both wrong
                string correctAnswer = _isImageMode ? _battleCurrentWord.word : _battleCurrentWord.meaning;
                bool playerWrong = _playerAnswered && (_playerAnswerText != correctAnswer);
                bool aiWrong = _aiAnswered && !_aiCorrect;

                bool isTimeout = (_battleTimer <= 0f);
                bool playerMissed = !_playerAnswered && isTimeout;
                bool aiMissed = !_aiAnswered && isTimeout;

                if (playerWrong || playerMissed)
                {
                    if (_battleShieldActive && !aiWrong && !aiMissed)
                    {
                        _battleShieldActive = false;
                    }
                    else
                    {
                        _battlePlayerHP -= 10;
                    }
                }

                if (aiWrong || aiMissed)
                {
                    _battleEnemyHP -= 10;
                }

                Debug.Log("⌛ Round Ended (Timeout/Both Wrong). Both -10 HP");
            }

            if (_battlePlayerHP < 0) _battlePlayerHP = 0;
            if (_battleEnemyHP < 0) _battleEnemyHP = 0;

            // --- Ghi lại kết quả câu hỏi này ---
            string correctAns = _isImageMode ? _battleCurrentWord.word : _battleCurrentWord.meaning;
            bool isTimeoutRound = (_battleTimer <= 0f);
            var roundRecord = new VocabLearning.Data.BattleRoundRecord
            {
                question = _isImageMode ? (_battleCurrentWord.imageSub ?? _battleCurrentWord.word) : _battleCurrentWord.word,
                correctAnswer = correctAns,
                playerAnswer = _playerAnswered ? _playerAnswerText : "",
                imageUrl = _isImageMode ? _battleCurrentWord.imageUrl : null,
                isCorrect = _playerAnswered && (_playerAnswerText == correctAns),
                isTimeout = isTimeoutRound && !_playerAnswered
            };
            _currentBattleRounds.Add(roundRecord);
            // ------------------------------------

            UpdateBattleUI();

            if (_battleEnemyHP <= 0 || _battlePlayerHP <= 0)
            {
                FinishBattle(_battleEnemyHP <= 0);
            }
            else
            {
                StartCoroutine(WaitAndNextQuestion());
            }
        }

        private System.Collections.IEnumerator WaitAndNextQuestion()
        {
            yield return new WaitForSeconds(1.5f);
            NextBattleQuestion();
        }

        private void RenderBattleSkills()
        {
            VisualElement container = _root.Q<VisualElement>("BattleItemContainer");
            if (container == null) return;
            container.Clear();

            foreach (var item in _selectedBattleItems)
            {
                Button skillBtn = new Button { text = item.icon };
                skillBtn.AddToClassList("skill-btn");
                skillBtn.tooltip = item.name;

                skillBtn.clicked += () =>
                {
                    ApplyBattleItemEffect(item);
                    skillBtn.SetEnabled(false);
                    skillBtn.style.opacity = 0.3f;
                };

                container.Add(skillBtn);
            }
        }

        private void ApplyBattleItemEffect(VocabLearning.Data.InventoryItemJson item)
        {
            // Use item logic (deduct from inventory - in-memory for this session)
            if (item.quantity > 0)
            {
                item.quantity--;
            }

            // Apply COMBAT ONLY Effects
            if (item.name.Contains("Fire"))
            {
                _battleEnemyHP -= 25;
                Debug.Log($"🔥 Fire Potion used! Enemy -25 HP");
            }
            else if (item.name.Contains("Freeze") || item.icon == "🧪")
            {
                _battleEnemyHP -= 15;
                Debug.Log($"❄️ Freeze Potion used! Enemy -15 HP");
            }
            else if (item.name.Contains("Health") || item.icon == "❤️")
            {
                _battlePlayerHP += 40;
                if (_battlePlayerHP > 100) _battlePlayerHP = 100;
                Debug.Log($"❤️ Health Potion used! Player +40 HP");
            }
            else if (item.name.Contains("Shield") || item.icon == "🛡️")
            {
                _battleShieldActive = true;
                Debug.Log($"🛡️ Combat Shield activated!");
            }

            UpdateBattleUI();
            if (_battleEnemyHP <= 0 || _battlePlayerHP <= 0)
            {
                FinishBattle(_battleEnemyHP <= 0);
            }
        }

        private void UpdateBattleUI()
        {
            if (_jsonDb == null || _jsonDb.currentUser == null) return;
            var curUser = _jsonDb.currentUser;

            // Player UI
            Label pName = _root.Q<Label>("PlayerName");
            if (pName != null) pName.text = "You";
            Label pAvatar = _root.Q<Label>("PlayerAvatar");
            if (pAvatar != null) pAvatar.text = GetUserAvatar(curUser.id);
            var pTier = GetRankTier(curUser.rankPoints);
            Label pTierLbl = _root.Q<Label>("PlayerTier");
            if (pTierLbl != null) { pTierLbl.text = pTier.name; pTierLbl.style.color = new StyleColor(pTier.color); }

            Label pHPLbl = _root.Q<Label>("PlayerHPLbl");
            if (pHPLbl != null) pHPLbl.text = $"{_battlePlayerHP} / 100 HP";

            VisualElement pHPFill = _root.Q<VisualElement>("PlayerHPFill");
            if (pHPFill != null)
            {
                pHPFill.style.width = Length.Percent(_battlePlayerHP);
                pHPFill.style.backgroundColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f)); // Emerald-500 (#10B981)
            }
            else
            {
                Debug.LogWarning("[Battle] Không tìm thấy PlayerHPFill trong UI!");
            }

            // Enemy UI
            if (_battleEnemyData != null)
            {
                Label eName = _root.Q<Label>("EnemyName");
                if (eName != null) eName.text = _battleEnemyData.username;
                Label eAvatar = _root.Q<Label>("EnemyAvatar");
                if (eAvatar != null) eAvatar.text = GetUserAvatar(_battleEnemyData.id);
                var eTier = GetRankTier(_battleEnemyData.rankPoints);
                Label eTierLbl = _root.Q<Label>("EnemyTier");
                if (eTierLbl != null) { eTierLbl.text = eTier.name; eTierLbl.style.color = new StyleColor(eTier.color); }

                Label eHPLbl = _root.Q<Label>("EnemyHPLbl");
                if (eHPLbl != null) eHPLbl.text = $"{_battleEnemyHP} / 100 HP";

                VisualElement eHPFill = _root.Q<VisualElement>("EnemyHPFill");
                if (eHPFill != null)
                {
                    eHPFill.style.width = Length.Percent(_battleEnemyHP);
                    eHPFill.style.backgroundColor = new StyleColor(new Color(0.94f, 0.27f, 0.27f)); // Red-500 (#EF4444)
                }
                else
                {
                    Debug.LogWarning("[Battle] Không tìm thấy EnemyHPFill trong UI!");
                }
            }
        }

        // --- RANK HELPERS ---
        private string GetRankKey(int rankPoints)
        {
            if (rankPoints >= 20000) return "SieuCap";
            if (rankPoints >= 10000) return "KimCuong";
            if (rankPoints >= 5000) return "BachKim";
            if (rankPoints >= 2500) return "Vang";
            if (rankPoints >= 1000) return "Bac";
            return "Dong";
        }

        private int GetRankOrder(string rankKey)
        {
            switch (rankKey)
            {
                case "Dong": return 0;
                case "Bac": return 1;
                case "Vang": return 2;
                case "BachKim": return 3;
                case "KimCuong": return 4;
                case "SieuCap": return 5;
                default: return 0;
            }
        }

        // --- BUILD BATTLE WORD POOL TỪ BẢNG WORDS TRUNG TÂM ---
        private void BuildBattleWordPool()
        {
            _battleWordPool.Clear();
            _battleAllPoolMeanings.Clear();
            _battleAllPoolWords.Clear();
            _battlePoolIndex = 0;

            if (_jsonDb == null || _jsonDb.words == null || _jsonDb.currentUser == null)
            {
                Debug.LogWarning("[Battle] Bảng words trung tâm bị rỗng hoặc chưa được tải!");
                return;
            }

            string playerRankKey = GetRankKey(_jsonDb.currentUser.rankPoints);
            int playerRankOrder = GetRankOrder(playerRankKey);

            foreach (var w in _jsonDb.words)
            {
                string wordRankKey = string.IsNullOrEmpty(w.rankRequired) ? "Dong" : w.rankRequired;
                int wordRankOrder = GetRankOrder(wordRankKey);

                // Chỉ lấy từ có rank <= rank người chơi
                if (wordRankOrder <= playerRankOrder)
                {
                    _battleWordPool.Add(w);
                    if (!_battleAllPoolMeanings.Contains(w.meaning))
                        _battleAllPoolMeanings.Add(w.meaning);
                    if (!_battleAllPoolWords.Contains(w.word))
                        _battleAllPoolWords.Add(w.word);
                }
            }

            Debug.Log($"[Battle] Central words pool: {_battleWordPool.Count} từ cho rank {playerRankKey}.");

            // Shuffle toàn bộ pool (Fisher-Yates)
            ShuffleBattlePool();
        }

        private void ShuffleBattlePool()
        {
            for (int i = _battleWordPool.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                VocabLearning.Data.WordJson temp = _battleWordPool[i];
                _battleWordPool[i] = _battleWordPool[j];
                _battleWordPool[j] = temp;
            }
        }

        private void NextBattleQuestion()
        {
            if (_battleWordPool == null || _battleWordPool.Count == 0)
            {
                Debug.LogWarning("[Battle] Word pool rỗng! Fallback về vocabSets.");
                return;
            }

            _playerAnswered = false;
            _aiAnswered = false;
            _playerAnswerText = "";
            StopBattleTimer();
            _timerCoroutine = StartCoroutine(BattleTimerRoutine());

            // Re-enable buttons and reset their look
            for (int i = 0; i < 4; i++)
            {
                Button btn = _root.Q<Button>($"BtnAns{i}");
                if (btn != null)
                {
                    btn.SetEnabled(true);
                    btn.style.opacity = 1f;
                }
            }

            // Lấy từ tiếp theo trong pool (không trùng trong cùng phiên)
            if (_battlePoolIndex >= _battleWordPool.Count)
            {
                // Hết pool → shuffle lại để tránh nhàm
                ShuffleBattlePool();
                _battlePoolIndex = 0;
                Debug.Log("[Battle] Pool đã hết, shuffle lại để tiếp tục.");
            }

            VocabLearning.Data.WordJson current = _battleWordPool[_battlePoolIndex];
            _battlePoolIndex++;
            _battleCurrentWord = current;

            Label qPrompt = _root.Q<Label>("BattlePromptText");
            Label qText = _root.Q<Label>("BattleQuestionText");
            VisualElement imgFrame = _root.Q<VisualElement>("BattleQuestionImageFrame");
            VisualElement imgElem = _root.Q<VisualElement>("BattleQuestionImage");
            Label subText = _root.Q<Label>("BattleQuestionSub");

            System.Collections.Generic.List<string> options = new System.Collections.Generic.List<string>();
            System.Collections.Generic.List<string> distractorPool = new System.Collections.Generic.List<string>();

            _isImageMode = false;
            if (!string.IsNullOrEmpty(_battleCurrentWord.imageUrl))
            {
                _isImageMode = (UnityEngine.Random.value > 0.5f); // 50% chance
            }

            if (_isImageMode)
            {
                if (qPrompt != null) qPrompt.text = "What is this?";
                if (qText != null) qText.style.display = DisplayStyle.None;
                if (imgFrame != null) imgFrame.style.display = DisplayStyle.Flex;
                if (subText != null)
                {
                    subText.text = _battleCurrentWord.imageSub;
                    subText.style.display = string.IsNullOrEmpty(_battleCurrentWord.imageSub) ? DisplayStyle.None : DisplayStyle.Flex;
                }

                if (imgElem != null)
                {
                    imgElem.style.backgroundImage = null; // Clear previous
                    StartCoroutine(DownloadAndSetImage(_battleCurrentWord.imageUrl, imgElem));
                }

                options.Add(_battleCurrentWord.word);
                distractorPool.AddRange(_battleAllPoolWords);
                distractorPool.Remove(_battleCurrentWord.word);
            }
            else
            {
                if (qPrompt != null) qPrompt.text = "Translate this word:";
                if (qText != null)
                {
                    qText.text = _battleCurrentWord.word;
                    qText.style.display = DisplayStyle.Flex;
                }
                if (imgFrame != null) imgFrame.style.display = DisplayStyle.None;
                if (subText != null) subText.style.display = DisplayStyle.None;

                options.Add(_battleCurrentWord.meaning);
                distractorPool.AddRange(_battleAllPoolMeanings);
                distractorPool.Remove(_battleCurrentWord.meaning);
            }

            // Shuffle distractor pool
            for (int i = distractorPool.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                string tmp = distractorPool[i];
                distractorPool[i] = distractorPool[j];
                distractorPool[j] = tmp;
            }

            // Lấy đủ 3 distractors
            int needed = 3;
            for (int i = 0; i < distractorPool.Count && needed > 0; i++)
            {
                options.Add(distractorPool[i]);
                needed--;
            }

            // Nếu pool quá nhỏ, dùng fallback placeholder
            while (options.Count < 4)
            {
                options.Add($"...");
            }

            // Shuffle options để đáp án đúng không luôn ở vị trí 0
            for (int i = 0; i < options.Count; i++)
            {
                string temp = options[i];
                int randomIndex = UnityEngine.Random.Range(i, options.Count);
                options[i] = options[randomIndex];
                options[randomIndex] = temp;
            }

            for (int i = 0; i < 4; i++)
            {
                Button btnAns = _root.Q<Button>($"BtnAns{i}");
                if (btnAns != null)
                {
                    btnAns.text = options[i];
                }
            }
        }

        private void OnBattleAnswer(string answerText)
        {
            if (_playerAnswered) return;
            _playerAnswered = true;
            _playerAnswerText = answerText;

            // Visual feedback: Disable buttons
            for (int i = 0; i < 4; i++)
            {
                Button btn = _root.Q<Button>($"BtnAns{i}");
                if (btn != null)
                {
                    btn.SetEnabled(false);
                    btn.style.opacity = 0.5f;
                }
            }

            string correctAnswer = _isImageMode ? _battleCurrentWord.word : _battleCurrentWord.meaning;

            if (answerText == correctAnswer)
            {
                Debug.Log("✅ Player got it RIGHT first!");
                ResolveRound("Player");
            }
            else
            {
                Debug.Log("❌ Player chose WRONG answer. Waiting for AI or Timer...");
                if (_aiAnswered && !_aiCorrect)
                {
                    ResolveRound("None");
                }
            }
        }

        private System.Collections.IEnumerator DownloadAndSetImage(string url, VisualElement targetElement)
        {
            if (string.IsNullOrEmpty(url)) yield break;

            using (UnityEngine.Networking.UnityWebRequest uwr = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Texture2D tex = UnityEngine.Networking.DownloadHandlerTexture.GetContent(uwr);
                    targetElement.style.backgroundImage = new StyleBackground(tex);
                }
                else
                {
                    Debug.LogWarning($"[Battle] Failed to download image: {uwr.error}");
                }
            }
        }

        private void FinishBattle(bool isWin)
        {
            var curUser = _jsonDb.currentUser;
            curUser.totalGames++;

            VisualElement overlay = _root.Q<VisualElement>("ResultOverlay");
            Label title = _root.Q<Label>("ResultTitle");
            Label iconLbl = _root.Q<Label>("ResultIcon");
            Label subtext = _root.Q<Label>("ResultSubtext");
            Label expText = _root.Q<Label>("ResultExp");

            if (isWin)
            {
                curUser.wins++;
                if (title != null) title.text = "VICTORY!";
                if (title != null) title.style.color = new StyleColor(new Color(0.10f, 0.73f, 0.51f)); // Green
                if (iconLbl != null) iconLbl.text = "🏆";

                if (_isRankedBattle)
                {
                    curUser.rankPoints += 25;
                    curUser.exp += 50;
                    curUser.coins += 50;

                    var mysteryBox = _jsonDb.inventory.Find(i => i.id == "4" || i.name == "Mystery Box");
                    if (mysteryBox != null)
                    {
                        mysteryBox.quantity++;
                        VisualElement rewardBox = _root.Q<VisualElement>("ItemRewardBox");
                        if (rewardBox != null) rewardBox.style.display = DisplayStyle.Flex;
                    }

                    AddQuestProgressByType("WinRankedBattle", 1);

                    if (subtext != null) subtext.text = "+25 Rank Points";
                    if (expText != null) expText.text = "+50 EXP / +50 Coins";
                }
                else
                {
                    VisualElement rewardBox = _root.Q<VisualElement>("ItemRewardBox");
                    if (rewardBox != null) rewardBox.style.display = DisplayStyle.None;

                    curUser.exp += 30;
                    curUser.coins += 30;
                    if (subtext != null) subtext.text = "Casual Match";
                    if (expText != null) expText.text = "+30 EXP / +30 Coins";
                }

                AddQuestProgressByType("WinBattle", 1);
            }
            else
            {
                if (title != null) title.text = "DEFEAT";
                if (title != null) title.style.color = new StyleColor(new Color(0.93f, 0.26f, 0.26f)); // Red
                if (iconLbl != null) iconLbl.text = "💀";

                if (_isRankedBattle)
                {
                    curUser.rankPoints -= 15;
                    if (curUser.rankPoints < 0) curUser.rankPoints = 0;
                    curUser.exp += 15;
                    if (subtext != null) subtext.text = "-15 Rank Points";
                    if (expText != null) expText.text = "+15 EXP (Consolation)";
                }
                else
                {
                    curUser.exp += 10;
                    if (subtext != null) subtext.text = "Casual Match";
                    if (expText != null) expText.text = "+10 EXP (Consolation)";
                }
            }

            // --- Lưu lịch sử trận đấu ---
            string opponentName = _battleEnemyData != null ? _battleEnemyData.username : "Unknown";
            int correctCount = 0;
            foreach (var r in _currentBattleRounds) if (r.isCorrect) correctCount++;

            _lastBattleRecord = new VocabLearning.Data.BattleHistoryRecord
            {
                matchId = System.Guid.NewGuid().ToString(),
                date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                isRanked = _isRankedBattle,
                isWin = isWin,
                opponentName = opponentName,
                playerFinalHP = _battlePlayerHP,
                enemyFinalHP = _battleEnemyHP,
                correctCount = correctCount,
                totalRounds = _currentBattleRounds.Count,
                rounds = new List<VocabLearning.Data.BattleRoundRecord>(_currentBattleRounds)
            };

            if (curUser.battleHistory == null)
                curUser.battleHistory = new List<VocabLearning.Data.BattleHistoryRecord>();
            curUser.battleHistory.Insert(0, _lastBattleRecord); // Mới nhất lên đầu
            if (curUser.battleHistory.Count > 20) // Giới hạn 20 trận
                curUser.battleHistory.RemoveAt(curUser.battleHistory.Count - 1);
            // -----------------------------------

            // Hiện Summary trước, Result sau khi bấm nút
            ShowBattleSummaryOverlay(() =>
            {
                if (overlay != null) overlay.style.display = DisplayStyle.Flex;

                Button btnLeave = _root.Q<Button>("BtnLeaveBattle");
                if (btnLeave != null)
                {
                    btnLeave.clicked += () => LoadScreen(BattleScreenAsset);
                }
            });
        }

        private void ShowBattleSummaryOverlay(System.Action onContinue)
        {
            VisualElement summaryOverlay = _root.Q<VisualElement>("SummaryOverlay");
            if (summaryOverlay == null)
            {
                // Nếu không có overlay trong UI thì gọi callback ngay
                onContinue?.Invoke();
                return;
            }

            summaryOverlay.style.display = DisplayStyle.Flex;

            // Tiêu đề
            Label titleLbl = summaryOverlay.Q<Label>("SummaryTitle");
            if (titleLbl != null)
                titleLbl.text = _lastBattleRecord.isWin ? "🏆 Battle Summary - Victory!" : "💀 Battle Summary - Defeat";

            // Thống kê
            Label statsLbl = summaryOverlay.Q<Label>("SummaryStats");
            if (statsLbl != null)
                statsLbl.text = $"Correct: {_lastBattleRecord.correctCount} / {_lastBattleRecord.totalRounds}";

            // Danh sách câu hỏi
            VisualElement listContainer = summaryOverlay.Q<VisualElement>("SummaryList");
            if (listContainer != null)
            {
                listContainer.Clear();
                foreach (var round in _lastBattleRecord.rounds)
                {
                    VisualElement row = CreateBattleRoundRow(round);
                    listContainer.Add(row);
                }
            }

            // Nút Continue
            Button btnContinue = summaryOverlay.Q<Button>("BtnSummaryContinue");
            if (btnContinue != null)
            {
                btnContinue.clicked += () =>
                {
                    summaryOverlay.style.display = DisplayStyle.None;
                    onContinue?.Invoke();
                };
            }
        }

        private void BindFriendEvents()
        {
            Button backBtn = _root.Q<Button>("BtnBack");
            if (backBtn != null) backBtn.clicked += () => LoadScreen(HomeScreenAsset);

            // -- Thanh Tìm kiếm Bạn Bè --
            TextField searchBox = _root.Q<TextField>(className: "search-input");
            if (searchBox != null)
            {
                SetupPlaceholder(searchBox, "Search friends...");
            }
        }

        private void BindShopEvents()
        {
            BindBottomNav();

            Button btnInventory = _root.Q<Button>("BtnShopToInventory");
            if (btnInventory != null) btnInventory.clicked += () => LoadScreen(InventoryScreenAsset);

            Button tabCosmetics = _root.Q<Button>("TabShopCosmetics");
            Button tabConsumables = _root.Q<Button>("TabShopConsumables");

            if (tabCosmetics != null)
            {
                tabCosmetics.clicked += () =>
                {
                    _shopCurrentTab = "Cosmetic";
                    tabCosmetics.AddToClassList("tab-btn-active");
                    if (tabConsumables != null) tabConsumables.RemoveFromClassList("tab-btn-active");
                    RenderShopList();
                };
            }

            if (tabConsumables != null)
            {
                tabConsumables.clicked += () =>
                {
                    _shopCurrentTab = "Consumable";
                    tabConsumables.AddToClassList("tab-btn-active");
                    if (tabCosmetics != null) tabCosmetics.RemoveFromClassList("tab-btn-active");
                    RenderShopList();
                };
            }

            if (_shopCurrentTab == "Cosmetic" && tabCosmetics != null && tabConsumables != null)
            {
                tabCosmetics.AddToClassList("tab-btn-active");
                tabConsumables.RemoveFromClassList("tab-btn-active");
            }
            else if (_shopCurrentTab == "Consumable" && tabCosmetics != null && tabConsumables != null)
            {
                tabConsumables.AddToClassList("tab-btn-active");
                tabCosmetics.RemoveFromClassList("tab-btn-active");
            }

            RenderShopList();
        }

        private void RenderShopList()
        {
            if (_jsonDb == null || _jsonDb.shopItems == null) return;

            VisualElement listContainer = _root.Q<VisualElement>("ShopListContainer");
            if (listContainer == null) return;

            listContainer.Clear();

            // Cập nhật số dư
            Label coinLabel = _root.Q<Label>("LblShopCoinBalance");
            if (coinLabel != null && _jsonDb.currentUser != null)
            {
                coinLabel.text = _jsonDb.currentUser.coins.ToString();
            }

            var filteredItems = _jsonDb.shopItems.FindAll(x => x.category == _shopCurrentTab);

            foreach (var item in filteredItems)
            {
                VisualElement card = new VisualElement();
                card.AddToClassList("card");
                card.style.width = new Length(48, LengthUnit.Percent);
                card.style.paddingTop = 0;
                card.style.paddingBottom = 0;
                card.style.paddingLeft = 0;
                card.style.paddingRight = 0;
                card.style.overflow = Overflow.Hidden;
                card.style.marginBottom = 16;
                // Mặc định viền tối
                card.style.borderTopColor = new StyleColor(new Color(0.2f, 0.25f, 0.33f));
                card.style.borderBottomColor = new StyleColor(new Color(0.2f, 0.25f, 0.33f));
                card.style.borderLeftColor = new StyleColor(new Color(0.2f, 0.25f, 0.33f));
                card.style.borderRightColor = new StyleColor(new Color(0.2f, 0.25f, 0.33f));

                VisualElement imgBox = new VisualElement();
                imgBox.style.height = 120;
                imgBox.style.justifyContent = Justify.Center;
                imgBox.style.alignItems = Align.Center;

                // Pick color by rarity
                Color bgColor = new Color(0.2f, 0.25f, 0.33f);
                if (item.rarity == "Common") bgColor = new Color(0.23f, 0.51f, 0.96f); // Blue
                else if (item.rarity == "Rare") bgColor = new Color(0.96f, 0.62f, 0.04f); // Orange
                else if (item.rarity == "Epic") bgColor = new Color(0.55f, 0.36f, 0.96f); // Purple
                else if (item.rarity == "Legendary") bgColor = new Color(0.93f, 0.26f, 0.26f); // Red

                imgBox.style.backgroundColor = new StyleColor(new Color(bgColor.r, bgColor.g, bgColor.b, 0.2f));

                VisualElement iconBox = new VisualElement();
                iconBox.style.width = 80;
                iconBox.style.height = 80;
                iconBox.style.backgroundColor = new StyleColor(bgColor);
                iconBox.style.borderTopLeftRadius = 16;
                iconBox.style.borderTopRightRadius = 16;
                iconBox.style.borderBottomLeftRadius = 16;
                iconBox.style.borderBottomRightRadius = 16;
                iconBox.style.justifyContent = Justify.Center;
                iconBox.style.alignItems = Align.Center;

                Label iconLabel = new Label(item.icon);
                iconLabel.style.fontSize = 40;
                iconBox.Add(iconLabel);
                imgBox.Add(iconBox);

                VisualElement infoBox = new VisualElement();
                infoBox.style.paddingTop = 12;
                infoBox.style.paddingBottom = 12;
                infoBox.style.paddingLeft = 12;
                infoBox.style.paddingRight = 12;

                Label nameLbl = new Label(item.name);
                nameLbl.AddToClassList("font-bold");
                nameLbl.style.color = Color.white;
                nameLbl.style.marginBottom = 8;
                infoBox.Add(nameLbl);

                // KIỂM TRA SỞ HỮU (Chỉ Cosmetic mới có khái niệm "Owned")
                bool isOwned = false;
                if (item.category == "Cosmetic" && _jsonDb.inventory != null)
                {
                    isOwned = _jsonDb.inventory.Exists(inv => inv.name == item.name);
                }

                if (isOwned)
                {
                    card.style.borderTopColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f)); // Green border
                    card.style.borderBottomColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f));
                    card.style.borderLeftColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f));
                    card.style.borderRightColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f));

                    VisualElement ownedBox = new VisualElement();
                    ownedBox.style.backgroundColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f, 0.2f));
                    ownedBox.style.borderTopLeftRadius = 12;
                    ownedBox.style.borderTopRightRadius = 12;
                    ownedBox.style.borderBottomLeftRadius = 12;
                    ownedBox.style.borderBottomRightRadius = 12;
                    ownedBox.style.paddingTop = 8;
                    ownedBox.style.paddingBottom = 8;
                    ownedBox.style.paddingLeft = 8;
                    ownedBox.style.paddingRight = 8;
                    ownedBox.style.alignItems = Align.Center;

                    Label ownedLbl = new Label("Owned ✓");
                    ownedLbl.style.color = new StyleColor(new Color(0.06f, 0.73f, 0.51f));
                    ownedLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                    ownedLbl.style.fontSize = 12;
                    ownedBox.Add(ownedLbl);

                    infoBox.Add(ownedBox);
                }
                else
                {
                    Button buyBtn = new Button();
                    buyBtn.AddToClassList("btn-primary");
                    buyBtn.style.marginTop = 0;
                    buyBtn.style.marginBottom = 0;
                    buyBtn.style.marginLeft = 0;
                    buyBtn.style.marginRight = 0;
                    buyBtn.style.paddingTop = 8;
                    buyBtn.style.paddingBottom = 8;
                    buyBtn.style.paddingLeft = 8;
                    buyBtn.style.paddingRight = 8;
                    buyBtn.style.flexDirection = FlexDirection.Row;
                    buyBtn.style.justifyContent = Justify.Center;
                    buyBtn.style.backgroundColor = new StyleColor(new Color(0.23f, 0.51f, 0.96f)); // Blue
                    buyBtn.style.borderTopLeftRadius = 12;
                    buyBtn.style.borderTopRightRadius = 12;
                    buyBtn.style.borderBottomLeftRadius = 12;
                    buyBtn.style.borderBottomRightRadius = 12;
                    buyBtn.style.minHeight = 30;

                    Label coinIcon = new Label("O");
                    coinIcon.style.color = new StyleColor(new Color(0.96f, 0.62f, 0.04f));
                    coinIcon.style.marginRight = 4;
                    buyBtn.Add(coinIcon);

                    Label priceLbl = new Label(item.price.ToString());
                    priceLbl.style.color = Color.white;
                    priceLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                    buyBtn.Add(priceLbl);

                    buyBtn.clicked += () =>
                    {
                        PurchaseShopItem(item);
                    };

                    infoBox.Add(buyBtn);
                }

                card.Add(imgBox);
                card.Add(infoBox);

                listContainer.Add(card);
            }
        }

        private void PurchaseShopItem(VocabLearning.Data.ShopItemJson item)
        {
            if (_jsonDb == null || _jsonDb.currentUser == null || _jsonDb.inventory == null) return;

            if (_jsonDb.currentUser.coins >= item.price)
            {
                // Trừ tiền
                _jsonDb.currentUser.coins -= item.price;

                // Kiểm tra xem đã có trong kho chưa
                var invItem = _jsonDb.inventory.Find(i => i.name == item.name);
                if (invItem != null)
                {
                    if (invItem.category == "Consumable")
                    {
                        invItem.quantity++; // Tăng số lượng nếu là vật phẩm tiêu hao
                    }
                }
                else
                {
                    // Tạo mới
                    string newId = (_jsonDb.inventory.Count + 1).ToString();
                    _jsonDb.inventory.Add(new VocabLearning.Data.InventoryItemJson()
                    {
                        id = newId,
                        icon = item.icon,
                        name = item.name,
                        description = item.description,
                        quantity = 1,
                        rarity = item.rarity,
                        category = item.category,
                        equipType = item.equipType,
                        isEquipped = false
                    });
                }

                RenderShopList(); // render lại để cập nhật balance và UI Owned
            }
            else
            {
                Debug.Log("Không đủ tiền mua vật phẩm này!");
            }
        }

        private void BindRankingEvents()
        {
            BindBottomNav();
            RenderRankingList();
        }

        private void RenderRankingList()
        {
            if (_jsonDb == null || _jsonDb.currentUser == null) return;

            // 1. Combine
            System.Collections.Generic.List<VocabLearning.Data.UserJson> allUsers = new System.Collections.Generic.List<VocabLearning.Data.UserJson>();
            allUsers.Add(_jsonDb.currentUser);
            if (_jsonDb.leaderboardUsers != null)
            {
                allUsers.AddRange(_jsonDb.leaderboardUsers);
            }

            // 2. Sort by Rank Points descending
            allUsers.Sort((a, b) => b.rankPoints.CompareTo(a.rankPoints));

            // 3. Fill Podium (Top 1, 2, 3)
            if (allUsers.Count > 0)
            {
                var top1 = allUsers[0];
                var tier1 = GetRankTier(top1.rankPoints);
                Label top1Name = _root.Q<Label>("Podium1Name");
                if (top1Name != null)
                {
                    top1Name.text = $"{tier1.icon} {(top1.id == _jsonDb.currentUser.id ? "You" : top1.username)}";
                    top1Name.style.color = new StyleColor(tier1.color);
                }
                Label top1Score = _root.Q<Label>("Podium1Score");
                if (top1Score != null) top1Score.text = top1.rankPoints.ToString();
                Label top1Avatar = _root.Q<Label>("Podium1Avatar");
                if (top1Avatar != null) top1Avatar.text = GetUserAvatar(top1.id);
            }
            if (allUsers.Count > 1)
            {
                var top2 = allUsers[1];
                var tier2 = GetRankTier(top2.rankPoints);
                Label top2Name = _root.Q<Label>("Podium2Name");
                if (top2Name != null)
                {
                    top2Name.text = $"{tier2.icon} {(top2.id == _jsonDb.currentUser.id ? "You" : top2.username)}";
                    top2Name.style.color = new StyleColor(tier2.color);
                }
                Label top2Score = _root.Q<Label>("Podium2Score");
                if (top2Score != null) top2Score.text = top2.rankPoints.ToString();
                Label top2Avatar = _root.Q<Label>("Podium2Avatar");
                if (top2Avatar != null) top2Avatar.text = GetUserAvatar(top2.id);
            }
            if (allUsers.Count > 2)
            {
                var top3 = allUsers[2];
                var tier3 = GetRankTier(top3.rankPoints);
                Label top3Name = _root.Q<Label>("Podium3Name");
                if (top3Name != null)
                {
                    top3Name.text = $"{tier3.icon} {(top3.id == _jsonDb.currentUser.id ? "You" : top3.username)}";
                    top3Name.style.color = new StyleColor(tier3.color);
                }
                Label top3Score = _root.Q<Label>("Podium3Score");
                if (top3Score != null) top3Score.text = top3.rankPoints.ToString();
                Label top3Avatar = _root.Q<Label>("Podium3Avatar");
                if (top3Avatar != null) top3Avatar.text = GetUserAvatar(top3.id);
            }

            // 4. Fill MyRankContainer
            int myRankIndex = allUsers.FindIndex(u => u.id == _jsonDb.currentUser.id);
            if (myRankIndex >= 0)
            {
                var myTier = GetRankTier(_jsonDb.currentUser.rankPoints);

                Label myRankVal = _root.Q<Label>("MyRankValue");
                if (myRankVal != null) myRankVal.text = (myRankIndex + 1).ToString();

                Label myRankLvl = _root.Q<Label>("MyRankLevel");
                if (myRankLvl != null)
                {
                    myRankLvl.text = $"{myTier.icon} {myTier.name} • Level {(_jsonDb.currentUser.exp / 1000) + 1}";
                    myRankLvl.style.color = new StyleColor(myTier.color);
                }

                Label myRankScore = _root.Q<Label>("MyRankScore");
                if (myRankScore != null) myRankScore.text = _jsonDb.currentUser.rankPoints.ToString();

                Label myRankAvatar = _root.Q<Label>("MyRankAvatar");
                if (myRankAvatar != null) myRankAvatar.text = GetUserAvatar(_jsonDb.currentUser.id);
            }

            // 5. Fill Other Rankings (Rank 4 onwards)
            VisualElement listContainer = _root.Q<VisualElement>("OtherRankingsContainer");
            if (listContainer != null)
            {
                listContainer.Clear();
                for (int i = 3; i < allUsers.Count; i++)
                {
                    var u = allUsers[i];

                    VisualElement card = new VisualElement();
                    card.AddToClassList("card");
                    card.style.flexDirection = FlexDirection.Row;
                    card.style.alignItems = Align.Center;
                    card.style.justifyContent = Justify.SpaceBetween;
                    card.style.marginBottom = 8;

                    if (u.id == _jsonDb.currentUser.id)
                    {
                        card.style.borderTopColor = new StyleColor(new Color(0.23f, 0.51f, 0.96f)); // Blue highlight
                        card.style.borderBottomColor = new StyleColor(new Color(0.23f, 0.51f, 0.96f));
                        card.style.borderLeftColor = new StyleColor(new Color(0.23f, 0.51f, 0.96f));
                        card.style.borderRightColor = new StyleColor(new Color(0.23f, 0.51f, 0.96f));
                        card.style.backgroundColor = new StyleColor(new Color(0.23f, 0.51f, 0.96f, 0.1f));
                    }

                    // Left Side Group
                    VisualElement leftGroup = new VisualElement();
                    leftGroup.style.flexDirection = FlexDirection.Row;
                    leftGroup.style.alignItems = Align.Center;

                    VisualElement rankBox = new VisualElement();
                    rankBox.style.width = 40;
                    rankBox.style.height = 40;
                    rankBox.style.backgroundColor = new StyleColor(new Color(0.12f, 0.16f, 0.23f));
                    rankBox.style.borderTopLeftRadius = 8;
                    rankBox.style.borderTopRightRadius = 8;
                    rankBox.style.borderBottomLeftRadius = 8;
                    rankBox.style.borderBottomRightRadius = 8;
                    rankBox.style.justifyContent = Justify.Center;
                    rankBox.style.alignItems = Align.Center;
                    rankBox.style.marginRight = 12;

                    Label rankLbl = new Label((i + 1).ToString());
                    rankLbl.AddToClassList("font-bold");
                    rankLbl.style.color = new StyleColor(new Color(0.58f, 0.64f, 0.72f));
                    rankBox.Add(rankLbl);

                    VisualElement avatarBox = new VisualElement();
                    avatarBox.style.width = 48;
                    avatarBox.style.height = 48;
                    avatarBox.style.backgroundColor = new StyleColor(new Color(0.39f, 0.45f, 0.55f));
                    if (u.id == _jsonDb.currentUser.id) avatarBox.style.backgroundColor = new StyleColor(new Color(0.23f, 0.51f, 0.96f));
                    avatarBox.style.borderTopLeftRadius = 12;
                    avatarBox.style.borderTopRightRadius = 12;
                    avatarBox.style.borderBottomLeftRadius = 12;
                    avatarBox.style.borderBottomRightRadius = 12;
                    avatarBox.style.marginRight = 12;
                    avatarBox.style.justifyContent = Justify.Center;
                    avatarBox.style.alignItems = Align.Center;

                    Label avatarLbl = new Label(GetUserAvatar(u.id));
                    avatarLbl.style.fontSize = 24;
                    avatarBox.Add(avatarLbl);

                    VisualElement infoBox = new VisualElement();

                    var tier = GetRankTier(u.rankPoints);

                    Label nameLbl = new Label(u.id == _jsonDb.currentUser.id ? "You" : u.username);
                    nameLbl.AddToClassList("font-bold");
                    nameLbl.style.color = Color.white;
                    nameLbl.style.fontSize = 16;
                    nameLbl.style.marginBottom = 2;
                    infoBox.Add(nameLbl);

                    Label levelLbl = new Label($"{tier.icon} {tier.name} • Level {(u.exp / 1000) + 1}");
                    levelLbl.AddToClassList("font-bold");
                    levelLbl.style.fontSize = 12;
                    levelLbl.style.color = new StyleColor(tier.color);
                    infoBox.Add(levelLbl);

                    leftGroup.Add(rankBox);
                    leftGroup.Add(avatarBox);
                    leftGroup.Add(infoBox);

                    // Right Side Group
                    VisualElement rightGroup = new VisualElement();
                    rightGroup.style.alignItems = Align.FlexEnd;

                    Label scoreLbl = new Label(u.rankPoints.ToString());
                    scoreLbl.AddToClassList("font-bold");
                    scoreLbl.style.color = Color.white;
                    scoreLbl.style.fontSize = 18;
                    rightGroup.Add(scoreLbl);

                    Label ptsLbl = new Label("Rank Points");
                    ptsLbl.AddToClassList("text-muted");
                    ptsLbl.style.fontSize = 12;
                    rightGroup.Add(ptsLbl);

                    card.Add(leftGroup);
                    card.Add(rightGroup);

                    listContainer.Add(card);
                }
            }
        }

        private void BindProfileEvents()
        {
            BindBottomNav();

            Button btnAchievements = _root.Q<Button>("BtnAchievements");
            if (btnAchievements != null) btnAchievements.clicked += () => LoadScreen(AchievementScreenAsset);

            Button btnInventory = _root.Q<Button>("BtnInventory");
            if (btnInventory != null) btnInventory.clicked += () => LoadScreen(InventoryScreenAsset);

            // BIND USER DATA
            if (_jsonDb != null && _jsonDb.currentUser != null)
            {
                Label lblName = _root.Q<Label>("LblProfileName");
                if (lblName != null) lblName.text = _jsonDb.currentUser.username;

                int currentLevel = (_jsonDb.currentUser.exp / 1000) + 1;
                Label lblLevel = _root.Q<Label>("LblProfileLevel");
                if (lblLevel != null) lblLevel.text = $"Level {currentLevel}";

                Label lblExp = _root.Q<Label>("LblProfileExp");
                if (lblExp != null) lblExp.text = $"{_jsonDb.currentUser.exp} / {currentLevel * 1000} EXP";

                Label lblCoins = _root.Q<Label>("LblProfileCoins");
                if (lblCoins != null) lblCoins.text = _jsonDb.currentUser.coins.ToString();

                if (_jsonDb.inventory != null)
                {
                    var equippedAvatar = _jsonDb.inventory.Find(i => i.equipType == "Avatar" && i.isEquipped);
                    var equippedBorder = _jsonDb.inventory.Find(i => i.equipType == "Border" && i.isEquipped);
                    var equippedEffect = _jsonDb.inventory.Find(i => i.equipType == "Effect" && i.isEquipped);

                    Label avatarIcon = _root.Q<Label>("ProfileAvatarIcon");
                    if (avatarIcon != null && equippedAvatar != null)
                    {
                        avatarIcon.text = equippedAvatar.icon;
                    }

                    Label effectIcon = _root.Q<Label>("ProfileAvatarEffect");
                    if (effectIcon != null)
                    {
                        effectIcon.text = equippedEffect != null ? equippedEffect.icon : "";
                        if (equippedEffect != null) AnimateAura(effectIcon);
                    }

                    VisualElement borderContainer = _root.Q<VisualElement>("ProfileBorderContainer");
                    if (borderContainer != null && equippedBorder != null)
                    {
                        Color borderColor = Color.white;
                        if (equippedBorder.rarity == "Common") borderColor = new Color(0.23f, 0.51f, 0.96f);
                        else if (equippedBorder.rarity == "Rare") borderColor = new Color(0.96f, 0.62f, 0.04f);
                        else if (equippedBorder.rarity == "Epic") borderColor = new Color(0.55f, 0.36f, 0.96f);
                        else if (equippedBorder.rarity == "Legendary") borderColor = new Color(0.93f, 0.26f, 0.26f);

                        borderContainer.style.borderTopColor = new StyleColor(borderColor);
                        borderContainer.style.borderBottomColor = new StyleColor(borderColor);
                        borderContainer.style.borderLeftColor = new StyleColor(borderColor);
                        borderContainer.style.borderRightColor = new StyleColor(borderColor);
                    }
                }
            }
        }

        private void BindAchievementEvents()
        {
            Button backBtn = _root.Q<Button>("BtnBack");
            if (backBtn != null) backBtn.clicked += () => LoadScreen(ProfileScreenAsset);

            if (_jsonDb != null && _jsonDb.achievements != null)
            {
                int unlockedCount = 0;
                int totalCount = _jsonDb.achievements.Count;

                VisualElement listContainer = _root.Q<VisualElement>("AchievementListContainer");
                if (listContainer != null)
                {
                    listContainer.Clear();
                    foreach (var ach in _jsonDb.achievements)
                    {
                        if (ach.isUnlocked) unlockedCount++;

                        VisualElement card = new VisualElement();
                        card.AddToClassList("card");
                        card.style.marginBottom = 16;
                        card.style.flexDirection = FlexDirection.Row;
                        card.style.alignItems = Align.Center;

                        if (!ach.isUnlocked)
                        {
                            card.style.opacity = 0.6f;
                        }
                        else
                        {
                            card.style.borderTopColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f, 1f)); // #10B981
                            card.style.borderBottomColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f, 1f));
                            card.style.borderLeftColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f, 1f));
                            card.style.borderRightColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f, 1f));
                            card.style.borderTopWidth = 1;
                            card.style.borderBottomWidth = 1;
                            card.style.borderLeftWidth = 1;
                            card.style.borderRightWidth = 1;
                        }

                        // Icon box
                        VisualElement iconBox = new VisualElement();
                        iconBox.style.width = 60;
                        iconBox.style.height = 60;
                        iconBox.style.borderTopLeftRadius = 16;
                        iconBox.style.borderTopRightRadius = 16;
                        iconBox.style.borderBottomLeftRadius = 16;
                        iconBox.style.borderBottomRightRadius = 16;
                        iconBox.style.justifyContent = Justify.Center;
                        iconBox.style.alignItems = Align.Center;
                        iconBox.style.marginRight = 16;

                        if (ach.isUnlocked)
                            iconBox.style.backgroundColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f, 0.2f));
                        else
                            iconBox.style.backgroundColor = new StyleColor(new Color(0.2f, 0.25f, 0.33f, 1f)); // #334155

                        Label iconLbl = new Label(ach.icon);
                        iconLbl.style.fontSize = 28;
                        iconBox.Add(iconLbl);
                        card.Add(iconBox);

                        // Text Group
                        VisualElement textGroup = new VisualElement();
                        textGroup.style.flexGrow = 1;

                        Label titleLbl = new Label(ach.title);
                        titleLbl.AddToClassList("font-bold");
                        titleLbl.AddToClassList("text-lg");
                        titleLbl.style.color = Color.white;
                        titleLbl.style.marginBottom = 4;
                        textGroup.Add(titleLbl);

                        Label descLbl = new Label(ach.description);
                        descLbl.AddToClassList("text-muted");
                        descLbl.style.whiteSpace = WhiteSpace.Normal;
                        descLbl.style.fontSize = 13;
                        textGroup.Add(descLbl);

                        if (ach.isUnlocked)
                        {
                            Label unlockedDateLbl = new Label($"Unlocked: {ach.unlockDate}");
                            unlockedDateLbl.style.color = new StyleColor(new Color(0.06f, 0.73f, 0.51f, 1f));
                            unlockedDateLbl.style.fontSize = 11;
                            unlockedDateLbl.style.marginTop = 6;
                            unlockedDateLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                            textGroup.Add(unlockedDateLbl);
                        }
                        else
                        {
                            // Progress bar
                            VisualElement progressRow = new VisualElement();
                            progressRow.style.flexDirection = FlexDirection.Row;
                            progressRow.style.alignItems = Align.Center;
                            progressRow.style.marginTop = 8;

                            VisualElement progContainer = new VisualElement();
                            progContainer.AddToClassList("progress-bar-container");
                            progContainer.style.flexGrow = 1;
                            progContainer.style.height = 6;
                            progContainer.style.marginRight = 8;
                            progContainer.style.borderTopLeftRadius = 3;
                            progContainer.style.borderTopRightRadius = 3;
                            progContainer.style.borderBottomLeftRadius = 3;
                            progContainer.style.borderBottomRightRadius = 3;

                            VisualElement progFill = new VisualElement();
                            float pct = ach.maxProgress > 0 ? ((float)ach.currentProgress / ach.maxProgress) * 100f : 0;
                            progFill.style.width = new Length(pct, LengthUnit.Percent);
                            progFill.style.height = new Length(100, LengthUnit.Percent);
                            progFill.style.backgroundColor = new StyleColor(new Color(0.58f, 0.64f, 0.72f, 1f)); // #94A3B8
                            progFill.style.borderTopLeftRadius = 3;
                            progFill.style.borderTopRightRadius = 3;
                            progFill.style.borderBottomLeftRadius = 3;
                            progFill.style.borderBottomRightRadius = 3;
                            progContainer.Add(progFill);
                            progressRow.Add(progContainer);

                            Label progText = new Label($"{ach.currentProgress}/{ach.maxProgress}");
                            progText.AddToClassList("text-muted");
                            progText.style.fontSize = 11;
                            progressRow.Add(progText);

                            textGroup.Add(progressRow);
                        }

                        card.Add(textGroup);
                        listContainer.Add(card);
                    }
                }

                // Update Completion Stats
                Label lblCompletionCount = _root.Q<Label>("LblCompletionCount");
                if (lblCompletionCount != null)
                {
                    lblCompletionCount.text = $"{unlockedCount}/{totalCount}";
                }

                VisualElement progressFill = _root.Q<VisualElement>("ProgressBarCompletionFill");
                if (progressFill != null)
                {
                    if (totalCount > 0)
                    {
                        float fillPct = ((float)unlockedCount / totalCount) * 100f;
                        progressFill.style.width = new Length(fillPct, LengthUnit.Percent);
                    }
                    else
                    {
                        progressFill.style.width = new Length(0, LengthUnit.Percent);
                    }
                }
            }
        }

        private void BindInventoryEvents()
        {
            Button backBtn = _root.Q<Button>("BtnBack");
            if (backBtn != null) backBtn.clicked += () => LoadScreen(ProfileScreenAsset);

            Button tabConsumables = _root.Q<Button>("TabConsumables");
            Button tabCosmetics = _root.Q<Button>("TabCosmetics");

            if (tabConsumables != null)
            {
                tabConsumables.clicked += () =>
                {
                    _inventoryCurrentTab = "Consumable";
                    tabConsumables.AddToClassList("tab-btn-active");
                    if (tabCosmetics != null) tabCosmetics.RemoveFromClassList("tab-btn-active");
                    RenderInventoryList();
                };
            }

            if (tabCosmetics != null)
            {
                tabCosmetics.clicked += () =>
                {
                    _inventoryCurrentTab = "Cosmetic";
                    tabCosmetics.AddToClassList("tab-btn-active");
                    if (tabConsumables != null) tabConsumables.RemoveFromClassList("tab-btn-active");
                    RenderInventoryList();
                };
            }

            // Init state
            if (_inventoryCurrentTab == "Consumable" && tabConsumables != null && tabCosmetics != null)
            {
                tabConsumables.AddToClassList("tab-btn-active");
                tabCosmetics.RemoveFromClassList("tab-btn-active");
            }
            else if (_inventoryCurrentTab == "Cosmetic" && tabConsumables != null && tabCosmetics != null)
            {
                tabCosmetics.AddToClassList("tab-btn-active");
                tabConsumables.RemoveFromClassList("tab-btn-active");
            }

            RenderInventoryList();
        }

        private void RenderInventoryList()
        {
            if (_jsonDb == null || _jsonDb.inventory == null) return;

            VisualElement listContainer = _root.Q<VisualElement>("InventoryListContainer");
            if (listContainer == null) return;

            listContainer.Clear();

            var filteredItems = _jsonDb.inventory.FindAll(i => i.category == _inventoryCurrentTab);

            foreach (var item in filteredItems)
            {
                VisualElement card = new VisualElement();
                card.AddToClassList("card");
                card.style.width = new Length(48, LengthUnit.Percent); // Two columns
                card.style.marginBottom = 16;
                card.style.alignItems = Align.Center;
                card.style.paddingTop = 16;
                card.style.paddingBottom = 16;
                card.style.paddingLeft = 12;
                card.style.paddingRight = 12;

                // Rarity Border Color
                Color rarityColor = new Color(0.58f, 0.64f, 0.72f); // Default/Common
                if (item.rarity == "Rare") rarityColor = new Color(0.23f, 0.51f, 0.96f); // Blue
                else if (item.rarity == "Epic") rarityColor = new Color(0.55f, 0.36f, 0.96f); // Purple
                else if (item.rarity == "Legendary") rarityColor = new Color(0.96f, 0.62f, 0.04f); // Orange

                card.style.borderTopColor = new StyleColor(rarityColor);
                card.style.borderBottomColor = new StyleColor(rarityColor);
                card.style.borderLeftColor = new StyleColor(rarityColor);
                card.style.borderRightColor = new StyleColor(rarityColor);
                card.style.borderTopWidth = 1;
                card.style.borderBottomWidth = 1;
                card.style.borderLeftWidth = 1;
                card.style.borderRightWidth = 1;

                // Icon box
                VisualElement iconBox = new VisualElement();
                iconBox.style.width = 64;
                iconBox.style.height = 64;
                iconBox.style.borderTopLeftRadius = 32;
                iconBox.style.borderTopRightRadius = 32;
                iconBox.style.borderBottomLeftRadius = 32;
                iconBox.style.borderBottomRightRadius = 32;
                iconBox.style.justifyContent = Justify.Center;
                iconBox.style.alignItems = Align.Center;
                iconBox.style.marginBottom = 12;
                iconBox.style.backgroundColor = new StyleColor(new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.2f));

                Label iconLbl = new Label(item.icon);
                iconLbl.style.fontSize = 28;
                iconBox.Add(iconLbl);

                if (_inventoryCurrentTab == "Consumable")
                {
                    // Quantity Badge
                    VisualElement badge = new VisualElement();
                    badge.style.position = Position.Absolute;
                    badge.style.top = -5;
                    badge.style.right = -5;
                    badge.style.backgroundColor = new StyleColor(new Color(0.9f, 0.2f, 0.2f)); // Red badge
                    badge.style.borderTopLeftRadius = 10;
                    badge.style.borderTopRightRadius = 10;
                    badge.style.borderBottomLeftRadius = 10;
                    badge.style.borderBottomRightRadius = 10;
                    badge.style.paddingLeft = 6;
                    badge.style.paddingRight = 6;
                    badge.style.paddingTop = 2;
                    badge.style.paddingBottom = 2;

                    Label qtyLbl = new Label($"x{item.quantity}");
                    qtyLbl.style.color = Color.white;
                    qtyLbl.style.fontSize = 10;
                    qtyLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                    badge.Add(qtyLbl);
                    iconBox.Add(badge);
                }

                card.Add(iconBox);

                // Title
                Label titleLbl = new Label(item.name);
                titleLbl.AddToClassList("font-bold");
                titleLbl.style.color = Color.white;
                titleLbl.style.fontSize = 14;
                titleLbl.style.marginBottom = 4;
                titleLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
                card.Add(titleLbl);

                // Description
                Label descLbl = new Label(item.description);
                descLbl.AddToClassList("text-muted");
                descLbl.style.fontSize = 11;
                descLbl.style.whiteSpace = WhiteSpace.Normal;
                descLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
                descLbl.style.marginBottom = 8;
                card.Add(descLbl);

                if (_inventoryCurrentTab == "Consumable" && item.name == "Mystery Box" && item.quantity > 0)
                {
                    Button openBtn = new Button();
                    openBtn.text = "Open";
                    openBtn.style.paddingTop = 6;
                    openBtn.style.paddingBottom = 6;
                    openBtn.style.paddingLeft = 16;
                    openBtn.style.paddingRight = 16;
                    openBtn.style.borderTopLeftRadius = 8;
                    openBtn.style.borderTopRightRadius = 8;
                    openBtn.style.borderBottomLeftRadius = 8;
                    openBtn.style.borderBottomRightRadius = 8;
                    openBtn.style.borderTopWidth = 0;
                    openBtn.style.borderBottomWidth = 0;
                    openBtn.style.borderLeftWidth = 0;
                    openBtn.style.borderRightWidth = 0;
                    openBtn.style.backgroundColor = new StyleColor(new Color(0.96f, 0.62f, 0.04f, 1f)); // Amber/Gold
                    openBtn.style.color = Color.white;
                    openBtn.style.unityFontStyleAndWeight = FontStyle.Bold;

                    openBtn.clicked += () => OpenMysteryBox(item);
                    card.Add(openBtn);
                }

                if (_inventoryCurrentTab == "Cosmetic")
                {
                    Button equipBtn = new Button();
                    equipBtn.text = item.isEquipped ? "Equipped" : "Equip";
                    equipBtn.style.paddingTop = 6;
                    equipBtn.style.paddingBottom = 6;
                    equipBtn.style.paddingLeft = 16;
                    equipBtn.style.paddingRight = 16;
                    equipBtn.style.borderTopLeftRadius = 8;
                    equipBtn.style.borderTopRightRadius = 8;
                    equipBtn.style.borderBottomLeftRadius = 8;
                    equipBtn.style.borderBottomRightRadius = 8;
                    equipBtn.style.borderTopWidth = 0;
                    equipBtn.style.borderBottomWidth = 0;
                    equipBtn.style.borderLeftWidth = 0;
                    equipBtn.style.borderRightWidth = 0;
                    equipBtn.style.color = Color.white;
                    equipBtn.style.unityFontStyleAndWeight = FontStyle.Bold;

                    if (item.isEquipped)
                    {
                        equipBtn.style.backgroundColor = new StyleColor(new Color(0.12f, 0.16f, 0.23f, 1f)); // Dark gray
                    }
                    else
                    {
                        equipBtn.style.backgroundColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f, 1f)); // Green primary
                    }

                    // Equip logic
                    equipBtn.clicked += () =>
                    {
                        if (!item.isEquipped)
                        {
                            // Unequip others OF THE SAME EQUIP TYPE
                            foreach (var i in _jsonDb.inventory.FindAll(x => x.category == "Cosmetic" && x.equipType == item.equipType))
                            {
                                i.isEquipped = false;
                            }

                            item.isEquipped = true;
                            RenderInventoryList(); // re-render
                        }
                    };

                    card.Add(equipBtn);
                }

                listContainer.Add(card);
            }
        }

        private void OpenMysteryBox(VocabLearning.Data.InventoryItemJson box)
        {
            if (box.quantity <= 0) return;
            box.quantity--;

            // Logic Random phần thưởng
            int r = UnityEngine.Random.Range(0, 100);
            string icon = "🎁";
            string name = "Reward";
            string rewardMsg = "";

            if (r < 40) // 40% nhận Coins
            {
                int coins = UnityEngine.Random.Range(50, 201);
                _jsonDb.currentUser.coins += coins;
                icon = "💰";
                name = $"{coins} Coins";
                rewardMsg = "Bạn đã tìm thấy một túi tiền vàng!";
            }
            else if (r < 80) // 40% nhận một Battle Potion ngẫu nhiên
            {
                var combatItems = _jsonDb.inventory.FindAll(i => i.isCombatItem);
                if (combatItems.Count > 0)
                {
                    var rewardItem = combatItems[UnityEngine.Random.Range(0, combatItems.Count)];
                    rewardItem.quantity++;
                    icon = rewardItem.icon;
                    name = rewardItem.name;
                    rewardMsg = "Một vật phẩm chiến đấu hữu ích!";
                }
                else
                {
                    _jsonDb.currentUser.coins += 100;
                    icon = "💰";
                    name = "100 Coins";
                    rewardMsg = "Hộp quà chứa một ít tiền mặt.";
                }
            }
            else // 20% "Jackpot"
            {
                _jsonDb.currentUser.coins += 500;
                icon = "💎";
                name = "500 Coins";
                rewardMsg = "SIÊU CẤP MAY MẮN! Phần thưởng lớn nhất!";
            }

            ShowRewardOverlay(icon, name, rewardMsg);
            RenderInventoryList();
        }

        private void ShowRewardOverlay(string icon, string title, string message)
        {
            VisualElement overlay = new VisualElement();
            overlay.AddToClassList("reward-overlay");

            VisualElement card = new VisualElement();
            card.AddToClassList("reward-card");

            Label iconLbl = new Label(icon);
            iconLbl.AddToClassList("reward-icon-anim");
            card.Add(iconLbl);

            Label titleLbl = new Label(title);
            titleLbl.AddToClassList("font-bold");
            titleLbl.style.fontSize = 24;
            titleLbl.style.color = Color.white;
            titleLbl.style.marginBottom = 8;
            card.Add(titleLbl);

            Label msgLbl = new Label(message);
            msgLbl.AddToClassList("text-muted");
            msgLbl.style.marginBottom = 24;
            msgLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            msgLbl.style.whiteSpace = WhiteSpace.Normal;
            card.Add(msgLbl);

            Button claimBtn = new Button();
            claimBtn.text = "CLAIM";
            claimBtn.AddToClassList("btn-primary");
            claimBtn.style.width = 160;
            claimBtn.clicked += () =>
            {
                _root.Remove(overlay);
            };
            card.Add(claimBtn);

            overlay.Add(card);
            _root.Add(overlay);

            // Trigger animations
            overlay.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                overlay.style.opacity = 1;
                card.style.scale = new StyleScale(new Vector2(1, 1));
            });
        }

        private void BindBottomNav()
        {
            Button navHome = _root.Q<Button>("NavHome");
            if (navHome != null) navHome.clicked += () => LoadScreen(HomeScreenAsset);

            Button navQuest = _root.Q<Button>("NavQuest");
            if (navQuest != null) navQuest.clicked += () => LoadScreen(QuestScreenAsset);

            Button navShop = _root.Q<Button>("NavShop");
            if (navShop != null) navShop.clicked += () => LoadScreen(ShopScreenAsset);

            Button navRank = _root.Q<Button>("NavRank");
            if (navRank != null) navRank.clicked += () => LoadScreen(RankingScreenAsset);

            Button navProfile = _root.Q<Button>("NavProfile");
            if (navProfile != null) navProfile.clicked += () => LoadScreen(ProfileScreenAsset);
        }

        // --- TIỆN ÍCH DÀNH CHO TEXTFIELD (PLACEHOLDER) ---
        private void SetupPlaceholder(TextField textField, string placeholderText)
        {
            if (textField == null) return;

            // Đặt trạng thái ban đầu
            if (string.IsNullOrEmpty(textField.value))
            {
                textField.value = placeholderText;
                textField.style.color = new StyleColor(new Color(0.5f, 0.5f, 0.5f, 1f)); // Màu xám
                textField.style.unityFontStyleAndWeight = FontStyle.Italic;
            }

            // Khi click vào để gõ
            textField.RegisterCallback<FocusInEvent>(evt =>
            {
                if (textField.value == placeholderText)
                {
                    textField.value = "";
                    textField.style.color = new StyleColor(Color.white); // Chữ trắng khi đang gõ
                    textField.style.unityFontStyleAndWeight = FontStyle.Normal;
                }
            });

            // Khi click ra ngoài
            textField.RegisterCallback<FocusOutEvent>(evt =>
            {
                if (string.IsNullOrEmpty(textField.value))
                {
                    textField.value = placeholderText;
                    textField.style.color = new StyleColor(new Color(0.5f, 0.5f, 0.5f, 1f));
                    textField.style.unityFontStyleAndWeight = FontStyle.Italic;
                }
            });
        }

        // --- TIỆN ÍCH DÀNH CHO NHIỆM VỤ ---
        private void AddQuestProgressByType(string questType, int amount)
        {
            if (_jsonDb == null || _jsonDb.quests == null) return;

            // Tìm tất cả nhiệm vụ có cùng loại (hỗ trợ nhiều nhiệm vụ cùng trigger)
            var targetQuests = _jsonDb.quests.FindAll(q => q.questType == questType);
            foreach (var quest in targetQuests)
            {
                if (quest != null && !quest.isClaimed && quest.currentProgress < quest.maxProgress)
                {
                    quest.currentProgress += amount;
                    if (quest.currentProgress > quest.maxProgress)
                        quest.currentProgress = quest.maxProgress;
                }
            }
        }

        private void AnimateAura(VisualElement target)
        {
            if (target == null || target.ClassListContains("is-animated")) return;
            target.AddToClassList("is-animated");

            float scale = 1.0f;
            float step = 0.015f;
            target.schedule.Execute(() =>
            {
                scale += step;
                if (scale >= 1.2f) step = -0.015f;
                if (scale <= 0.9f) step = 0.015f;

                target.style.scale = new StyleScale(new Vector2(scale, scale));
                target.style.opacity = new StyleFloat((scale - 0.9f) * 2f + 0.4f); // 0.4 -> 1.0
            }).Every(30);
        }

        private (string name, string icon, Color color) GetRankTier(int exp)
        {
            if (exp >= 20000) return ("Siêu Cấp", "👑", new Color(0.93f, 0.26f, 0.26f)); // Red
            if (exp >= 10000) return ("Kim Cương", "💎", new Color(0.12f, 0.8f, 0.98f)); // Cyan
            if (exp >= 5000) return ("Bạch Kim", "💠", new Color(0.6f, 0.4f, 0.8f)); // Purple
            if (exp >= 2500) return ("Vàng", "🌟", new Color(1.0f, 0.84f, 0.0f)); // Gold
            if (exp >= 1000) return ("Bạc", "🔹", new Color(0.75f, 0.75f, 0.75f)); // Silver/Steel
            return ("Đồng", "🔸", new Color(0.8f, 0.5f, 0.2f)); // Bronze/Orange
        }

        private string GetUserAvatar(string userId)
        {
            if (_jsonDb != null && _jsonDb.currentUser != null && userId == _jsonDb.currentUser.id)
            {
                if (_jsonDb.inventory != null)
                {
                    var equippedAvatar = _jsonDb.inventory.Find(i => i.equipType == "Avatar" && i.isEquipped);
                    if (equippedAvatar != null) return equippedAvatar.icon;
                }
                return "👤";
            }

            // Fallback for AI bots - deterministic random based on id string
            string[] botAvatars = { "🐼", "🦊", "🐯", "🐰", "🐻", "🦖", "🐧", "🦉", "🦄" };
            int hash = 0;
            if (userId != null)
            {
                foreach (char c in userId) hash += c;
            }
            return botAvatars[hash % botAvatars.Length];
        }
        #region DYNAMIC DIALOG SYSTEM [NEW]
        private void ShowConfirmationDialog(string title, string message, System.Action onConfirm, System.Action onCancel)
        {
            // 1. Overlay (Màn phủ mờ)
            VisualElement overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.width = Length.Percent(100);
            overlay.style.height = Length.Percent(100);
            overlay.style.backgroundColor = new Color(0, 0, 0, 0.7f);
            overlay.style.justifyContent = Justify.Center;
            overlay.style.alignItems = Align.Center;

            // 2. Dialog Box
            VisualElement dialog = new VisualElement();
            dialog.style.width = 320;
            dialog.style.backgroundColor = new Color(0.07f, 0.11f, 0.19f); // Dark background
            dialog.style.borderTopLeftRadius = 16;
            dialog.style.borderTopRightRadius = 16;
            dialog.style.borderBottomLeftRadius = 16;
            dialog.style.borderBottomRightRadius = 16;
            dialog.style.paddingTop = 24;
            dialog.style.paddingBottom = 24;
            dialog.style.paddingLeft = 24;
            dialog.style.paddingRight = 24;
            dialog.style.borderTopWidth = 1;
            dialog.style.borderBottomWidth = 1;
            dialog.style.borderLeftWidth = 1;
            dialog.style.borderRightWidth = 1;
            dialog.style.borderTopColor = new Color(1, 1, 1, 0.1f);
            dialog.style.borderBottomColor = new Color(1, 1, 1, 0.1f);
            dialog.style.borderLeftColor = new Color(1, 1, 1, 0.1f);
            dialog.style.borderRightColor = new Color(1, 1, 1, 0.1f);

            // 3. Content
            Label titleLbl = new Label(title);
            titleLbl.style.fontSize = 20;
            titleLbl.AddToClassList("font-bold");
            titleLbl.style.color = Color.white;
            titleLbl.style.marginBottom = 12;
            titleLbl.style.whiteSpace = WhiteSpace.Normal;
            dialog.Add(titleLbl);

            Label msgLbl = new Label(message);
            msgLbl.style.fontSize = 14;
            msgLbl.style.color = new Color(0.58f, 0.64f, 0.72f);
            msgLbl.style.marginBottom = 24;
            msgLbl.style.whiteSpace = WhiteSpace.Normal;
            dialog.Add(msgLbl);

            // 4. Buttons Container
            VisualElement btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.justifyContent = Justify.SpaceBetween;

            Button btnCancel = new Button();
            btnCancel.text = "QUAY LẠI";
            btnCancel.AddToClassList("btn-secondary");
            btnCancel.style.flexGrow = 1;
            btnCancel.style.marginRight = 6;
            btnCancel.clicked += () =>
            {
                _root.Remove(overlay);
                onCancel?.Invoke();
            };

            Button btnConfirm = new Button();
            btnConfirm.text = "ĐỒNG Ý";
            btnConfirm.AddToClassList("btn-primary");
            btnConfirm.style.flexGrow = 1;
            btnConfirm.style.marginLeft = 6;
            btnConfirm.clicked += () =>
            {
                _root.Remove(overlay);
                onConfirm?.Invoke();
            };

            btnRow.Add(btnCancel);
            btnRow.Add(btnConfirm);
            dialog.Add(btnRow);

            overlay.Add(dialog);
            _root.Add(overlay);
        }
        #endregion
        // =====================================================================
        // SOLO QUIZ LOGIC
        // =====================================================================

        private void BindSoloQuizEvents()
        {
            Button btnBack = _root.Q<Button>("BtnBack");
            if (btnBack != null) btnBack.clicked += () => LoadScreen(HomeScreenAsset);

            _root.Q<Button>("BtnModeSurvivor")?.RegisterCallback<ClickEvent>(_ => StartSoloQuiz(SoloMode.Survivor));
            _root.Q<Button>("BtnModeQuick10")?.RegisterCallback<ClickEvent>(_ => StartSoloQuiz(SoloMode.Quick10));
            _root.Q<Button>("BtnModeTimeRush")?.RegisterCallback<ClickEvent>(_ => StartSoloQuiz(SoloMode.TimeRush));

            RefreshSoloHubUI();

            _root.Q<Button>("BtnExitGameplay")?.RegisterCallback<ClickEvent>(_ =>
            {
                _root.Q<VisualElement>("SoloExitOverlay").style.display = DisplayStyle.Flex;
            });

            _root.Q<Button>("BtnConfirmExitNo")?.RegisterCallback<ClickEvent>(_ =>
            {
                _root.Q<VisualElement>("SoloExitOverlay").style.display = DisplayStyle.None;
            });

            _root.Q<Button>("BtnConfirmExitYes")?.RegisterCallback<ClickEvent>(_ => 
            {
                _root.Q<VisualElement>("SoloExitOverlay").style.display = DisplayStyle.None;
                if (_isReviewMode) FinishReviewQuiz();
                else FinishSoloQuiz();
            });

            for (int i = 0; i < 4; i++)
            {
                int index = i;
                Button btn = _root.Q<Button>($"SoloAns{index}");
                if (btn != null) btn.clicked += () => StartCoroutine(HandleSoloAnswerSelection(btn, btn.text));
            }
        }

        private void StartSoloQuiz(SoloMode mode)
        {
            _soloMode = mode;
            _soloHearts = 3;
            _soloScore = 0;
            _soloTimer = (_soloMode == SoloMode.Quick10) ? 0 : 60;
            _soloQuestionCount = 0;
            _soloRoundRecords.Clear();
            _soloUsedWordIds.Clear();

            _root.Q<VisualElement>("SoloGameplayOverlay").style.display = DisplayStyle.Flex;
            _root.Q<VisualElement>("SoloExitOverlay").style.display = DisplayStyle.None; 
            _root.Q<Label>("SoloScore").text = _isReviewMode ? "" : "Score: 0";

            UpdateSoloStatusUI();

            if (!_isReviewMode && (_soloMode == SoloMode.TimeRush || _soloMode == SoloMode.Quick10))
            {
                _soloTimerCoroutine = StartCoroutine(SoloTimerTick());
            }

            if (_isReviewMode) NextReviewQuestion();
            else NextSoloQuestion();
        }

        private void StartReviewQuiz()
        {
            if (_currentVocabSet == null || _currentVocabSet.wordIds == null || _currentVocabSet.wordIds.Count == 0) return;

            _isReviewMode = true;
            _reviewPool.Clear();
            foreach (var wId in _currentVocabSet.wordIds)
            {
                var w = _jsonDb.words.Find(x => x.id == wId);
                if (w != null) _reviewPool.Add(w);
            }
            
            _reviewPool = _reviewPool.OrderBy(x => UnityEngine.Random.value).ToList();
            _reviewCurrentIndex = 0;
            _soloRoundRecords.Clear();

            // QUAN TRỌNG: Phải tải màn hình Quiz trước khi truy cập UI gameplay
            LoadScreen(SoloQuizScreenAsset);
            StartSoloQuiz(SoloMode.Quick10); 
        }

        private void RefreshSoloHubUI()
        {
            // Chỉ reset nếu không phải đang trong chế độ Review
            if (!_isReviewMode) 
            {
                var user = _jsonDb.currentUser;
                if (_root.Q<Label>("BestSurvivor") != null) _root.Q<Label>("BestSurvivor").text = user.bestSurvivor.ToString();
                if (_root.Q<Label>("BestQuick") != null) _root.Q<Label>("BestQuick").text = user.bestQuick10 + "/10";
                if (_root.Q<Label>("BestTimeRush") != null) _root.Q<Label>("BestTimeRush").text = user.bestTimeRush.ToString();
            }
        }

        private void UpdateSoloStatusUI()
        {
            VisualElement container = _root.Q<VisualElement>("SoloStatusContainer");
            if (container == null) return;
            container.Clear();

            if (_isReviewMode)
            {
                Label progLbl = new Label($"Review: {_reviewCurrentIndex}/{_reviewPool.Count}");
                progLbl.style.color = new StyleColor(new Color(0.54f, 0.45f, 0.94f));
                progLbl.style.fontSize = 18;
                progLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                container.Add(progLbl);
                return;
            }

            if (_soloMode == SoloMode.Survivor)
            {
                for (int i = 0; i < 3; i++)
                {
                    Label heart = new Label(i < _soloHearts ? "❤️" : "🖤");
                    heart.style.fontSize = 20;
                    heart.style.marginRight = 4;
                    container.Add(heart);
                }
            }
            else if (_soloMode == SoloMode.TimeRush)
            {
                Label timerLbl = new Label($"⏱ {_soloTimer}s");
                timerLbl.style.color = _soloTimer < 10 ? Color.red : Color.white;
                timerLbl.style.fontSize = 18;
                timerLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                container.Add(timerLbl);
            }
            else
            {
                Label progLbl = new Label($"{_soloQuestionCount}/10");
                progLbl.style.color = Color.white;
                progLbl.style.fontSize = 18;
                progLbl.style.marginRight = 12;
                container.Add(progLbl);

                Label timerLbl = new Label($"⏱ {_soloTimer}s");
                timerLbl.style.color = _soloTimer < 10 ? Color.red : Color.white;
                timerLbl.style.fontSize = 18;
                timerLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                container.Add(timerLbl);
            }
        }

        private System.Collections.IEnumerator SoloTimerTick()
        {
            while (true)
            {
                yield return new UnityEngine.WaitForSeconds(1f);
                if (_soloMode == SoloMode.TimeRush)
                {
                    _soloTimer--;
                    UpdateSoloStatusUI();
                    if (_soloTimer <= 0) break;
                }
                else if (_soloMode == SoloMode.Quick10)
                {
                    _soloTimer++;
                    UpdateSoloStatusUI();
                }
                else break;
            }
            if (_soloMode == SoloMode.TimeRush && _soloTimer <= 0) FinishSoloQuiz();
        }

        private void StopSoloTimer()
        {
            if (_soloTimerCoroutine != null) StopCoroutine(_soloTimerCoroutine);
        }

        private void NextSoloQuestion()
        {
            if (_soloMode == SoloMode.Quick10 && _soloQuestionCount >= 10)
            {
                FinishSoloQuiz();
                return;
            }

            _soloQuestionCount++;
            UpdateSoloStatusUI();

            var availableWords = _jsonDb.words.Where(w => !_soloUsedWordIds.Contains(w.id)).ToList();
            if (availableWords.Count == 0)
            {
                _soloUsedWordIds.Clear();
                availableWords = _jsonDb.words;
            }

            _battleCurrentWord = availableWords[UnityEngine.Random.Range(0, availableWords.Count)];
            _soloUsedWordIds.Add(_battleCurrentWord.id);

            SetupSoloQuestionUI();
        }

        private void NextReviewQuestion()
        {
            if (_reviewCurrentIndex >= _reviewPool.Count)
            {
                FinishReviewQuiz();
                return;
            }

            _battleCurrentWord = _reviewPool[_reviewCurrentIndex];
            _reviewCurrentIndex++;
            UpdateSoloStatusUI();

            SetupSoloQuestionUI();
        }

        private void SetupSoloQuestionUI()
        {
            _isImageMode = UnityEngine.Random.value > 0.5f && !string.IsNullOrEmpty(_battleCurrentWord.imageUrl);
            _root.Q<Label>("SoloQuestionText").text = _isImageMode ? (_battleCurrentWord.imageSub ?? "What is this?") : _battleCurrentWord.word;
            
            VisualElement imgCont = _root.Q<VisualElement>("SoloImageContainer");
            VisualElement imgElem = _root.Q<VisualElement>("SoloQuestionImage");
            if (_isImageMode)
            {
                imgCont.style.display = DisplayStyle.Flex;
                StartCoroutine(DownloadAndSetImage(_battleCurrentWord.imageUrl, imgElem));
            }
            else imgCont.style.display = DisplayStyle.None;

            string correctAns = _isImageMode ? _battleCurrentWord.word : _battleCurrentWord.meaning;
            List<string> options = new List<string> { correctAns };
            
            List<string> distPool = (_isReviewMode && _reviewPool.Count > 4) 
                ? (_isImageMode ? _reviewPool.Select(w => w.word).ToList() : _reviewPool.Select(w => w.meaning).ToList())
                : (_isImageMode ? _jsonDb.words.Select(w => w.word).ToList() : _jsonDb.words.Select(w => w.meaning).ToList());
            
            distPool.Remove(correctAns);
            for (int i = 0; i < 3; i++)
            {
                if (distPool.Count == 0) break;
                string dist = distPool[UnityEngine.Random.Range(0, distPool.Count)];
                options.Add(dist);
                distPool.Remove(dist);
            }

            options = options.OrderBy(x => UnityEngine.Random.value).ToList();

            for (int i = 0; i < 4; i++)
            {
                Button btn = _root.Q<Button>($"SoloAns{i}");
                if (btn != null)
                {
                    if (i < options.Count)
                    {
                        btn.style.display = DisplayStyle.Flex;
                        btn.text = options[i];
                        btn.SetEnabled(true);
                        btn.style.backgroundColor = new StyleColor(new Color(0.12f, 0.16f, 0.23f));
                    }
                    else btn.style.display = DisplayStyle.None;
                }
            }

            float progress = 0;
            if (_soloMode == SoloMode.Quick10 && !_isReviewMode) progress = (_soloQuestionCount / 10f) * 100f;
            else if (_soloMode == SoloMode.TimeRush) progress = (_soloTimer / 60f) * 100f;
            else if (_isReviewMode) progress = ((float)_reviewCurrentIndex / _reviewPool.Count) * 100f;
            _root.Q<VisualElement>("SoloProgressBar").style.width = new Length(progress, LengthUnit.Percent);
        }

        private System.Collections.IEnumerator HandleSoloAnswerSelection(Button btn, string ans)
        {
            string correctAns = _isImageMode ? _battleCurrentWord.word : _battleCurrentWord.meaning;
            bool isCorrect = (ans == correctAns);

            for (int i = 0; i < 4; i++) _root.Q<Button>($"SoloAns{i}")?.SetEnabled(false);

            if (isCorrect)
            {
                btn.style.backgroundColor = Color.green;
                if (!_isReviewMode)
                {
                    _soloScore++;
                    _root.Q<Label>("SoloScore").text = $"Score: {_soloScore}";
                }
            }
            else
            {
                btn.style.backgroundColor = Color.red;
                if (!_isReviewMode)
                {
                    _soloHearts--;
                    UpdateSoloStatusUI();
                }
            }

            _soloRoundRecords.Add(new VocabLearning.Data.BattleRoundRecord
            {
                question = _isImageMode ? (_battleCurrentWord.imageSub ?? _battleCurrentWord.word) : _battleCurrentWord.word,
                correctAnswer = correctAns,
                playerAnswer = ans,
                isCorrect = isCorrect,
                imageUrl = _isImageMode ? _battleCurrentWord.imageUrl : null
            });

            yield return new UnityEngine.WaitForSeconds(1.0f);

            if (!_isReviewMode && (_soloHearts <= 0 || (_soloMode == SoloMode.Quick10 && _soloQuestionCount >= 10))) FinishSoloQuiz();
            else if (_isReviewMode && _reviewCurrentIndex >= _reviewPool.Count) FinishReviewQuiz();
            else if (_isReviewMode) NextReviewQuestion();
            else NextSoloQuestion();
        }

        private void FinishSoloQuiz()
        {
            StopSoloTimer();
            _root.Q<VisualElement>("SoloGameplayOverlay").style.display = DisplayStyle.None;

            var user = _jsonDb.currentUser;
            if (_soloMode == SoloMode.Survivor && _soloScore > user.bestSurvivor) user.bestSurvivor = _soloScore;
            if (_soloMode == SoloMode.Quick10 && _soloScore > user.bestQuick10) user.bestQuick10 = _soloScore;
            if (_soloMode == SoloMode.TimeRush && _soloScore > user.bestTimeRush) user.bestTimeRush = _soloScore;

            int exp = _soloScore * 2;
            int coins = _soloScore;
            user.exp += exp;
            user.coins += coins;

            _lastBattleRecord = new VocabLearning.Data.BattleHistoryRecord
            {
                opponentName = "Solo Quiz - " + _soloMode.ToString(),
                date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                isWin = true,
                correctCount = _soloScore,
                totalRounds = _soloRoundRecords.Count,
                rounds = new List<VocabLearning.Data.BattleRoundRecord>(_soloRoundRecords)
            };

            ShowBattleSummaryOverlay(() =>
            {
                VisualElement resOverlay = _root.Q<VisualElement>("ResultOverlay");
                if (resOverlay != null) resOverlay.style.display = DisplayStyle.Flex;

                if (_root.Q<Label>("ResultTitle") != null)
                {
                    _root.Q<Label>("ResultTitle").text = "QUIZ FINISHED!";
                    _root.Q<Label>("ResultTitle").style.color = new StyleColor(new Color(0.39f, 0.40f, 0.95f)); // Purple-ish
                }
                if (_root.Q<Label>("ResultSubtext") != null) _root.Q<Label>("ResultSubtext").text = $"Mode: {_soloMode} • Score: {_soloScore}";
                if (_root.Q<Label>("ResultExp") != null) _root.Q<Label>("ResultExp").text = $"+{exp} EXP / +{coins} Coins";
                if (_root.Q<Label>("ResultIcon") != null) _root.Q<Label>("ResultIcon").text = "🏁";

                Button btnLeave = _root.Q<Button>("BtnLeaveBattle");
                if (btnLeave != null)
                {
                    btnLeave.clicked += () =>
                    {
                        // Refresh HUB before returning
                        RefreshSoloHubUI();
                        // Hide all overlays
                        _root.Q<VisualElement>("ResultOverlay").style.display = DisplayStyle.None;
                    };
                }
            });
        }

        private void FinishReviewQuiz()
        {
            _root.Q<VisualElement>("SoloGameplayOverlay").style.display = DisplayStyle.None;
            int correctCount = _soloRoundRecords.Count(r => r.isCorrect);

            _lastBattleRecord = new VocabLearning.Data.BattleHistoryRecord
            {
                opponentName = "Review: " + (_currentVocabSet?.title ?? "Set"),
                date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                isWin = true,
                correctCount = correctCount,
                totalRounds = _soloRoundRecords.Count,
                rounds = new List<VocabLearning.Data.BattleRoundRecord>(_soloRoundRecords)
            };

            ShowBattleSummaryOverlay(() => 
            {
                VisualElement resOverlay = _root.Q<VisualElement>("ResultOverlay");
                if (resOverlay != null) resOverlay.style.display = DisplayStyle.Flex;

                if (_root.Q<Label>("ResultTitle") != null)
                {
                    _root.Q<Label>("ResultTitle").text = "REVIEW FINISHED!";
                    _root.Q<Label>("ResultTitle").style.color = new StyleColor(new Color(0.54f, 0.45f, 0.94f));
                }
                if (_root.Q<Label>("ResultSubtext") != null) _root.Q<Label>("ResultSubtext").text = $"Score: {correctCount}/{_soloRoundRecords.Count}";
                if (_root.Q<Label>("ResultExp") != null) _root.Q<Label>("ResultExp").text = "Nice practice!";
                if (_root.Q<Label>("ResultIcon") != null) _root.Q<Label>("ResultIcon").text = "📚";

                Button btnLeave = _root.Q<Button>("BtnLeaveBattle");
                if (btnLeave != null)
                {
                    btnLeave.clicked += () => 
                    {
                        _isReviewMode = false;
                        _root.Q<VisualElement>("ResultOverlay").style.display = DisplayStyle.None;
                        // Quay lại màn hình chi tiết bộ từ thay vì ở lại Hub Quiz
                        LoadScreen(VocabDetailScreenAsset);
                    };
                }
            });
        }
    }
}
