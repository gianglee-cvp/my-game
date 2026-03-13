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
    public Sprite previewSprite;
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
    public TMP_Text coinText;
    public Image tankPreviewImage;
    public Button actionButton; // Nút Buy hoặc Select
    public TMP_Text actionButtonText;

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

    public void UpdateShopDisplay(int index)
    {
        if (tankItems == null || index < 0 || index >= tankItems.Count) return;

        TankShopData data = tankItems[index];
        if (tankNameText != null) tankNameText.text = data.tankName;
        if (tankDescriptionText != null) tankDescriptionText.text = data.description;
        if (tankPreviewImage != null) tankPreviewImage.sprite = data.previewSprite;
        
        if (coinText != null && SaveSystem.Data != null) 
            coinText.text = "Coins: " + SaveSystem.Data.coins;

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
            if (priceText != null) priceText.text = "CURRENT TANK";
        }
        else if (isUnlocked)
        {
            actionButtonText.text = "SELECT";
            actionButton.interactable = true;
            if (priceText != null) priceText.text = "UNLOCKED";
        }
        else
        {
            actionButtonText.text = "BUY";
            if (priceText != null) priceText.text = data.price.ToString() + " COINS";
            
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
