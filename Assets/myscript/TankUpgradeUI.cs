using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel Upgrade — hỗ trợ upgrade HP và Damage riêng cho từng tank.
/// Gọi LoadTank(tankId) để chuyển sang tank cần upgrade.
/// </summary>
public class TankUpgradeUI : MonoBehaviour
{
    [Header("Upgrade Settings")]
    [Tooltip("Số cấp tối đa cho mỗi loại upgrade")]
    public int maxUpgradeLevel = 5;

    [Tooltip("Lượng HP thêm vào mỗi cấp upgrade")]
    public float hpBonusPerLevel = 25f;

    [Tooltip("Lượng Damage nhân với mỗi cấp upgrade (phần trăm dạng 0.1 = +10%)")]
    public float damageBonusPerLevel = 0.10f;

    [Tooltip("Giá cơ bản (mỗi cấp tiếp theo nhân giá lên x levelCostMultiplier)")]
    public int baseUpgradeCost = 100;

    [Tooltip("Hệ số nhân giá cho mỗi cấp")]
    public float levelCostMultiplier = 1.5f;

    [Header("HP Upgrade UI")]
    public TMP_Text hpLevelText;       // "Level 2 / 5"
    public TMP_Text hpBonusText;       // "+50 HP"
    public TMP_Text hpCostText;        // "150 Coins"
    public Button   hpUpgradeButton;

    [Header("Damage Upgrade UI")]
    public TMP_Text damageLevelText;   // "Level 1 / 5"
    public TMP_Text damageBonusText;   // "+10% DMG"
    public TMP_Text damageCostText;    // "100 Coins"
    public Button   damageUpgradeButton;

    [Header("Shared UI")]
    public TMP_Text coinText;
    public TMP_Text tankNameText;      // Hiển thị tên tank đang upgrade (optional)

    // Tank đang được upgrade
    private string currentTankId;

    // ---------------------------------------------------------------
    void Start()
    {
        if (hpUpgradeButton != null)
            hpUpgradeButton.onClick.AddListener(OnUpgradeHP);

        if (damageUpgradeButton != null)
            damageUpgradeButton.onClick.AddListener(OnUpgradeDamage);
    }

    // ---------------------------------------------------------------
    void OnEnable()
    {
        SaveSystem.OnCoinChanged += RefreshUI;
        RefreshUI();
    }

    void OnDisable()
    {
        SaveSystem.OnCoinChanged -= RefreshUI;
    }

    // ---------------------------------------------------------------
    /// <summary>
    /// Được gọi từ TankShopUI khi chuyển sang tab Upgrade hoặc khi đổi tank bằng slider.
    /// Load dữ liệu upgrade của tank cụ thể và refresh UI.
    /// </summary>
    public void LoadTank(string tankId)
    {
        currentTankId = tankId;
        Debug.Log($"[Upgrade] Loaded tank: {tankId}");
        RefreshUI();
    }

    // ---------------------------------------------------------------
    private int GetCostForLevel(int currentLevel)
    {
        return Mathf.RoundToInt(baseUpgradeCost * Mathf.Pow(levelCostMultiplier, currentLevel));
    }

