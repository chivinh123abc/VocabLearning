using TMPro;
using UnityEngine;
using VocabLearning.Core;

namespace VocabLearning.Home
{
    public class HomeInfoUI : MonoBehaviour
    {
        public TextMeshProUGUI usernameText;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI expText;
        public TextMeshProUGUI coinText;

        private void Start()
        {
            RefreshUI();
        }

        public void RefreshUI()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("GameManager not found in MainMenuScene.");
                return;
            }

            var player = GameManager.Instance.currentPlayer;

            if (usernameText != null)
                usernameText.text = player.username;

            if (levelText != null)
                levelText.text = "Level: " + player.level;

            if (expText != null)
                expText.text = "EXP: " + player.exp + "/" + player.expNeeded;

            if (coinText != null)
                coinText.text = "Coins: " + player.coins;
        }
    }
}