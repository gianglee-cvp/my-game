using UnityEngine;
using ProceduralForceField;


public class ControllerTank : MonoBehaviour
{
    [SerializeField] private bool enableDebugLogs = false;
    public GameObject shieldObject;
    private ProceduralForceFieldOverlay shieldOverlay;

    public float Movespeed = 8f;
    float RotateSpeed = 60f;

    Rigidbody TankEngine;

    public GameObject Tower;
    public Camera CameraFollow;

    public ParticleSystem[] ShootFX;

    [Header("Bullet System")]
    public GameObject[] bulletPrefabs;
    [Min(0)] public int prewarmPerBulletPrefab = 24;
    [Range(0, 10)]
    public int currentBulletIndex = 0; // chá»n loáº¡i Ä‘áº¡n trong Inspector
    public Transform shootElement;

    private float nextFireTime = 0f;

    // Sá»‘ coin nháº·t Ä‘Æ°á»£c
    public int coinCount = 0;

    // Tráº¡ng thÃ¡i khiÃªn
    public bool isShield = false;
    private float shieldTimer = 0f;

    [Header("Debuff (from enemy bullets)")]
    public Transform[] stunEffectPoints;
    public float knockbackResistance = 1f;

    private bool isStunned = false;
    private float stunTimer = 0f;
    private readonly System.Collections.Generic.List<ParticleSystem> stunEffectInstances =
        new System.Collections.Generic.List<ParticleSystem>();
    private bool isKnockedBack = false;
    private float knockbackTimer = 0f;
    private Vector3 knockbackVelocity = Vector3.zero;
    private Vector3 baseLocalScale;

    void Awake()
    {
        baseLocalScale = transform.localScale;
    }

    void Start()
    {
        ApplyGlobalScale();

        TankEngine = GetComponent<Rigidbody>();
        if (shieldObject != null)
        {
            shieldOverlay = shieldObject.GetComponent<ProceduralForceFieldOverlay>();
            shieldObject.SetActive(false); // ban Ä‘áº§u táº¯t
        }

        if (bulletPrefabs != null && prewarmPerBulletPrefab > 0)
        {
            for (int i = 0; i < bulletPrefabs.Length; i++)
            {
                ProjectilePool.Prewarm(bulletPrefabs[i], prewarmPerBulletPrefab);
            }
        }
    }

    void OnEnable()
    {
        ApplyGlobalScale();
    }

    void Move()
    {
        Vector3 move = transform.forward *
                       Input.GetAxis("Vertical") *
                       Movespeed *
                       Time.deltaTime;

        TankEngine.MovePosition(TankEngine.position + move);
    }

    void Rotate()
    {
        float r = Input.GetAxis("Horizontal") *
                  RotateSpeed *
                  Time.deltaTime;

        Quaternion rotate = Quaternion.Euler(0, r, 0);
        TankEngine.MoveRotation(TankEngine.rotation * rotate);
    }

