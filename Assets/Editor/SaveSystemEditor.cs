#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class SaveSystemEditor
{
    [MenuItem("Tools/Save System/Delete Save File")]
    public static void DeleteSaveFile()
    {
        string path = Path.Combine(Application.persistentDataPath, "game_data.json");

        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("[Editor] Save file deleted: " + path);
            EditorUtility.DisplayDialog("Done", "Save file deleted!\n" + path, "OK");
        }
        else
        {
            Debug.Log("[Editor] No save file found at: " + path);
            EditorUtility.DisplayDialog("Not Found", "No save file found.\n" + path, "OK");
        }
    }

    [MenuItem("Tools/Save System/Show Save Path")]
    public static void ShowSavePath()
    {
        string path = Path.Combine(Application.persistentDataPath, "game_data.json");
        Debug.Log("[Editor] Save path: " + path);
        EditorUtility.DisplayDialog("Save Path", path, "OK");
    }
}
#endif
