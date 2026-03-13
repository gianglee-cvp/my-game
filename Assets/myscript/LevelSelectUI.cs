using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[System.Serializable]
public class MapData
{
    public string mapName;
    public string mapScene; // Tên Scene chính xác trong Build Settings
    public string description;
    public Sprite previewSprite;
}

public class LevelSelectUI : MonoBehaviour
{
    [Header("Data")]
    public List<MapData> maps;

    [Header("UI References")]
    public Slider mapSlider;
    public TMP_Text mapNameText;
    public TMP_Text mapDescriptionText;
    public Image mapPreviewImage;
    public GameObject levelSelectPanel;
    public Button playButton;

    private int currentMapIndex = 0;

    private void Start()
    {
        if (mapSlider != null)
        {
            mapSlider.minValue = 0;
            mapSlider.maxValue = maps.Count - 1;
            mapSlider.wholeNumbers = true;
            mapSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        if (playButton != null)
        {
            playButton.onClick.AddListener(ConfirmSelection);
        }

        UpdateMapDisplay(0);
    }

    private void OnSliderValueChanged(float value)
    {
        currentMapIndex = Mathf.RoundToInt(value);
        UpdateMapDisplay(currentMapIndex);
    }

    private void UpdateMapDisplay(int index)
    {
        if (index < 0 || index >= maps.Count) return;

        MapData data = maps[index];
        if (mapNameText != null) mapNameText.text = data.mapName;
        if (mapDescriptionText != null) mapDescriptionText.text = data.description;
        if (mapPreviewImage != null)
        {
            mapPreviewImage.sprite = data.previewSprite;
            // Add a nice fade effect or just change it
        }

        // Thêm âm thanh nhấp chuột nhẹ nếu có
    }

    public void ConfirmSelection()
    {

        // SceneManager.LoadScene("Map1");
        // return;
        Debug.Log("[LevelSelectUI] ConfirmSelection button pressed.");
        
        if (maps == null || maps.Count == 0)
        {
            Debug.LogError("[LevelSelectUI] Maps list is EMPTY in Inspector!");
            return;
        }

        if (currentMapIndex >= maps.Count)
        {
            Debug.LogError("[LevelSelectUI] Index " + currentMapIndex + " is out of range!");
            return;
        }

        string sceneToLoad = maps[currentMapIndex].mapScene;
        
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("[LevelSelectUI] Map Scene name is EMPTY for index " + currentMapIndex);
            return;
        }

        Debug.Log("[LevelSelectUI] Sending load request for: " + sceneToLoad);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadLevel(sceneToLoad);
        }
        else
        {
            Debug.LogError("[LevelSelectUI] GameManager.Instance is NULL!");
        }
    }

    public void GoBack()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.BackToMenu();
        }
    }

    private void Update()
    {
        // Hiển thị/ẩn panel dựa trên trạng thái của GameManager
        if (GameManager.Instance != null && levelSelectPanel != null)
        {
            bool isLevelSelect = GameManager.Instance.currentState == GameState.LevelSelect;
            if (levelSelectPanel.activeSelf != isLevelSelect)
            {
                levelSelectPanel.SetActive(isLevelSelect);
                if (isLevelSelect) UpdateMapDisplay(currentMapIndex);
            }
        }
    }
}
