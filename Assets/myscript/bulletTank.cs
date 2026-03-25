using System.Collections;
using UnityEngine;
using ProceduralForceField;

public enum Team
{
    Player,
    Enemy
}

public enum BulletType
{
    Normal,
    Tesla,
    Plougher,
    rocket,
    CurvedHoming
}

public class bulletTank : MonoBehaviour
{
    [SerializeField] private bool enableDebugLogs = false;
    [Header("Stats (Set in Inspector)")]
    public float speed = 20f;
    public float damage = 20f;
    public float fireCooldown = 1f;
    public float effectScale = 1f;
    public float lifetime = 10f;
    public float damageMultiplier = 1f;
    private float destroyTime;

    [Header("Team")]
    public Team bulletTeam;   // Xác định phe của đạn

    [Header("Bullet Type")]
    public BulletType bulletType = BulletType.Normal;

    [Header("Effect")]
    public ParticleSystem effectImpact;

    [Header("Scale From Shooter (Optional)")]
    public bool applyShooterScale = true;
    [Min(0.01f)] public float bulletScaleMultiplier = 1f;
    [Min(0.01f)] public float impactScaleMultiplier = 1f;

    [Header("Tesla Settings")]
    public float stunDuration = 3f;
    public ParticleSystem stunEffect;

    [Header("Plougher Settings")]
    public float knockbackForce = 5f;
    public float knockbackDuration = 1f;

    [Header("Player Knockback Tuning")]
    [Range(0f, 1f)] public float playerKnockbackForceMultiplier = 0.35f;
    [Range(0f, 1f)] public float playerKnockbackDurationMultiplier = 0.5f;

    [Header("Info")]
    public string bulletName = "Tank Bullet";

    [Header("Homing Settings")]
    public float steerSpeed = 5f;
    public float targetSearchRange = 100f;
    public Transform homingTarget;

    Rigidbody bulletEngine;
    private Vector3 baseBulletScale;
    private float baseSpeed;
    private float runtimeScaleFactor = 1f;
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private HP targetHP;

    void Awake()
    {
        bulletEngine = GetComponent<Rigidbody>();
        baseBulletScale = transform.localScale;
        baseSpeed = speed;
        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;
    }

    void Start()
    {
        if (bulletName == "plougher_bullet")
        {
            effectScale = 3f;
        }
        AudioManager.Instance.PlayShoot();
        LogDebug("position bullet: " + transform.position);
    }

    void OnEnable()
    {
        // Clear target state when re-enabled (from pool)
        ClearTarget();

        // Only reset local transform for nested bullets (root-wrapper prefabs like rocket/plougher).
        // For root bullets (e.g. Tesla), keep spawn world rotation from shootElement.
        if (transform.parent != null)
        {
            transform.localPosition = baseLocalPosition;
            transform.localRotation = baseLocalRotation;
        }

        speed = baseSpeed * GlobalScaleManager.GetBulletSpeedMultiplier();
        destroyTime = lifetime;
        runtimeScaleFactor = GlobalScaleManager.GetScale(GlobalScaleCategory.Bullet);
        transform.localScale = baseBulletScale * runtimeScaleFactor;

        if (bulletEngine != null)
        {
            if (!bulletEngine.isKinematic)
            {
                bulletEngine.linearVelocity = Vector3.zero;
                bulletEngine.angularVelocity = Vector3.zero;
            }
            bulletEngine.useGravity = false;
            bulletEngine.isKinematic = true;
        }
    }

    void OnDisable()
    {
        ClearTarget();
    }

    void OnDestroy()
    {
        ClearTarget();
    }

    void Update()
    {
        if (bulletType == BulletType.CurvedHoming)
        {
            UpdateHoming();
        }

        transform.position += transform.forward * speed * Time.deltaTime;

        destroyTime -= Time.deltaTime;
        if (destroyTime <= 0)
        {
            DestroyBullet();
        }
    }

