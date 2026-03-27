using UnityEngine;

public class PauseUI : MonoBehaviour
{
    public GameObject pausePanel;

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameState;
        }
        
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    void HandleGameState(GameState state)
    {
        Debug.Log($"[PauseUI] Game State Changed: {state}");
        if (state == GameState.Paused)
        {
            if (pausePanel != null) pausePanel.SetActive(true);
        }
        else
        {
            if (pausePanel != null) pausePanel.SetActive(false);
        }
    }

    public void PauseGame()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.PauseGame();
    }

    public void ResumeGame()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ResumeGame();
    }

    public void TogglePause()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TogglePause();
        }
    }


    public void QuitToMainMenu()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.BackToMenu();
        }
        
        GameObject mainpanel = GameObject.Find("MainMenuPanel");
        if (mainpanel != null)
        {
            mainpanel.SetActive(true);
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameState;
        }
    }


}