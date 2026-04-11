using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using VocabLearning.Core;
using VocabLearning.Data;

namespace VocabLearning.Learning
{
    public class QuizManager : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI questionText;
        public TextMeshProUGUI progressText;
        public Button[] answerButtons;
        public TextMeshProUGUI[] answerButtonTexts;

        private VocabSetData currentSet;
        private List<WordData> quizWords;
        private int currentQuestionIndex = 0;
        private WordData currentCorrectWord;

        private void Start()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("GameManager not found.");
                return;
            }

            currentSet = GameManager.Instance.selectedVocabSet;

            if (currentSet == null)
            {
                Debug.LogError("No vocabulary set selected.");
                return;
            }

            if (currentSet.words == null || currentSet.words.Count < 4)
            {
                Debug.LogError("Vocabulary set must have at least 4 words to create quiz answers.");
                return;
            }

            quizWords = new List<WordData>(currentSet.words);

            GameManager.Instance.ResetQuizData();
            ShowQuestion();
        }

        private void ShowQuestion()
        {
            if (currentQuestionIndex >= quizWords.Count)
            {
                FinishQuiz();
                return;
            }

            currentCorrectWord = quizWords[currentQuestionIndex];

            questionText.text = $"What is the meaning of \"{currentCorrectWord.word}\"?";
            progressText.text = $"{currentQuestionIndex + 1}/{quizWords.Count}";

            List<string> answers = GenerateAnswers(currentCorrectWord);

            for (int i = 0; i < answerButtons.Length; i++)
            {
                string answer = answers[i];
                answerButtonTexts[i].text = answer;

                answerButtons[i].onClick.RemoveAllListeners();
                answerButtons[i].onClick.AddListener(() => OnAnswerSelected(answer));
            }
        }

        private List<string> GenerateAnswers(WordData correctWord)
        {
            List<string> answers = new List<string>();
            answers.Add(correctWord.meaning);

            List<string> wrongAnswers = new List<string>();

            foreach (WordData word in currentSet.words)
            {
                if (word.id != correctWord.id && word.meaning != correctWord.meaning)
                {
                    wrongAnswers.Add(word.meaning);
                }
            }

            ShuffleList(wrongAnswers);

            for (int i = 0; i < wrongAnswers.Count && answers.Count < 4; i++)
            {
                answers.Add(wrongAnswers[i]);
            }

            ShuffleList(answers);
            return answers;
        }

        private void OnAnswerSelected(string selectedAnswer)
        {
            if (GameManager.Instance == null) return;

            if (selectedAnswer == currentCorrectWord.meaning)
            {
                GameManager.Instance.correctAnswers++;
            }
            else
            {
                GameManager.Instance.wrongAnswers++;
            }

            currentQuestionIndex++;
            ShowQuestion();
        }

        private void FinishQuiz()
        {
            if (GameManager.Instance != null)
            {
                int total = GameManager.Instance.correctAnswers + GameManager.Instance.wrongAnswers;

                if (total > 0)
                {
                    GameManager.Instance.quizScore =
                        Mathf.RoundToInt((float)GameManager.Instance.correctAnswers / total * 100f);
                }
            }

            SceneManager.LoadScene("ResultScene");
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int randomIndex = Random.Range(i, list.Count);
                T temp = list[i];
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }
    }
}