    private void UpdateHoming()
    {
        if (homingTarget == null || !homingTarget.gameObject.activeInHierarchy)
        {
            if (homingTarget != null) ClearTarget();
            FindTarget();
        }

        if (homingTarget != null)
        {
            // Range check
            float dist = Vector3.Distance(transform.position, homingTarget.position);
            if (dist > targetSearchRange)
            {
                ClearTarget();
                return;
            }

            Vector3 direction = (homingTarget.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, steerSpeed * Time.deltaTime);
        }
    }

    private void ClearTarget()
    {
        if (targetHP != null)
        {
            targetHP.OnDied -= HandleTargetDied;
            targetHP = null;
        }
        homingTarget = null;
    }

    private void HandleTargetDied()
    {
        ClearTarget();
    }

    private void FindTarget()
    {
        float closestBossDist = Mathf.Infinity;
        float closestBomberDist = Mathf.Infinity;
        float closestEnemyDist = Mathf.Infinity;

        Transform bossTarget = null;
        Transform bomberTarget = null;
        Transform enemyTarget = null;

        EnemyAI[] enemies = Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);

        foreach (var enemy in enemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

            // Kiểm tra HP nếu có, bỏ qua nếu đã chết
            HP enemyHP = enemy.GetComponent<HP>();
            if (enemyHP != null && enemyHP.CurrentHP <= 0) continue;

            float dist = Vector3.Distance(transform.position, enemy.transform.position);

            // Skip nếu ngoài range
            if (dist > targetSearchRange) continue;

            // Boss (ưu tiên cao nhất)
            if (enemy.CompareTag("Boss"))
            {
                if (dist < closestBossDist)
                {
                    closestBossDist = dist;
                    bossTarget = enemy.transform;
                }
            }
            // Bomber
            else if (enemy.type == EnemyAI.EnemyType.Bomber)
            {
                if (dist < closestBomberDist)
                {
                    closestBomberDist = dist;
                    bomberTarget = enemy.transform;
                }
            }
            // Enemy thường
            else
            {
                if (dist < closestEnemyDist)
                {
                    closestEnemyDist = dist;
                    enemyTarget = enemy.transform;
                }
            }
        }

        // Ưu tiên
        Transform selectedTarget = null;
        if (bossTarget != null)
            selectedTarget = bossTarget;
        else if (bomberTarget != null)
            selectedTarget = bomberTarget;
        else
            selectedTarget = enemyTarget;

