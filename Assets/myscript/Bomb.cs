using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralForceField;

public class Bomb : MonoBehaviour
{
    public float explosionDamage = 50f;
    public GameObject explosionFX;
    private bool hasExploded = false;
    public float explosionRadius = 5f;
    public float contactFuse = 0.05f;

    [Header("Homing Movement")]
    public float homingSpeed = 18f;
    public float homingAcceleration = 40f;

    [Header("Bomb Physics")] 
    public float bombMass = 500f;
    public float bombDrag = 0.6f;
    public float bombAngularDrag = 2f;
    public float postContactSpeedMultiplier = 1.1f;
    public float contactPushImpulse = 900f;

    private Transform homingTarget;
    private Rigidbody rb;
    private bool isHoming = false;
    private bool isFuseStarted = false;
    private bool shakeOnExplode = false;
    private CameraFollow cameraFollow;
    private Vector3 baseLocalScale;

    private void Awake()
    {
        baseLocalScale = transform.localScale;
        rb = GetComponent<Rigidbody>();
        Camera mainCam = Camera.main;
        if (mainCam != null)
            cameraFollow = mainCam.GetComponent<CameraFollow>();

        if (cameraFollow == null)
            cameraFollow = Object.FindFirstObjectByType<CameraFollow>();
    }

    private void OnEnable()
    {
        transform.localScale = baseLocalScale * GlobalScaleManager.GetScale(GlobalScaleCategory.Bomb);
    }

    private void FixedUpdate()
    {
        if (!isHoming || hasExploded || homingTarget == null)
            return;

        if (rb == null)
            return;

        Vector3 direction = homingTarget.position - rb.position;
        if (direction.sqrMagnitude < 0.001f)
            return;

        Vector3 targetVelocity = direction.normalized * homingSpeed;
        rb.linearVelocity = Vector3.MoveTowards(
            rb.linearVelocity,
            targetVelocity,
            homingAcceleration * Time.fixedDeltaTime
        );
    }

    public void LaunchAtTarget(Transform target)
    {
        if (target == null)
            return;

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = false;
        rb.mass = bombMass;
        rb.linearDamping = bombDrag;
        rb.angularDamping = bombAngularDrag;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Collider[] bombColliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in bombColliders)
            col.isTrigger = false;

        homingTarget = target;
        isHoming = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleImpact(collision.gameObject, "Collision", true);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleImpact(other.gameObject, "Trigger", false);
    }

    private void HandleImpact(GameObject hitObj, string type, bool fromPhysicsCollision)
    {
        if (hasExploded)
            return;

        GameObject rootObj = hitObj.transform.root.gameObject;
        Debug.Log($"[{type}] Bomb hit: {hitObj.name} | Root: {rootObj.name} | Root Tag: {rootObj.tag}");

        bool hitPlayerOrShield =
            hitObj.CompareTag("Player") ||
            rootObj.CompareTag("Player") ||
            hitObj.CompareTag("Shield") ||
            rootObj.CompareTag("Shield") ||
            hitObj.CompareTag("b") ||
            rootObj.CompareTag("b");

        if (hitPlayerOrShield)
        {
            if (!fromPhysicsCollision)
                return;

            ProceduralForceFieldOverlay overlay = hitObj.GetComponentInParent<ProceduralForceFieldOverlay>();
            if (overlay == null)
                overlay = rootObj.GetComponentInChildren<ProceduralForceFieldOverlay>();

            if (overlay != null)
            {
                Collider hitCollider = hitObj.GetComponent<Collider>();
                Vector3 hitPoint = hitCollider != null ? hitCollider.ClosestPoint(transform.position) : transform.position;
                overlay.Trigger(hitPoint);
            }

            isHoming = false;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (!isFuseStarted)
                ApplyContactPush(rootObj);

            if (!isFuseStarted)
            {
                shakeOnExplode = true;
                isFuseStarted = true;
                StartCoroutine(ExplodeAfterContactFuse(hitObj));
            }
            return;
        }

        if (hitObj.CompareTag("Enemy") || rootObj.CompareTag("Enemy"))
        {
            shakeOnExplode = false;
            hasExploded = true;
            Explode(hitObj);
        }
    }

    private IEnumerator ExplodeAfterContactFuse(GameObject target)
    {
        yield return new WaitForSeconds(contactFuse);

        if (hasExploded)
            yield break;

        hasExploded = true;
        Explode(target != null ? target : gameObject);
    }

    private void ApplyContactPush(GameObject rootObj)
    {
        if (rootObj == null || rb == null)
            return;

        Rigidbody targetRb = rootObj.GetComponent<Rigidbody>();
        if (targetRb == null || targetRb.isKinematic)
            return;

        Vector3 pushDir = rootObj.transform.position - transform.position;
        if (pushDir.sqrMagnitude < 0.0001f)
            pushDir = rb.linearVelocity;

        pushDir.y = 0f;
        if (pushDir.sqrMagnitude < 0.0001f)
            return;

        targetRb.AddForce(pushDir.normalized * contactPushImpulse, ForceMode.Impulse);
    }

    void Explode(GameObject target)
    {
        Debug.Log("Bomb Exploding on impact with: " + target.name);

        if (shakeOnExplode && cameraFollow != null)
            cameraFollow.Shake();

        if (explosionFX != null)
        {
            GameObject fx = Instantiate(explosionFX, transform.position, Quaternion.identity);
            float effectScale = explosionRadius * GlobalScaleManager.GetScale(GlobalScaleCategory.Effect);
            fx.transform.localScale *= effectScale;
        }

        Collider[] hitColliders = Physics.OverlapSphere(
            transform.position,
            explosionRadius
        );

        HashSet<GameObject> damagedObjects = new HashSet<GameObject>();

        foreach (Collider col in hitColliders)
        {
            GameObject rootObj = col.transform.root.gameObject;

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
                    ControllerTank ctrl = rootObj.GetComponent<ControllerTank>();
                    if (ctrl != null && ctrl.isShield)
                    {
                        Debug.Log("Shielded, skipping damage: " + rootObj.name);
                    }
                    else
                    {
                        hp.TakeDamage(explosionDamage);
                        damagedObjects.Add(rootObj);
                        Debug.Log("Damaged: " + rootObj.name);
                    }
                }
            }
        }

        Destroy(gameObject);
    }
}
