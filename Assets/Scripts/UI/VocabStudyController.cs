using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Linq;

namespace VocabLearning.UI
{
    [RequireComponent(typeof(UIDocument))]
    public partial class VocabStudyController : MonoBehaviour
    {
        [Header("UI Templates (UXML)")]
        public VisualTreeAsset AuthScreenAsset; // NEW: Màn hình Đăng nhập/Đăng ký
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
        public VisualTreeAsset SettingScreenAsset;
        public VisualTreeAsset WordScrambleScreenAsset;

        [Header("Remaining Features")]
        public VisualTreeAsset ShopScreenAsset;
        public VisualTreeAsset RankingScreenAsset;
        public VisualTreeAsset ProfileScreenAsset;
        public VisualTreeAsset ResultScreenAsset;
        public VisualTreeAsset AchievementScreenAsset;
        public VisualTreeAsset InventoryScreenAsset;
       
        [Header("Admin Features")]
        public VisualTreeAsset AdminScreenAsset;
        public VisualTreeAsset VocabAdminScreenAsset;
        public VisualTreeAsset QuestAdminScreenAsset;
        public VisualTreeAsset VocabListAdminScreenAsset;
        public VisualTreeAsset AchievementAdminScreenAsset;
        public VisualTreeAsset UserAdminScreenAsset;

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
        private bool _battleShieldActive = false;

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
        private bool _isScrambleMode = false;
        private bool _isInteractiveScramble = false;
        private bool _isBattleScrambleChecking = false;
        private System.Collections.Generic.List<Button> _battleScrambleSlots = new System.Collections.Generic.List<Button>();
        private System.Collections.Generic.List<Button> _battleScrambleLetterButtons = new System.Collections.Generic.List<Button>();
        private Button[] _battleScrambleSelectedButtons;
        private string _battleScrambleTargetWord = "";
        private bool _isScrambleMinigameResult = false;

        // --- Current Study Set Words ---
        private System.Collections.Generic.List<VocabLearning.Data.WordJson> _currentVocabSetWords = new System.Collections.Generic.List<VocabLearning.Data.WordJson>();

        private void OnEnable()
        {
            _doc = GetComponent<UIDocument>();
            LoadJsonDatabase();

            // Khởi chạy AuthScreen mặc định (Bắt buộc đè lên giá trị có sẵn trong Inspector)
            if (AuthScreenAsset != null)
            {
                _doc.visualTreeAsset = AuthScreenAsset;
            }
            else if (HomeScreenAsset != null)
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

            // Tự động phát âm thanh click 
            _root.RegisterCallback<ClickEvent>(evt => 
            {
                VisualElement el = evt.target as VisualElement;
                while (el != null)
                {
                    if (el is Button ||
                    el.ClassListContains("card") || el.name == "VocabSetFeaturedCard")
                    {
                       
                        if (el.name != null && el.name.StartsWith("BtnAns"))
                        {
                            break; 
                        }
                        
                        SoundManager.PlayClick();
                        break; 
                    }
                    el = el.parent; 
                }
            }, TrickleDown.TrickleDown);

            BindCurrentEvents();
        }

        // --- Hàm Quản Lý Chuyển Màn Hình ---
        private void LoadScreen(VisualTreeAsset newScreenAsset)
        {
            if (newScreenAsset == null) return;

            if (newScreenAsset != ResultScreenAsset)
            {
                _isScrambleMinigameResult = false;
            }

            // Chuyển nhạc nền phù hợp với màn hình
            if (newScreenAsset == BattleGameplayScreenAsset)
            {
                BackgroundMusic.PlayBattle();
            }
            else
            {
                BackgroundMusic.PlayBGM();
            }

            _root.Clear();
            newScreenAsset.CloneTree(_root);
            BindCurrentEvents(newScreenAsset);
        }

