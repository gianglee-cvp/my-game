using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyType { Shooter, Bomber }
    public EnemyType type;

    public Transform player;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotateSpeed = 5f;
    public float detectionRange = 40f;
    public float stopRange = 10f;
    public float shootRange = 30f;

    [Header("Bomber Flight")]
    public float flyHeight = 7f;
    public float ascendSpeed = 3f;

    [Header("Attack")]
    public GameObject bullet;
    public Transform shootElement;
    public ParticleSystem[] ShootFX;

    public EnemySpawner mySpawner;

    private float nextFireTime = 0f;
    private bool hasActed = false;
    private bool isAscending = false;

    private NavMeshAgent agent;
    private Rigidbody rb;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        if (type == EnemyType.Shooter)
        {
            agent = GetComponent<NavMeshAgent>();
            agent.speed = moveSpeed;
            agent.stoppingDistance = stopRange;
            agent.updateRotation = false;
        }
        else if (type == EnemyType.Bomber)
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;      // Không cho rơi
            isAscending = true;         // Bắt đầu bay lên
        }
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > detectionRange) return;

        if (type == EnemyType.Shooter)
        {
            ShooterLogic(distanceToPlayer);
        }
    }

    void FixedUpdate()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > detectionRange) return;

        if (type == EnemyType.Bomber)
        {
            if (isAscending)
            {
                Ascend();
                return;
            }

            BomberLogic(distanceToPlayer);
        }
    }

    // ================= SHOOTER =================

    void ShooterLogic(float distance)
    {
        agent.SetDestination(player.position);

        if (distance <= stopRange)
        {
            agent.isStopped = true;
            RotateToPlayer();
            Fire();
        }
        else
        {
            agent.isStopped = false;
            RotateToPlayer();

            if (distance <= shootRange)
            {
                Fire();
            }
        }
    }

    // ================= BOMBER =================

    void Ascend()
    {
        Vector3 targetPos = new Vector3(rb.position.x, flyHeight, rb.position.z);

        Vector3 newPos = Vector3.MoveTowards(
            rb.position,
            targetPos,
            ascendSpeed * Time.fixedDeltaTime
        );

        rb.MovePosition(newPos);

        if (Mathf.Abs(rb.position.y - flyHeight) < 0.1f)
        {
            isAscending = false;
        }
    }

    void BomberLogic(float distance)
    {
        RotateToPlayerRB();

        if (distance <= stopRange)
        {
            DropBomb();
            return;
        }

        MoveForwardRB();
    }

    void RotateToPlayerRB()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        Quaternion smoothRotation = Quaternion.Slerp(
            rb.rotation,
            targetRotation,
            rotateSpeed * Time.fixedDeltaTime
        );

        rb.MoveRotation(smoothRotation);
    }

    void MoveForwardRB()
    {
        // Giữ cố định độ cao 7f
        Vector3 forwardMove = transform.forward * moveSpeed * Time.fixedDeltaTime;
        Vector3 newPos = rb.position + forwardMove;
        newPos.y = flyHeight;

        rb.MovePosition(newPos);
    }

    // ================= COMMON =================

    void RotateToPlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotateSpeed * Time.deltaTime
        );
    }

    void Fire()
    {
        if (Time.time < nextFireTime) return;

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
            bombObject.transform.SetParent(null);

            Rigidbody bombRb = bombObject.GetComponent<Rigidbody>();
            if (bombRb == null)
                bombRb = bombObject.AddComponent<Rigidbody>();

            bombRb.isKinematic = false;
            bombRb.useGravity = true;

            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (mySpawner != null)
            mySpawner.EnemyDied();
    }
}