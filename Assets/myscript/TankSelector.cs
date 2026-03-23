using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct TankEntry
{
    public TankID type;
    public GameObject tankObject;
}

public class TankSelector : MonoBehaviour
{
    public static System.Action<GameObject> OnPlayerTankSelected;
    public static GameObject ActivePlayer { get; private set; }
    
    public List<TankEntry> tanks = new List<TankEntry>();

    void Start()
    {
        // 1. Get the selected tank enum from SaveSystem
        TankID selected = SaveSystem.SelectedTank;
        Debug.Log("[TankSelector] Selected Tank: " + selected);
        
        // 2. Set active status for each tank in the list
        foreach (var tank in tanks)
        {
            if (tank.tankObject != null)
            {
                bool isActive = (tank.type == selected);
                tank.tankObject.SetActive(isActive);
                
                if (isActive)
                {
                    ActivePlayer = tank.tankObject;
                    OnPlayerTankSelected?.Invoke(ActivePlayer);
                    Debug.Log("[TankSelector] Activating " + tank.type);
                }
            }
        }
    }
}
