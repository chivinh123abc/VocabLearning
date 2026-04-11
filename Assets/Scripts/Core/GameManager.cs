using UnityEngine;
using VocabLearning.Data;

namespace VocabLearning.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        public VocabSetData selectedVocabSet;

        public int quizScore;
        public int correctAnswers;
        public int wrongAnswers;

        public PlayerData currentPlayer = new PlayerData();

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

        public void SelectVocabSet(VocabSetData vocabSet)
        {
            selectedVocabSet = vocabSet;
        }

        public void ResetQuizData()
        {
            quizScore = 0;
            correctAnswers = 0;
            wrongAnswers = 0;
        }
    }
}