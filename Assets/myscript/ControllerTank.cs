using UnityEngine;
using ProceduralForceField;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems; // Thêm để kiểm tra UI



public class ControllerTank : MonoBehaviour
{
    [SerializeField] private bool enableDebugLogs = false;
    public GameObject shieldObject;
    private ProceduralForceFieldOverlay shieldOverlay;

    public float Movespeed = 8f;
    public float RotateSpeed = 60f;
    public float TowerRotateSpeed = 10f; // Tốc độ xoay tháp pháo (để mượt hơn)
    public float extraDownForce = 15f; // Giáº£m lá»±c nháº¥n xuá»‘ng Ä‘á»ƒ trÃ¡nh bá»‹ dÃnh cháº·t/giáº­t


    [Header("Tank Identity & Upgrades")]
    public TankID tankID;
    private float damageMultiplier = 1f;

    Rigidbody TankEngine;

    [Header("Input Actions (Input System)")]
    public UnityEngine.InputSystem.InputActionReference moveAction;
    public UnityEngine.InputSystem.InputActionReference lookAction;
    public UnityEngine.InputSystem.InputActionReference fireAction;
    public UnityEngine.InputSystem.InputActionReference specialFireAction;
    public UnityEngine.InputSystem.InputActionReference switchWeaponAction;


    private Vector2 moveInput;

    // Turret aim (Joystick always)
    [Header("Turret Aim (Joystick)")]



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

    [Header("Special Ammo System")]
    public int specialAmmoCount = 10;
    public int specialBulletIndex = 0;
    [Min(0.01f)] public float specialFireCooldown = 0.5f;
    private float nextSpecialFireTime = 0f;

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
    public float wallCheckDistance = 1.5f;
    public Vector3 wallCheckBoxSize = new Vector3(2.5f, 1.5f, 0.5f);
    public LayerMask wallLayer;
    public Transform centerCheckPoint;
    private HP playerHP;

    void Awake()
    {
        baseLocalScale = transform.localScale;
        playerHP = GetComponent<HP>();
    }



    void Start()
    {
        ApplyGlobalScale();

        // Cưỡng bức kích hoạt Action ngay khi Start (Phòng trường hợp OnEnable chạy trước khi gán Action)
        EnableAllActions();

        TankEngine = GetComponent<Rigidbody>();
        if (shieldObject != null)
        {
            shieldOverlay = shieldObject.GetComponent<ProceduralForceFieldOverlay>();
            shieldObject.SetActive(false); // ban Ä‘áº§u táº¯t
        }

        if (TankEngine != null)
        {
            // Ngăn Tank bị đổ nhào (chỉ cho phép xoay quanh trục Y)
            TankEngine.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            
            // Hạ thấp trọng tâm vừa đủ (không nên quá sâu gây dính đất)
            TankEngine.centerOfMass = new Vector3(0, -0.7f, 0);
            
            // Đảm bảo xe dùng Interpolate để mượt mà
            TankEngine.interpolation = RigidbodyInterpolation.Interpolate;
        }

        if (bulletPrefabs != null && prewarmPerBulletPrefab > 0)
        {
            for (int i = 0; i < bulletPrefabs.Length; i++)
            {
                ProjectilePool.Prewarm(bulletPrefabs[i], prewarmPerBulletPrefab);
            }
        }

        ApplyUpgrades();
    }

    private void ApplyUpgrades()
    {
        if (SaveSystem.Data == null) return;

        // Láº¥y thÃ´ng tin upgrade cá»§a tank nÃ y
        TankUpgradeData upgrade = SaveSystem.Data.GetUpgrade(tankID.ToString());
        
        // --- Táº¡m thá» i láº¥y thÃ´ng sá»‘ bonus tá»« TankUpgradeUI (hoáº·c fix cá»©ng náº¿u cáº§n) ---
        // Má»—i cáº¥p HP: +25 HP, Má»—i cáº¥p Damage: +10%
        float hpBonus = upgrade.hpLevel * 25f;
        damageMultiplier = 1f + (upgrade.damageLevel * 0.10f);

        if (playerHP != null)
        {
            playerHP.AddMaxHP(hpBonus);
        }

        Debug.Log($"[Upgrade Applied] Tank: {tankID}, HP Bonus: +{hpBonus}, Damage Mult: x{damageMultiplier}");
    }

