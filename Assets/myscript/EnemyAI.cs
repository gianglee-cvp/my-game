using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyType { Shooter, Bomber }
    public EnemyType type;

    public Transform player;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotateSpeed = 5f;
    public float detectionRange = 40f;
    public float stopRange = 10f;
    public float shootRange = 30f;

    [Header("Bomber Flight")]
    public float flyHeight = 7f;
    public float ascendSpeed = 3f;
    public float bomberLifetime = 15f;
    [Min(0)] public int bombPrewarmCount = 4;
    public Transform bombDropPoint;

    [Header("Attack")]
    public GameObject bullet;
    [Min(0)] public int bulletPrewarmCount = 16;
    public Transform shootElement;
    public ParticleSystem[] ShootFX;

    public EnemySpawner mySpawner;

    private float nextFireTime = 0f;
    private bool hasActed = false;
    private bool isAscending = false;
    private float bomberSpawnTime = 0f;

    private NavMeshAgent agent;
    private Rigidbody rb;
    [SerializeField] private bool enableDebugLogs = false;

    // ================= STUN =================
    [Header("Stun Effect")]
    public Transform[] stunEffectPoints;       // danh sach diem bi hit de gan stun effect

    private bool isStunned = false;
    private float stunTimer = 0f;
    private readonly List<ParticleSystem> stunEffectInstances = new List<ParticleSystem>();

    [Header("Knockback")]
    public float knockbackResistance = 1f;
    private bool isKnockedBack = false;
    private float knockbackTimer = 0f;
    private Vector3 knockbackVelocity = Vector3.zero;
    private Vector3 baseLocalScale;
    private float baseMoveSpeed;
    private float baseRotateSpeed;
    private Bomb bomberBombTemplate;
    private Vector3 bombTemplateLocalPosition;
    private Quaternion bombTemplateLocalRotation = Quaternion.identity;
    private Vector3 bombTemplateLocalScale = Vector3.one;
    private HP myHP;
    private ItemSpawnManager itemSpawner;

    void Awake()
    {
        baseLocalScale = transform.localScale;
        baseMoveSpeed = moveSpeed;
        baseRotateSpeed = rotateSpeed;
        myHP = GetComponent<HP>();
        itemSpawner = Object.FindFirstObjectByType<ItemSpawnManager>();
    }

    void OnEnable()
    {
        ApplyGlobalSpeed();
        ApplyGlobalScale();
        ResetRuntimeState();
        CacheBomberBombTemplateIfNeeded();
        EnsureBomberCarriesBombIfMissing();

        if (myHP != null)
        {
            myHP.OnDied += HandleDeath;
        }
    }

    void OnDisable()
    {
        if (myHP != null)
        {
            myHP.OnDied -= HandleDeath;
        }
    }

    private void HandleDeath()
    {
        // Chi shooter moi duoc drop item
        if (type == EnemyType.Shooter && itemSpawner != null)
        {
            itemSpawner.SpawnItemAtTransform(transform);
        }
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogError("Player object not found in scene.");
        }
        else
        {
            LogDebug("Found player: " + playerObj.name);
            player = playerObj.transform;
        }
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Ngăn địch bị đổ nhào khi va chạm (chỉ cho phép xoay quanh trục Y)
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            
            // Hạ thấp trọng tâm để địch không bị "bay" hoặc lật khi đụng map/enemy khác
            rb.centerOfMass = new Vector3(0, -0.7f, 0);
            
            // Sử dụng Interpolate để chuyển động mượt mà hơn
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        if (type == EnemyType.Shooter)
        {
            agent = GetComponent<NavMeshAgent>();
            ApplyGlobalSpeed();
            if (agent != null)
            {
                agent.stoppingDistance = stopRange;
                agent.updateRotation = false;
            }
            ProjectilePool.Prewarm(bullet, bulletPrewarmCount);
        }
        else if (type == EnemyType.Bomber)
        {
            if (rb != null) rb.useGravity = false;      // Không cho rơi
            isAscending = true;         // Bắt đầu bay lên
            bomberSpawnTime = Time.time;
            CacheBomberBombTemplateIfNeeded();
            if (bomberBombTemplate != null)
            {
                ProjectilePool.Prewarm(bomberBombTemplate.gameObject, bombPrewarmCount);
            }
        }

        ResetRuntimeState();
    }

    void Update()
    {
        if (type == EnemyType.Bomber && Time.time - bomberSpawnTime >= bomberLifetime)
        {
            EnemyPool.Despawn(gameObject);
            return;
        }

        if (player == null) return;

        if (isKnockedBack)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.Move(knockbackVelocity * Time.deltaTime);
            }

            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0f)
            {
                isKnockedBack = false;
                knockbackVelocity = Vector3.zero;

                if (agent != null && agent.enabled && agent.isOnNavMesh && !isStunned)
                    agent.isStopped = false;
            }
            return;
        }

        // âš¡ Äang bá»‹ stun: Ä‘áº¿m ngÆ°á»£c vÃ  cháº·n má»i hÃ nh Ä‘á»™ng
        if (isStunned)
        {
            // ðŸ›‘ Shooter: Dá»«ng NavMeshAgent ngay láº­p tá»©c
            if (agent != null && agent.enabled && agent.isOnNavMesh) {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;  // triá»‡t tiÃªu váº­n tá»‘c hiá»‡n táº¡i
            }

            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                isStunned = false;

                // ï¿½ Huá»· effect stun
                if (stunEffectInstances.Count > 0)
                {
                    for (int i = 0; i < stunEffectInstances.Count; i++)
                    {
                        ParticleSystem effect = stunEffectInstances[i];
                        if (effect == null) continue;
                        effect.Stop();
                        EffectPool.Despawn(effect);
                    }
                    stunEffectInstances.Clear();
                }

                // ï¿½ðŸŸ¢ Tháº£ cho AI tiáº¿p tá»¥c cháº¡y sau khi háº¿t stun
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                    agent.isStopped = false;
            }
            return;  // khÃ´ng lÃ m gÃ¬ khi stun
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > detectionRange) return;

        if (type == EnemyType.Shooter)
        {
            ShooterLogic(distanceToPlayer);
        }
    }

    void FixedUpdate()
    {
        if (player == null) return;

        if (isKnockedBack)
        {
            if (rb != null)
            {
                rb.MovePosition(rb.position + knockbackVelocity * Time.fixedDeltaTime);
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            knockbackTimer -= Time.fixedDeltaTime;
            if (knockbackTimer <= 0f)
            {
                isKnockedBack = false;
                knockbackVelocity = Vector3.zero;
            }
            return;
        }

        if (isStunned) 
        {
            // ðŸ›‘ Bomber: Triá»‡t tiÃªu má»i lá»±c Ä‘á»ƒ Ä‘á»©ng yÃªn táº¡i chá»—
            if (rb != null) {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            return; 
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > detectionRange) return;

        if (type == EnemyType.Bomber)
        {
            if (isAscending)
            {
                Ascend();
                return;
            }

            BomberLogic(distanceToPlayer);
        }
    }

    // ================= SHOOTER =================

    void ShooterLogic(float distance)
    {
        agent.SetDestination(player.position);

        if (distance <= stopRange)
        {
            agent.isStopped = true;
            RotateToPlayer();
            Fire();
        }
        else
        {
            agent.isStopped = false;
            RotateToPlayer();

            if (distance <= shootRange)
            {
                Fire();
            }
        }
    }

    // ================= BOMBER =================

    void Ascend()
    {
        Vector3 targetPos = new Vector3(rb.position.x, flyHeight, rb.position.z);

        Vector3 newPos = Vector3.MoveTowards(
            rb.position,
            targetPos,
            ascendSpeed * Time.fixedDeltaTime
        );

        rb.MovePosition(newPos);

        if (Mathf.Abs(rb.position.y - flyHeight) < 0.1f)
        {
            isAscending = false;
        }
    }

    void BomberLogic(float distance)
    {
        RotateToPlayerRB();
        if (distance <= stopRange)
        {
            DropBomb();
            return;
        }

        MoveForwardRB();
    }

    void RotateToPlayerRB()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        Quaternion smoothRotation = Quaternion.Slerp(
            rb.rotation,
            targetRotation,
            rotateSpeed * Time.fixedDeltaTime
        );

        rb.MoveRotation(smoothRotation);
    }

    void MoveForwardRB()
    {
        // Giá»¯ cá»‘ Ä‘á»‹nh Ä‘á»™ cao 7f
        Vector3 forwardMove = transform.forward * moveSpeed * Time.fixedDeltaTime;
        Vector3 newPos = rb.position + forwardMove;
        newPos.y = flyHeight;

        rb.MovePosition(newPos);
    }

    // ================= COMMON =================

    void RotateToPlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotateSpeed * Time.deltaTime
        );
    }

    void Fire()
    {
        if (Time.time < nextFireTime) return;

        for (int i = 0; i < ShootFX.Length; i++)
        {
            ShootFX[i].Play();
        }

        GameObject bulletInstance = ProjectilePool.Spawn(
            bullet,
            shootElement.position,
            shootElement.rotation
        );

        bulletTank bulletScript = bulletInstance.GetComponentInChildren<bulletTank>();
        if (bulletScript != null)
        {
            bulletScript.bulletTeam = Team.Enemy;
            nextFireTime = Time.time + bulletScript.fireCooldown;
        }
        else
        {
            nextFireTime = Time.time + 0.5f;
        }
    }

    // ================= STUN API =================

    public void Stun(float duration, ParticleSystem stunEffect)
    {
        // Náº¿u Ä‘ang stun â†’ chá»‰ refresh thá»i gian
        if (isStunned)
        {
            stunTimer = duration;
            return;
        }

        isStunned = true;
        stunTimer = duration;

        // Táº¡o effect stun tá»« prefab cá»§a Ä‘áº¡n
        if (stunEffect != null && stunEffectPoints != null && stunEffectPoints.Length > 0)
        {
            for (int i = 0; i < stunEffectPoints.Length; i++)
            {
                Transform point = stunEffectPoints[i];
                if (point == null) continue;

                ParticleSystem effectInstance = EffectPool.Spawn(
                    stunEffect,
                    point.position,
                    point.rotation,
                    point   // gan theo enemy
                );

                // â™¾ï¸ Ã‰p hiá»‡u á»©ng luÃ´n láº·p láº¡i cho Ä‘áº¿n khi bá»‹ Stop á»Ÿ Update
                var main = effectInstance.main;
                main.loop = true;
                effectInstance.transform.localScale = Vector3.one * 2f;
                effectInstance.Play();
                stunEffectInstances.Add(effectInstance);
            }
        }

        LogDebug(gameObject.name + " stunned for " + duration + " seconds");
    }
    public void Knockback(Vector3 direction, float force, float duration)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;

        float finalResistance = Mathf.Max(0.01f, knockbackResistance);
        Vector3 finalDirection = direction.normalized;

        knockbackVelocity = finalDirection * (force / finalResistance);
        knockbackTimer = duration;
        isKnockedBack = true;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    void DropBomb()
    {
        if (hasActed) return;

        Bomb bombScript = GetBombForDrop();
        if (bombScript != null)
        {
            hasActed = true;

            GameObject bombObject = bombScript.gameObject;
            bombObject.transform.SetParent(null);

            if (player != null)
            {
                bombScript.LaunchStraightAtPosition(player.position);
            }

            // âœ… Bá»  qua collision giá»¯a bom vÃ  enemy Ä‘Ã£ drop nÃ³
            // â†’ TrÃ¡nh bom ná»• ngay khi vá»«a tÃ¡ch ra khá» i enemy
            Collider bombCol = bombObject.GetComponent<Collider>();
            Collider[] enemyCols = GetComponentsInChildren<Collider>();
            if (bombCol != null)
            {
                foreach (Collider ec in enemyCols)
                    Physics.IgnoreCollision(bombCol, ec, true);
            }

            EnemyPool.Despawn(gameObject);
        }
    }

    private void CacheBomberBombTemplateIfNeeded()
    {
        if (type != EnemyType.Bomber || bomberBombTemplate != null)
        {
            return;
        }

        Bomb existingBomb = GetComponentInChildren<Bomb>(true);
        if (existingBomb == null)
        {
            return;
        }

        GameObject templateObject = Instantiate(existingBomb.gameObject, transform);
        templateObject.name = existingBomb.gameObject.name + "_Template";
        templateObject.SetActive(false);
        bomberBombTemplate = templateObject.GetComponent<Bomb>();
        bombTemplateLocalPosition = existingBomb.transform.localPosition;
        bombTemplateLocalRotation = existingBomb.transform.localRotation;
        bombTemplateLocalScale = existingBomb.transform.localScale;
    }

    private Bomb GetBombForDrop()
    {
        Bomb attachedBomb = FindAttachedCarriedBomb();
        if (attachedBomb != null)
        {
            return attachedBomb;
        }

        if (bomberBombTemplate == null)
        {
            return null;
        }

        Transform dropOrigin = bombDropPoint != null ? bombDropPoint : transform;
        GameObject bombObject = ProjectilePool.Spawn(
            bomberBombTemplate.gameObject,
            dropOrigin.position,
            dropOrigin.rotation
        );

        if (bombObject == null)
        {
            return null;
        }

        return bombObject.GetComponent<Bomb>();
    }

    private void EnsureBomberCarriesBombIfMissing()
    {
        if (type != EnemyType.Bomber || bomberBombTemplate == null)
        {
            return;
        }

        Bomb attachedBomb = FindAttachedCarriedBomb();
        if (attachedBomb != null)
        {
            return;
        }

        Transform attachRoot = bombDropPoint != null ? bombDropPoint : transform;
        GameObject bombObject = ProjectilePool.Spawn(
            bomberBombTemplate.gameObject,
            attachRoot.position,
            attachRoot.rotation
        );

        if (bombObject == null)
        {
            return;
        }

        bombObject.transform.SetParent(attachRoot, false);
        bombObject.transform.localPosition = bombDropPoint != null ? Vector3.zero : bombTemplateLocalPosition;
        bombObject.transform.localRotation = bombDropPoint != null ? Quaternion.identity : bombTemplateLocalRotation;
        bombObject.transform.localScale = bombTemplateLocalScale;

        Rigidbody bombRb = bombObject.GetComponent<Rigidbody>();
        if (bombRb != null)
        {
            bombRb.isKinematic = true;
            bombRb.useGravity = false;
            bombRb.linearVelocity = Vector3.zero;
            bombRb.angularVelocity = Vector3.zero;
        }
    }

    private Bomb FindAttachedCarriedBomb()
    {
        Bomb[] bombs = GetComponentsInChildren<Bomb>(true);
        for (int i = 0; i < bombs.Length; i++)
        {
            Bomb bomb = bombs[i];
            if (bomb == null)
            {
                continue;
            }

            if (bomberBombTemplate != null && bomb == bomberBombTemplate)
            {
                continue;
            }

            if (bomb.transform.IsChildOf(transform))
            {
                return bomb;
            }
        }

        return null;
    }

    private void ResetRuntimeState()
    {
        nextFireTime = 0f;
        hasActed = false;
        isKnockedBack = false;
        knockbackTimer = 0f;
        knockbackVelocity = Vector3.zero;
        isStunned = false;
        stunTimer = 0f;
        ClearStunEffects();

        if (type == EnemyType.Bomber)
        {
            isAscending = true;
            bomberSpawnTime = Time.time;
            if (rb != null)
            {
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        if (type == EnemyType.Shooter)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.velocity = Vector3.zero;
            }
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    private void ClearStunEffects()
    {
        if (stunEffectInstances.Count == 0) return;

        for (int i = 0; i < stunEffectInstances.Count; i++)
        {
            ParticleSystem effect = stunEffectInstances[i];
            if (effect == null) continue;
            effect.Stop();
            EffectPool.Despawn(effect);
        }
        stunEffectInstances.Clear();
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
        transform.localScale = baseLocalScale * GlobalScaleManager.GetScale(GlobalScaleCategory.Enemy);
    }

    private void ApplyGlobalSpeed()
    {
        float speedMul = GlobalScaleManager.GetEnemySpeedMultiplier();
        moveSpeed = baseMoveSpeed * speedMul;
        rotateSpeed = baseRotateSpeed * speedMul;

        if (agent != null)
        {
            agent.speed = moveSpeed;
        }
    }
}
