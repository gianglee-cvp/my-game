using UnityEngine;

public class PauseUI : MonoBehaviour
{
    public GameObject pausePanel;

    void Start()
    {
        GameManager.Instance.OnGameStateChanged += HandleGameState;
        pausePanel.SetActive(false);
    }
    void HandleGameState(GameState state)
    {
        Debug.Log($"[PauseUI] Game State Changed: {state}");
        if (state == GameState.Paused)
        {
            pausePanel.SetActive(true);
        }
        else
        {
            pausePanel.SetActive(false);
        }
    }
    public void ResumeGame()
    {
        GameManager.Instance.ResumeGame();
    }
    public void QuitToMainMenu()
    {
        GameManager.Instance.BackToMenu();
        GameObject mainpanel = GameObject.Find("MainMenuPanel");
        if (mainpanel != null)        {
            mainpanel.SetActive(true);
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= HandleGameState;
    }
}