    // ---------------------------------------------------------------
    public void RefreshUI()
    {
        if (SaveSystem.Data == null || string.IsNullOrEmpty(currentTankId)) return;

        // Lấy upgrade data của tank hiện tại
        TankUpgradeData upgrade = SaveSystem.Data.GetUpgrade(currentTankId);
        int coins       = SaveSystem.Data.coins;
        int hpLevel     = upgrade.hpLevel;
        int damageLevel = upgrade.damageLevel;

        // ---- Coin ----
        if (coinText != null)
            coinText.text = coins.ToString();

        // ---- Tank Name (optional) ----
        if (tankNameText != null)
            tankNameText.text = currentTankId;

        // ---- HP ----
        if (hpLevelText != null)
            hpLevelText.text = $"Level {hpLevel} / {maxUpgradeLevel}";

        if (hpBonusText != null)
            hpBonusText.text = $"+{hpLevel * hpBonusPerLevel:0} HP";

        if (hpUpgradeButton != null)
        {
            bool maxed = hpLevel >= maxUpgradeLevel;
            int  cost  = GetCostForLevel(hpLevel);

            hpUpgradeButton.interactable = !maxed && coins >= cost;

            if (hpCostText != null)
                hpCostText.text = maxed ? "MAX" : $"{cost}";
        }

        // ---- Damage ----
        if (damageLevelText != null)
            damageLevelText.text = $"Level {damageLevel} / {maxUpgradeLevel}";

        if (damageBonusText != null)
            damageBonusText.text = $"+{damageLevel * damageBonusPerLevel * 100f:0}% DMG";

        if (damageUpgradeButton != null)
        {
            bool maxed = damageLevel >= maxUpgradeLevel;
            int  cost  = GetCostForLevel(damageLevel);

            damageUpgradeButton.interactable = !maxed && coins >= cost;

            if (damageCostText != null)
                damageCostText.text = maxed ? "MAX" : $"{cost}";
        }
    }

    // ---------------------------------------------------------------
    /// <summary>Gọi khi nhấn nút Upgrade HP</summary>
    public void OnUpgradeHP()
    {
        if (SaveSystem.Data == null || string.IsNullOrEmpty(currentTankId)) return;

        TankUpgradeData upgrade = SaveSystem.Data.GetUpgrade(currentTankId);
        if (upgrade.hpLevel >= maxUpgradeLevel) return;

        int cost = GetCostForLevel(upgrade.hpLevel);
        if (SaveSystem.Data.coins < cost) return;

        upgrade.hpLevel++;
        SaveSystem.ChangeCoins(-cost); // Save + bắn event

        Debug.Log($"[Upgrade] Tank '{currentTankId}' HP lên cấp {upgrade.hpLevel}. Chi phí: {cost}");
    }

    // ---------------------------------------------------------------
    /// <summary>Gọi khi nhấn nút Upgrade Damage</summary>
    public void OnUpgradeDamage()
    {
        if (SaveSystem.Data == null || string.IsNullOrEmpty(currentTankId)) return;

        TankUpgradeData upgrade = SaveSystem.Data.GetUpgrade(currentTankId);
        if (upgrade.damageLevel >= maxUpgradeLevel) return;

        int cost = GetCostForLevel(upgrade.damageLevel);
        if (SaveSystem.Data.coins < cost) return;

        upgrade.damageLevel++;
        SaveSystem.ChangeCoins(-cost); // Save + bắn event

        Debug.Log($"[Upgrade] Tank '{currentTankId}' Damage lên cấp {upgrade.damageLevel}. Chi phí: {cost}");
    }

    // ---------------------------------------------------------------
    // STATIC HELPERS — gọi từ gameplay scripts khi cần biết bonus
    // ---------------------------------------------------------------

    /// <summary>
    /// Trả về tổng bonus HP của tank đang được chọn.
    /// Gọi từ HP script khi spawn tank.
    /// </summary>
    public static float GetTotalHPBonus(float hpBonusPerLvl)
    {
        if (SaveSystem.Data == null) return 0f;
        TankUpgradeData upgrade = SaveSystem.Data.GetUpgrade(SaveSystem.Data.selectedTankId);
        return upgrade.hpLevel * hpBonusPerLvl;
    }

    /// <summary>
    /// Trả về hệ số nhân Damage của tank đang được chọn (ví dụ 1.30 = +30%).
    /// Gọi từ bulletTank khi tính damage.
    /// </summary>
    public static float GetDamageMultiplier(float damageBonusPerLvl)
    {
        if (SaveSystem.Data == null) return 1f;
        TankUpgradeData upgrade = SaveSystem.Data.GetUpgrade(SaveSystem.Data.selectedTankId);
        return 1f + upgrade.damageLevel * damageBonusPerLvl;
    }
}
