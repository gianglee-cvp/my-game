using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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

    [Header("Bar Fill Customization")]
    [Tooltip("Danh sách % fill cho từng level HP (0 -> maxLevel). Ví dụ: Level 0 = 0, Level 1 = 0.3...")]
    public float[] hpFillPerLevel;
    [Tooltip("Danh sách % fill cho từng level Damage (0 -> maxLevel)")]
    public float[] damageFillPerLevel;

    [Header("Bar Fill Refs (Image Type = Filled, Method = Horizontal, Origin = Left)")]
    [Tooltip("Kéo Image thanh HP bar xanh vào đây")]
    public Image hpBarFill;
    [Tooltip("Kéo Image thanh Damage bar vào đây")]
    public Image damageBarFill;

    [Tooltip("Thời gian animation fill (giây)")]
    public float fillAnimDuration = 0.3f;

    [Header("Shared UI")]
    public TMP_Text coinText;
    public TMP_Text tankNameText;      // Hiá»ƒn thá»‹ tÃªn tank Ä‘ang upgrade (optional)

    // Tank Ä‘ang Ä‘Æ°á»£c upgrade
    private TankID currentTankId;
    private Coroutine _hpFillCo;
    private Coroutine _dmgFillCo;

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
    /// Load dá»¯ liá»‡u upgrade cá»§a tank cá»¥ thá»ƒ vÃ  refresh UI.
    /// </summary>
    public void LoadTank(TankID tankId)
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
        if (SaveSystem.Data == null) return;

        // Láº¥y upgrade data cá»§a tank hiá»‡n táº¡i
        TankUpgradeData upgrade = SaveSystem.Data.GetUpgrade(currentTankId.ToString());
        int coins       = SaveSystem.Data.coins;
        int hpLevel     = upgrade.hpLevel;
        int damageLevel = upgrade.damageLevel;

        // ---- Coin ----
        if (coinText != null)
            coinText.text = coins.ToString();

        // ---- Tank Name (optional) ----
        if (tankNameText != null)
            tankNameText.text = currentTankId.ToString();

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

        // ---- HP Bar Fill ----
        UpdateBarFill(hpBarFill, hpLevel);

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

        // ---- Damage Bar Fill ----
        UpdateBarFill(damageBarFill, damageLevel);
    }

    // ---------------------------------------------------------------
    /// <summary>Gọi khi nhấn nút Upgrade HP</summary>
    public void OnUpgradeHP()
    {
        if (SaveSystem.Data == null) return;

        TankUpgradeData upgrade = SaveSystem.Data.GetUpgrade(currentTankId.ToString());
        if (upgrade.hpLevel >= maxUpgradeLevel) return;

        int cost = GetCostForLevel(upgrade.hpLevel);
        if (SaveSystem.Data.coins < cost) return;

        // Lưu fill cũ để animate
        float oldFill = hpBarFill != null ? hpBarFill.fillAmount : 0f;

        upgrade.hpLevel++;
        SaveSystem.ChangeCoins(-cost); // Save + bắn event → RefreshUI đặt fill ngay

        // Animate bar fill từ cũ sang mới
        float newFill = GetFillForLevel(hpFillPerLevel, upgrade.hpLevel);
        AnimateHpBar(oldFill, newFill);

        Debug.Log($"[Upgrade] Tank '{currentTankId}' HP lên cấp {upgrade.hpLevel}. Chi phí: {cost}");
    }

    // ---------------------------------------------------------------
    /// <summary>Gọi khi nhấn nút Upgrade Damage</summary>
    public void OnUpgradeDamage()
    {
        if (SaveSystem.Data == null) return;

        TankUpgradeData upgrade = SaveSystem.Data.GetUpgrade(currentTankId.ToString());
        if (upgrade.damageLevel >= maxUpgradeLevel) return;

        int cost = GetCostForLevel(upgrade.damageLevel);
        if (SaveSystem.Data.coins < cost) return;

        // Lưu fill cũ để animate
        float oldFill = damageBarFill != null ? damageBarFill.fillAmount : 0f;

        upgrade.damageLevel++;
        SaveSystem.ChangeCoins(-cost); // Save + bắn event → RefreshUI đặt fill ngay

        // Animate bar fill từ cũ sang mới
        float newFill = GetFillForLevel(damageFillPerLevel, upgrade.damageLevel);
        AnimateDmgBar(oldFill, newFill);

        Debug.Log($"[Upgrade] Tank '{currentTankId}' Damage lÃªn cáº¥p {upgrade.damageLevel}. Chi phÃ: {cost}");
    }

    // ---------------------------------------------------------------
    // BAR FILL HELPERS
    // ---------------------------------------------------------------

    /// <summary>Đặt fill ngay lập tức (dùng trong RefreshUI)</summary>
    private void UpdateBarFill(Image bar, int level)
    {
        if (bar == null) return;
        
        float targetFill = 0f;
        if (bar == hpBarFill) targetFill = GetFillForLevel(hpFillPerLevel, level);
        else if (bar == damageBarFill) targetFill = GetFillForLevel(damageFillPerLevel, level);
        
        bar.fillAmount = targetFill;
    }

    private float GetFillForLevel(float[] fillArray, int level)
    {
        // Nếu mảng chưa được set hoặc không đủ phần tử -> dùng chia đều mặc định
        if (fillArray == null || fillArray.Length <= level)
        {
            return (float)level / maxUpgradeLevel;
        }
        return fillArray[level];
    }

    /// <summary>Animate thanh HP bar từ fromFill đến toFill</summary>
    private void AnimateHpBar(float from, float to)
    {
        if (hpBarFill == null) return;
        if (_hpFillCo != null) StopCoroutine(_hpFillCo);
        _hpFillCo = StartCoroutine(CoFillBar(hpBarFill, from, to));
    }

    /// <summary>Animate thanh Damage bar từ fromFill đến toFill</summary>
    private void AnimateDmgBar(float from, float to)
    {
        if (damageBarFill == null) return;
        if (_dmgFillCo != null) StopCoroutine(_dmgFillCo);
        _dmgFillCo = StartCoroutine(CoFillBar(damageBarFill, from, to));
    }

    private IEnumerator CoFillBar(Image bar, float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fillAnimDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / fillAnimDuration);
            bar.fillAmount = Mathf.Lerp(from, to, t);
            yield return null;
        }
        bar.fillAmount = to;
    }
}
