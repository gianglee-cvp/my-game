using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class SpecialAmmoButtonUI : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private ControllerTank playerTank;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        FindPlayer();
    }

    void Update()
    {
        if (playerTank == null)
        {
            FindPlayer();
            return;
        }

        // Nếu hết đạn (<= 0) thì chỉnh alpha về 127/255 (~0.5f), ngược lại để 1f (rõ ràng)
        if (playerTank.specialAmmoCount <= 0)
        {
            canvasGroup.alpha = 0.5f; 
        }
        else
        {
            canvasGroup.alpha = 1f;
        }
    }

    void FindPlayer()
    {
        ControllerTank[] tanks = Object.FindObjectsByType<ControllerTank>(FindObjectsSortMode.None);
        foreach (var tank in tanks)
        {
            if (tank.gameObject.activeInHierarchy && tank.tankID.ToString() == SaveSystem.Data.selectedTankId)
            {
                playerTank = tank;
                return;
            }
        }
        
        // Fallback fallback
        playerTank = Object.FindFirstObjectByType<ControllerTank>();
    }
}
