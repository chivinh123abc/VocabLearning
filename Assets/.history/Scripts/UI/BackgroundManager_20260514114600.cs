using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
    [Header("Nhạc nền thường")]
    public AudioClip[] bgmClips;

    [Header("Nhạc battle")]
    public AudioClip[] battleClips;

    [Range(0f, 1f)]
    public float volume = 0.4f;

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
        audioSource.volume = volume;

        // Mặc định chạy nhạc nền
        PlayBGM();
    }

    void Update()
    {
        if (!audioSource.isPlaying)
            PlayNext();
    }

    // Chạy nhạc nền thường
    public static void PlayBGM()
    {
        if (instance == null) return;
        instance.currentPlaylist = instance.bgmClips;
        instance.currentIndex = 0;
        instance.PlayNext();
    }

    // Chạy nhạc battle
    public static void PlayBattle()
    {
        if (instance == null) return;
        instance.currentPlaylist = instance.battleClips;
        instance.currentIndex = 0;
        instance.PlayNext();
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
        if (instance != null) instance.audioSource.volume = vol;
    }

    public static void SetMute(bool mute)
    {
        if (instance != null) instance.audioSource.mute = mute;
    }
}