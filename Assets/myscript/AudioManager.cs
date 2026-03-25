using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Singleton AudioManager – tự tạo AudioSource, không phụ thuộc vào
/// GameObject của GameManager. Tồn tại xuyên suốt các scene.
/// </summary>
public class AudioManager : MonoBehaviour
{
    // ───────────── Singleton ─────────────
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Tìm trong scene trước
                _instance = FindAnyObjectByType<AudioManager>();

                // Nếu không có thì tự tạo
                if (_instance == null)
                {
                    GameObject go = new GameObject("[AudioManager]");
                    _instance = go.AddComponent<AudioManager>();
                }
            }
            return _instance;
        }
    }

    // ───────────── Enum ─────────────
    public enum BGMType
    {
        Menu,
        Level1
    }

    // ───────────── Clips (gán trong Inspector) ─────────────
    [Header("BGM Clips")]
    public AudioClip bgmMenu;
    public AudioClip bgmLevel1;

    [Header("SFX Clips")]
    public AudioClip shoot;
    public AudioClip explosion;

    [Header("Loop SFX Clips")]
    public AudioClip homingLoop;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Fade Settings")]
    [Range(0.1f, 3f)] public float fadeDuration = 0.5f;

    // ───────────── Runtime ─────────────
    private AudioSource _bgmSource;
    private AudioSource _sfxSource;
    private AudioSource _homingSource;
    private int _homingRefCount = 0;
    private Coroutine _fadeCoroutine;
    private bool _subscribedToGameManager;

    // ───────────── Lifecycle ─────────────

    void Awake()
    {
        // Singleton guard — giữ instance đầu tiên, destroy các bản sao
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioSources();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TrySubscribeToGameManager();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnsubscribeFromGameManager();
    }

    void Start()
    {
        ApplyVolumes();

        // Phát nhạc dựa trên state hiện tại của GameManager (nếu có)
        if (GameManager.Instance != null)
        {
            SyncBGMToState(GameManager.Instance.currentState);
        }
        else
        {
            // Mặc định phát nhạc menu
            PlayBGM(BGMType.Menu);
        }
    }

    // ───────────── Scene Load Callback ─────────────

    /// <summary>
    /// Mỗi khi scene mới load xong, thử subscribe lại nếu chưa,
    /// vì GameManager có thể mới được tạo ở scene mới.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TrySubscribeToGameManager();
    }

    // ───────────── GameManager Event Wiring ─────────────

    private void TrySubscribeToGameManager()
    {
        // Nếu đã subscribe rồi → bỏ qua
        if (_subscribedToGameManager) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            _subscribedToGameManager = true;
            Debug.Log("[AudioManager] Subscribed to GameManager events.");
        }
    }

    private void UnsubscribeFromGameManager()
    {
        if (!_subscribedToGameManager) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
        _subscribedToGameManager = false;
    }

    // ───────────── Event Handler ─────────────

    private void HandleGameStateChanged(GameState state)
    {
        SyncBGMToState(state);

        // Khi rời khỏi trạng thái Playing → force stop mọi loop SFX
        if (state != GameState.Playing)
            ForceStopHomingLoop();
    }

    private void SyncBGMToState(GameState state)
    {
        Debug.Log("[AudioManager] State → " + state);

        switch (state)
        {
            case GameState.MainMenu:
                PlayBGM(BGMType.Menu);
                break;

            case GameState.Playing:
                PlayBGM(BGMType.Level1);
                break;

            // Các state khác (Paused, GameOver, Victory …) không đổi nhạc
        }
    }

    // ───────────── AudioSource Setup ─────────────

    private void EnsureAudioSources()
    {
        if (_bgmSource == null)
        {
            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;
            _bgmSource.priority = 0; // BGM = highest priority
        }

        if (_sfxSource == null)
        {
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.loop = false;
            _sfxSource.priority = 128;
        }

        if (_homingSource == null)
        {
            _homingSource = gameObject.AddComponent<AudioSource>();
            _homingSource.playOnAwake = false;
            _homingSource.loop = true;
            _homingSource.spatialBlend = 0f; // 2D
            _homingSource.priority = 64;
        }
    }

    private void ApplyVolumes()
    {
        if (_bgmSource != null) _bgmSource.volume = bgmVolume;
        if (_sfxSource != null) _sfxSource.volume = sfxVolume;
    }

    // ───────────── BGM ─────────────

    public void PlayBGM(BGMType type)
    {
        AudioClip clip = GetBGMClip(type);

        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] Không tìm thấy BGM clip cho: " + type);
            return;
        }

        // Tránh play lại cùng clip
        if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;

        Debug.Log("[AudioManager] Playing BGM: " + type);

        // Dừng fade cũ nếu đang chạy
        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        _fadeCoroutine = StartCoroutine(FadeBGM(clip));
    }

    public void StopBGM()
    {
        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        if (_bgmSource != null)
        {
            _bgmSource.Stop();
            _bgmSource.clip = null;
        }
    }

    private AudioClip GetBGMClip(BGMType type)
    {
        switch (type)
        {
            case BGMType.Menu:  return bgmMenu;
            case BGMType.Level1: return bgmLevel1;
            default: return null;
        }
    }

    private IEnumerator FadeBGM(AudioClip newClip)
    {
        float speed = bgmVolume / Mathf.Max(fadeDuration, 0.1f);

        // Fade-out nhạc cũ (nếu đang phát)
        if (_bgmSource.isPlaying)
        {
            while (_bgmSource.volume > 0.01f)
            {
                _bgmSource.volume -= speed * Time.unscaledDeltaTime;
                yield return null;
            }
            _bgmSource.Stop();
        }

        // Chuyển clip mới
        _bgmSource.clip = newClip;
        _bgmSource.volume = 0f;
        _bgmSource.Play();

        // Fade-in
        while (_bgmSource.volume < bgmVolume)
        {
            _bgmSource.volume += speed * Time.unscaledDeltaTime;
            yield return null;
        }

        _bgmSource.volume = bgmVolume;
        _fadeCoroutine = null;
    }

    // ───────────── SFX ─────────────

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || _sfxSource == null) return;

        _sfxSource.pitch = Random.Range(0.9f, 1.1f);
        _sfxSource.PlayOneShot(clip, sfxVolume);
    }

    // Phím tắt SFX
    public void PlayShoot()    => PlaySFX(shoot);
    public void PlayExplosion() => PlaySFX(explosion);

    // ───────────── Homing Loop ─────────────

    /// <summary>
    /// Bật loop homing. Dùng ref-count để nhiều bomb cùng gọi mà không bị trùng.
    /// </summary>
    public void StartHomingLoop()
    {
        _homingRefCount++;
        Debug.Log($"[AudioManager] StartHomingLoop called. RefCount={_homingRefCount}, clip={(homingLoop != null ? homingLoop.name : "NULL")}, source={(_homingSource != null ? "OK" : "NULL")}");

        if (_homingSource != null && !_homingSource.isPlaying && homingLoop != null)
        {
            _homingSource.clip = homingLoop;
            _homingSource.volume = sfxVolume;
            _homingSource.Play();
            Debug.Log("[AudioManager] Homing loop PLAYING.");
        }
    }

    /// <summary>
    /// Giảm ref-count. Khi không còn bomb nào homing → tắt loop.
    /// </summary>
    public void StopHomingLoop()
    {
        _homingRefCount = Mathf.Max(0, _homingRefCount - 1);

        if (_homingRefCount <= 0 && _homingSource != null && _homingSource.isPlaying)
        {
            _homingSource.Stop();
            _homingSource.clip = null;
        }
    }

    /// <summary>
    /// Force stop — bất kể ref-count. Gọi khi chuyển scene hoặc reset.
    /// </summary>
    public void ForceStopHomingLoop()
    {
        _homingRefCount = 0;

        if (_homingSource != null)
        {
            _homingSource.Stop();
            _homingSource.clip = null;
        }
    }

    // ───────────── Volume Control (runtime) ─────────────

    public void SetBGMVolume(float vol)
    {
        bgmVolume = Mathf.Clamp01(vol);
        if (_bgmSource != null && _fadeCoroutine == null)
            _bgmSource.volume = bgmVolume;
    }

    public void SetSFXVolume(float vol)
    {
        sfxVolume = Mathf.Clamp01(vol);
        if (_sfxSource != null)
            _sfxSource.volume = sfxVolume;
    }
}