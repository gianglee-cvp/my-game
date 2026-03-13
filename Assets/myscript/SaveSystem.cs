using UnityEngine;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class GameData
{
    public int coins = 999;
    public string selectedTankId = "DefaultTank";
    public List<string> unlockedTankIds = new List<string> { "DefaultTank" };
}

public static class SaveSystem
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "game_data.json");
    public static GameData Data { get; private set; }

    static SaveSystem()
    {
        Data = new GameData(); // Đảm bảo Data không null ngay lập tức
        Load();
    }

    public static void Save()
    {
        if (Data == null) Data = new GameData();
        string json = JsonUtility.ToJson(Data);
        File.WriteAllText(SavePath, json);
        Debug.Log("Game Saved to: " + SavePath);
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
