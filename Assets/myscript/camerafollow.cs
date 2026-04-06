using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform followPoint; // Điểm camera cần đứng (con của tank, đã setup vị trí sẵn)

    [Header("Orbit Settings (Vuốt nửa phải màn hình)")]
    public float orbitSensitivity = 0.2f;          // Độ nhạy kéo ngang
    public float orbitVerticalSensitivity = 0.1f;   // Độ nhạy kéo dọc
    public float minVerticalAngle = -10f;           // Giới hạn nghiêng (nhìn lên)
    public float maxVerticalAngle = 30f;            // Giới hạn nghiêng (nhìn xuống)
    public float orbitSmooth = 8f;                  // Độ mượt
    public float returnSpeed = 3f;                  // Tốc độ tự quay về góc mặc định khi thả tay

    [Header("Shake Settings")]
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 0.5f;

    // Góc offset thêm so với vị trí mặc định của followPoint
    private float yOrbitOffset = 0f;
    private float xOrbitOffset = 0f;

    // Touch tracking
    private int orbitTouchId = -1;
    private Vector2 lastOrbitTouchPos;
    private bool isDragging = false;

    private float currentShakeTime = 0f;

    void LateUpdate()
    {
        if (followPoint == null) return;

        HandleOrbitInput();

        // Lấy vị trí tâm xoay = parent của followPoint (tank)
        Transform orbitCenter = followPoint.parent != null ? followPoint.parent : followPoint;
        Vector3 pivotPos = orbitCenter.position;

        // Lấy offset mặc định của followPoint so với tank
        Vector3 defaultOffset = followPoint.position - pivotPos;
        Quaternion defaultLookDir = followPoint.rotation;

        // Áp dụng orbit offset (xoay thêm quanh tâm tank)
        Quaternion orbitRotation = Quaternion.Euler(xOrbitOffset, yOrbitOffset, 0);
        Vector3 rotatedOffset = orbitRotation * defaultOffset;

        Vector3 targetPos = pivotPos + rotatedOffset;
        Quaternion targetRot = Quaternion.LookRotation(pivotPos - targetPos, Vector3.up);

        // Nếu không có orbit offset thì dùng đúng rotation mặc định của followPoint
        if (Mathf.Abs(yOrbitOffset) < 0.1f && Mathf.Abs(xOrbitOffset) < 0.1f)
        {
            targetPos = followPoint.position;
            targetRot = followPoint.rotation;
        }

        // Smooth di chuyển
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * orbitSmooth);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * orbitSmooth);

        // Khi thả tay, tự quay về góc mặc định
        if (!isDragging)
        {
            yOrbitOffset = Mathf.Lerp(yOrbitOffset, 0f, Time.deltaTime * returnSpeed);
            xOrbitOffset = Mathf.Lerp(xOrbitOffset, 0f, Time.deltaTime * returnSpeed);
        }

        // Camera Shake
        if (currentShakeTime > 0)
        {
            transform.position += Random.insideUnitSphere * shakeMagnitude;
            currentShakeTime -= Time.deltaTime;
        }
    }

    void HandleOrbitInput()
    {
        isDragging = false;

        // ===== TOUCH INPUT (Mobile) =====
        if (Touchscreen.current != null)
        {
            bool currentOrbitTouchStillActive = false;

            foreach (var touch in Touchscreen.current.touches)
            {
                int tid = touch.touchId.ReadValue();
                var phase = touch.phase.ReadValue();
                Vector2 pos = touch.position.ReadValue();

                // Ngón tay mới chạm vào nửa phải màn hình
                if (phase == UnityEngine.InputSystem.TouchPhase.Began && pos.x > Screen.width / 2f)
                {
                    // Lọc chạm vào UI
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(tid))
                        continue;

                    orbitTouchId = tid;
                    lastOrbitTouchPos = pos;
                    currentOrbitTouchStillActive = true;
                    isDragging = true;
                    continue;
                }

                // Ngón tay đang track
                if (tid == orbitTouchId)
                {
                    if (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                    {
                        orbitTouchId = -1;
                        continue;
                    }

                    Vector2 delta = pos - lastOrbitTouchPos;
                    lastOrbitTouchPos = pos;

                    ApplyOrbitDelta(delta);
                    currentOrbitTouchStillActive = true;
                    isDragging = true;
                }
            }

            if (orbitTouchId >= 0 && !currentOrbitTouchStillActive)
            {
                orbitTouchId = -1;
            }
        }

        // ===== MOUSE INPUT (PC/Editor) - Chuột phải =====
        if (Mouse.current != null)
        {
            var mouse = Mouse.current;
            Vector2 pos = mouse.position.ReadValue();

            if (mouse.rightButton.wasPressedThisFrame && pos.x > Screen.width / 2f)
            {
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                {
                    orbitTouchId = -2;
                    lastOrbitTouchPos = pos;
                }
            }

            if (orbitTouchId == -2)
            {
                if (mouse.rightButton.wasReleasedThisFrame)
                {
                    orbitTouchId = -1;
                }
                else if (mouse.rightButton.isPressed)
                {
                    Vector2 delta = pos - lastOrbitTouchPos;
                    lastOrbitTouchPos = pos;
                    ApplyOrbitDelta(delta);
                    isDragging = true;
                }
            }
        }
    }

    void ApplyOrbitDelta(Vector2 delta)
    {
        if (Mathf.Abs(delta.x) > 0.5f)
        {
            yOrbitOffset += delta.x * orbitSensitivity;
        }

        if (Mathf.Abs(delta.y) > 0.5f)
        {
            xOrbitOffset -= delta.y * orbitVerticalSensitivity;
            xOrbitOffset = Mathf.Clamp(xOrbitOffset, minVerticalAngle, maxVerticalAngle);
        }
    }

    // Gọi khi muốn rung camera
    public void Shake(float duration, float magnitude)
    {
        currentShakeTime = duration;
        shakeMagnitude = magnitude;
    }

    public void Shake()
    {
        currentShakeTime = shakeDuration;
    }
}