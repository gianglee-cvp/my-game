using UnityEngine;

public enum GlobalScaleCategory
{
    Tank,
    Enemy,
    Bullet,
    Effect,
    Pickup,
    Bomb
}

[System.Serializable]
public class GlobalScaleConfig
{
    [Min(0.01f)] public float tankScaleMul = 1f;
    [Min(0.01f)] public float enemyScaleMul = 1f;
    [Min(0.01f)] public float enemySpeedMul = 1f;
    [Min(0.01f)] public float bulletScaleMul = 1f;
    [Min(0.01f)] public float bulletSpeedMul = 1f;
    [Min(0.01f)] public float effectScaleMul = 1f;
    [Min(0.01f)] public float pickupScaleMul = 1f;
    [Min(0.01f)] public float bombScaleMul = 1f;
}

public class GlobalScaleManager : MonoBehaviour
{
    public static GlobalScaleManager Instance { get; private set; }

    [Header("Global Visual Scale")]
    public bool useGlobalVisualScale = true;
    [Min(0.01f)] public float globalVisualScale = 1f;
    public GlobalScaleConfig config = new GlobalScaleConfig();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public static float GetScale(GlobalScaleCategory category)
    {
        if (Instance == null || !Instance.useGlobalVisualScale)
        {
            return 1f;
        }

        float categoryMul = 1f;
        GlobalScaleConfig cfg = Instance.config;

        switch (category)
        {
            case GlobalScaleCategory.Tank:
                categoryMul = cfg.tankScaleMul;
                break;
            case GlobalScaleCategory.Enemy:
                categoryMul = cfg.enemyScaleMul;
                break;
            case GlobalScaleCategory.Bullet:
                categoryMul = cfg.bulletScaleMul;
                break;
            case GlobalScaleCategory.Effect:
                categoryMul = cfg.effectScaleMul;
                break;
            case GlobalScaleCategory.Pickup:
                categoryMul = cfg.pickupScaleMul;
                break;
            case GlobalScaleCategory.Bomb:
                categoryMul = cfg.bombScaleMul;
                break;
        }

        return Mathf.Max(0.01f, Instance.globalVisualScale * Mathf.Max(0.01f, categoryMul));
    }

    public static float GetEnemySpeedMultiplier()
    {
        if (Instance == null)
        {
            return 1f;
        }

        return Mathf.Max(0.01f, Instance.config.enemySpeedMul);
    }

    public static float GetBulletSpeedMultiplier()
    {
        if (Instance == null)
        {
            return 1f;
        }

        return Mathf.Max(0.01f, Instance.config.bulletSpeedMul);
    }
}
