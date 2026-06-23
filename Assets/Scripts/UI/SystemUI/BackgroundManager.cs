using System.Collections;
using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
    [Header("Nhạc nền thường")]
    public AudioClip[] bgmClips;

    [Header("Nhạc battle")]
    public AudioClip[] battleClips;

    [Range(0f, 1f)]
    public float volume = 0.4f;

    [Header("Fade")]
    public float fadeDuration = 1f; // thời gian fade tính bằng giây

    private AudioSource audioSource;
    private static BackgroundMusic instance;
    private AudioClip[] currentPlaylist;
    private int currentIndex = 0;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        audioSource.loop = false;

        // Tải âm lượng từ PlayerPrefs (mặc định 0.4f)
        volume = PlayerPrefs.GetFloat("BGM_Volume", 0.4f);
        audioSource.volume = volume;

        PlayBGM();
    }

    void Update()
    {
        if (!audioSource.isPlaying)
            PlayNext();
    }

    public static void PlayBGM()
    {
        if (instance == null) return;
        if (instance.currentPlaylist == instance.bgmClips && instance.audioSource.isPlaying) return;
        instance.StopAllCoroutines();
        instance.StartCoroutine(instance.FadeAndSwitch(instance.bgmClips));
    }

    public static void PlayBattle()
    {
        if (instance == null) return;
        if (instance.currentPlaylist == instance.battleClips && instance.audioSource.isPlaying) return;
        instance.StopAllCoroutines();
        instance.StartCoroutine(instance.FadeAndSwitch(instance.battleClips));
    }

    IEnumerator FadeAndSwitch(AudioClip[] newPlaylist)
    {
        // Fade out
        yield return StartCoroutine(FadeOut());

        // Đổi playlist
        currentPlaylist = newPlaylist;
        currentIndex = 0;
        PlayNext();

        // Fade in
        yield return StartCoroutine(FadeIn());
    }

    IEnumerator FadeOut()
    {
        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.Stop();
    }

    IEnumerator FadeIn()
    {
        float elapsed = 0f;
        audioSource.volume = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, volume, elapsed / fadeDuration);
            yield return null;
        }

        audioSource.volume = volume;
    }

    void PlayNext()
    {
        if (currentPlaylist == null || currentPlaylist.Length == 0) return;
        audioSource.clip = currentPlaylist[currentIndex];
        audioSource.Play();
        currentIndex = (currentIndex + 1) % currentPlaylist.Length;
    }

    public static void SetVolume(float vol)
    {
        if (instance == null) return;
        instance.volume = vol;
        instance.audioSource.volume = vol;
        PlayerPrefs.SetFloat("BGM_Volume", vol);
        PlayerPrefs.Save();
    }

    public static void SetMute(bool mute)
    {
        if (instance != null) instance.audioSource.mute = mute;
    }
}