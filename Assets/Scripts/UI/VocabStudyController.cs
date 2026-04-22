using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

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

            // -- Các nút tính năng chính (Quest, Battle, Friends) --
            Button actionQuest = _root.Q<Button>("ActionQuest");
            if (actionQuest != null) actionQuest.clicked += () => LoadScreen(QuestScreenAsset);

            Button actionBattle = _root.Q<Button>("ActionBattle");
            if (actionBattle != null) actionBattle.clicked += () => LoadScreen(BattleScreenAsset);

            Button actionFriends = _root.Q<Button>("ActionFriends");
            if (actionFriends != null) actionFriends.clicked += () => LoadScreen(FriendScreenAsset);

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

                        Label countLbl = new Label($"{set.wordCount} words");
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

                        Label diffLbl = new Label(set.difficulty);
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
                        tagsGroup.Add(diffLbl);

                        // [NEW] Hiển thị nhãn Đã Học (Learned) nếu User đã hoàn thành
                        if (_jsonDb.currentUser != null && _jsonDb.currentUser.learnedSets != null && _jsonDb.currentUser.learnedSets.Contains(set.id))
                        {
                            Label learnedBadge = new Label("✔ Learned");
                            learnedBadge.style.backgroundColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f, 0.2f)); // Emerald-500 tint
                            learnedBadge.style.color = new StyleColor(new Color(0.06f, 0.73f, 0.51f, 1f));
                            learnedBadge.style.paddingTop = 4;
                            learnedBadge.style.paddingBottom = 4;
                            learnedBadge.style.paddingLeft = 8;
                            learnedBadge.style.paddingRight = 8;
                            learnedBadge.style.borderTopLeftRadius = 8;
                            learnedBadge.style.borderTopRightRadius = 8;
                            learnedBadge.style.borderBottomLeftRadius = 8;
                            learnedBadge.style.borderBottomRightRadius = 8;
                            learnedBadge.style.fontSize = 12;
                            tagsGroup.Add(learnedBadge);
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
            if (set == null || set.wordIds == null) return;
            foreach (var id in set.wordIds)
            {
                var word = FindWordById(id);
                if (word != null) _currentVocabSetWords.Add(word);
            }
        }

        // --- BINDING CHO DETAIL SCREEN ---
        private void BindDetailEvents()
        {
            if (_currentVocabSet != null)
            {
                ResolveSetWords(_currentVocabSet);

                // Binding Dữ Liệu Động (Tiêu đề, Số từ, Level)
                Label lblTitle = _root.Q<Label>("LblVocabTitle");
                if (lblTitle != null) lblTitle.text = _currentVocabSet.title;

                Label lblDesc = _root.Q<Label>("LblVocabDesc");
                if (lblDesc != null) lblDesc.text = _currentVocabSet.description;

                Label lblCount = _root.Q<Label>("LblVocabCount");
                if (lblCount != null) lblCount.text = _currentVocabSet.wordCount.ToString();

                Label lblLevel = _root.Q<Label>("LblVocabLevel");
                if (lblLevel != null) lblLevel.text = _currentVocabSet.difficulty;

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
                        LoadScreen(PracticeModeScreenAsset);
                    }
                    else
                    {
                        Debug.LogWarning("Không thể bắt đầu Practice vì bộ từ vựng rỗng!");
                    }
                };
            }

            // Nút Solo Quiz 
            Button quizBtn = _root.Q<Button>(className: "mode-btn-quiz");
            if (quizBtn != null) quizBtn.clicked += () => Debug.Log("DetailScreen: Solo Quiz clicked!");

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
            if (btnClose != null) btnClose.clicked += () => LoadScreen(VocabDetailScreenAsset);

            VisualElement flashcard = _root.Q<VisualElement>("FlashcardContainer");
            Button btnPrev = _root.Q<Button>("BtnPracticePrev");
            Button btnNext = _root.Q<Button>("BtnPracticeNext");

            // Logic khi bấm vào thẻ để mở đáp án
            if (flashcard != null)
            {
                flashcard.RegisterCallback<ClickEvent>(evt =>
                {
                    _practiceShowMeaning = !_practiceShowMeaning;
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
                btnNext.clicked += () =>
                {
                    // Tăng tiến độ nhiệm vụ "Học 10 từ mới"
                    AddQuestProgressByType("LearnWord", 1);

                    if (_practiceCurrentIndex < _currentVocabSetWords.Count - 1)
                    {
                        _practiceCurrentIndex++;
                        _practiceShowMeaning = false;
                        UpdatePracticeUI();
                    }
                    else
                    {
                        // Hoàn thành bộ -> Tăng tiến độ nhiệm vụ "Đạt điểm tuyệt đối"
                        AddQuestProgressByType("PerfectPractice", 1);

                        // Từ cuối cùng -> Finish -> Ghi nhận Đã Học và chuyển sang Result
                        if (_jsonDb != null && _jsonDb.currentUser != null)
                        {
                            if (_jsonDb.currentUser.learnedSets == null)
                                _jsonDb.currentUser.learnedSets = new System.Collections.Generic.List<string>();

                            if (!_jsonDb.currentUser.learnedSets.Contains(_currentVocabSet.id))
                            {
                                _jsonDb.currentUser.learnedSets.Add(_currentVocabSet.id);
                                _jsonDb.currentUser.coins += 50; // Thưởng coin tự do gập khuôn
                                _jsonDb.currentUser.exp += 100;
                            }
                        }
                        LoadScreen(ResultScreenAsset);
                    }
                };
            }

            // Lần đầu mở lên -> Vẽ UI
            UpdatePracticeUI();
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

            // Nghĩa và gợi ý
            VisualElement meaningBox = _root.Q<VisualElement>("MeaningBox");
            Label lblTapHint = _root.Q<Label>("LblTapHint");
            Label lblVietnamese = _root.Q<Label>("LblVietnamese");

            if (meaningBox != null && lblTapHint != null && lblVietnamese != null)
            {
                if (_practiceShowMeaning)
                {
                    meaningBox.style.display = DisplayStyle.Flex;
                    lblTapHint.style.display = DisplayStyle.None;
                    lblVietnamese.text = currentWord.meaning;
                }
                else
                {
                    meaningBox.style.display = DisplayStyle.None;
                    lblTapHint.style.display = DisplayStyle.Flex;
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
                    btnNext.style.backgroundColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f, 1f)); // Trả về xanh lục của primary
                }
            }
        }

        // --- BINDING CHO RESULT SCREEN ---
        private void BindResultEvents()
        {
            Button btnHome = _root.Q<Button>("BtnResultHome");
            if (btnHome != null) btnHome.clicked += () => LoadScreen(HomeScreenAsset);

            Button btnReplay = _root.Q<Button>("BtnResultReplay");
            if (btnReplay != null) btnReplay.clicked += () =>
            {
                _practiceCurrentIndex = 0;
                _practiceShowMeaning = false;
                LoadScreen(PracticeModeScreenAsset);
            };
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

        // --- Battle Word Pool (Rank-based, No Repeat) ---
        // Dùng bảng words trung tâm, lọc theo rankRequired của từng từ
        private System.Collections.Generic.List<VocabLearning.Data.WordJson> _battleWordPool = new System.Collections.Generic.List<VocabLearning.Data.WordJson>();
        private System.Collections.Generic.List<string> _battleAllPoolMeanings = new System.Collections.Generic.List<string>();
        private int _battlePoolIndex = 0;

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
            if (btnFlee != null) btnFlee.clicked += () => LoadScreen(BattleScreenAsset);

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
                Debug.Log("❌ Opponent was FASTER! You -10 HP");
            }
            else
            {
                // Timeout or both wrong
                bool playerWrong = _playerAnswered && (_playerAnswerText != _battleCurrentWord.meaning);
                bool aiWrong = _aiAnswered && !_aiCorrect;
                bool timeout = !(_playerAnswered || _aiAnswered);

                if (playerWrong || timeout)
                {
                    if (_battleShieldActive && !aiWrong)
                    {
                        _battleShieldActive = false;
                    }
                    else
                    {
                        _battlePlayerHP -= 10;
                    }
                }

                if (aiWrong || timeout)
                {
                    _battleEnemyHP -= 10;
                }

                Debug.Log("⌛ Round Ended (Timeout/Both Wrong). Both -10 HP");
            }

            if (_battlePlayerHP < 0) _battlePlayerHP = 0;
            if (_battleEnemyHP < 0) _battleEnemyHP = 0;

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

            Label qText = _root.Q<Label>("BattleQuestionText");
            if (qText != null) qText.text = _battleCurrentWord.word;

            // Tạo distractors từ toàn bộ pool meanings (không phải chỉ trong 1 bộ)
            System.Collections.Generic.List<string> options = new System.Collections.Generic.List<string> { _battleCurrentWord.meaning };

            // Tạo danh sách meanings có thể dùng làm distractor (loại đáp án đúng)
            System.Collections.Generic.List<string> distractorPool = new System.Collections.Generic.List<string>(_battleAllPoolMeanings);
            distractorPool.Remove(_battleCurrentWord.meaning);

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

            if (answerText == _battleCurrentWord.meaning)
            {
                Debug.Log("✅ Player got it RIGHT first!");
                ResolveRound("Player");
            }
            else
            {
                Debug.Log("❌ Player chose WRONG answer. Waiting for AI or Timer...");
            }
        }

        private void FinishBattle(bool isWin)
        {
            var curUser = _jsonDb.currentUser;
            curUser.totalGames++;

            VisualElement overlay = _root.Q<VisualElement>("ResultOverlay");
            Label title = _root.Q<Label>("ResultTitle");
            Label subtext = _root.Q<Label>("ResultSubtext");
            Label expText = _root.Q<Label>("ResultExp");

            if (isWin)
            {
                curUser.wins++;
                title.text = "VICTORY!";
                title.style.color = new StyleColor(new Color(0.10f, 0.73f, 0.51f)); // Green

                if (_isRankedBattle)
                {
                    curUser.rankPoints += 25;
                    curUser.exp += 50;
                    curUser.coins += 50;

                    // Thưởng Mystery Box cho trận Ranked
                    var mysteryBox = _jsonDb.inventory.Find(i => i.id == "4" || i.name == "Mystery Box");
                    if (mysteryBox != null)
                    {
                        mysteryBox.quantity++;
                        // Hiển thị box phần thưởng đặc biệt trên UI
                        VisualElement rewardBox = _root.Q<VisualElement>("ItemRewardBox");
                        if (rewardBox != null) rewardBox.style.display = DisplayStyle.Flex;
                    }

                    // Tăng tiến độ nhiệm vụ "Thắng 5 trận Ranked" (mới)
                    AddQuestProgressByType("WinRankedBattle", 1);

                    subtext.text = "+25 Rank Points";
                    expText.text = "+50 EXP / +50 Coins";
                }
                else
                {
                    // Casual: Ẩn phần thưởng item
                    VisualElement rewardBox = _root.Q<VisualElement>("ItemRewardBox");
                    if (rewardBox != null) rewardBox.style.display = DisplayStyle.None;

                    curUser.exp += 30;
                    curUser.coins += 30;
                    subtext.text = "Casual Match";
                    expText.text = "+30 EXP / +30 Coins";
                }

                // Tăng tiến độ nhiệm vụ "Thắng 3 trận bất kỳ"
                AddQuestProgressByType("WinBattle", 1);
            }
            else
            {
                title.text = "DEFEAT";
                title.style.color = new StyleColor(new Color(0.93f, 0.26f, 0.26f)); // Red

                if (_isRankedBattle)
                {
                    curUser.rankPoints -= 15;
                    if (curUser.rankPoints < 0) curUser.rankPoints = 0;
                    curUser.exp += 15;
                    subtext.text = "-15 Rank Points";
                    expText.text = "+15 EXP";
                }
                else
                {
                    curUser.exp += 10;
                    subtext.text = "Casual Match";
                    expText.text = "+10 EXP";
                }
            }

            // Changes exist in memory for this session
            if (overlay != null) overlay.style.display = DisplayStyle.Flex;

            Button btnLeave = _root.Q<Button>("BtnLeaveBattle");
            if (btnLeave != null)
            {
                btnLeave.clicked += () => LoadScreen(BattleScreenAsset);
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
    }
}
