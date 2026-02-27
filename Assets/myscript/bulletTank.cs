using UnityEngine;

public class bulletTank : MonoBehaviour
{
    [Header("Stats (Set in Inspector)")]
    public float speed = 20;
    public float damage = 20f;
    public float fireCooldown = 1f;
    public float effectScale = 1f;
    float destroyTime = 10f;

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

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[{bulletName}] hit {other.gameObject.tag}");
        if (other.CompareTag("Enemy") || other.CompareTag("Player"))
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

            HP hp = other.GetComponent<HP>();
            if (hp != null)
            {
                hp.TakeDamage(damage);
            }
            else
            {
                Debug.LogWarning($"[{bulletName}] hit {other.gameObject.name} but no HP component found!");
            }

            DestroyBullet();
        }
        else if (other.transform.root.CompareTag("b") || other.CompareTag("Shield"))
        {
            Debug.Log($"[{bulletName}] hit barrier, destroying...");
            DestroyBullet();
        }
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        destroyTime -= Time.deltaTime;
        if (destroyTime <= 0)
        {
            DestroyBullet();
            Debug.Log("destroy bullet");
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