    void RotateTower()
    {
        Ray ray = CameraFollow.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 target = ray.GetPoint(distance);
            Vector3 direction = target - transform.position;

            float rotation = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            Tower.transform.rotation = Quaternion.Euler(0, rotation, 0);
        }
    }

    void Fire()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (Time.time < nextFireTime) return;

        // âœ… Kiá»ƒm tra máº£ng Ä‘áº¡n há»£p lá»‡
        if (bulletPrefabs == null || bulletPrefabs.Length == 0)
        {
            Debug.LogWarning("ChÆ°a gÃ¡n bulletPrefabs!");
            return;
        }

        if (currentBulletIndex < 0 || currentBulletIndex >= bulletPrefabs.Length)
        {
            Debug.LogWarning("Index Ä‘áº¡n khÃ´ng há»£p lá»‡: " + currentBulletIndex);
            return;
        }

        LogDebug("Player Fire - Äáº¡n index: " + currentBulletIndex);

        for (int i = 0; i < ShootFX.Length; i++)
        {
            ShootFX[i].Play();
        }

        GameObject bulletInstance = ProjectilePool.Spawn(
            bulletPrefabs[currentBulletIndex],
            shootElement.position,
            shootElement.rotation
        );

        // ðŸ”¥ Láº¥y cooldown tá»« bullet
        bulletTank bulletScript = bulletInstance.GetComponent<bulletTank>();
        if (bulletScript != null)
        {
            bulletScript.bulletTeam = Team.Player;
            nextFireTime = Time.time + 0.2f * bulletScript.fireCooldown;
        }
        else
        {
            nextFireTime = Time.time + 0.5f; // fallback
        }
    }
    void SwitchWeapon()
    {
        if (bulletPrefabs == null || bulletPrefabs.Length == 0)
            return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll > 0f)
        {
            currentBulletIndex++;
            if (currentBulletIndex >= bulletPrefabs.Length)
                currentBulletIndex = 0;
        }
        else if (scroll < 0f)
        {
            currentBulletIndex--;
            if (currentBulletIndex < 0)
                currentBulletIndex = bulletPrefabs.Length - 1;
        }
    }

    void Update()
    {
        UpdateStatusEffects();
        if (isKnockedBack || isStunned)
        {
            UpdateShield();
            return;
        }

        Move();
        Rotate();
        RotateTower();
        Fire();
        SwitchWeapon();
        UpdateShield();
    }

    void UpdateStatusEffects()
    {
        if (isKnockedBack)
        {
            TankEngine.MovePosition(TankEngine.position + knockbackVelocity * Time.deltaTime);

            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0f)
            {
                isKnockedBack = false;
                knockbackVelocity = Vector3.zero;
            }
        }

        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                isStunned = false;
                ClearStunEffects();
            }
        }
    }

    void UpdateShield()
    {
        if (!isShield) return;

        shieldTimer -= Time.deltaTime;

        if (shieldTimer <= 0f)
        {
            isShield = false;
            shieldTimer = 0f;

            if (shieldObject != null)
            {
                shieldObject.SetActive(false);
        }

        LogDebug("Shield Ä‘Ã£ háº¿t!");
    }
    }
    /// collect item 
    public void AddCoin(int amount)
    {
        coinCount += amount;
        LogDebug("Player nháº­n " + amount + " coin. Tá»•ng coin: " + coinCount);
    }

    public void ActivateShield(float duration)
    {
        isShield = true;
        shieldTimer = duration;
        ClearAllDebuffs();

        if (shieldObject != null)
        {
            shieldObject.SetActive(true);

            if (shieldOverlay != null)
            {
                shieldOverlay.Trigger(transform.position);
            }
        }

        LogDebug("Player báº­t shield trong " + duration + " giÃ¢y");
    }

    public void Stun(float duration, ParticleSystem stunEffect)
    {
        if (duration <= 0f) return;
        if (isShield) return;

        isStunned = true;
        stunTimer = Mathf.Max(stunTimer, duration);

        if (stunEffect != null && stunEffectPoints != null && stunEffectPoints.Length > 0)
        {
            for (int i = 0; i < stunEffectPoints.Length; i++)
            {
                Transform point = stunEffectPoints[i];
                if (point == null) continue;

                ParticleSystem fx = EffectPool.Spawn(stunEffect, point.position, point.rotation, point);
                var main = fx.main;
                main.loop = true;
                fx.Play();
                stunEffectInstances.Add(fx);
            }
        }
    }

    public void Knockback(Vector3 direction, float force, float duration)
    {
        if (isShield) return;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;
        if (duration <= 0f || force <= 0f) return;

        float finalResistance = Mathf.Max(0.01f, knockbackResistance);
        knockbackVelocity = direction.normalized * (force / finalResistance);
        knockbackTimer = Mathf.Max(knockbackTimer, duration);
        isKnockedBack = true;
    }

    void ClearStunEffects()
    {
        for (int i = 0; i < stunEffectInstances.Count; i++)
        {
            ParticleSystem fx = stunEffectInstances[i];
            if (fx == null) continue;
            fx.Stop();
            EffectPool.Despawn(fx);
        }
        stunEffectInstances.Clear();
    }

    void ClearAllDebuffs()
    {
        isStunned = false;
        stunTimer = 0f;
        isKnockedBack = false;
        knockbackTimer = 0f;
        knockbackVelocity = Vector3.zero;
        ClearStunEffects();
    }

    void OnDestroy()
    {
        ClearStunEffects();
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log(message);
        }
    }

    private void ApplyGlobalScale()
    {
        transform.localScale = baseLocalScale * GlobalScaleManager.GetScale(GlobalScaleCategory.Tank);
    }

}
