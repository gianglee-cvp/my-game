using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform followPoint;   // điểm làm chuẩn cho camera

    void LateUpdate()
    {
        if (followPoint == null) return;
        transform.position = followPoint.position;
        transform.rotation = followPoint.rotation;
    }
}
