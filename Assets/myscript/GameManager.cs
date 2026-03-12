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

    public void BackToMenu()
    {
        ChangeState(GameState.MainMenu);
        SceneManager.LoadScene("MainMenu");
    }

    public void LoadLevel(string levelName)
    {
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
    }
}