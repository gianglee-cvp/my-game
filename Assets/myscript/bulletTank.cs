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

        if (other.CompareTag("Enemy") || other.CompareTag("Player"))
        {
            ParticleSystem effect = Instantiate(
                effectImpact,
                transform.position,
                Quaternion.identity
            );
            effect.transform.localScale = Vector3.one * effectScale;
            Destroy(effect.gameObject, effect.main.duration);
            
            HP hp = other.GetComponent<HP>();
            if (hp != null)
            {
                hp.TakeDamage(damage);
            }
            else
            {
                Debug.LogWarning($"[{bulletName}] hit {other.gameObject.name} but no HP component found!");
            }
            Destroy(transform.parent.gameObject);
        }
        else if (other.transform.root.CompareTag("b"))
        {
            Debug.Log($"[{bulletName}] hit barrier, destroying...");
            Destroy(transform.parent.gameObject);
        }
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
        destroyTime -= Time.deltaTime;
        if (destroyTime <= 0)
        {
            Destroy(transform.parent.gameObject);
            Debug.Log("destroy bullet");
        }
    }
}