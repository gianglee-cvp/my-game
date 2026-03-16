using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    MainMenu,
    LevelSelect,
    Playing,
    Shop,
    Paused,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public GameState currentState = GameState.MainMenu;

    [Header("UI Panels (Menu Scene Only)")]
    public GameObject mainMenuPanel;
    public GameObject levelSelectPanel;
    public GameObject shopPanel;

    public delegate void OnStateChanged(GameState newState);
    public event OnStateChanged OnGameStateChanged;

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

    void Start()
    {
        ChangeState(currentState);
    }

    void Update()
    {
        // Tự động quản lý ẩn/hiện mọi Panel dựa trên GameState
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(currentState == GameState.MainMenu);

        if (levelSelectPanel != null)
            levelSelectPanel.SetActive(currentState == GameState.LevelSelect);

        if (shopPanel != null)
            shopPanel.SetActive(currentState == GameState.Shop);
    }

    public void ChangeState(GameState newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        switch (currentState)
        {
            case GameState.MainMenu:
            case GameState.LevelSelect:
            case GameState.Shop:
            case GameState.Paused:
            case GameState.GameOver:
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;

            case GameState.Playing:
                Time.timeScale = 1f;
                Cursor.visible = true;
                break;
        }

        OnGameStateChanged?.Invoke(currentState);

        Debug.Log("[GameManager] State Changed to: " + newState);
    }

    // ===== UI FUNCTIONS =====

    public void PlayButton()
    {
        ChangeState(GameState.LevelSelect);
    }

    [Header("Scene Settings")]
    public string menuSceneName = "Menu";

    public void BackToMenu()
    {
        Debug.Log("[GameManager] BackToMenu called. Current Scene: " + SceneManager.GetActiveScene().name);
        ChangeState(GameState.MainMenu);
        
        // Chỉ load lại scene nếu chúng ta KHÔNG ở trong scene Menu
        if (SceneManager.GetActiveScene().name != menuSceneName)
        {
            if (Application.CanStreamedLevelBeLoaded(menuSceneName))
            {
                SceneManager.LoadScene(menuSceneName);
            }
            else
            {
                Debug.LogError("[GameManager] Scene '" + menuSceneName + "' not found in Build Settings!");
            }
        }
    }

    public void LoadLevel(string levelName)
    {
        if (string.IsNullOrEmpty(levelName))
        {
            Debug.LogError("[GameManager] Scene name is empty!");
            return;
        }
        Debug.Log("[GameManager] Loading Scene: " + levelName);
        SceneManager.LoadScene(levelName);
        ChangeState(GameState.Playing);
    }

    public void OpenShop()
    {
        ChangeState(GameState.Shop);
    }

    public void PauseGame()
    {
        ChangeState(GameState.Paused);
    }

    public void ResumeGame()
    {
        ChangeState(GameState.Playing);
    }

    public void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}