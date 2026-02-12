using UnityEngine;

public class ControllerTank : MonoBehaviour
{
    public float Movespeed = 4f;
    float RotateSpeed = 60f;

    Rigidbody TankEngine;

    public GameObject Tower;
    public Camera CameraFollow;

    public ParticleSystem[] ShootFX;

    public GameObject bullet;
    public Transform shootElement;

    private float nextFireTime = 0f;

    void Start()
    {
        TankEngine = GetComponent<Rigidbody>();
    }

    void Move()
    {
        Vector3 move = transform.forward *
                       Input.GetAxis("Vertical") *
                       Movespeed *
                       Time.deltaTime;

        TankEngine.MovePosition(TankEngine.position + move);
    }

    void Rotate()
    {
        float r = Input.GetAxis("Horizontal") *
                  RotateSpeed *
                  Time.deltaTime;

        Quaternion rotate = Quaternion.Euler(0, r, 0);
        TankEngine.MoveRotation(TankEngine.rotation * rotate);
    }

    void RotateTower()
    {
        Ray ray = CameraFollow.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 target = ray.GetPoint(distance);
            Vector3 direction = target - transform.position;

            float rotation = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            Tower.transform.rotation = Quaternion.Euler(0, rotation, 0);
        }
    }

    void Fire()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        if (Time.time < nextFireTime) return;

        Debug.Log("Player Fire");

        for (int i = 0; i < ShootFX.Length; i++)
        {
            ShootFX[i].Play();
        }

        GameObject bulletInstance = Instantiate(
            bullet,
            shootElement.position,
            shootElement.rotation
        );

        // 🔥 Lấy cooldown từ bullet
        bulletTank bulletScript = bulletInstance.GetComponent<bulletTank>();

        if (bulletScript != null)
        {
            nextFireTime = Time.time + 0.2f*bulletScript.fireCooldown;
        }
        else
        {
            nextFireTime = Time.time + 0.5f; // fallback
        }
    }

    void Update()
    {
        Move();
        Rotate();
        RotateTower();
        Fire();
    }
}