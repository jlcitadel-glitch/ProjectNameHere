using UnityEngine;

/// <summary>
/// Persistent singleton that manages music playback across scenes.
/// Reads volume from PlayerPrefs and reacts to GameManager state changes.
/// </summary>
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    private AudioSource audioSource;
    private AudioClip gameplayMusic;
    private float musicVolume = 1f;
    private float masterVolume = 1f;
    private bool isDucked;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[MusicManager] Duplicate instance on {gameObject.name}, destroying.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        masterVolume = PlayerPrefs.GetFloat("Audio_Master", 1f);
        musicVolume = PlayerPrefs.GetFloat("Audio_Music", 1f);
        ApplyVolume();

        gameplayMusic = Resources.Load<AudioClip>("GameplayMusic");
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PlayTrack(AudioClip clip)
    {
        if (clip == null || (audioSource.clip == clip && audioSource.isPlaying))
            return;

        audioSource.clip = clip;
        audioSource.Play();
    }

    public void Stop()
    {
        audioSource.Stop();
    }

    public void SetVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        ApplyVolume();
    }

    private void OnGameStateChanged(GameManager.GameState previous, GameManager.GameState current)
    {
        isDucked = current == GameManager.GameState.Paused;
        ApplyVolume();

        if (current == GameManager.GameState.Playing && previous != GameManager.GameState.Paused)
        {
            if (gameplayMusic != null && audioSource.clip != gameplayMusic)
            {
                PlayTrack(gameplayMusic);
            }
        }
    }

    private void ApplyVolume()
    {
        masterVolume = PlayerPrefs.GetFloat("Audio_Master", 1f);
        float duck = isDucked ? 0.5f : 1f;
        audioSource.volume = masterVolume * musicVolume * duck;
    }
}
