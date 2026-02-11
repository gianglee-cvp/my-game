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
    public float fireCooldown = 2f;
    private float nextFireTime = 0f;

    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        Debug.Log("Distance to Player: " + distanceToPlayer);
        if (distanceToPlayer > detectionRange) return;
        if (distanceToPlayer <= stopRange)
        {
            RotateToPlayer();
            Fire();
            return;
        }
        if(distanceToPlayer <= shootRange)
        {
            RotateToPlayer();
            Fire();
        }
        RotateToPlayer();
        MoveForward();
    }

    void RotateToPlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f; // Không nghiêng lên xuống

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
        nextFireTime = Time.time + fireCooldown;
        Debug.Log("Enemy Fire");
        for (int i = 0; i < ShootFX.Length; i++)
        {
            ShootFX[i].Play();
        }
        GameObject bulletInstance = Instantiate(bullet, shootElement.position, shootElement.rotation);
    }
}