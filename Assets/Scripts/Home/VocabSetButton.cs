using UnityEngine;
using UnityEngine.SceneManagement;
using VocabLearning.Core;
using VocabLearning.Data;

namespace VocabLearning.Home
{
  public class VocabSetButton : MonoBehaviour
  {
    public VocabSetData vocabSet;

    public void OnClickSelectSet()
    {
      if (GameManager.Instance == null)
      {
        Debug.LogError("GameManager Instance not found.");
        return;
      }
      Debug.Log($"Selected vocab set: {vocabSet.title}");
      GameManager.Instance.SelectVocabSet(vocabSet);
      SceneManager.LoadScene("LearningScene");
    }
  }
}