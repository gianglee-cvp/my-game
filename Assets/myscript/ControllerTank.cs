using UnityEngine;
using ProceduralForceField;


public class ControllerTank : MonoBehaviour
{
    [SerializeField] private bool enableDebugLogs = false;
    public GameObject shieldObject;
    private ProceduralForceFieldOverlay shieldOverlay;

    public float Movespeed = 8f;
    public float RotateSpeed = 60f;
    public float extraDownForce = 15f; // Giảm lực nhấn xuống để tránh bị dính chặt/giật

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
    public float wallCheckDistance = 1.5f;
    public Vector3 wallCheckBoxSize = new Vector3(2.5f, 1.5f, 0.5f);
    public LayerMask wallLayer;
    public Transform centerCheckPoint;

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
    }

    void OnEnable()
    {
        ApplyGlobalScale();
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
        float v = Input.GetAxis("Vertical");
        if (Mathf.Abs(v) < 0.01f) return;

        // Chặn khi đi tới
        if (v > 0 && IsWallAhead())
        {
            return;
        }

        // Chặn khi lùi
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
        float r = Input.GetAxis("Horizontal") *
                  RotateSpeed *
                  Time.fixedDeltaTime;

        // Chỉ cho phép xoay quanh trục Y, ép X và Z về 0
        float currentY = transform.rotation.eulerAngles.y;
        TankEngine.MoveRotation(Quaternion.Euler(0, currentY + r, 0));
        
        // Triệt tiêu vận tốc góc X và Z để đảm bảo không bị lực lạ làm nghiêng xe
        Vector3 av = TankEngine.angularVelocity;
        TankEngine.angularVelocity = new Vector3(0, av.y, 0);
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
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameState.Playing)
            return;

        UpdateStatusEffects();
        UpdateShield();
        
        // Luôn hiển thị tia Raycast để debug trong Scene view
        DrawDebugRays();
        
        // Input logic and non-physics updates
        RotateTower();
        Fire();
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
