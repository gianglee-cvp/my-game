using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public enum BGMType
    {
        Menu,
        Level1
    }

    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Clips")]
    public AudioClip bgmMenu;
    public AudioClip bgmLevel1;
    public AudioClip shoot;
    public AudioClip explosion;

    [Header("Volume")]
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
    }

    void HandleGameStateChanged(GameState state)
    {
        Debug.Log("Playing BGM: " + state);
        switch (state)
        {
            case GameState.MainMenu:
                PlayBGM(BGMType.Menu);
                break;

            case GameState.Playing:
                PlayBGM(BGMType.Level1);
                break;
        }
    }

    void Start()
    {
        // setup volume
        if (bgmSource != null) bgmSource.volume = bgmVolume;
        if (sfxSource != null) sfxSource.volume = sfxVolume;

        // play nhạc menu mặc định
        PlayBGM(BGMType.Menu);
    }

    // ===================== BGM =====================

    public void PlayBGM(BGMType type)
    {
        AudioClip clip = GetBGMClip(type);

        if (clip == null || bgmSource == null)
        {
            Debug.LogWarning("[AudioManager] Missing BGM or AudioSource!");
            return;
        }
        Debug.Log("Playing BGM: " + type);
        // tránh play lại cùng nhạc
        if (bgmSource.clip == clip && bgmSource.isPlaying)
            return;

        StopAllCoroutines();
        StartCoroutine(FadeBGM(clip));
    }

    private AudioClip GetBGMClip(BGMType type)
    {
        switch (type)
        {
            case BGMType.Menu: return bgmMenu;
            case BGMType.Level1: return bgmLevel1;
        }
        return null;
    }

    IEnumerator FadeBGM(AudioClip newClip)
    {
        if (bgmSource.isPlaying)
        {
            // fade out
            while (bgmSource.volume > 0.05f)
            {
                bgmSource.volume -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        bgmSource.clip = newClip;
        bgmSource.loop = true;
        bgmSource.Play();

        // fade in
        while (bgmSource.volume < bgmVolume)
        {
            bgmSource.volume += Time.unscaledDeltaTime;
            yield return null;
        }

        bgmSource.volume = bgmVolume;
    }

    // ===================== SFX =====================

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;

        // random pitch cho tự nhiên
        sfxSource.pitch = Random.Range(0.9f, 1.1f);
        sfxSource.PlayOneShot(clip);
    }

    // tiện dùng bằng enum sau này
    public void PlayShoot()
    {
        PlaySFX(shoot);
    }

    public void PlayExplosion()
    {
        PlaySFX(explosion);
    }
}