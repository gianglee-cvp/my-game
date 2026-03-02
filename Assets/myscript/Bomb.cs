using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    public float explosionDamage = 50f;
    public GameObject explosionFX;
    private bool hasExploded = false;
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
        // ✅ Chặn Explode() gọi nhiều lần (OnCollision + OnTrigger cùng fire)
        if (hasExploded) return;

        GameObject rootObj = hitObj.transform.root.gameObject;
        Debug.Log($"[{type}] Bomb hit: {hitObj.name} | Root: {rootObj.name} | Root Tag: {rootObj.tag}");

        if (hitObj.CompareTag("Player") || rootObj.CompareTag("Player") || rootObj.CompareTag("b") || rootObj.CompareTag("Enemy"))
        {
            hasExploded = true;  // 🔒 Khóa ngay, không cho gọi lại
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

        // 2️⃣ Lấy tất cả collider trong bán kính
        Collider[] hitColliders = Physics.OverlapSphere(
            transform.position,
            explosionRadius
        );

        // ✅ Dùng HashSet để tránh xử lý cùng một root nhiều lần
        HashSet<GameObject> damagedObjects = new HashSet<GameObject>();

        foreach (Collider col in hitColliders)
        {
            GameObject rootObj = col.transform.root.gameObject;

            // nếu đã tính damage rồi thì bỏ qua
            if (damagedObjects.Contains(rootObj))
                continue;

            if (rootObj.CompareTag("Player") ||
                rootObj.CompareTag("Enemy") ||
                rootObj.CompareTag("b"))
            {
                HP hp = rootObj.GetComponentInParent<HP>();
                if (hp == null)
                    hp = rootObj.GetComponentInChildren<HP>();

                if (hp != null)
                {
                    // nếu đối tượng có controller và đang bật shield thì không bị damage
                    ControllerTank ctrl = rootObj.GetComponent<ControllerTank>();
                    if (ctrl != null && ctrl.isShield)
                    {
                        Debug.Log("Shielded, skipping damage: " + rootObj.name);
                    }
                    else
                    {
                        hp.TakeDamage(explosionDamage);
                        damagedObjects.Add(rootObj);   // 👈 đánh dấu đã damage
                        Debug.Log("Damaged: " + rootObj.name);
                    }
                }
            }
        }

        Destroy(gameObject);
    }
}
