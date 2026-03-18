using UnityEngine;

public class Spin : MonoBehaviour
{
    public enum ItemType
    {
        Coin,
        Cross,
        Shield,
        SpecialAmmo
    }

    public ItemType itemType;
    public float rotateSpeed = 100f;
    private Vector3 baseLocalScale;

    void Awake()
    {
        baseLocalScale = transform.localScale;
    }

    void OnEnable()
    {
        transform.localScale = baseLocalScale * GlobalScaleManager.GetScale(GlobalScaleCategory.Pickup);
    }

    void Update()
    {
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        ControllerTank player = other.GetComponentInParent<ControllerTank>();
        if (player == null && other.attachedRigidbody != null)
        {
            player = other.attachedRigidbody.GetComponent<ControllerTank>();
        }
        if (player == null) return;

        bool isPlayerHit =
            other.CompareTag("Player") ||
            other.transform.root.CompareTag("Player") ||
            player.CompareTag("Player");
        if (!isPlayerHit) return;
        Debug.Log("Player hit item " + itemType);
        switch (itemType)
        {
            case ItemType.Coin:
                player.AddCoin(10);
                break;

            case ItemType.Shield:
                player.ActivateShield(20f);
                break;

            case ItemType.Cross:
                HP hp = player.GetComponent<HP>();
                if (hp == null)
                    hp = player.GetComponentInParent<HP>();
                if (hp != null)
                    hp.Heal(50f);
                break;

            case ItemType.SpecialAmmo:
                player.AddSpecialAmmo(10);
                break;
        }

        Destroy(gameObject);
    }
}
