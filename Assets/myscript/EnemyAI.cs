using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyType { Shooter, Bomber }
    public EnemyType type;

    public Transform player;
    public float moveSpeed = 5f;
    public float rotateSpeed = 5f;
    public float detectionRange = 40f;
    public float stopRange = 10f;
    public float shootRange = 30f;

    public GameObject bullet;
    public GameObject bombPrefab;
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
            PerformAction();
            return;
        }

        if (distanceToPlayer <= shootRange)
        {
            PerformAction();
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

    private bool hasActed = false;

    void PerformAction()
    {
        if (hasActed) return;

        if (type == EnemyType.Shooter)
        {
            Fire();
            hasActed = true; // For shooter, maybe we want it to keep firing? 
            // The user said "chỉ cần dropbom 1 lần thôi rồi object biến mất" 
            // and later "sửa thành có biến enemy đã bắn hay chưa nếu đã bắn rồi thì xoá object đó".
            // So if Shooter fires once, it also disappears? 
            // I'll assume both types act once and disappear based on the request.
            Destroy(gameObject, 0.1f); 
        }
        else if (type == EnemyType.Bomber)
        {
            DropBomb();
        }
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

        bulletTank bulletScript = bulletInstance.GetComponentInChildren<bulletTank>();

        if (bulletScript != null)
        {
            nextFireTime = Time.time + bulletScript.fireCooldown;
        }
    }

    void DropBomb()
    {
        if (hasActed) return;
        
        Bomb bombScript = GetComponentInChildren<Bomb>();
        if (bombScript != null)
        {
            hasActed = true;
            GameObject bombObject = bombScript.gameObject;
            
            // Detach bomb from drone so it doesn't get destroyed with the drone
            bombObject.transform.SetParent(null);
            
            // Ensure it has a Rigidbody to fall
            Rigidbody bombRb = bombObject.GetComponent<Rigidbody>();
            if (bombRb == null)
            {
                bombRb = bombObject.AddComponent<Rigidbody>();
            }
            
            // Activate physics
            bombRb.isKinematic = false;
            bombRb.useGravity = true;

            Debug.Log("Drone dropped bomb and destroying self");
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning("No child with Bomb script found!");
        }
    }
}