using UnityEngine;
using UnityEngine.SceneManagement;

namespace VocabLearning.Core
{
  public class BootLoader : MonoBehaviour
  {
    private void Start()
    {
      SceneManager.LoadScene("MainMenuScene");
    }
  }
}