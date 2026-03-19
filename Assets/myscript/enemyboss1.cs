using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class enemyboss1 : MonoBehaviour
{
    public Transform player;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotateSpeed = 5f;
    public float detectionRange = 40f;
    public float stopRange = 10f;
    public float shootRange = 30f;

    [Header("Boss Fire")]
    public GameObject bullet;
    [Min(0)] public int bulletPrewarmCount = 32;
    public Transform[] firePoints;
    public ParticleSystem[] shootFX;
    public float shotInterval = 0.25f;
    public float aimRayDistance = 200f;
    public LayerMask aimMask = ~0;
    public bool loopFirePoints = true;
    public float preFireAimDuration = 0.2f;

    [Header("Spiral (Prepared API)")]
    public GameObject[] spiralBulletPrefabs;
    public GameObject spiralBulletPrefab;
    [Min(0)] public int spiralPrewarmPerPrefab = 12;
    public float spiralAngleStep = 25f;
    public float spiralFireInterval = 0.2f;
    public Transform spiralFireOrigin;
    public int spiralBulletCount = 12;
    private float spiralCurrentAngle = 0f;
    private float nextSpiralTime = 0f;

    [Header("Stun Effect")]
    public Transform[] stunEffectPoints;

    [Header("Knockback")]
    public float knockbackResistance = 1f;

    public EnemySpawner mySpawner;

    private NavMeshAgent agent;
    private bool isStunned = false;
    private float stunTimer = 0f;
    private readonly List<ParticleSystem> stunEffectInstances = new List<ParticleSystem>();
    private bool isKnockedBack = false;
    private float knockbackTimer = 0f;
    private Vector3 knockbackVelocity = Vector3.zero;
    private float nextFireTime = 0f;
    private int nextFirePointIndex = 0;
    private bool isPreparingShot = false;
    private Vector3 baseLocalScale;
    private float baseMoveSpeed;
    private float baseRotateSpeed;

    private HP myHP;

    void Awake()
    {
        baseLocalScale = transform.localScale;
        baseMoveSpeed = moveSpeed;
        baseRotateSpeed = rotateSpeed;
        myHP = GetComponent<HP>();
    }

    void Start()
    {
        ApplyGlobalScale();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            ApplyGlobalSpeed();
            agent.stoppingDistance = stopRange;
            agent.updateRotation = false;
        }

        ProjectilePool.Prewarm(bullet, bulletPrewarmCount);
        if (spiralBulletPrefabs != null)
        {
            for (int i = 0; i < spiralBulletPrefabs.Length; i++)
            {
                ProjectilePool.Prewarm(spiralBulletPrefabs[i], spiralPrewarmPerPrefab);
            }
        }
        ProjectilePool.Prewarm(spiralBulletPrefab, spiralPrewarmPerPrefab);
    }

    void OnEnable()
    {
        ApplyGlobalSpeed();
        ApplyGlobalScale();

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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerVictory();
        }
    }

    void Update()
    {
        if (player == null || agent == null) return;

        if (isKnockedBack)
        {
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.Move(knockbackVelocity * Time.deltaTime);
            }

            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0f)
            {
                isKnockedBack = false;
                knockbackVelocity = Vector3.zero;

                if (agent.enabled && agent.isOnNavMesh && !isStunned)
                    agent.isStopped = false;
            }
            return;
        }

        if (isStunned)
        {
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }

            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                isStunned = false;
                ClearStunEffects();

                if (agent.enabled && agent.isOnNavMesh)
                    agent.isStopped = false;
            }
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > detectionRange) return;

        agent.SetDestination(player.position);
        RotateToPlayer();

        if (distanceToPlayer <= stopRange)
            agent.isStopped = true;
        else
            agent.isStopped = false;

        if (distanceToPlayer <= shootRange)
        {
            FireAllPoints();

            if (Time.time >= nextSpiralTime)
            {
                FireSpiralOnce(spiralBulletCount);
                nextSpiralTime = Time.time + Mathf.Max(0.01f, spiralFireInterval);
            }
        }
        else
        {
            if (Time.time >= nextSpiralTime)
            {
                FireSpiralOnce(spiralBulletCount);
                nextSpiralTime = Time.time + Mathf.Max(0.01f, spiralFireInterval);
            }
        }
    }

    void RotateToPlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotateSpeed * Time.deltaTime
        );
    }
    // bắn từ firePoints theo thứ tự, có thể loop hoặc không loop, có thể lock hướng bắn trong vài giây trước khi bắn, có thể raycast để lock hướng bắn chính xác hơn nếu có vật cản giữa boss và player.
    void FireSingleFromNextPoint()
    {
        if (Time.time < nextFireTime) return;
        if (bullet == null || firePoints == null || firePoints.Length == 0) return;
        if (isPreparingShot) return;

        int index = GetNextFirePointIndex();
        Transform muzzle = firePoints[index];
        if (muzzle == null) return;

        StartCoroutine(PrepareAndFire(muzzle));
    }
    // bắn tất cả firePoints cùng lúc, không lock hướng bắn, không raycast, chỉ dùng forward của firePoint làm hướng bắn.
    void FireAllPoints()
    {
        if (Time.time < nextFireTime) return;
        if (bullet == null || firePoints == null || firePoints.Length == 0) return;
        if (isPreparingShot) return;

        for (int i = 0; i < firePoints.Length; i++)
        {
            Transform muzzle = firePoints[i];
            if (muzzle == null) continue;

            // giữ nguyên logic aim cũ
            Vector3 lockedDirection = GetLockedShotDirection(muzzle);

            SpawnBullet(bullet, muzzle.position, lockedDirection);
        }

        PlayShootFX();

        nextFireTime = Time.time + Mathf.Max(0.01f, shotInterval);
    }

    IEnumerator PrepareAndFire(Transform muzzle)
    {
        isPreparingShot = true;

        float aimDuration = Mathf.Max(0f, preFireAimDuration);
        Vector3 lockedDirection = GetLockedShotDirection(muzzle);

        if (aimDuration > 0f)
        {
            float endTime = Time.time + aimDuration;
            while (Time.time < endTime)
            {
                if (player == null || isStunned || isKnockedBack)
                {
                    isPreparingShot = false;
                    yield break;
                }

                lockedDirection = GetLockedShotDirection(muzzle);
                yield return null;
            }
        }

        PlayShootFX();
        SpawnBullet(bullet, muzzle.position, lockedDirection);
        nextFireTime = Time.time + Mathf.Max(0.01f, shotInterval);
        isPreparingShot = false;
    }

    int GetNextFirePointIndex()
    {
        if (!loopFirePoints)
            return Mathf.Clamp(nextFirePointIndex, 0, firePoints.Length - 1);

        int current = nextFirePointIndex;
        nextFirePointIndex = (nextFirePointIndex + 1) % firePoints.Length;
        return current;
    }

    Vector3 GetLockedShotDirection(Transform muzzle)
    {
        if (player == null) return muzzle.forward;

        Vector3 toPlayer = player.position - muzzle.position;
        if (toPlayer.sqrMagnitude < 0.001f) return muzzle.forward;

        Vector3 rayDir = toPlayer.normalized;
        if (Physics.Raycast(muzzle.position, rayDir, out RaycastHit hit, aimRayDistance, aimMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != null && hit.collider.transform.root == transform)
                return rayDir;

            Vector3 hitDirection = hit.point - muzzle.position;
            if (hitDirection.sqrMagnitude > 0.0001f)
                return hitDirection.normalized;
        }

        return rayDir;
    }

    void SpawnBullet(GameObject bulletPrefab, Vector3 spawnPos, Vector3 direction)
    {
        if (bulletPrefab == null) return;

        Quaternion rot = Quaternion.LookRotation(direction);
        GameObject bulletInstance = ProjectilePool.Spawn(bulletPrefab, spawnPos, rot);

        bulletTank bulletScript = bulletInstance.GetComponent<bulletTank>();
        if (bulletScript == null)
            bulletScript = bulletInstance.GetComponentInChildren<bulletTank>();

        if (bulletScript != null)
            bulletScript.bulletTeam = Team.Enemy;
    }

    void PlayShootFX()
    {
        if (shootFX == null) return;

        for (int i = 0; i < shootFX.Length; i++)
        {
            if (shootFX[i] != null)
                shootFX[i].Play();
        }
    }

    // Prepared API: call this from animation/event/phase logic when you want spiral shots.
    public void FireSpiralOnce(int bulletCount)
    {
        if (bulletCount <= 0) return;
        GameObject spiralBullet = GetRandomSpiralBulletPrefab();
        if (spiralBullet == null) return;

        Transform origin = spiralFireOrigin != null ? spiralFireOrigin : transform;
        float step = 360f / bulletCount;

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = spiralCurrentAngle + (i * step);
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
            SpawnBullet(spiralBullet, origin.position, dir.normalized);
        }

        spiralCurrentAngle += spiralAngleStep;
        if (spiralCurrentAngle >= 360f)
            spiralCurrentAngle -= 360f;
    }

    GameObject GetRandomSpiralBulletPrefab()
    {
        if (spiralBulletPrefabs != null && spiralBulletPrefabs.Length > 0)
        {
            List<GameObject> validPrefabs = new List<GameObject>();
            for (int i = 0; i < spiralBulletPrefabs.Length; i++)
            {
                if (spiralBulletPrefabs[i] != null)
                    validPrefabs.Add(spiralBulletPrefabs[i]);
            }

            if (validPrefabs.Count > 0)
                return validPrefabs[Random.Range(0, validPrefabs.Count)];
        }

        if (spiralBulletPrefab != null) return spiralBulletPrefab;
        return bullet;
    }

    public void Stun(float duration, ParticleSystem stunEffect)
    {
        Debug.Log($"Boss stunned for {duration} seconds");
        if (isStunned)
        {
            stunTimer = duration;
            return;
        }

        isStunned = true;
        stunTimer = duration;

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
                    point
                );

                var main = effectInstance.main;
                main.loop = true;
                effectInstance.transform.localScale = Vector3.one * 2f;
                effectInstance.Play();
                stunEffectInstances.Add(effectInstance);
            }
        }
    }

    public void Knockback(Vector3 direction, float force, float duration)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;

        float finalResistance = Mathf.Max(0.01f, knockbackResistance);
        knockbackVelocity = direction.normalized * (force / finalResistance);
        knockbackTimer = duration;
        isKnockedBack = true;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    void ClearStunEffects()
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

    void OnDestroy()
    {
        ClearStunEffects();

        if (mySpawner != null)
            mySpawner.EnemyDied();
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
