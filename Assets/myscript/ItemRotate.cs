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

    void Update()
    {
        transform.localRotation *= Quaternion.Euler(0f, rotateSpeed * Time.deltaTime, 0f);
    }
}