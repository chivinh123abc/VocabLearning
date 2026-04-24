using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace VocabLearning.Helpers
{
    /// <summary>
    /// Text-to-Speech helper sử dụng Google Translate TTS (không cần API key).
    /// Hoạt động trên tất cả nền tảng có kết nối Internet.
    /// Gắn script này vào một GameObject trong Scene, nó sẽ tự tồn tại xuyên scene.
    /// </summary>
    public class TextToSpeechHelper : MonoBehaviour
    {
        private static TextToSpeechHelper _instance;
        public static TextToSpeechHelper Instance => _instance;

        private AudioSource _audioSource;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                Debug.Log("[TTS] TextToSpeechHelper initialized.");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>Đọc một từ hoặc cụm từ tiếng Anh.</summary>
        public void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            StartCoroutine(FetchAndPlay(text));
        }

        private IEnumerator FetchAndPlay(string text)
        {
            // Dùng Google Translate TTS endpoint (miễn phí, không cần key)
            string encoded = UnityWebRequest.EscapeURL(text);
            string url = $"https://translate.google.com/translate_tts?ie=UTF-8&q={encoded}&tl=en-us&client=tw-ob";

            using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG);
            // Phải đặt User-Agent, nếu không Google sẽ từ chối
            request.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                if (clip != null && _audioSource != null)
                {
                    _audioSource.Stop();
                    _audioSource.clip = clip;
                    _audioSource.Play();
                }
            }
            else
            {
                Debug.LogWarning($"[TTS] Không tải được audio: {request.error} | URL: {url}");
            }
        }
    }
}
