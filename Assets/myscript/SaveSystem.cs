using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;

// ---------------------------------------------------------------
// Dữ liệu upgrade của MỘT tank cụ thể
// ---------------------------------------------------------------
[System.Serializable]
public class TankUpgradeData
{
    public int hpLevel = 0;
    public int damageLevel = 0;
}

// ---------------------------------------------------------------
// Wrapper để JsonUtility serialize được Dictionary
// (JsonUtility không hỗ trợ Dictionary trực tiếp)
// ---------------------------------------------------------------
[System.Serializable]
public class TankUpgradeEntry
{
    public string tankId;
    public TankUpgradeData data;
}

// ---------------------------------------------------------------
// Dữ liệu chính của game
// ---------------------------------------------------------------
[System.Serializable]
public class GameData
{
    public int coins = 999;
    public string selectedTankId = "DefaultTank";
    public List<string> unlockedTankIds = new List<string> { "DefaultTank" };
    public bool useJoystickAim = false;

    // Upgrade data cho từng tank (thay thế hpUpgradeLevel / damageUpgradeLevel cũ)
    public List<TankUpgradeEntry> tankUpgradeEntries = new List<TankUpgradeEntry>();

    // --- Helper methods để truy cập nhanh ---

    /// <summary>Lấy upgrade data của tank. Tự tạo mới nếu chưa có.</summary>
    public TankUpgradeData GetUpgrade(string tankId)
    {
        foreach (var entry in tankUpgradeEntries)
        {
            if (entry.tankId == tankId) return entry.data;
        }
        // Chưa có → tạo mới
        var newData = new TankUpgradeData();
        tankUpgradeEntries.Add(new TankUpgradeEntry { tankId = tankId, data = newData });
        return newData;
    }

    /// <summary>Kiểm tra tank đã unlock chưa</summary>
    public bool IsTankUnlocked(string tankId)
    {
        return unlockedTankIds != null && unlockedTankIds.Contains(tankId);
    }
}

// ---------------------------------------------------------------
// SaveSystem — static, quản lý lưu/load
// ---------------------------------------------------------------
public static class SaveSystem
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "game_data.json");
    public static GameData Data { get; private set; }

    public static event Action OnCoinChanged;

    public static void ChangeCoins(int amount)
    {
        if (Data == null) Data = new GameData();
        Data.coins += amount;
        Save();
        OnCoinChanged?.Invoke();
    }

    /// <summary>Truy cập tank đang chọn dưới dạng enum TankID</summary>
    public static TankID SelectedTank
    {
        get
        {
            if (System.Enum.TryParse(Data.selectedTankId, out TankID id))
                return id;
            return TankID.DefaultTank;
        }
        set
        {
            Data.selectedTankId = value.ToString();
            Save();
        }
    }

    static SaveSystem()
    {
        Data = new GameData();
        Load();
    }

    public static void Save()
    {
        if (Data == null) Data = new GameData();
        string json = JsonUtility.ToJson(Data, true);
        File.WriteAllText(SavePath, json);
        Debug.Log("Game Saved to: " + SavePath);
    }

    /// <summary>Xóa file save và reset Data về mặc định</summary>
    public static void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("Save file deleted: " + SavePath);
        }
        Data = new GameData();
        Debug.Log("Game data reset to default.");
    }

    /// <summary>Reset Data về mặc định và lưu lại</summary>
    public static void Reset()
    {
        Data = new GameData();
        Save();
        Debug.Log("Game data reset and saved.");
    }

    public static void Load()
    {
        if (File.Exists(SavePath))
        {
            try
            {
                string json = File.ReadAllText(SavePath);
                Data = JsonUtility.FromJson<GameData>(json);
                if (Data == null) Data = new GameData();
            }
            catch
            {
                Data = new GameData();
            }
        }
        else
        {
            Data = new GameData();
        }
    }
}
