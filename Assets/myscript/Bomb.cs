using UnityEngine;

public class Bomb : MonoBehaviour
{
    public float explosionDamage = 50f;
    public GameObject explosionFX;

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

        if (hitObj.CompareTag("Player") || rootObj.CompareTag("Player") || rootObj.CompareTag("b"))
        {
            Explode(hitObj);
        }
    }

    void Explode(GameObject target)
    {
        Debug.Log("Bomb Exploding on impact with: " + target.name);

        if (explosionFX != null)
        {
            Instantiate(explosionFX, transform.position, Quaternion.identity);
        }

        // Apply damage if criteria met
        GameObject rootObj = target.transform.root.gameObject;
        if (target.CompareTag("Player") || rootObj.CompareTag("Player") || rootObj.CompareTag("b"))
        {
            HP targetHP = target.GetComponentInParent<HP>();
            if (targetHP == null) targetHP = target.GetComponentInChildren<HP>();
            
            if (targetHP != null)
            {
                targetHP.TakeDamage(explosionDamage);
            }
        }

        Destroy(gameObject);
    }
}
