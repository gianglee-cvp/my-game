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
        TankID selected = SaveSystem.SelectedTank;
        Debug.Log("[TankSelector] Start Selection for: " + selected);
        
        // 1. Tắt tất cả các xe tăng trước (Đảm bảo các Disable() chạy hết)
        foreach (var tank in tanks)
        {
            if (tank.tankObject != null)
            {
                tank.tankObject.SetActive(false);
            }
        }

        // 2. Chỉ bật chiếc xe tăng đã được chọn (Để OnEnable() của xe này chạy cuối cùng)
        foreach (var tank in tanks)
        {
            if (tank.tankObject != null && tank.type == selected)
            {
                tank.tankObject.SetActive(true);
                ActivePlayer = tank.tankObject;
                OnPlayerTankSelected?.Invoke(ActivePlayer);
                Debug.Log("[TankSelector] Activating " + tank.type);
                break; // Tìm thấy rồi thì thôi
            }
        }
    }

}
