using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BackgroundMusicPlayer : MonoBehaviour
{
    [Header("Music Settings")]
    public AudioClip[] musicClips;
    public bool playOnStart = true;
    public bool dontDestroyOnLoad = true;

    private AudioSource audioSource;
    private int lastPlayedIndex = -1;
    private static BackgroundMusicPlayer instance;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        audioSource = GetComponent<AudioSource>();
        audioSource.loop = false;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (playOnStart)
            PlayRandomMusic();
    }

    void Update()
    {
        // Kalau lagu selesai → mainkan random lagi
        if (!audioSource.isPlaying && audioSource.clip != null)
        {
            PlayRandomMusic();
        }
    }

    void PlayRandomMusic()
    {
        if (musicClips == null || musicClips.Length == 0)
        {
            Debug.LogWarning("Music array kosong!");
            return;
        }

        int newIndex;

        // Hindari lagu yang sama diputar dua kali berturut-turut
        do
        {
            newIndex = Random.Range(0, musicClips.Length);
        }
        while (musicClips.Length > 1 && newIndex == lastPlayedIndex);

        lastPlayedIndex = newIndex;

        audioSource.clip = musicClips[newIndex];
        audioSource.Play();
    }

    // Optional: Stop music
    public void StopMusic()
    {
        audioSource.Stop();
    }

    // Optional: Set volume
    public void SetVolume(float volume)
    {
        audioSource.volume = Mathf.Clamp01(volume);
    }
}