        if (selectedTarget != null)
        {
            SetTarget(selectedTarget);
        }
    }

    private void SetTarget(Transform target)
    {
        ClearTarget();

        homingTarget = target;
        if (homingTarget != null)
        {
            targetHP = homingTarget.GetComponent<HP>();
            if (targetHP == null)
            {
                targetHP = homingTarget.GetComponentInParent<HP>();
            }

            if (targetHP != null)
            {
                targetHP.OnDied += HandleTargetDied;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[{bulletName}] hit {other.gameObject.tag}");
        Debug.Log($"[{bulletName}] hit {other.gameObject.name} (tag: {other.transform.root.gameObject.tag})");
        Transform hitRoot = other.transform.root;
        bool hitPlayer = other.CompareTag("Player") || hitRoot.CompareTag("Player");
        bool hitEnemy = other.CompareTag("Enemy") || hitRoot.CompareTag("Enemy");
        bool hitBoss = other.CompareTag("Boss") || hitRoot.CompareTag("Boss");
        AudioManager.Instance.PlayExplosion();
        // =============================
        // HIT SHIELD
        // =============================
        if (other.CompareTag("Shield"))
        {
            Transform root = other.transform.root;
            ProceduralForceFieldOverlay overlay = other.GetComponentInParent<ProceduralForceFieldOverlay>();
            if (overlay != null)
            {
                Vector3 hitPoint = other.ClosestPoint(transform.position);
                overlay.Trigger(hitPoint);
            }

            // Nếu đạn cùng phe với shield → bỏ qua
            if ((bulletTeam == Team.Player && root.CompareTag("Player")) ||
                (bulletTeam == Team.Enemy && (root.CompareTag("Enemy") || root.CompareTag("Boss"))))
            {
                return;
            }

            SpawnImpactEffect();
            DestroyBullet();
            return;
        }

        // =============================
        // HIT PLAYER / ENEMY
        // =============================
        if (hitPlayer || hitEnemy || hitBoss)
        {
            if (hitPlayer)
            {
                ControllerTank playerController = other.GetComponentInParent<ControllerTank>();
                if (playerController != null && playerController.isShield)
                {
                    SpawnImpactEffect();
                    DestroyBullet();
                    return;
                }
            }

            // ❌ Không gây damage cùng phe
            if ((bulletTeam == Team.Player && hitPlayer) ||
                (bulletTeam == Team.Enemy && (hitEnemy || hitBoss)))
            {
                return;
            }

            SpawnImpactEffect();

            HP hp = other.GetComponent<HP>();
            if (hp == null)
            {
                hp = other.GetComponentInParent<HP>();
            }
            if (hp != null)
            {
                if (bulletType == BulletType.CurvedHoming)
                {
                    EnemyAI enemy = other.GetComponentInParent<EnemyAI>();
                    if (enemy != null && enemy.type == EnemyAI.EnemyType.Bomber)
                    {
                        hp.TakeDamage(hp.MaxHP /2);
                    }
                    else
                    {
                        hp.TakeDamage(damage * damageMultiplier);
                    }
                }
                else
                {
                    hp.TakeDamage(damage * damageMultiplier);
                }
            }

            // ⚡ Tesla: gây stun cho enemy (không dùng hiệu ứng)
            if (bulletType == BulletType.rocket)
            {
                if (hitPlayer)
                {
                    ControllerTank playerController = other.GetComponentInParent<ControllerTank>();
                    if (playerController != null)
                    {
                        float playerKnockbackForce = knockbackForce * playerKnockbackForceMultiplier;
                        float playerKnockbackDuration = knockbackDuration * playerKnockbackDurationMultiplier;

                        playerController.Stun(playerKnockbackDuration, null);
                        playerController.Knockback(transform.forward, playerKnockbackForce, playerKnockbackDuration);
                    }
                }
                else if (hitEnemy || hitBoss)
                {
                    EnemyAI enemy = other.GetComponentInParent<EnemyAI>();
                    if (enemy != null)
                    {
                        enemy.Stun(knockbackDuration, null);
                        Vector3 knockbackDir = transform.forward;
                        enemy.Knockback(knockbackDir, knockbackForce, knockbackDuration);
                    }
                    else
                    {
                        enemyboss1 boss = other.GetComponentInParent<enemyboss1>();
                        if (boss != null)
                        {
                            float bossStunDuration = knockbackDuration * 0.5f;
                            float bossKnockbackForce = knockbackForce * 0.5f;
                            float bossKnockbackDuration = knockbackDuration * 0.5f;

                            boss.Stun(bossStunDuration, null);
                            Vector3 knockbackDir = transform.forward;
                            boss.Knockback(knockbackDir, bossKnockbackForce, bossKnockbackDuration);
                        }
                    }
                }

                return;
            }
            if (bulletType == BulletType.Tesla)
            {
                if (hitPlayer)
                {
                    ControllerTank playerController = other.GetComponentInParent<ControllerTank>();
                    if (playerController != null)
                    {
                        playerController.Stun(stunDuration, stunEffect);
                    }
                }
                else
                {
                    EnemyAI enemy = other.GetComponentInParent<EnemyAI>();
                    if (enemy != null)
                    {
                        enemy.Stun(stunDuration , stunEffect);
                    }
                    else
                    {
                        enemyboss1 boss = other.GetComponentInParent<enemyboss1>();
                        if (boss != null)
                        {
                            boss.Stun(stunDuration * 0.5f, stunEffect);
                        }
                    }
                }
            }

            DestroyBullet();
        }

        // =============================
        // HIT BARRIER / MAP
        // =============================
        else if (other.transform.root.CompareTag("b"))
        {
            SpawnImpactEffect();
            DestroyBullet();
        }
    }

    void SpawnImpactEffect()
    {
        if (effectImpact != null)
        {
            ParticleSystem effect = EffectPool.Spawn(effectImpact, transform.position, Quaternion.identity);
            if (effect == null) return;
            float finalEffectScale =
                effectScale *
                runtimeScaleFactor *
                impactScaleMultiplier *
                GlobalScaleManager.GetScale(GlobalScaleCategory.Effect);
            effect.transform.localScale = Vector3.one * finalEffectScale;
            effect.Play(true);

            AutoReturnParticle autoReturn = effect.GetComponent<AutoReturnParticle>();
            if (autoReturn == null)
            {
                autoReturn = effect.gameObject.AddComponent<AutoReturnParticle>();
            }
            autoReturn.ScheduleReturn(effect.main.duration);
        }
    }

    public void ApplyScaleFromShooter(Transform shooterRoot)
    {
        if (!applyShooterScale || shooterRoot == null)
        {
            runtimeScaleFactor = GlobalScaleManager.GetScale(GlobalScaleCategory.Bullet);
            transform.localScale = baseBulletScale * runtimeScaleFactor;
            return;
        }

        Vector3 shooterScale = shooterRoot.lossyScale;
        float maxAxis = Mathf.Max(shooterScale.x, Mathf.Max(shooterScale.y, shooterScale.z));
        runtimeScaleFactor =
            Mathf.Max(0.01f, maxAxis * bulletScaleMultiplier) *
            GlobalScaleManager.GetScale(GlobalScaleCategory.Bullet);
        transform.localScale = baseBulletScale * runtimeScaleFactor;
    }

    void DestroyBullet()
    {
        PooledProjectile pooledRoot = GetComponentInParent<PooledProjectile>();
        if (pooledRoot != null)
        {
            ProjectilePool.Despawn(pooledRoot.gameObject);
            return;
        }

        ProjectilePool.Despawn(gameObject);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log(message);
        }
    }
}

