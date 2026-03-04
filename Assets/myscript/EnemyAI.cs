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

    [Header("Attack")]
    public GameObject bullet;
    public Transform shootElement;
    public ParticleSystem[] ShootFX;

    public EnemySpawner mySpawner;

    private float nextFireTime = 0f;
    private bool hasActed = false;
    private bool isAscending = false;
    private float bomberSpawnTime = 0f;

    private NavMeshAgent agent;
    private Rigidbody rb;

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

    void Start()
    {

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        Debug.Log("Player object: " + playerObj.name);
        if (playerObj == null)
        {
            Debug.LogError("❌ KHÔNG TÌM THẤY PLAYER TRONG SCENE!");
        }
        else
        {
            Debug.Log("✅ TÌM THẤY PLAYER: " + playerObj.name);
            player = playerObj.transform;
        }
        if (type == EnemyType.Shooter)
        {
            agent = GetComponent<NavMeshAgent>();
            agent.speed = moveSpeed;
            agent.stoppingDistance = stopRange;
            agent.updateRotation = false;
        }
        else if (type == EnemyType.Bomber)
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;      // KhÃ´ng cho rÆ¡i
            isAscending = true;         // Báº¯t Ä‘áº§u bay lÃªn
            bomberSpawnTime = Time.time;
        }
    }

    void Update()
    {
        if (type == EnemyType.Bomber && Time.time - bomberSpawnTime >= bomberLifetime)
        {
            Destroy(gameObject);
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
                        Destroy(effect.gameObject);
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

        GameObject bulletInstance = Instantiate(
            bullet,
            shootElement.position,
            shootElement.rotation
        );

        bulletTank bulletScript = bulletInstance.GetComponentInChildren<bulletTank>();
        bulletScript.bulletTeam = Team.Enemy;
        if (bulletScript != null)
        {
            nextFireTime = Time.time + bulletScript.fireCooldown;
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

                ParticleSystem effectInstance = Instantiate(
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

        Debug.Log(gameObject.name + " bá»‹ stun trong " + duration + " giÃ¢y");
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

        Bomb bombScript = GetComponentInChildren<Bomb>();
        if (bombScript != null)
        {
            hasActed = true;

            GameObject bombObject = bombScript.gameObject;
            bombObject.transform.SetParent(null);

            Rigidbody bombRb = bombObject.GetComponent<Rigidbody>();
            if (bombRb == null)
                bombRb = bombObject.AddComponent<Rigidbody>();

            bombRb.isKinematic = false;
            bombRb.useGravity = false;

            bombScript.LaunchAtTarget(player);

            // âœ… Bá» qua collision giá»¯a bom vÃ  enemy Ä‘Ã£ drop nÃ³
            // â†’ TrÃ¡nh bom ná»• ngay khi vá»«a tÃ¡ch ra khá»i enemy
            Collider bombCol = bombObject.GetComponent<Collider>();
            Collider[] enemyCols = GetComponentsInChildren<Collider>();
            if (bombCol != null)
            {
                foreach (Collider ec in enemyCols)
                    Physics.IgnoreCollision(bombCol, ec, true);
            }

            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (mySpawner != null)
            mySpawner.EnemyDied();
    }
}
