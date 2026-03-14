using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class TankShopData
{
    public string tankId;
    public string tankName;
    [TextArea] public string description;
    public GameObject tankPrefab; // Thêm Prefab 3D để hiển thị trong Shop
    public int price;
}

public class TankShopUI : MonoBehaviour
{
    [Header("Data")]
    public List<TankShopData> tankItems;

    [Header("UI References")]
    public Slider itemSlider;
    public TMP_Text tankNameText;
    public TMP_Text tankDescriptionText;
    public TMP_Text priceText;
    public TMP_Text statusText;
    public TMP_Text coinText;
    public Button actionButton; 
    public TMP_Text actionButtonText;
    public Button backButton;

    [Header("3D Preview")]
    public Transform previewPoint; // Điểm đặt xe tăng 3D trong Shop
    public float rotationSpeed = 50f;
    private GameObject currentPreviewObject;

    private int currentIndex = 0;

    private void Start()
    {
        if (itemSlider != null)
        {
            itemSlider.minValue = 0;
            itemSlider.maxValue = tankItems.Count - 1;
            itemSlider.wholeNumbers = true;
            itemSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        if (actionButton != null)
        {
            actionButton.onClick.AddListener(OnActionButtonClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(GoBack);
        }

        UpdateShopDisplay(0);
    }

    private void OnEnable()
    {
        UpdateShopDisplay(currentIndex);
    }

    private void OnSliderValueChanged(float value)
    {
        currentIndex = Mathf.RoundToInt(value);
        UpdateShopDisplay(currentIndex);
    }

    private void Update()
    {
        // Làm cho xe tăng trong Shop tự xoay tròn
        if (currentPreviewObject != null)
        {
            currentPreviewObject.transform.Rotate(Vector3.up, rotationSpeed * Time.unscaledDeltaTime);
        }
    }

    public void UpdateShopDisplay(int index)
    {
        if (tankItems == null || index < 0 || index >= tankItems.Count) return;

        TankShopData data = tankItems[index];

        // Xử lý hiển thị 3D
        if (previewPoint != null)
        {
            if (currentPreviewObject != null) Destroy(currentPreviewObject);

            if (data.tankPrefab != null)
            {
                currentPreviewObject = Instantiate(data.tankPrefab, previewPoint.position, previewPoint.rotation, previewPoint);
                currentPreviewObject.SetActive(true); 
                currentPreviewObject.transform.localScale = Vector3.one * 4f; // Chỉnh scale xe tăng lên 4
                
                // Tắt các script điều khiển để xe không tự chạy trong Shop
                MonoBehaviour[] scripts = currentPreviewObject.GetComponentsInChildren<MonoBehaviour>();
                foreach (var s in scripts)
                {
                    // Tắt hết script trừ script Rotate nếu có
                    if (s != null && s.GetType().Name != "ItemRotate") s.enabled = false;
                }
            }
        }

        if (tankNameText != null) tankNameText.text = data.tankName;
        if (tankDescriptionText != null) tankDescriptionText.text = data.description;
        
        if (coinText != null && SaveSystem.Data != null) 
            coinText.text = SaveSystem.Data.coins.ToString();

        UpdateActionButtonState(data);
    }

    private void UpdateActionButtonState(TankShopData data)
    {
        if (data == null || SaveSystem.Data == null || actionButton == null || actionButtonText == null) return;

        bool isUnlocked = SaveSystem.Data.unlockedTankIds != null && SaveSystem.Data.unlockedTankIds.Contains(data.tankId);
        bool isSelected = SaveSystem.Data.selectedTankId == data.tankId;

        if (isSelected)
        {
            actionButtonText.text = "SELECTED";
            actionButton.interactable = false;
            
            if (priceText != null) priceText.gameObject.SetActive(false);
            if (statusText != null) 
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "CURRENT TANK";
            }
        }
        else if (isUnlocked)
        {
            actionButtonText.text = "SELECT";
            actionButton.interactable = true;
            
            if (priceText != null) priceText.gameObject.SetActive(false);
            if (statusText != null) 
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "UNLOCKED";
            }
        }
        else
        {
            actionButtonText.text = "BUY";
            
            if (priceText != null) 
            {
                priceText.gameObject.SetActive(true);
                priceText.text = data.price.ToString() + " COINS";
            }
            if (statusText != null) 
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "LOCKED";
            }
            
            // Log giá tiền ra Console để kiểm tra
            Debug.Log("[Shop] Tank: " + data.tankName + " | Price: " + data.price + " | Coins: " + SaveSystem.Data.coins);
            
            actionButton.interactable = SaveSystem.Data.coins >= data.price;
        }
    }

    public void OnActionButtonClicked()
    {
        TankShopData data = tankItems[currentIndex];
        bool isUnlocked = SaveSystem.Data.unlockedTankIds.Contains(data.tankId);

        if (isUnlocked)
        {
            // Chọn xe tăng
            SaveSystem.Data.selectedTankId = data.tankId;
            SaveSystem.Save();
            Debug.Log("Selected Tank: " + data.tankId);
        }
        else
        {
            // Mua xe tăng
            if (SaveSystem.Data.coins >= data.price)
            {
                SaveSystem.Data.coins -= data.price;
                SaveSystem.Data.unlockedTankIds.Add(data.tankId);
                SaveSystem.Data.selectedTankId = data.tankId;
                SaveSystem.Save();
                Debug.Log("Purchased Tank: " + data.tankId);
            }
        }
        UpdateShopDisplay(currentIndex);
    }

    public void GoBack()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.BackToMenu();
        }
    }
}
