using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using VocabLearning.Core;

namespace VocabLearning.Learning
{
    public class ResultManager : MonoBehaviour
    {
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI correctText;
        public TextMeshProUGUI wrongText;
        public TextMeshProUGUI rewardText;

        private bool rewardsApplied = false;

        private void Start()
        {
            ShowResults();
            ApplyRewards();
        }

        private void ShowResults()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("GameManager instance not found in ResultScene");
                return;
            }

            int score = GameManager.Instance.quizScore;
            int correct = GameManager.Instance.correctAnswers;
            int wrong = GameManager.Instance.wrongAnswers;

            if (scoreText != null) scoreText.text = "Score: " + score + "%";
            if (correctText != null) correctText.text = "Correct: " + correct;
            if (wrongText != null) wrongText.text = "Wrong: " + wrong;
            if (rewardText != null)
            {
                rewardText.text = "Rewards: " + ProgressManager.Instance.CalculateEarnedExp(correct) + " EXP, " + ProgressManager.Instance.CalculateEarnedCoins(correct) + " Coins";
            }
        }

        private void ApplyRewards()
        {
            if (rewardsApplied) return;

            if (GameManager.Instance == null || ProgressManager.Instance == null)
            {
                Debug.LogError("GameManager or ProgressManager instance not found when applying rewards");
                return;
            }

            int correct = GameManager.Instance.correctAnswers;
            int total = GameManager.Instance.correctAnswers + GameManager.Instance.wrongAnswers;

            ProgressManager.Instance.ApplyQuizRewards(correct, total);
            rewardsApplied = true;

            Debug.Log("Rewards applied");
            Debug.Log("Player EXP: " + GameManager.Instance.currentPlayer.exp);
            Debug.Log("Player Coins: " + GameManager.Instance.currentPlayer.coins);
            Debug.Log("Player Level: " + GameManager.Instance.currentPlayer.level);
        }

        public void OnClickBackToMenu()
        {
            SceneManager.LoadScene("MainMenuScene");
        }

        public void OnClickRetry()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetQuizData();
            }

            SceneManager.LoadScene("QuizScene");
        }
    }
}