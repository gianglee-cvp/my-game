using UnityEngine;

public class Spin : MonoBehaviour
{
    public enum ItemType
    {
        Coin,
        Cross,
        Shield
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
        if (!other.CompareTag("Player")) return;

        ControllerTank player = other.GetComponent<ControllerTank>();
        if (player == null) return;

        switch (itemType)
        {
            case ItemType.Coin:
                player.AddCoin(10);
                break;

            case ItemType.Shield:
                player.ActivateShield(20f);
                break;

            case ItemType.Cross:
                HP hp = other.GetComponent<HP>();   // ✅ lấy trực tiếp
                if (hp != null)
                    hp.Heal(50f);
                break;
        }

        Destroy(gameObject);
    }
}
