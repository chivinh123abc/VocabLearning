using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace VocabLearning.UI
{
    public partial class VocabStudyController
    {
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
                SoundManager.PlayCorrect();
                btn.style.backgroundColor = Color.green;
                if (!_isReviewMode)
                {
                    _soloScore++;
                    _root.Q<Label>("SoloScore").text = $"Score: {_soloScore}";
                }
            }
            else
            {   
                SoundManager.PlayWrong();
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
            if (coins > 0)
            {
                AddQuestProgressByType("CollectCoin", coins);
            }

            _lastBattleRecord = new VocabLearning.Data.BattleHistoryRecord
            {
                opponentName = "Solo Quiz - " + _soloMode.ToString(),
                date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                isWin = true,
                correctCount = _soloScore,
                totalRounds = _soloRoundRecords.Count,
                rounds = new List<VocabLearning.Data.BattleRoundRecord>(_soloRoundRecords)
            };

            SaveJsonDatabase();
            UpdateAllCoinLabels();

            ShowBattleSummaryOverlay(() =>
            {       
                SoundManager.PlayAchievement();
                VisualElement resOverlay = _root.Q<VisualElement>("ResultOverlay");
                if (resOverlay != null) resOverlay.style.display = DisplayStyle.Flex;

                if (_root.Q<Label>("ResultTitle") != null)
                {
                    _root.Q<Label>("ResultTitle").text = "QUIZ FINISHED!";
                    _root.Q<Label>("ResultTitle").style.color = new StyleColor(new Color(0.39f, 0.40f, 0.95f)); // Purple-ish
                }
                if (_root.Q<Label>("ResultSubtext") != null) _root.Q<Label>("ResultSubtext").text = $"Mode: {_soloMode}  •  Score: {_soloScore} / {_soloRoundRecords.Count}";
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