    void OnEnable()
    {
        // Kích hoạt các Action khi bật tank
        EnableAllActions();

        ApplyGlobalScale();
        if (playerHP != null)
        {
            playerHP.OnDied += HandleDeath;
        }
    }

    void OnDisable()
    {
        // Tắt các Action khi ẩn tank để tránh lỗi
        DisableAllActions();

        if (playerHP != null)
        {
            playerHP.OnDied -= HandleDeath;
        }
    }

    private void EnableAllActions()
    {
        if (moveAction != null) moveAction.action.Enable();
        if (lookAction != null) lookAction.action.Enable();
        if (fireAction != null) fireAction.action.Enable();
        if (specialFireAction != null) specialFireAction.action.Enable();
        if (switchWeaponAction != null) switchWeaponAction.action.Enable();
    }

    private void DisableAllActions()
    {
        if (moveAction != null) moveAction.action.Disable();
        if (lookAction != null) lookAction.action.Disable();
        if (fireAction != null) fireAction.action.Disable();
        if (specialFireAction != null) fireAction.action.Disable();
        if (switchWeaponAction != null) switchWeaponAction.action.Disable();
    }






    private void HandleDeath()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }
    }
    bool IsWallAhead()
    {
        return CheckBoxCast(transform.forward, Color.red);
    }

    bool IsWallBehind()
    {
        return CheckBoxCast(-transform.forward, Color.blue);
    }

    bool CheckBoxCast(Vector3 direction, Color debugColor)
    {
        if (centerCheckPoint == null) return false;

        RaycastHit hit;
        // Nâng vị trị quét lên một chút (0.5f) để không quét trúng mặt đất khi leo dốc
        Vector3 sourcePos = centerCheckPoint.position + Vector3.up * 0.5f;

        bool isHit = Physics.BoxCast(
            sourcePos,
            wallCheckBoxSize / 2,
            direction,
            out hit,
            transform.rotation,
            wallCheckDistance,
            wallLayer
        );

        if (isHit)
        {
            Debug.Log($"<color=orange>[BoxCast Hit]</color> {hit.collider.name} at {hit.distance}m");
        }

        return isHit;
    }

    void DrawDebugRays()
    {
        // Debug.DrawRay vẫn có thể dùng để chỉ hướng tâm
        if (centerCheckPoint != null)
        {
            Debug.DrawRay(centerCheckPoint.position, transform.forward * wallCheckDistance, Color.red);
            Debug.DrawRay(centerCheckPoint.position, -transform.forward * wallCheckDistance, Color.blue);
        }
    }

    // Hiển thị Box quét trong Scene View
    void OnDrawGizmos()
    {
        if (centerCheckPoint == null) return;

        // Lưu Matrix cũ để xoay Gizmos theo xe
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(centerCheckPoint.position, transform.rotation, Vector3.one);

        // Vẽ Box phía trước (Màu đỏ)
        Gizmos.color = Color.red;
        Vector3 aheadCenter = Vector3.forward * wallCheckDistance;
        Gizmos.DrawWireCube(aheadCenter, wallCheckBoxSize);

        // Vẽ Box phía sau (Màu xanh)
        Gizmos.color = Color.blue;
        Vector3 behindCenter = -Vector3.forward * wallCheckDistance;
        Gizmos.DrawWireCube(behindCenter, wallCheckBoxSize);

        // Trả lại Matrix cũ
        Gizmos.matrix = oldMatrix;
    }

    void Move()
    {
        float v = moveInput.y; // GiÃ¡ trá»‹ tiáº¿n lÃ¹i (W/S hoáº·c Joystick lÃªn/xuá»‘ng)
        if (Mathf.Abs(v) < 0.01f) return;

        // Cháº·n khi Ä‘i tá»›i
        if (v > 0 && IsWallAhead())
        {
            return;
        }

        // Cháº·n khi lÃ¹i
        if (v < 0 && IsWallBehind())
        {
            return;
        }

        Vector3 move =
            transform.forward *
            v *
            Movespeed *
            Time.fixedDeltaTime;

        TankEngine.MovePosition(TankEngine.position + move);
    }


    void Rotate()
    {
        float r = moveInput.x; // GiÃ¡ trá»‹ xoay (A/D hoáº·c Joystick trÃ¡i/pháº£i)
        if (Mathf.Abs(r) < 0.01f) return;

        float rotationAmount = r * RotateSpeed * Time.fixedDeltaTime;
        float currentY = transform.rotation.eulerAngles.y;
        TankEngine.MoveRotation(Quaternion.Euler(0, currentY + rotationAmount, 0));
        
        // Triệt tiêu vận tốc góc X và Z để đảm bảo không bị lực lạ làm nghiêng xe
        Vector3 av = TankEngine.angularVelocity;
        TankEngine.angularVelocity = new Vector3(0, av.y, 0);
    }

    void HandleAimAndFire()
    {
        // ===== 1. XOAY THÁP PHÁO BẰNG RIGHT JOYSTICK (luôn luôn) =====
        bool isAiming = false;

        if (lookAction != null)
        {
            Vector2 aimInput = lookAction.action.ReadValue<Vector2>();
            if (aimInput.sqrMagnitude > 0.01f)
            {
                isAiming = true;

                float targetAngle = Mathf.Atan2(aimInput.x, aimInput.y) * Mathf.Rad2Deg;

                // Cộng thêm góc xoay hiện tại của xe tăng để hướng xoay của Joystick
                // luôn tương đối so với hướng mũi xe tăng
                targetAngle += transform.eulerAngles.y;

                Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
                Tower.transform.rotation = Quaternion.Slerp(
                    Tower.transform.rotation,
                    targetRotation,
                    Time.deltaTime * TowerRotateSpeed
                );
            }
        }

        // ===== 2. TỰ ĐỘNG BẮN KHI ĐANG KÉO JOYSTICK NGẮM =====
        if (isAiming)
        {
            ExecuteFire();
        }

        // Vẫn giữ nút Fire riêng để bắn khi không kéo joystick (tuỳ chọn)
        if (fireAction != null && fireAction.action.IsPressed())
        {
            ExecuteFire();
        }
    }

    void ExecuteFire()
    {
        // Chặn tốc độ bắn
        if (Time.time < nextFireTime) return;

        LogDebug("Player Fire - Đạn index: " + currentBulletIndex);

        // ✅ Kiểm tra mảng đạn hợp lệ
        if (bulletPrefabs == null || bulletPrefabs.Length == 0)
        {
            Debug.LogWarning("Chưa gán bulletPrefabs!");
            return;
        }

        if (currentBulletIndex < 0 || currentBulletIndex >= bulletPrefabs.Length)
        {
            Debug.LogWarning("Index đạn không hợp lệ: " + currentBulletIndex);
            return;
        }

        LogDebug("Player Fire - Đạn index: " + currentBulletIndex);

        for (int i = 0; i < ShootFX.Length; i++)
        {
            ShootFX[i].Play();
        }
        
        GameObject bulletInstance = ProjectilePool.Spawn(
            bulletPrefabs[currentBulletIndex],
            shootElement.position,
            shootElement.rotation
        );

        // Gán Damage Multiplier cho đạn
        bulletTank bulletScript = bulletInstance.GetComponentInChildren<bulletTank>();
        if (bulletScript != null)
        {
            bulletScript.damageMultiplier = damageMultiplier;
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

        // 1. Nhận từ nút bấm Mobile (hoặc phím tắt trên PC gán vào SwitchWeaponAction)
        bool buttonPressed = (switchWeaponAction != null && switchWeaponAction.action.WasPressedThisFrame());
        if (buttonPressed)
        {
            currentBulletIndex++;
            if (currentBulletIndex >= bulletPrefabs.Length)
                currentBulletIndex = 0;
            
            Debug.Log("[SwitchWeapon] Switched to bullet index: " + currentBulletIndex);
            return; // Đã đổi xong bằng nút thì không check chuột nữa
        }

        // 2. Nhận từ cuộn chuột (PC fallback)
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

    void SpecialFire()
    {
        // 1. Chặn nếu đang nhấn vào UI (Bình luận lại để On-Screen Button hoạt động)
        // if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        //     return;

        // 2. Kiểm tra lệnh bắn đặc biệt (Dùng Action mới, fallback về phím Space)
        if(specialFireAction != null) Debug.Log("SpecialFire Check: " + specialFireAction.action.IsPressed());
        bool isSpecialFiring = (specialFireAction != null) ? specialFireAction.action.IsPressed() : Input.GetKey(KeyCode.Space);
        
        if (!isSpecialFiring) return;
        Debug.Log("SpecialFire 4" );
        if (specialAmmoCount <= 0) return;
        Debug.Log("SpecialFire 5");
        if (Time.time < nextSpecialFireTime) return;


        if (bulletPrefabs == null || specialBulletIndex < 0 || specialBulletIndex >= bulletPrefabs.Length)
        {
            Debug.LogWarning("Index Ä‘áº¡n Ä‘áº·c biá»‡t khÃ´ng há»£p lá»‡: " + specialBulletIndex);
            return;
        }
        Debug.Log("SpecialFire 6");
        specialAmmoCount--;
        nextSpecialFireTime = Time.time + specialFireCooldown;
        Debug.Log("SpecialFire 7");
        for (int i = 0; i < ShootFX.Length; i++)
        {
            ShootFX[i].Play();  
        }
        Debug.Log("SpecialFire 8");
        GameObject bulletInstance = ProjectilePool.Spawn(
            bulletPrefabs[specialBulletIndex],
            shootElement.position,
            shootElement.rotation
        );
        Debug.Log("SpecialFire 9");
        bulletTank bulletScript = bulletInstance.GetComponentInChildren<bulletTank>();
        if (bulletScript != null)
        {
            bulletScript.damageMultiplier = damageMultiplier;
            bulletScript.bulletTeam = Team.Player;
            bulletScript.bulletType = BulletType.CurvedHoming; // Explicitly set type for special bullet
            Debug.Log("SpecialFire 10");
        }
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameState.Playing)
            return;

        // Đọc giá trị trái/phải, tiến/lùi từ Move Action
        if (moveAction != null)
        {
            moveInput = moveAction.action.ReadValue<Vector2>();
        }






        UpdateStatusEffects();

        UpdateShield();
        
        // Luôn hiển thị tia Raycast để debug trong Scene view
        DrawDebugRays();
        
        // Input logic and non-physics updates
        HandleAimAndFire();
        SpecialFire();
        SwitchWeapon();
    }

    void FixedUpdate()
    {
        if (TankEngine == null) return;

        if (GameManager.Instance != null && GameManager.Instance.currentState != GameState.Playing)
        {
            TankEngine.linearVelocity = Vector3.zero;
            TankEngine.angularVelocity = Vector3.zero;
            return;
        }

        // Thêm lực nhấn xuống vừa phải
        TankEngine.AddForce(Vector3.down * extraDownForce, ForceMode.Acceleration);

        if (isKnockedBack || isStunned)
        {
            return;
        }

        Move();
        Rotate();
        
        // Chỉ cưỡng bức góc xoay nếu lệch quá lớn để tránh rung giật (jittering)
        Quaternion currentRot = transform.rotation;
        if (Mathf.Abs(currentRot.x) > 0.1f || Mathf.Abs(currentRot.z) > 0.1f)
        {
            TankEngine.MoveRotation(Quaternion.Euler(0, currentRot.eulerAngles.y, 0));
        }
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
        SaveSystem.Data.coins += amount;
        SaveSystem.Save();  
        LogDebug("Player nháº­n " + amount + " coin. Tá»•ng coin: " + coinCount);
    }

    public void AddSpecialAmmo(int amount)
    {
        specialAmmoCount += amount;
        LogDebug("Player nháº­n " + amount + " Ä‘áº¡n Ä‘áº·c biá»‡t. Tá»•ng ammo: " + specialAmmoCount);
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
