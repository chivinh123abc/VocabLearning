using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace VocabLearning.UI
{
    public partial class VocabStudyController
    {
        // State variables for Word Scramble Minigame
        private List<VocabLearning.Data.WordJson> _scrambleWordPool = new List<VocabLearning.Data.WordJson>();
        private int _scramblePoolIndex = 0;
        private string _scrambleTargetWord = "";
        private int _scrambleScore = 0;
        private List<Button> _scrambleLetterButtons = new List<Button>();
        private List<Button> _scrambleSlots = new List<Button>();
        private Button[] _scrambleSelectedButtons;
        private bool _isScrambleChecking = false;

        // Entry point to start the scramble game
        private void StartWordScrambleGame(List<VocabLearning.Data.WordJson> sourceWords)
        {
            if (sourceWords == null || sourceWords.Count == 0)
            {
                Debug.LogWarning("[Scramble] Danh sách từ vựng rỗng!");
                return;
            }

            // Lọc các từ hợp lệ (không chứa khoảng trắng và chỉ có chữ cái)
            _scrambleWordPool = sourceWords
                .Where(w => !string.IsNullOrEmpty(w.word) && !w.word.Contains(" "))
                .OrderBy(w => UnityEngine.Random.value)
                .ToList();

            if (_scrambleWordPool.Count == 0)
            {
                Debug.LogWarning("[Scramble] Không tìm thấy từ đơn nào hợp lệ để chơi minigame!");
                return;
            }

            _scramblePoolIndex = 0;
            _scrambleScore = 0;

            // Chuyển sang màn hình WordScramble
            LoadScreen(WordScrambleScreenAsset);
        }

        // Bắt sự kiện trên màn hình Word Scramble
        private void BindWordScrambleEvents()
        {
            Button btnClose = _root.Q<Button>("BtnCloseScramble");
            if (btnClose != null)
            {
                btnClose.clicked += () =>
                {
                    ShowConfirmationDialog(
                        "Thoát Minigame?",
                        "Bạn có muốn kết thúc chơi Minigame và quay lại trang Bộ từ vựng không?",
                        () => { FinishScrambleGame(); },
                        null
                    );
                };
            }

            Button btnReset = _root.Q<Button>("BtnResetScramble");
            if (btnReset != null) btnReset.clicked += ResetScrambleInput;

            Button btnSkip = _root.Q<Button>("BtnSkipScramble");
            if (btnSkip != null) btnSkip.clicked += SkipScrambleQuestion;

            // Load câu hỏi đầu tiên
            NextScrambleQuestion();
        }

        // Tải từ vựng tiếp theo
        private void NextScrambleQuestion()
        {
            if (_scramblePoolIndex >= _scrambleWordPool.Count || _scramblePoolIndex >= 10) // Tối đa 10 từ mỗi lượt chơi
            {
                FinishScrambleGame();
                return;
            }

            _isScrambleChecking = false;

            var currentWord = _scrambleWordPool[_scramblePoolIndex];
            _scrambleTargetWord = currentWord.word.ToUpper().Trim();

            // Cập nhật UI Progress & Score
            Label progressLbl = _root.Q<Label>("ScrambleProgressText");
            if (progressLbl != null) progressLbl.text = $"Word: {_scramblePoolIndex + 1}/{Mathf.Min(10, _scrambleWordPool.Count)}";

            Label scoreLbl = _root.Q<Label>("ScrambleScoreText");
            if (scoreLbl != null) scoreLbl.text = $"Score: {_scrambleScore}";

            VisualElement progressBar = _root.Q<VisualElement>("ScrambleProgressBar");
            if (progressBar != null)
            {
                float percent = ((float)_scramblePoolIndex / Mathf.Min(10, _scrambleWordPool.Count)) * 100f;
                progressBar.style.width = Length.Percent(percent);
            }

            // Nghĩa tiếng Việt
            Label meaningLbl = _root.Q<Label>("ScrambleMeaningText");
            if (meaningLbl != null) meaningLbl.text = currentWord.meaning;

            // Gợi ý hình ảnh
            VisualElement imgFrame = _root.Q<VisualElement>("ScrambleImageFrame");
            VisualElement imgElem = _root.Q<VisualElement>("ScrambleQuestionImage");
            if (imgFrame != null && imgElem != null)
            {
                if (!string.IsNullOrEmpty(currentWord.imageUrl))
                {
                    imgFrame.style.display = DisplayStyle.Flex;
                    imgElem.style.backgroundImage = null; // Clear old
                    StartCoroutine(DownloadAndSetImage(currentWord.imageUrl, imgElem));
                }
                else
                {
                    imgFrame.style.display = DisplayStyle.None;
                }
            }

            // Tạo các ô chứa chữ kết quả (Slots) làm Button để bấm sửa lại
            VisualElement resultContainer = _root.Q<VisualElement>("ScrambleResultContainer");
            if (resultContainer != null)
            {
                resultContainer.Clear();
                _scrambleSlots.Clear();
                _scrambleSelectedButtons = new Button[_scrambleTargetWord.Length];
                for (int i = 0; i < _scrambleTargetWord.Length; i++)
                {
                    int index = i;
                    Button slot = new Button();
                    slot.text = "_";
                    slot.AddToClassList("scramble-slot");
                    slot.clicked += () => HandleSlotClick(index);
                    resultContainer.Add(slot);
                    _scrambleSlots.Add(slot);
                }
            }

            // Tạo danh sách ký tự đã xáo trộn (Scrambled Letters)
            List<char> letters = _scrambleTargetWord.ToList();

            // Fisher-Yates Shuffle

            for (int i = letters.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                char temp = letters[i];
                letters[i] = letters[j];
                letters[j] = temp;
            }

            // Nếu ngẫu nhiên xáo trộn trùng từ gốc (đặc biệt với từ ngắn), xáo trộn lại một lần nữa
            if (new string(letters.ToArray()) == _scrambleTargetWord && letters.Count > 1)
            {
                char first = letters[0];
                letters[0] = letters[1];
                letters[1] = first;
            }

            // Tạo các nút ký tự bên dưới
            VisualElement lettersContainer = _root.Q<VisualElement>("ScrambleLettersContainer");
            if (lettersContainer != null)
            {
                lettersContainer.Clear();
                _scrambleLetterButtons.Clear();

                foreach (char c in letters)
                {
                    Button btn = new Button();
                    btn.text = c.ToString();
                    btn.AddToClassList("scramble-letter-btn");


                    char capturedChar = c;
                    btn.clicked += () => HandleLetterClick(btn, capturedChar);

                    lettersContainer.Add(btn);
                    _scrambleLetterButtons.Add(btn);
                }
            }
        }

        // Nhấn vào một ký tự để chọn
        private void HandleLetterClick(Button letterBtn, char character)
        {
            if (_isScrambleChecking) return;

            // Tìm ô trống đầu tiên để điền chữ vào
            int emptyIndex = -1;
            for (int i = 0; i < _scrambleSelectedButtons.Length; i++)
            {
                if (_scrambleSelectedButtons[i] == null)
                {
                    emptyIndex = i;
                    break;
                }
            }

            if (emptyIndex == -1) return; // Đã hết ô trống

            // Cập nhật kết quả vào ô trống
            _scrambleSelectedButtons[emptyIndex] = letterBtn;
            _scrambleSlots[emptyIndex].text = character.ToString();

            // Vô hiệu hóa nút đã chọn
            letterBtn.SetEnabled(false);
            letterBtn.style.opacity = 0.3f;

            // Kiểm tra xem đã điền đầy đủ các ô chưa
            bool allFilled = true;
            for (int i = 0; i < _scrambleSelectedButtons.Length; i++)
            {
                if (_scrambleSelectedButtons[i] == null)
                {
                    allFilled = false;
                    break;
                }
            }

            if (allFilled)
            {
                StartCoroutine(CheckScrambleCorrectness());
            }
        }

        // Nhấn vào một ô kết quả (Slot) để hủy/sửa lại chữ đó
        private void HandleSlotClick(int index)
        {
            if (_isScrambleChecking) return;
            if (index < 0 || index >= _scrambleSlots.Count) return;

            Button letterBtn = _scrambleSelectedButtons[index];
            if (letterBtn == null) return; // Ô này chưa được điền chữ

            // Bỏ vô hiệu hóa nút chữ ở dưới để có thể chọn lại
            letterBtn.SetEnabled(true);
            letterBtn.style.opacity = 1.0f;

            // Trả ô kết quả về trạng thái trống
            _scrambleSelectedButtons[index] = null;
            _scrambleSlots[index].text = "_";
        }

        // Reset lại từ hiện tại
        private void ResetScrambleInput()
        {
            _isScrambleChecking = false;

            for (int i = 0; i < _scrambleSlots.Count; i++)
            {
                _scrambleSlots[i].text = "_";
                _scrambleSlots[i].style.color = new StyleColor(new Color(245f / 255f, 158f / 255f, 11f / 255f)); // Reset color to gold
            }

            if (_scrambleSelectedButtons != null)
            {
                for (int i = 0; i < _scrambleSelectedButtons.Length; i++)
                {
                    _scrambleSelectedButtons[i] = null;
                }
            }

            foreach (var btn in _scrambleLetterButtons)
            {
                btn.SetEnabled(true);
                btn.style.opacity = 1.0f;
            }
        }

        // Bỏ qua từ hiện tại
        private void SkipScrambleQuestion()
        {
            _scramblePoolIndex++;
            NextScrambleQuestion();
        }

        // Kiểm tra đúng/sai sau khi gõ hết chữ
        private System.Collections.IEnumerator CheckScrambleCorrectness()
        {
            _isScrambleChecking = true;

            // Tạm thời vô hiệu hóa tất cả để tránh click đè khi đang kiểm tra
            foreach (var btn in _scrambleLetterButtons) btn.SetEnabled(false);
            foreach (var slot in _scrambleSlots) slot.SetEnabled(false);

            string spelledText = "";
            foreach (var slot in _scrambleSlots)
            {
                spelledText += slot.text;
            }

            bool correct = (spelledText == _scrambleTargetWord);

            if (correct)
            {
                SoundManager.PlayCorrect();
                _scrambleScore++;

                // Đổi màu các ô slot sang xanh lá để báo hiệu đúng
                foreach (var slot in _scrambleSlots)
                {
                    slot.style.color = new StyleColor(Color.green);
                }
            }
            else
            {
                SoundManager.PlayWrong();

                // Đổi màu các ô slot sang đỏ báo hiệu sai

                foreach (var slot in _scrambleSlots)
                {
                    slot.style.color = new StyleColor(Color.red);
                }
            }

            yield return new WaitForSeconds(1.0f);

            foreach (var slot in _scrambleSlots) slot.SetEnabled(true);
            _isScrambleChecking = false;

            if (correct)
            {
                _scramblePoolIndex++;
                NextScrambleQuestion();
            }
            else
            {
                // Cho phép thử lại nếu sai
                ResetScrambleInput();
            }
        }

        // Hoàn thành Minigame và tính thưởng
        private void FinishScrambleGame()
        {
            int expEarned = _scrambleScore * 15;
            int coinsEarned = _scrambleScore * 8;

            if (_jsonDb != null && _jsonDb.currentUser != null)
            {
                _jsonDb.currentUser.exp += expEarned;
                _jsonDb.currentUser.coins += coinsEarned;
                if (coinsEarned > 0)
                {
                    AddQuestProgressByType("CollectCoin", coinsEarned);
                }

                // Cập nhật nhiệm vụ "Học từ mới" nếu đạt điểm số
                AddQuestProgressByType("LearnWord", _scrambleScore);


                SaveJsonDatabase();
                UpdateAllCoinLabels();
            }

            // Chuyển sang màn hình Result (Hoặc quay lại VocabDetail trực tiếp)
            _lastBattleRecord = new VocabLearning.Data.BattleHistoryRecord
            {
                opponentName = "Minigame: Word Scramble",
                date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                isWin = true,
                correctCount = _scrambleScore,
                totalRounds = Mathf.Min(10, _scrambleWordPool.Count),
                rounds = new List<VocabLearning.Data.BattleRoundRecord>()
            };

            // Tái sử dụng màn hình ResultScreenAsset để hiện điểm thưởng
            LoadScreen(ResultScreenAsset);

            // Ghi đè các nhãn trên màn hình Result để phù hợp với Minigame
            VisualElement resOverlay = _root.Q<VisualElement>("ResultOverlay");
            if (resOverlay != null) resOverlay.style.display = DisplayStyle.Flex;

            Label titleLbl = _root.Q<Label>("ResultTitle");
            if (titleLbl != null)
            {
                titleLbl.text = "MINIGAME FINISHED!";
                titleLbl.style.color = new StyleColor(new Color(245f / 255f, 158f / 255f, 11f / 255f)); // Amber/Gold
            }

            Label subText = _root.Q<Label>("ResultSubtext");
            if (subText != null) subText.text = $"Spelled Correctly: {_scrambleScore} / {Mathf.Min(10, _scrambleWordPool.Count)} words";

            Label rewardsLbl = _root.Q<Label>("ResultExp");
            if (rewardsLbl != null) rewardsLbl.text = $"+{expEarned} EXP / +{coinsEarned} Coins";

            Label iconLbl = _root.Q<Label>("ResultIcon");
            if (iconLbl != null) iconLbl.text = "🧩";

            Button btnLeave = _root.Q<Button>("BtnLeaveBattle");
            if (btnLeave != null)
            {
                btnLeave.clicked += () =>
                {
                    // Quay lại màn hình chi tiết bộ từ vựng
                    LoadScreen(VocabDetailScreenAsset);
                };
            }
        }
    }
}
