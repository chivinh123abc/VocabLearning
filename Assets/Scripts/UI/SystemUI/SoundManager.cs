using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public AudioClip clickSound;
    public AudioClip correctSound;
    public AudioClip wrongSound;
    public AudioClip achievementSound;
    public AudioClip fireSound;
    public AudioClip poisonSound;
    public AudioClip healingSound;
    public AudioClip gameOverSound;
   public AudioClip  winBattleSound;

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

    public static void PlayClick()
    {
        if (instance != null && instance.audioSource != null && instance.clickSound != null)
            instance.audioSource.PlayOneShot(instance.clickSound);
    }

    public static void PlayCorrect()
    {
        if (instance != null && instance.audioSource != null && instance.correctSound != null)
            instance.audioSource.PlayOneShot(instance.correctSound);
    }

    public static void PlayWrong()
    {
        if (instance != null && instance.audioSource != null && instance.wrongSound != null)
            instance.audioSource.PlayOneShot(instance.wrongSound);
    }

    public static void PlayAchievement()
    {
        if (instance != null && instance.audioSource != null && instance.achievementSound != null)
            instance.audioSource.PlayOneShot(instance.achievementSound); 
    }

    public static void PlayFire()
    {
        if (instance != null && instance.audioSource != null && instance.fireSound != null)
            instance.audioSource.PlayOneShot(instance.fireSound);
    }

    public static void PlayPoison()
    {
        if (instance != null && instance.audioSource != null && instance.poisonSound != null)
            instance.audioSource.PlayOneShot(instance.poisonSound);
    }

    public static void PlayHealing()
    {
        if (instance != null && instance.audioSource != null && instance.healingSound != null)
            instance.audioSource.PlayOneShot(instance.healingSound);
    }

    public static void PlayGameOver()
    {
        if (instance != null && instance.audioSource != null && instance.gameOverSound != null)
            instance.audioSource.PlayOneShot(instance.gameOverSound);
    }

    public static void PlayWinBattle()
    {
        if (instance != null && instance.audioSource != null && instance.winBattleSound != null)
            instance.audioSource.PlayOneShot(instance.winBattleSound);
    }

    public static float GetVolume()
    {
        return instance != null ? instance.audioSource.volume : 1f;
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