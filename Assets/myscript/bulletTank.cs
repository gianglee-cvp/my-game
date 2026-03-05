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
    rocket
}

public class bulletTank : MonoBehaviour
{
    [Header("Stats (Set in Inspector)")]
    public float speed = 20f;
    public float damage = 20f;
    public float fireCooldown = 1f;
    public float effectScale = 1f;
    float destroyTime = 10f;

    [Header("Team")]
    public Team bulletTeam;   // Xác định phe của đạn

    [Header("Bullet Type")]
    public BulletType bulletType = BulletType.Normal;

    [Header("Effect")]
    public ParticleSystem effectImpact;

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

    Rigidbody bulletEngine;

    void Start()
    {
        bulletEngine = GetComponent<Rigidbody>();

        if (bulletName == "plougher_bullet")
        {
            effectScale = 3f;
        }

        Debug.Log("position bullet: " + transform.position);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        destroyTime -= Time.deltaTime;
        if (destroyTime <= 0)
        {
            DestroyBullet();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[{bulletName}] hit {other.gameObject.tag}");
        Transform hitRoot = other.transform.root;
        bool hitPlayer = other.CompareTag("Player") || hitRoot.CompareTag("Player");
        bool hitEnemy = other.CompareTag("Enemy") || hitRoot.CompareTag("Enemy");
        bool hitBoss = other.CompareTag("Boss") || hitRoot.CompareTag("Boss");

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
                hp.TakeDamage(damage);
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
            ParticleSystem effect = Instantiate(
                effectImpact,
                transform.position,
                Quaternion.identity
            );

            effect.transform.localScale = Vector3.one * effectScale;
            Destroy(effect.gameObject, effect.main.duration);
        }
    }

    void DestroyBullet()
    {
        if (transform.parent != null)
        {
            Destroy(transform.parent.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
