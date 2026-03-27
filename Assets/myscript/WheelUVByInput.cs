using UnityEngine;
using UnityEngine.InputSystem;

public class WheelUVByInput : MonoBehaviour
{
    [Header("Settings")]
    public float scrollSpeed = 1f;
    
    [Header("Input (Mobile Support)")]
    public InputActionReference moveAction;

    private Renderer rend;
    private Material wheelMaterial;

    void OnEnable()
    {
        // Kích hoạt action mỗi khi object được bật (đặc biệt quan trọng khi dùng TankSelector)
        if (moveAction != null) moveAction.action.Enable();
    }

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            wheelMaterial = rend.material;
        }
    }

    void Update()
    {
        // Tự động bật lại nếu bị script khác tắt nhầm
        if (moveAction != null && !moveAction.action.enabled) moveAction.action.Enable();

        // 1. Lấy dữ liệu từ Joystick (Trục Y của Vector2)
        float vertical = (moveAction != null) ? moveAction.action.ReadValue<Vector2>().y : Input.GetAxis("Vertical");

        // 2. Nếu có chuyển động, cập nhật UV Offset cho xích xe
        if (wheelMaterial != null && Mathf.Abs(vertical) > 0.01f)
        {
            float offset = wheelMaterial.mainTextureOffset.y
                         + Time.deltaTime * scrollSpeed * Mathf.Sign(-vertical);

            wheelMaterial.mainTextureOffset = new Vector2(0, offset);
        }
    }
}
