using UnityEngine;

public class Bomb : MonoBehaviour
{
    public float explosionDamage = 50f;
    public GameObject explosionFX;
    int firsttime = 0 ; 
    public float explosionRadius = 5f ; 
    private void OnCollisionEnter(Collision collision)
    {
        HandleImpact(collision.gameObject, "Collision");
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleImpact(other.gameObject, "Trigger");
    }

    private void HandleImpact(GameObject hitObj, string type)
    {
        GameObject rootObj = hitObj.transform.root.gameObject;
        Debug.Log($"[{type}] Bomb hit: {hitObj.name} | Root: {rootObj.name} | Root Tag: {rootObj.tag}");

        if (hitObj.CompareTag("Player") || rootObj.CompareTag("Player") || rootObj.CompareTag("b") || rootObj.CompareTag("Enemy"))
        {   
            if(firsttime < 2 ){
                firsttime ++ ; 
                return ; 
            }
            Explode(hitObj);
        }
    }

    void Explode(GameObject target)
    {
    Debug.Log("Bomb Exploding on impact with: " + target.name);

    // 1️⃣ Spawn effect
    if (explosionFX != null)
    {
        Instantiate(explosionFX, transform.position, Quaternion.identity)
            .transform.localScale *= explosionRadius;
    }

    // 2️⃣ Lấy TẤT CẢ collider trong bán kính nổ
    Collider[] hitColliders = Physics.OverlapSphere(
        transform.position,
        explosionRadius
    );

    // 3️⃣ DUYỆT TỪNG collider (đây là chỗ bạn thiếu)
    foreach (Collider col in hitColliders)
    {
        GameObject rootObj = col.transform.root.gameObject;

        // Chỉ damage Player / Enemy / b
        if (rootObj.CompareTag("Player") ||
            rootObj.CompareTag("Enemy") ||
            rootObj.CompareTag("b"))
        {
            HP hp = rootObj.GetComponentInParent<HP>();
            if (hp == null)
                hp = rootObj.GetComponentInChildren<HP>();

            if (hp != null)
            {
                hp.TakeDamage(explosionDamage);
                Debug.Log("Damaged: " + rootObj.name);
            }
        }
    }

    // 4️⃣ Hủy bomb
    Destroy(gameObject);
    }
}
