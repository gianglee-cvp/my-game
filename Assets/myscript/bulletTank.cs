using UnityEngine;

public enum Team
{
    Player,
    Enemy
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

    [Header("Effect")]
    public ParticleSystem effectImpact;

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

        // =============================
        // HIT PLAYER / ENEMY
        // =============================
        if (other.CompareTag("Player") || other.CompareTag("Enemy"))
        {
            // ❌ Không gây damage cùng phe
            if ((bulletTeam == Team.Player && other.CompareTag("Player")) ||
                (bulletTeam == Team.Enemy && other.CompareTag("Enemy")))
            {
                return;
            }

            SpawnImpactEffect();

            HP hp = other.GetComponent<HP>();
            if (hp != null)
            {
                hp.TakeDamage(damage);
            }

            DestroyBullet();
        }

        // =============================
        // HIT SHIELD
        // =============================
        else if (other.CompareTag("Shield"))
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