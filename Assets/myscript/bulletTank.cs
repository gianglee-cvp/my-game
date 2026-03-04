using UnityEngine;

public enum Team
{
    Player,
    Enemy
}

public enum BulletType
{
    Normal,
    Tesla,
    Plougher
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
    public float stunDuration = 5f;
    public ParticleSystem stunEffect;

    [Header("Plougher Settings")]
    public float knockbackForce = 18f;
    public float knockbackDuration = 1f;

    [Header("Info")]
    public string bulletName = "Tank Bullet";

    Rigidbody bulletEngine;

    void Start()
    {
        bulletEngine = GetComponent<Rigidbody>();

        if (bulletName == "plougher_bullet")
        {
            effectScale = 2f;
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

        // =============================
        // HIT SHIELD
        // =============================
        if (other.CompareTag("Shield"))
        {
            Transform root = other.transform.root;

            // Nếu đạn cùng phe với shield → bỏ qua
            if ((bulletTeam == Team.Player && root.CompareTag("Player")) ||
                (bulletTeam == Team.Enemy && root.CompareTag("Enemy")))
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
        if (hitPlayer || hitEnemy)
        {
            // ❌ Không gây damage cùng phe
            if ((bulletTeam == Team.Player && hitPlayer) ||
                (bulletTeam == Team.Enemy && hitEnemy))
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
            if (bulletType == BulletType.Plougher)
            {
                if (hitEnemy)
                {
                    EnemyAI enemy = other.GetComponentInParent<EnemyAI>();
                    if (enemy != null)
                    {
                        enemy.Stun(knockbackDuration, null);
                        Vector3 knockbackDir = transform.forward;
                        enemy.Knockback(knockbackDir, knockbackForce, knockbackDuration);
                    }
                }

                return;
            }
            if (bulletType == BulletType.Tesla)
            {
                EnemyAI enemy = other.GetComponentInParent<EnemyAI>();
                if (enemy != null)
                {
                    enemy.Stun(stunDuration , stunEffect);
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
