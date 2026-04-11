using UnityEngine;
using VocabLearning.Data;

namespace VocabLearning.Core
{
    public class ProgressManager : MonoBehaviour
    {
        public static ProgressManager Instance;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public int CalculateEarnedExp(int correctAnswers)
        {
            return correctAnswers * 10;
        }

        public int CalculateEarnedCoins(int correctAnswers)
        {
            return correctAnswers * 2;
        }

        public void ApplyQuizRewards(int correctAnswers, int totalQuestions)
        {
            if (GameManager.Instance == null) return;

            PlayerData player = GameManager.Instance.currentPlayer;

            int earnedExp = CalculateEarnedExp(correctAnswers);
            int earnedCoins = CalculateEarnedCoins(correctAnswers);

            player.exp += earnedExp;
            player.coins += earnedCoins;
            player.totalGames += 1;
            player.learnedWords += totalQuestions;

            if (correctAnswers == totalQuestions)
            {
                player.wins += 1;
            }

            while (player.exp >= player.expNeeded)
            {
                player.exp -= player.expNeeded;
                player.level += 1;
            }
        }
    }
}