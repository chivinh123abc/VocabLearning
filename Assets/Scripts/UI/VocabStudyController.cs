using UnityEngine;
using UnityEngine.UIElements;

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
        public VisualTreeAsset FriendScreenAsset;

        [Header("Remaining Features")]
        public VisualTreeAsset ShopScreenAsset;
        public VisualTreeAsset RankingScreenAsset;
        public VisualTreeAsset ProfileScreenAsset;
        public VisualTreeAsset ResultScreenAsset;

        [Header("Databases (JSON)")]
        public TextAsset OverrideJsonDb; // Cửa hậu nếu muốn kéo thủ công từ Inspector (tùy chọn)

        private UIDocument _doc;
        private VisualElement _root;

        private VocabLearning.Data.MockDatabase _jsonDb;
        private VocabLearning.Data.VocabSetJson _currentVocabSet;

        // --- STATE DÀNH CHO PRACTICE SCREEN ---
        private int _practiceCurrentIndex = 0;
        private bool _practiceShowMeaning = false;

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
            else if (targetAsset == FriendScreenAsset) BindFriendEvents();
            else if (targetAsset == ShopScreenAsset) BindShopEvents();
            else if (targetAsset == RankingScreenAsset) BindRankingEvents();
            else if (targetAsset == ProfileScreenAsset) BindProfileEvents();
            else if (targetAsset == ResultScreenAsset) BindResultEvents();
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
            }
            else
            {
                Debug.LogError("🚨 LỖI: Không tìm thấy file JSON trong thư mục Resources/Mockdata/db.json");
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

        // --- BINDING CHO DETAIL SCREEN ---
        private void BindDetailEvents()
        {
            if (_currentVocabSet != null)
            {
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
                if (wordsContainer != null && _currentVocabSet.words != null)
                {
                    wordsContainer.Clear(); // Xóa sạch dữ liệu cũ

                    int count = 1;
                    foreach (var wordData in _currentVocabSet.words)
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
                    if (_currentVocabSet != null && _currentVocabSet.words != null && _currentVocabSet.words.Count > 0)
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
            if (_currentVocabSet == null || _currentVocabSet.words == null || _currentVocabSet.words.Count == 0) return;

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
                    if (_practiceCurrentIndex < _currentVocabSet.words.Count - 1)
                    {
                        _practiceCurrentIndex++;
                        _practiceShowMeaning = false;
                        UpdatePracticeUI();
                    }
                    else
                    {
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
            if (_currentVocabSet == null || _currentVocabSet.words == null) return;
            var currentWord = _currentVocabSet.words[_practiceCurrentIndex];
            int totalWords = _currentVocabSet.words.Count;

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
        }

        private void BindBattleEvents()
        {
            Button backBtn = _root.Q<Button>("BtnBack");
            if (backBtn != null) backBtn.clicked += () => LoadScreen(HomeScreenAsset);
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
        }

        private void BindRankingEvents()
        {
            BindBottomNav();
        }

        private void BindProfileEvents()
        {
            BindBottomNav();
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
    }
}
