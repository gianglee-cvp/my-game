using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform followPoint; // điểm camera theo dõi

    [Header("Shake Settings")]
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 0.5f;

    private Vector3 originalPos;
    private float currentShakeTime = 0f;

    void Start()
    {
        if (followPoint != null)
            originalPos = followPoint.position;
        else
            originalPos = transform.position;
    }

    void LateUpdate()
    {
        if (followPoint == null) return;

        // vị trí gốc camera theo followPoint
        Vector3 targetPos = followPoint.position;
        Quaternion targetRot = followPoint.rotation;

        // nếu đang rung → cộng offset
        if (currentShakeTime > 0)
        {
            targetPos += Random.insideUnitSphere * shakeMagnitude;
            currentShakeTime -= Time.deltaTime;
        }

        // gán cho camera
        transform.position = targetPos;
        transform.rotation = targetRot;
    }

    // Gọi khi muốn rung camera
    public void Shake(float duration, float magnitude)
    {
        currentShakeTime = duration;
        shakeMagnitude = magnitude;
    }

    // Gọi khi muốn rung theo mặc định
    public void Shake()
    {
        currentShakeTime = shakeDuration;
    }
}