using UnityEngine;

public class Spin : MonoBehaviour
{
    public float rotateSpeed = 100f;

    void Update()
    {
        transform.localRotation *= Quaternion.Euler(0f, rotateSpeed * Time.deltaTime, 0f);
    }
}