using UnityEngine;
using UnityEngine.SceneManagement;
using VocabLearning.Core;

namespace VocabLearning.Learning
{
    public class StartQuizButton : MonoBehaviour
    {
        public void OnClickStartQuiz()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetQuizData();
            }

            SceneManager.LoadScene("QuizScene");
        }
    }
}