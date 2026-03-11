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

    [Header("Player Fire Tuning")]
    [Min(0.01f)] public float playerFireCooldownFactor = 0.2f;
    [Min(0.01f)] public float maxPlayerBulletCooldown = 1f;
    [Min(0.01f)] public float minPlayerFireInterval = 0.05f;

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

    [Header("Raycast Settings")]
    public float raycastDistance = 1.5f; // Khoảng cách để dừng lại (tùy kích thước Tank)
    public LayerMask wallLayer;        // Chỉ định Layer của Tường để tránh Raycast tự va vào chính mình

    void Move()
    {
        // float v = Input.GetAxis("Vertical");
        // if (Mathf.Abs(v) < 0.01f) return;

        // Vector3 moveStep = transform.forward * v * Movespeed * Time.fixedDeltaTime;

        // // Tạo một điểm kiểm tra ở phía trước Tank
        // // raycastDistance nên lớn hơn một chút so với khoảng cách từ tâm Tank đến mũi Tank
        // float checkDist = 0.6f;
        // Vector3 checkPos = transform.position + transform.forward * Mathf.Sign(v) * checkDist + Vector3.up * 0.5f;

        // // Kiểm tra xem phía trước có "Wall" không
        // if (Physics.CheckSphere(checkPos, 0.2f, wallLayer))
        // {
        //     // Nếu có tường, ta gán vận tốc về 0 và không thực hiện MovePosition
        //     TankEngine.linearVelocity = new Vector3(0, TankEngine.linearVelocity.y, 0);
        //     return;
        // }

        // // Nếu đường trống, thực hiện di chuyển
        // TankEngine.MovePosition(TankEngine.position + moveStep);

        float v = Input.GetAxis("Vertical");
        if (Mathf.Abs(v) < 0.01f) return;

        Vector3 direction = transform.forward * Mathf.Sign(v);
        float distance = Movespeed * Time.fixedDeltaTime + 0.1f; // Tốc độ hiện tại cộng thêm một khoảng đệm

        // Quét một hình hộp về phía trước trước khi di chuyển
        bool isBlocked = Physics.BoxCast(
            transform.position + Vector3.up * 0.5f,
            transform.localScale * 0.45f, // Hơi nhỏ hơn kích thước Tank một chút để tránh kẹt giả
            direction,
            transform.rotation,
            distance,
            wallLayer
        );

        if (!isBlocked)
        {
            TankEngine.MovePosition(TankEngine.position + transform.forward * v * Movespeed * Time.fixedDeltaTime);
        }
    }

    // void Move()
    // {
    //     // Vector3 move = transform.forward *
    //     //                Input.GetAxis("Vertical") *
    //     //                Movespeed *
    //     //                Time.deltaTime;

    //     // TankEngine.MovePosition(TankEngine.position + move);

    //     // Sử dụng Time.fixedDeltaTime thay vì deltaTime
    //     Vector3 move = transform.forward * Input.GetAxis("Vertical") * Movespeed * Time.fixedDeltaTime;
    //     TankEngine.MovePosition(TankEngine.position + move);
    // }

    void Rotate()
    {
        // float r = Input.GetAxis("Horizontal") *
        //           RotateSpeed *
        //           Time.deltaTime;

        // Quaternion rotate = Quaternion.Euler(0, r, 0);
        // TankEngine.MoveRotation(TankEngine.rotation * rotate);

        float horizontalInput = Input.GetAxis("Horizontal");

        // Chỉ tính toán khi người chơi thực sự nhấn phím xoay
        if (Mathf.Abs(horizontalInput) > 0.01f)
        {
            float r = horizontalInput * RotateSpeed * Time.fixedDeltaTime;

            // Tính toán vòng quay mới
            Quaternion deltaRotation = Quaternion.Euler(0, r, 0);
            Quaternion targetRotation = TankEngine.rotation * deltaRotation;

            // Sử dụng MoveRotation để Engine vật lý xử lý va chạm mượt mà
            TankEngine.MoveRotation(targetRotation);
        }
        else
        {
            // Khi không nhấn xoay, triệt tiêu vận tốc góc để tránh bị lực tác động làm xoay Tank ngoài ý muốn
            TankEngine.angularVelocity = Vector3.zero;
        }
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
        if (!Input.GetMouseButton(0)) return;
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
        bulletTank bulletScript = bulletInstance.GetComponentInChildren<bulletTank>();
        if (bulletScript != null)
        {
            bulletScript.bulletTeam = Team.Player;
            float clampedBulletCooldown = Mathf.Min(
                Mathf.Max(0.01f, bulletScript.fireCooldown),
                maxPlayerBulletCooldown
            );
            float fireInterval = Mathf.Max(
                minPlayerFireInterval,
                playerFireCooldownFactor * clampedBulletCooldown
            );
            nextFireTime = Time.time + fireInterval;
        }
        else
        {
            nextFireTime = Time.time + minPlayerFireInterval; // fallback
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
        // Chỉ đọc Input và xử lý Logic không liên quan vật lý ở đây
        UpdateStatusEffects();
        RotateTower();
        Fire();
        SwitchWeapon();
        UpdateShield();
    }

    void FixedUpdate()
    {
        // Xử lý di chuyển vật lý ở đây để đồng bộ với Engine
        if (isKnockedBack || isStunned)
        {
            // Xử lý knockback bằng MovePosition trong FixedUpdate
            TankEngine.MovePosition(TankEngine.position + knockbackVelocity * Time.fixedDeltaTime);
            return;
        }

        Move();
        Rotate();
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