        private void BindCurrentEvents(VisualTreeAsset targetAsset = null)
        {
            if (_root == null) return;

            // Nếu không được truyền vào, thử lấy từ assigned UIDocument (lúc vừa mở lên)
            if (targetAsset == null) targetAsset = _doc.visualTreeAsset;

            if (targetAsset == AuthScreenAsset) BindAuthEvents();
            else if (targetAsset == HomeScreenAsset) BindHomeEvents();
            else if (targetAsset == VocabDetailScreenAsset) BindDetailEvents();
            else if (targetAsset == PracticeModeScreenAsset) BindPracticeEvents();
            else if (targetAsset == QuestScreenAsset) BindQuestEvents();
            else if (targetAsset == BattleScreenAsset) BindBattleEvents();
            else if (targetAsset == BattleLoadoutScreenAsset) BindBattleLoadoutEvents();
            else if (targetAsset == BattleGameplayScreenAsset) BindBattleGameplayEvents();
            else if (targetAsset == FriendScreenAsset) BindFriendEvents();
            else if (targetAsset == SoloQuizScreenAsset) BindSoloQuizEvents();
            else if (targetAsset == WordScrambleScreenAsset) BindWordScrambleEvents();
            else if (targetAsset == ShopScreenAsset) BindShopEvents();
            else if (targetAsset == RankingScreenAsset) BindRankingEvents();
            else if (targetAsset == ProfileScreenAsset) BindProfileEvents();
            else if (targetAsset == ResultScreenAsset) BindResultEvents();
            else if (targetAsset == AchievementScreenAsset) BindAchievementEvents();
            else if (targetAsset == InventoryScreenAsset) BindInventoryEvents();
            else if (targetAsset == AdminScreenAsset) BindAdminEvents();
            else if (targetAsset == VocabAdminScreenAsset) BindAdminVocabEvents();
            else if (targetAsset == QuestAdminScreenAsset) BindAdminQuestEvents();
            else if (targetAsset == VocabListAdminScreenAsset) BindAdminVocabListEvents();
            else if (targetAsset == AchievementAdminScreenAsset) BindAdminAchievementEvents();
            else if (targetAsset == UserAdminScreenAsset) BindAdminUserEvents();
            else BindOtherScreens();
        }

        // --- QUẢN LÝ DỮ LIỆU JSON ---
        private void AutoPartitionSetLevels(VocabLearning.Data.VocabSetJson set)
        {
            if (set == null || set.wordIds == null || set.wordIds.Count == 0 || _jsonDb == null || _jsonDb.words == null) return;

            string setDiff = set.difficulty ?? "Easy";

            if (setDiff.Equals("Multi-Level", System.StringComparison.OrdinalIgnoreCase))
            {
                var levelEasy = new VocabLearning.Data.VocabLevelJson { difficulty = "Easy", wordIds = new List<string>() };
                var levelMedium = new VocabLearning.Data.VocabLevelJson { difficulty = "Medium", wordIds = new List<string>() };
                var levelHard = new VocabLearning.Data.VocabLevelJson { difficulty = "Hard", wordIds = new List<string>() };

                foreach (var id in set.wordIds)
                {
                    var w = _jsonDb.words.Find(x => x.id == id);
                    string rankKey = w != null && !string.IsNullOrEmpty(w.rankRequired) ? w.rankRequired.Trim().ToLower() : "dong";
                    
                    if (rankKey == "dong" || rankKey == "bac")
                    {
                        levelEasy.wordIds.Add(id);
                    }
                    else if (rankKey == "vang" || rankKey == "bachkim")
                    {
                        levelMedium.wordIds.Add(id);
                    }
                    else if (rankKey == "kimcuong" || rankKey == "sieucap")
                    {
                        levelHard.wordIds.Add(id);
                    }
                    else
                    {
                        levelEasy.wordIds.Add(id);
                    }
                }

                var autoGeneratedLevels = new List<VocabLearning.Data.VocabLevelJson>();
                if (levelEasy.wordIds.Count > 0) autoGeneratedLevels.Add(levelEasy);
                if (levelMedium.wordIds.Count > 0) autoGeneratedLevels.Add(levelMedium);
                if (levelHard.wordIds.Count > 0) autoGeneratedLevels.Add(levelHard);

                set.levels = autoGeneratedLevels;
            }
            else
            {
                // Single-level set: Put all words into a single level matching set.difficulty
                var singleLevel = new VocabLearning.Data.VocabLevelJson { difficulty = setDiff, wordIds = new List<string>(set.wordIds) };
                set.levels = new List<VocabLearning.Data.VocabLevelJson> { singleLevel };
            }
        }