public static class ProjectilePool
{
    private static readonly System.Collections.Generic.Dictionary<int, System.Collections.Generic.Queue<GameObject>> Pools =
        new System.Collections.Generic.Dictionary<int, System.Collections.Generic.Queue<GameObject>>();
    private const int DefaultExpandBatchSize = 8;

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        int key = prefab.GetInstanceID();
        if (!Pools.TryGetValue(key, out System.Collections.Generic.Queue<GameObject> queue))
        {
            queue = new System.Collections.Generic.Queue<GameObject>();
            Pools[key] = queue;
        }

        if (queue.Count == 0)
        {
            WarmPool(prefab, key, DefaultExpandBatchSize, queue);
        }

        while (queue.Count > 0)
        {
            GameObject pooled = queue.Dequeue();
            if (pooled == null) continue;

            pooled.transform.SetPositionAndRotation(position, rotation);
            pooled.SetActive(true); 
            return pooled;
        }

        GameObject instance = CreatePooledInstance(prefab, key);
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.SetActive(true);
        return instance;
    }

    public static void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;

        int key = prefab.GetInstanceID();
        if (!Pools.TryGetValue(key, out System.Collections.Generic.Queue<GameObject> queue))
        {
            queue = new System.Collections.Generic.Queue<GameObject>();
            Pools[key] = queue;
        }

        int missing = count - queue.Count;
        if (missing > 0)
        {
            WarmPool(prefab, key, missing, queue);
        }
    }

    public static void Despawn(GameObject obj)
    {
        if (obj == null) return;

        PooledProjectile pooled = obj.GetComponent<PooledProjectile>();
        if (pooled == null)
        {
            Object.Destroy(obj);
            return;
        }

        int key = pooled.PoolKey;
        if (!Pools.TryGetValue(key, out System.Collections.Generic.Queue<GameObject> queue))
        {
            queue = new System.Collections.Generic.Queue<GameObject>();
            Pools[key] = queue;
        }

        obj.SetActive(false);
        queue.Enqueue(obj);
    }

    private static void WarmPool(
        GameObject prefab,
        int key,
        int count,
        System.Collections.Generic.Queue<GameObject> queue)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject instance = CreatePooledInstance(prefab, key);
            instance.SetActive(false);
            queue.Enqueue(instance);
        }
    }

    private static GameObject CreatePooledInstance(GameObject prefab, int key)
    {
        GameObject instance = Object.Instantiate(prefab);
        PooledProjectile pooledProjectile = instance.GetComponent<PooledProjectile>();
        if (pooledProjectile == null)
        {
            pooledProjectile = instance.AddComponent<PooledProjectile>();
        }
        pooledProjectile.PoolKey = key;
        return instance;
    }
}

