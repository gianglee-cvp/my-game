using UnityEngine;

public class LocalUIManager : MonoBehaviour
{
    [Header("UI Panels for this Screen")]
    public GameObject gameOverPanel;
    public GameObject victoryPanel;

    void Start()
    {
        // Khi scene b?t d?u, dãng ký UI v?i GameManager (Global)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterInGameUI(gameOverPanel, victoryPanel);
        }
        else
        {
            Debug.LogWarning("[LocalUIManager] GameManager.Instance not found! Ensure GameManager exists in the scene or starts from Menu.");
        }
    }
}