        private void LoadJsonDatabase()
        {
            string localSavePath = System.IO.Path.Combine(Application.persistentDataPath, "local_db.json");
            bool loadedFromLocalFile = false;

            // 1. Cố gắng tải dữ liệu offline từ persistentDataPath (Cho bản Build chạy thật)
            if (System.IO.File.Exists(localSavePath))
            {
                try
                {
                    string fileContent = System.IO.File.ReadAllText(localSavePath);
                    _jsonDb = JsonUtility.FromJson<VocabLearning.Data.MockDatabase>(fileContent);
                    if (_jsonDb != null)
                    {
                        loadedFromLocalFile = true;
                        VocabLearning.Network.NetworkClient.Instance.JwtToken = _jsonDb.jwtToken; // Nạp lại token đã lưu từ tệp đĩa
                        Debug.Log($"[JSON DB] Đã tải Database offline từ tệp cục bộ (persistentDataPath): {localSavePath}. Số lượng bộ từ vựng: {_jsonDb.vocabSets.Count}");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[JSON DB] Lỗi tải database offline từ tệp cục bộ: {ex.Message}");
                }
            }

            // 2. Fallback tải từ Resources (Chế độ mặc định lần đầu hoặc lỗi)
            if (!loadedFromLocalFile)
            {
                TextAsset jsonAsset = OverrideJsonDb != null ? OverrideJsonDb : Resources.Load<TextAsset>("Mockdata/db");
                if (jsonAsset != null)
                {
                    _jsonDb = JsonUtility.FromJson<VocabLearning.Data.MockDatabase>(jsonAsset.text);
                    if (_jsonDb != null)
                    {
                        VocabLearning.Network.NetworkClient.Instance.JwtToken = _jsonDb.jwtToken; // Nạp lại token đã lưu từ tệp đĩa fallback
                    }
                    Debug.Log($"[JSON DB] Đã tải Database offline từ Resources fallback. Số lượng bộ từ vựng: {_jsonDb.vocabSets.Count}");
                }
                else
                {
                    Debug.LogWarning("[JSON DB] Không tìm thấy file Resources/Mockdata/db.json để làm dữ liệu offline dự phòng.");
                    _jsonDb = new VocabLearning.Data.MockDatabase();
                }
            }

            // BẮT BUỘC KHÔNG TỰ ĐỘNG ĐĂNG NHẬP MẪU: Khi khởi động game, người dùng chưa đăng nhập,
            // nên ta bắt buộc phải xóa sạch thông tin currentUser và token để họ đăng nhập thủ công trên AuthScreen
            if (_jsonDb != null)
            {
                _jsonDb.currentUser = null;
                _jsonDb.jwtToken = "";
                VocabLearning.Network.NetworkClient.Instance.JwtToken = "";
            }

            // Tự động phân chia levels cho tất cả các bộ từ vựng theo rank để đảm bảo tính đồng nhất
            if (_jsonDb != null && _jsonDb.vocabSets != null && _jsonDb.words != null)
            {
                foreach (var set in _jsonDb.vocabSets)
                {
                    AutoPartitionSetLevels(set);
                }
            }

            CheckLevelUp();

            // 3. Gọi API nạp dữ liệu toàn cục trực tiếp từ máy chủ Node.js SQL Server
            Debug.Log("[JSON DB - Network] Đang nạp dữ liệu game toàn cục từ Node.js SQL Server...");
            VocabLearning.Network.NetworkClient.Instance.GetGlobals((success, message, data) =>
            {
                if (success && data != null && data.success)
                {
                    _jsonDb.words = data.words;
                    _jsonDb.vocabSets = data.vocabSets;
                    _jsonDb.achievements = data.achievements;
                    _jsonDb.questPool = data.questPool;
                    _jsonDb.shopItems = data.shopItems;
                    _jsonDb.leaderboardUsers = data.leaderboardUsers;

                    // Tự động phân cấp lại cho các sets nạp từ server
                    if (_jsonDb.vocabSets != null)
                    {
                        foreach (var set in _jsonDb.vocabSets)
                        {
                            AutoPartitionSetLevels(set);
                        }
                    }

                    Debug.Log($"[JSON DB - Network] Nạp dữ liệu toàn cục thành công từ Backend SQL Server. Số bộ từ vựng: {_jsonDb.vocabSets.Count}, Số từ vựng: {_jsonDb.words.Count}");

                    // ĐỂ CHẠY 100% TRÊN DB & KHÔNG BỊ GHI ĐÈ TIẾN TRÌNH: Tải trực tiếp thông tin/tiến trình mới nhất của user từ DB về Client
                    if (_jsonDb != null && _jsonDb.currentUser != null && !string.IsNullOrEmpty(_jsonDb.currentUser.id) && !string.IsNullOrEmpty(VocabLearning.Network.NetworkClient.Instance.JwtToken))
                    {
                        Debug.Log($"[JSON DB - Sync] Đang tải thông tin & tiến trình mới nhất của '{_jsonDb.currentUser.username}' từ SQL Server DB...");
                        VocabLearning.Network.NetworkClient.Instance.GetUserProfile((profileSuccess, profileMsg, profileData) =>
                        {
                            if (profileSuccess && profileData != null && profileData.user != null)
                            {
                                _jsonDb.currentUser = profileData.user;
                                if (profileData.user.quests != null) _jsonDb.quests = profileData.user.quests;
                                if (profileData.user.achievements != null) _jsonDb.achievements = profileData.user.achievements;
                                if (profileData.user.inventory != null)
                                {
                                    _jsonDb.inventory = profileData.user.inventory;
                                }
                                
                                // Ghi đè cập nhật xuống file đĩa local để đồng bộ offline
                                try
                                {
                                    string localSavePath2 = System.IO.Path.Combine(Application.persistentDataPath, "local_db.json");
                                    string json = JsonUtility.ToJson(_jsonDb, true);
                                    System.IO.File.WriteAllText(localSavePath2, json);
                                }
                                catch (System.Exception ex)
                                {
                                    Debug.LogError($"[JSON DB] Lỗi cập nhật cache cục bộ sau khi tải profile từ DB: {ex.Message}");
                                }

                                CheckLevelUp();
                                Debug.Log($"[JSON DB - Sync] Tải tiến trình thành công từ DB. Cấp độ: {_jsonDb.currentUser.level}, Vàng: {_jsonDb.currentUser.coins}");
                            }
                            else
                            {
                                Debug.LogWarning($"[JSON DB - Sync] Không thể tải profile từ DB ({profileMsg}). Sử dụng tạm dữ liệu cache.");
                            }
                        });
                    }
                }
                else
                {
                    Debug.LogWarning($"[JSON DB - Network] Không thể nạp dữ liệu từ server ({message}). Sử dụng dữ liệu offline fallback.");
                }
            });
        }

        private void CheckDailyQuests()
        {
            if (_jsonDb == null || _jsonDb.currentUser == null || _jsonDb.questPool == null) return;

            string today = System.DateTime.Now.ToString("yyyy-MM-dd");
            var user = _jsonDb.currentUser;
            if (user.weeklyLogin == null) user.weeklyLogin = new VocabLearning.Data.WeeklyLoginData();

            // Nếu hôm nay chưa có trong danh sách ngày điểm danh -> Ngày mới!
            if (!user.weeklyLogin.loginDates.Contains(today))
            {
                Debug.Log("🌞 New day detected! Refreshing daily quests...");
                RefreshQuests();
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
                SaveJsonDatabase();
            }

            // Nếu đủ 5 ngày và chưa nhận quà -> Thưởng nóng
            if (user.weeklyLogin.loginDates.Count >= 5 && !user.weeklyLogin.isRewardClaimed)
            {
                user.weeklyLogin.isRewardClaimed = true;
                user.coins += 1000; // Thưởng lớn cho việc chuyên cần
                AddQuestProgressByType("CollectCoin", 1000);
                user.exp += 2000;
                Debug.Log("🎁 SIÊU CẤP CHUYÊN CẦN: Bạn đã nhận được 1000 Coins và 2000 EXP cho việc đăng nhập 5 ngày trong tuần!");
                SaveJsonDatabase();
                UpdateAllCoinLabels();
            }
        }

        // --- BINDING CHO HOME SCREEN ---
        private void BindHomeEvents()
        {
            // -- Đổ dữ liệu Username và Vàng từ JSON --
            if (_jsonDb != null && _jsonDb.currentUser != null)
            {
                Label lblNameFind = _root.Q<Label>("LblUserName");
                if (lblNameFind != null) lblNameFind.text = string.IsNullOrEmpty(_jsonDb.currentUser.displayName) ? _jsonDb.currentUser.username : _jsonDb.currentUser.displayName;

                Label lblLevelFind = _root.Q<Label>("LblUserLevel");
                if (lblLevelFind != null) lblLevelFind.text = $"★ Level {_jsonDb.currentUser.level}";

                // Cập nhật thông số EXP & Thanh Progress Bar động (Luỹ tiến nhân đôi)
                int lvl, curLevelExp, nextLevelExpNeeded;
                GetExpDetails(_jsonDb.currentUser.exp, out lvl, out curLevelExp, out nextLevelExpNeeded);
                float expPercent = (curLevelExp / (float)nextLevelExpNeeded) * 100f;

                Label lblHomeExp = _root.Q<Label>("LblHomeExp");
                if (lblHomeExp != null) lblHomeExp.text = $"{curLevelExp} / {nextLevelExpNeeded} EXP";

                VisualElement homeExpFill = _root.Q<VisualElement>("HomeExpFill");
                if (homeExpFill != null) homeExpFill.style.width = Length.Percent(expPercent);

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

            // -- Nút Admin trong bottom nav (chỉ hiển thị nếu role == "admin") --
            Button navAdmin = _root.Q<Button>("NavAdmin");
            if (navAdmin != null)
            {
                if (_jsonDb?.currentUser?.role == "admin" && AdminScreenAsset != null)
                {
                    navAdmin.style.display = DisplayStyle.Flex;
                    navAdmin.clicked += () =>
                    {
                        Debug.Log("[Home - NavAdmin] Chuyển đến trang Quản lý Admin.");
                        LoadScreen(AdminScreenAsset);
                    };
                }
                else
                {
                    navAdmin.style.display = DisplayStyle.None;
                }
            }

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
            if (actionFriends != null)
            {
                actionFriends.style.display = DisplayStyle.None;
            }

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
                if (lblFeaturedCount != null)
                {
                    int wc = (featuredSet.wordCount > 0) ? featuredSet.wordCount : (featuredSet.wordIds != null ? featuredSet.wordIds.Count : 0);
                    lblFeaturedCount.text = $"{wc} words";
                }

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
                        string displayCountText = $"{(set.wordCount > 0 ? set.wordCount : (set.wordIds != null ? set.wordIds.Count : 0))} words";

                        if (set.levels != null && set.levels.Count > 1)
                        {
                            displayDiff = "Multi-Level";
                            displayCountText = $"{set.levels.Count} Levels";
                        }
                        else if (set.levels != null && set.levels.Count == 1)
                        {
                            displayDiff = set.levels[0].difficulty;
                            displayCountText = $"{set.levels[0].wordIds.Count} words";
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
                        Color diffColor = new Color(0.55f, 0.36f, 0.96f); // Default Purple/Multi-Level
                        string diffLower = (displayDiff ?? "").ToLower().Trim();
                        if (diffLower == "easy" || diffLower == "dễ" || diffLower == "de") 
                            diffColor = new Color(0.10f, 0.73f, 0.51f); // Emerald
                        else if (diffLower == "medium" || diffLower == "trung bình" || diffLower == "trung binh" || diffLower == "normal") 
                            diffColor = new Color(0.23f, 0.51f, 0.96f); // Blue
                        else if (diffLower == "hard" || diffLower == "khó" || diffLower == "kho") 
                            diffColor = new Color(0.93f, 0.26f, 0.26f); // Red

                        diffLbl.style.backgroundColor = new StyleColor(new Color(diffColor.r, diffColor.g, diffColor.b, 0.2f));
                        diffLbl.style.color = new StyleColor(new Color(diffColor.r, diffColor.g, diffColor.b, 1f));
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

            bool progressChanged = false;
            // Tìm tất cả nhiệm vụ có cùng loại (hỗ trợ nhiều nhiệm vụ cùng trigger)
            var targetQuests = _jsonDb.quests.FindAll(q => q.questType == questType);
            foreach (var quest in targetQuests)
            {
                if (quest != null && !quest.isClaimed && quest.currentProgress < quest.maxProgress)
                {
                    quest.currentProgress += amount;
                    if (quest.currentProgress > quest.maxProgress)
                        quest.currentProgress = quest.maxProgress;
                    progressChanged = true;
                }
            }

            if (progressChanged)
            {
                SaveJsonDatabase();
            }
        }

        private void UpdateAllCoinLabels()
        {
            if (_jsonDb == null || _jsonDb.currentUser == null) return;
            string coinsStr = _jsonDb.currentUser.coins.ToString();

            Label lblCoinsFind = _root.Q<Label>("LblUserCoins");
            if (lblCoinsFind != null) lblCoinsFind.text = coinsStr;

            Label coinLabel = _root.Q<Label>("LblShopCoinBalance");
            if (coinLabel != null) coinLabel.text = coinsStr;

            Label lblCoins = _root.Q<Label>("LblProfileCoins");
            if (lblCoins != null) lblCoins.text = coinsStr;
        }

        // --- HÀM LƯU & TÍNH EXP ĐỒNG BỘ ĐÃ TRUNG TÂM HOÁ ---
        private void SaveJsonDatabase()
        {
            CheckLevelUp();
            if (_jsonDb != null && _jsonDb.currentUser != null)
            {
                _jsonDb.currentUser.inventory = _jsonDb.inventory;
                _jsonDb.currentUser.quests = _jsonDb.quests;
                _jsonDb.currentUser.achievements = _jsonDb.achievements;
            }
            // 1. Luôn lưu dữ liệu cục bộ bền vững trên mọi thiết bị (Editor & Bản Build chạy thật)
            try
            {
                string localSavePath = System.IO.Path.Combine(Application.persistentDataPath, "local_db.json");
                string json = JsonUtility.ToJson(_jsonDb, true);
                System.IO.File.WriteAllText(localSavePath, json);
                Debug.Log($"[JSON DB - Persistent] Đã lưu tiến trình cục bộ thành công: {localSavePath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[JSON DB - Persistent] Lỗi lưu database cục bộ: {ex.Message}");
            }

            // 2. Đồng bộ dữ liệu tiến trình người dùng lên SQL Server thông qua Node.js Backend API
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

        public static int CalculateLevel(int totalExp)
        {
            if (totalExp <= 0) return 1;
            return (totalExp / 1000) + 1;
        }

        public static int GetExpNeededForLevel(int level)
        {
            return 1000;
        }

        public static int GetCumulativeExpForLevel(int level)
        {
            if (level <= 1) return 0;
            return (level - 1) * 1000;
        }

        public static void GetExpDetails(int totalExp, out int level, out int curLevelExp, out int nextLevelExpNeeded)
        {
            level = CalculateLevel(totalExp);
            curLevelExp = totalExp >= 0 ? (totalExp % 1000) : 0;
            nextLevelExpNeeded = 1000;
        }

        private void CheckLevelUp()
        {
            if (_jsonDb == null || _jsonDb.currentUser == null) return;
            var user = _jsonDb.currentUser;

            int targetLevel = CalculateLevel(user.exp);
            if (user.level != targetLevel)
            {
                int oldLevel = user.level;
                user.level = targetLevel;
                if (oldLevel > 0 && targetLevel > oldLevel)
                {
                    Debug.Log($"🎉 CHÚC MỪNG! Bạn đã thăng cấp từ {oldLevel} lên cấp {user.level}!");
                    SoundManager.PlayAchievement();
                }
            }
        }
    }
}
