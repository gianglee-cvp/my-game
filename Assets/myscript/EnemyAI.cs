using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public Transform player;
    public float moveSpeed = 5f;
    public float rotateSpeed = 5f;
    public float detectionRange = 40f;
    public float stopRange = 10f;
    public float shootRange = 30f;

    public GameObject bullet;
    public Transform shootElement;
    public ParticleSystem[] ShootFX;

    private float nextFireTime = 0f;

    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > detectionRange) return;

        RotateToPlayer();

        if (distanceToPlayer <= stopRange)
        {
            Fire();
            return;
        }

        if (distanceToPlayer <= shootRange)
        {
            Fire();
        }

        MoveForward();
    }

    void RotateToPlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        Quaternion smoothRotation = Quaternion.Slerp(
            rb.rotation,
            targetRotation,
            rotateSpeed * Time.fixedDeltaTime
        );

        rb.MoveRotation(smoothRotation);
    }

    void MoveForward()
    {
        Vector3 move = transform.forward * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);
    }

    void Fire()
    {
        if (Time.time < nextFireTime) return;

        Debug.Log("Enemy Fire");

        for (int i = 0; i < ShootFX.Length; i++)
        {
            ShootFX[i].Play();
        }

        GameObject bulletInstance = Instantiate(
            bullet,
            shootElement.position,
            shootElement.rotation
        );

     //   bulletTank bulletScript = bulletInstance.GetComponent<bulletTank>();
        bulletTank bulletScript = bulletInstance.GetComponentInChildren<bulletTank>();

        if (bulletScript != null)
        {
            nextFireTime = Time.time + bulletScript.fireCooldown;
        }
        else
        {
            Debug.LogWarning("Bullet has no bulletTank script!");
            nextFireTime = Time.time + 1f; // fallback
        }
    }
}