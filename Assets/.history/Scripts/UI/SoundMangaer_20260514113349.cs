using UnityEngine;

public class SoundMangaer : MonoBehaviour
{
    public AudioClip clickSound;
    public AudioClip correctSound;
    public AudioClip wrongSound;
    public AudioClip achievementSound;

    static SoundMangaer instance;
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
        audioSource = GetComponent <AudioSource>();
    }
         

} 