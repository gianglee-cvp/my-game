using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class TankShopData
{
    public TankID tankId;
    public string tankName;
    [TextArea] public string description;
    public GameObject tankPrefab;
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

    [Header("Tab Navigation")]
    [Tooltip("Panel chứa nội dung mua xe tăng")]
    public GameObject tankShopContentPanel;
    [Tooltip("Panel chứa nội dung upgrade HP/Damage")]
    public GameObject upgradeContentPanel;
    [Tooltip("Script TankUpgradeUI gắn trên upgradeContentPanel")]
    public TankUpgradeUI tankUpgradeUI;
    public Button tabShopButton;
    public Button tabUpgradeButton;

    [Header("3D Preview")]
    public Transform previewPoint;
    public float rotationSpeed = 50f;
    private GameObject currentPreviewObject;

    private int currentIndex = 0;
    private bool isOnUpgradeTab = false; // Theo dõi đang ở tab nào

    // ---------------------------------------------------------------
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
            actionButton.onClick.AddListener(OnActionButtonClicked);

        if (backButton != null)
            backButton.onClick.AddListener(GoBack);

        // Mặc định hiện tab Shop
        ShowTab(false);
        UpdateShopDisplay(0);
    }

    // ---------------------------------------------------------------
    private void OnEnable()
    {
        SaveSystem.OnCoinChanged += UpdateCoinUI;
        UpdateShopDisplay(currentIndex);
        UpdateCoinUI();
    }

    private void OnDisable()
    {
        SaveSystem.OnCoinChanged -= UpdateCoinUI;
    }

    private void UpdateCoinUI()
    {
        if (coinText != null && SaveSystem.Data != null)
            coinText.text = SaveSystem.Data.coins.ToString();
    }

    // ---------------------------------------------------------------
    // Tab wrappers — gán vào OnClick trong Inspector
    // ---------------------------------------------------------------

    /// <summary>Gán vào OnClick của tabShopButton trong Inspector</summary>
    public void ShowShopTab() => ShowTab(false);

    /// <summary>Gán vào OnClick của tabUpgradeButton trong Inspector</summary>
    public void ShowUpgradeTab() => ShowTab(true);

    // ---------------------------------------------------------------
    /// <summary>
    /// Chuyển đổi giữa tab Shop và tab Upgrade.
    /// Nếu cố vào Upgrade mà tank chưa unlock → tự chuyển về Shop.
    /// </summary>
    public void ShowTab(bool isUpgrade)
    {
        // ---- Guard: Không cho vào Upgrade nếu tank chưa unlock ----
        if (isUpgrade && tankItems != null && currentIndex < tankItems.Count)
        {
            string idStr = tankItems[currentIndex].tankId.ToString();
            if (!SaveSystem.Data.IsTankUnlocked(idStr))
            {
                Debug.Log($"[Shop] Tank '{idStr}' chÆ°a unlock â€” khÃ´ng cho vÃ o Upgrade.");
                isUpgrade = false; // Force vá» Shop
            }
        }

        isOnUpgradeTab = isUpgrade;

        Debug.Log("ShowTab: " + (isUpgrade ? "UPGRADE" : "SHOP"));

        if (tankShopContentPanel != null)
            tankShopContentPanel.SetActive(!isUpgrade);
        if (upgradeContentPanel != null)
            upgradeContentPanel.SetActive(isUpgrade);

        // Nếu vào Upgrade → load đúng tank hiện tại
        if (isUpgrade && tankUpgradeUI != null)
        {
            TankID id = tankItems[currentIndex].tankId;
            tankUpgradeUI.LoadTank(id);
        }

        UpdateCoinUI();
    }

    // ---------------------------------------------------------------
    private void OnSliderValueChanged(float value)
    {
        currentIndex = Mathf.RoundToInt(value);
        UpdateShopDisplay(currentIndex);

        // ---- Xử lý tab khi đổi tank bằng slider ----
        if (isOnUpgradeTab)
        {
            TankID id = tankItems[currentIndex].tankId;

            if (SaveSystem.Data.IsTankUnlocked(id.ToString()))
            {
                // Tank mới unlocked → giữ tab Upgrade, load data mới
                if (tankUpgradeUI != null)
                    tankUpgradeUI.LoadTank(id);
            }
            else
            {
                // Tank má»›i locked â†’ tá»± chuyá»ƒn vá» tab Shop
                Debug.Log($"[Shop] Ä á»•i sang tank '{id}' (locked) â€” chuyá»ƒn vá» tab Shop.");
                ShowTab(false);
            }
        }
    }

    // ---------------------------------------------------------------
    private void Update()
    {
        if (currentPreviewObject != null)
        {
            currentPreviewObject.transform.Rotate(Vector3.up, rotationSpeed * Time.unscaledDeltaTime);
        }
    }

    // ---------------------------------------------------------------
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
                currentPreviewObject.transform.localScale = Vector3.one * 4f;
                
                MonoBehaviour[] scripts = currentPreviewObject.GetComponentsInChildren<MonoBehaviour>();
                foreach (var s in scripts)
                {
                    if (s != null && s.GetType().Name != "ItemRotate") s.enabled = false;
                }
            }
        }

        if (tankNameText != null) tankNameText.text = data.tankName;
        if (tankDescriptionText != null) tankDescriptionText.text = data.description;

        UpdateCoinUI();
        UpdateActionButtonState(data);
    }

    // ---------------------------------------------------------------
    private void UpdateActionButtonState(TankShopData data)
    {
        if (data == null || SaveSystem.Data == null || actionButton == null || actionButtonText == null) return;

        bool isUnlocked = SaveSystem.Data.IsTankUnlocked(data.tankId.ToString());
        bool isSelected = SaveSystem.SelectedTank == data.tankId;

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
            
            actionButton.interactable = SaveSystem.Data.coins >= data.price;
        }
    }

    // ---------------------------------------------------------------
    public void OnActionButtonClicked()
    {
        TankShopData data = tankItems[currentIndex];
        bool isUnlocked = SaveSystem.Data.IsTankUnlocked(data.tankId.ToString());

        if (isUnlocked)
        {
            SaveSystem.SelectedTank = data.tankId;
            Debug.Log("Selected Tank: " + data.tankId);
        }
        else
        {
            if (SaveSystem.Data.coins >= data.price)
            {
                SaveSystem.Data.unlockedTankIds.Add(data.tankId.ToString());
                SaveSystem.SelectedTank = data.tankId;
                SaveSystem.ChangeCoins(-data.price);
                Debug.Log("Purchased Tank: " + data.tankId);
            }
        }
        UpdateShopDisplay(currentIndex);
    }

    // ---------------------------------------------------------------
    public void GoBack()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.BackToMenu();
        }
    }
}
