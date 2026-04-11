using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VocabLearning.Core;
using VocabLearning.Data;

namespace VocabLearning.Learning
{
  public class LearningManager : MonoBehaviour
  {
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI wordText;
    public TextMeshProUGUI meaningText;
    public TextMeshProUGUI exampleText;
    public TextMeshProUGUI progressText;
    public Image wordImage;

    private VocabSetData currentSet;
    private int currentIndex = 0;

    private void Start()
    {
      if (GameManager.Instance == null)
      {
        Debug.LogError("GameManager Instance not found.");
        return;
      }

      currentSet = GameManager.Instance.selectedVocabSet;

      if (currentSet == null)
      {
        Debug.LogError("No vocabulary set selected.");
        return;
      }

      if (currentSet.words == null || currentSet.words.Count == 0)
      {
        Debug.LogWarning("Selected vocabulary set has no words.");
        return;
      }

      ShowCurrentWord();
    }

    private void ShowCurrentWord()
    {
      WordData currentWord = currentSet.words[currentIndex];

      titleText.text = currentSet.title;
      wordText.text = currentWord.word;
      meaningText.text = currentWord.meaning;
      exampleText.text = currentWord.example;
      progressText.text = $"{currentIndex + 1}/{currentSet.words.Count}";

      if (currentWord.image != null)
      {
        wordImage.sprite = currentWord.image;
        wordImage.gameObject.SetActive(true);
      }
      else
      {
        wordImage.gameObject.SetActive(false);
      }
    }

    public void OnClickNext()
    {
      if (currentSet == null || currentSet.words == null || currentSet.words.Count == 0)
        return;

      currentIndex++;

      if (currentIndex >= currentSet.words.Count)
      {
        currentIndex = 0;
      }

      ShowCurrentWord();
    }

    public void OnClickPrev()
    {
      if (currentSet == null || currentSet.words == null || currentSet.words.Count == 0)
        return;

      currentIndex--;

      if (currentIndex < 0)
      {
        currentIndex = currentSet.words.Count - 1;
      }

      ShowCurrentWord();
    }
  }
}