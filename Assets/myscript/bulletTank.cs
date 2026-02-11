using UnityEngine ; 
public class bulletTank : MonoBehaviour {
    float speed = 20 ; 
    public float damage = 20f ;
    Rigidbody bulletEngine ; 
    public ParticleSystem effectImpact ;
    void Start (){
        bulletEngine = GetComponent<Rigidbody>() ;
    }
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Collide with " + other.gameObject.tag);
        // Nếu trúng Tank → gây damage
        if (other.CompareTag("Enemy")|| other.CompareTag("Player"))
        {
            ParticleSystem effect = Instantiate(
                effectImpact,
                transform.position,
                Quaternion.identity
            );

            // Tự hủy theo thời gian particle
            Destroy(effect.gameObject, effect.main.duration);
            HP hp = other.GetComponent<HP>();
            if (hp != null)
            {
                hp.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
        else if (other.CompareTag("b"))
        {
            Destroy(gameObject);
        }
    }

    void Update (){
        transform.Translate(Vector3.forward * speed * Time.deltaTime) ;
    }
}