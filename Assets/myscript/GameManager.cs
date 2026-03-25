using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    MainMenu,
    LevelSelect,
    Playing,
    Shop,
    Paused,
    GameOver,
    Victory
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
    public GameObject gameOverPanel;
    public GameObject victoryPanel;

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
            // If we just loaded the Menu scene, keep the scene instance so UI button references stay valid.
            if (gameObject.scene.name == menuSceneName)
            {
                Destroy(Instance.gameObject);
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    void Start()
    {
        // Force-fire the event for the initial state so listeners (AudioManager, etc.) sync up
        ApplyState(currentState);
        OnGameStateChanged?.Invoke(currentState);
        Debug.Log("[GameManager] Initial State: " + currentState);
    }

    void Update()
    {
      //  Debug.Log("[GameManager] Current State: " + currentState);
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("[GameManager] Current State: " + currentState);
            if (currentState == GameState.Playing)
                PauseGame();
            else if (currentState == GameState.Paused)
                ResumeGame();
        }   
        // Tự động quản lý ẩn/hiện mọi Panel dựa trên GameState
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(currentState == GameState.MainMenu);

        if (levelSelectPanel != null)
            levelSelectPanel.SetActive(currentState == GameState.LevelSelect);

        if (shopPanel != null)
            shopPanel.SetActive(currentState == GameState.Shop);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(currentState == GameState.GameOver);

        if (victoryPanel != null)
            victoryPanel.SetActive(currentState == GameState.Victory);
    }

    public void ChangeState(GameState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        ApplyState(currentState);
        OnGameStateChanged?.Invoke(currentState);
        Debug.Log("[GameManager] State Changed to: " + newState);
    }

    /// <summary>
    /// Áp dụng các thay đổi phù hợp cho từng GameState (timeScale, cursor, panels…)
    /// </summary>
    private void ApplyState(GameState state)
    {
        switch (state)
        {
            case GameState.LevelSelect:
            case GameState.Shop:
            case GameState.Paused:
            case GameState.GameOver:
            case GameState.Victory:
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
            case GameState.MainMenu:
                Debug.Log("[GameManager] Entering Main Menu. Resetting Time Scale and Cursor.");
                if (mainMenuPanel != null)
                {
                    mainMenuPanel.SetActive(true);
                }
                else
                {
                    Debug.LogWarning("[GameManager] mainMenuPanel is not assigned in this scene.");
                }
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
            case GameState.Playing:
                Time.timeScale = 1f;
                Cursor.visible = true;
                break;
        }
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
        Debug.Log("[GameManager] BackToMenu called. Current Scene: " + SceneManager.GetActiveScene().name);
        ChangeState(GameState.MainMenu);
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

    public void RegisterInGameUI(GameObject gameOver, GameObject victory)
    {
        gameOverPanel = gameOver;
        victoryPanel = victory;
    }

    public void TriggerGameOver()
    {
        if (currentState == GameState.GameOver || currentState == GameState.Victory) return;
        
        ChangeState(GameState.GameOver);
        StartCoroutine(GameEndSequenceRoutine());
    }

    public void TriggerVictory()
    {
        if (currentState == GameState.GameOver || currentState == GameState.Victory) return;

        ChangeState(GameState.Victory);
        StartCoroutine(GameEndSequenceRoutine());
    }

    private System.Collections.IEnumerator GameEndSequenceRoutine()
    {
        // Sử dụng WaitForSecondsRealtime vì Time.timeScale bị set = 0 trong ChangeState
        yield return new WaitForSecondsRealtime(3f);
        
        // Trở về menu
        BackToMenu();
    }

}
