using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public AudioClip clickSound;
    public AudioClip correctSound;
    public AudioClip wrongSound;
    public AudioClip achievementSound;

    static SoundManager instance;
    AudioSource audioSource;
    
    void Awake(){
        if(instance == null){
            instance = this;
            audioSource = GetComponent<AudioSource>();
            DontDestroyOnLoad(gameObject);
        }
        else{
            Destroy(gameObject);
        }
    }

    public static void PlayClick()       => instance.audioSource.PlayOneShot(instance.clickSound);
    public static void PlayCorrect()     => instance.audioSource.PlayOneShot(instance.correctSound);
    public static void PlayWrong()       => instance.audioSource.PlayOneShot(instance.wrongSound);
    public static void PlayAchievement() => instance.audioSource.PlayOneShot(instance.achievementSound); 

    public static void SetVolume(float vol)
    {
        if (instance != null) instance.audioSource.volume = vol;
    }

    public static void SetMute(bool mute)
    {
        if (instance != null) instance.audioSource.mute = mute;
    } 
}