public class PooledProjectile : MonoBehaviour
{
    public int PoolKey { get; set; }
}

public static class EffectPool
{
    private static readonly System.Collections.Generic.Dictionary<int, System.Collections.Generic.Queue<ParticleSystem>> Pools =
        new System.Collections.Generic.Dictionary<int, System.Collections.Generic.Queue<ParticleSystem>>();

    public static ParticleSystem Spawn(ParticleSystem prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab == null) return null;

        int key = prefab.GetInstanceID();
        if (!Pools.TryGetValue(key, out System.Collections.Generic.Queue<ParticleSystem> queue))
        {
            queue = new System.Collections.Generic.Queue<ParticleSystem>();
            Pools[key] = queue;
        }

        while (queue.Count > 0)
        {
            ParticleSystem pooled = queue.Dequeue();
            if (pooled == null) continue;

            Transform t = pooled.transform;
            t.SetParent(parent, false);
            t.SetPositionAndRotation(position, rotation);
            pooled.gameObject.SetActive(true);
            pooled.Clear(true);
            return pooled;
        }

        ParticleSystem created = Object.Instantiate(prefab, position, rotation, parent);
        PooledEffect pooledEffect = created.GetComponent<PooledEffect>();
        if (pooledEffect == null)
        {
            pooledEffect = created.gameObject.AddComponent<PooledEffect>();
        }
        pooledEffect.PoolKey = key;
        return created;
    }

    public static void Despawn(ParticleSystem effect)
    {
        if (effect == null) return;

        PooledEffect pooled = effect.GetComponent<PooledEffect>();
        if (pooled == null)
        {
            Object.Destroy(effect.gameObject);
            return;
        }

        int key = pooled.PoolKey;
        if (!Pools.TryGetValue(key, out System.Collections.Generic.Queue<ParticleSystem> queue))
        {
            queue = new System.Collections.Generic.Queue<ParticleSystem>();
            Pools[key] = queue;
        }

        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.transform.SetParent(null);
        effect.gameObject.SetActive(false);
        queue.Enqueue(effect);
    }
}

public class PooledEffect : MonoBehaviour
{
    public int PoolKey { get; set; }
}

public class AutoReturnParticle : MonoBehaviour
{
    private Coroutine returnRoutine;
    private ParticleSystem effect;

    private void Awake()
    {
        effect = GetComponent<ParticleSystem>();
    }

    public void ScheduleReturn(float delay)
    {
        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
        }

        returnRoutine = StartCoroutine(ReturnAfterDelay(Mathf.Max(0.01f, delay)));
    }

    private IEnumerator ReturnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        EffectPool.Despawn(effect);
        returnRoutine = null;
    }
}
