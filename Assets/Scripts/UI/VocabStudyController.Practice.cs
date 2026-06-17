using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace VocabLearning.UI
{
    public partial class VocabStudyController
    {
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
            Label header = _root.Q<Label>("DifficultySelectionHeader");
            if (container == null) return;

            container.Clear();

            // Chỉ hiển thị chọn cấp độ khi có nhiều hơn 1 cấp độ (Multilevel)
            bool isMultiLevel = _currentVocabSet.levels != null && _currentVocabSet.levels.Count > 1;

            if (header != null)
            {
                header.style.display = isMultiLevel ? DisplayStyle.Flex : DisplayStyle.None;
            }
            container.style.display = isMultiLevel ? DisplayStyle.Flex : DisplayStyle.None;

            if (isMultiLevel)
            {
                foreach (var levelData in _currentVocabSet.levels)
                {
                    var levelName = levelData.difficulty;

                    // Tính tiến độ cho level này 
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
                UpdateLevelButtonsUI();
            }
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

                    Color activeColor = new Color(0.55f, 0.36f, 0.96f); // Default Purple
                    string lvlNameLower = (levelName ?? "").ToLower().Trim();
                    if (lvlNameLower == "easy" || lvlNameLower == "dễ" || lvlNameLower == "de") 
                        activeColor = new Color(0.10f, 0.73f, 0.51f); // Emerald
                    else if (lvlNameLower == "medium" || lvlNameLower == "trung bình" || lvlNameLower == "trung binh" || lvlNameLower == "normal") 
                        activeColor = new Color(0.23f, 0.51f, 0.96f); // Blue
                    else if (lvlNameLower == "hard" || lvlNameLower == "khó" || lvlNameLower == "kho") 
                        activeColor = new Color(0.93f, 0.26f, 0.26f); // Red

                    SetLevelButtonStyle(btn, isSelected, activeColor);
                }
            }
        }

        private void SetLevelButtonStyle(Button btn, bool isSelected, Color activeColor)
        {
            if (isSelected)
            {
                btn.style.backgroundColor = new StyleColor(activeColor);
                btn.style.color = new StyleColor(Color.white);
                btn.style.borderBottomWidth = 4;
                btn.style.borderBottomColor = new StyleColor(new Color(0, 0, 0, 0.3f));
                
                // Set solid border same as active color to keep button size stable
                btn.style.borderLeftColor = new StyleColor(activeColor);
                btn.style.borderRightColor = new StyleColor(activeColor);
                btn.style.borderTopColor = new StyleColor(activeColor);
                btn.style.borderLeftWidth = 1;
                btn.style.borderRightWidth = 1;
                btn.style.borderTopWidth = 1;
                btn.style.borderBottomWidth = 0; // Handled by borderBottomWidth and borderBottomColor
            }
            else
            {
                btn.style.backgroundColor = new StyleColor(new Color(0.12f, 0.16f, 0.23f)); // Dark slate background
                btn.style.color = new StyleColor(activeColor);
                
                // Subtle matching border tint with 0.4f alpha
                Color borderColor = new Color(activeColor.r, activeColor.g, activeColor.b, 0.4f);
                btn.style.borderLeftColor = new StyleColor(borderColor);
                btn.style.borderRightColor = new StyleColor(borderColor);
                btn.style.borderTopColor = new StyleColor(borderColor);
                btn.style.borderBottomColor = new StyleColor(borderColor);
                btn.style.borderLeftWidth = 1;
                btn.style.borderRightWidth = 1;
                btn.style.borderTopWidth = 1;
                btn.style.borderBottomWidth = 1;
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

                Label countLbl = new Label($"{(vocabSet.wordCount > 0 ? vocabSet.wordCount : (vocabSet.wordIds != null ? vocabSet.wordIds.Count : 0))} words - Đã hoàn thành");
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
                if (lblLevel != null)
                {
                    lblLevel.text = _currentSelectedLevel;
                    Color levelColor = new Color(0.55f, 0.36f, 0.96f); // Default Purple/Multi-Level
                    string lvlLower = (_currentSelectedLevel ?? "").ToLower().Trim();
                    if (lvlLower == "easy" || lvlLower == "dễ" || lvlLower == "de") 
                        levelColor = new Color(0.10f, 0.73f, 0.51f); // Emerald
                    else if (lvlLower == "medium" || lvlLower == "trung bình" || lvlLower == "trung binh" || lvlLower == "normal") 
                        levelColor = new Color(0.23f, 0.51f, 0.96f); // Blue
                    else if (lvlLower == "hard" || lvlLower == "khó" || lvlLower == "kho") 
                        levelColor = new Color(0.93f, 0.26f, 0.26f); // Red
                    
                    lblLevel.style.color = new StyleColor(levelColor);
                }

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

        private void BindOtherScreens()
        {
            // Các nút trở về chung chung (nếu có id là BtnBack)
            Button btnBack = _root.Q<Button>("BtnBack");
            if (btnBack != null) btnBack.clicked += () => LoadScreen(HomeScreenAsset);
        }

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
                if (!user.learnedSets.Contains(_currentVocabSet.id))
                {
                    user.learnedSets.Add(_currentVocabSet.id);
                    AddQuestProgressByType("CompleteSet", 1);
                }
            }
            // Chỉ cần học được ít nhất 1 từ thì cũng đưa vào Learned Sets
            else if (_sessionNewlyMasteredWords.Count > 0)
            {
                if (user.learnedSets == null) user.learnedSets = new List<string>();
                if (!user.learnedSets.Contains(_currentVocabSet.id)) user.learnedSets.Add(_currentVocabSet.id);
            }

            if (_lastSessionCoins > 0)
            {
                AddQuestProgressByType("CollectCoin", _lastSessionCoins);
            }
            SaveJsonDatabase();
            UpdateAllCoinLabels();
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

            Button btnNext = _root.Q<Button>("BtnNext"); // Fallback check BtnPracticeNext was mapping
            if (btnNext == null) btnNext = _root.Q<Button>("BtnPracticeNext");
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

        private void BindResultEvents()
        {
            // Phát âm thanh chiến thắng / hoàn thành
            SoundManager.PlayAchievement();

            // [NEW] Hiển thị thưởng đã tính toán
            Label lblCoins = _root.Q<Label>("LblRewardCoins");
            Label lblExp = _root.Q<Label>("LblRewardExp");
            if (lblCoins != null) lblCoins.text = "+" + _lastSessionCoins;
            if (lblExp != null) lblExp.text = "+" + _lastSessionExp;

            // [NEW] Hiển thị số từ đã thuộc (Mastered)
            Label lblScore = _root.Q<Label>("LblResultScore");
            if (lblScore != null && _currentVocabSetWords != null)
            {
                int masteredThisSession = (_sessionNewlyMasteredWords != null) ? _sessionNewlyMasteredWords.Count : 0;
                lblScore.text = $"Mastered: {masteredThisSession} / {_currentVocabSetWords.Count} words";
            }

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
    }
}
