using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;

// Script này gán vào một panel trong suốt (Image có color alpha = 0)
// căng ra bằng nửa màn hình bên TRÁI.
public class FloatingJoystickArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Tooltip("Kéo cục Joystick (hình tròn to) của bạn vào đây")]
    public RectTransform joystickRoot;
    
    [Tooltip("Vị trí mặc định của Joystick khi thả tay ra")]
    public Vector3 defaultLocalPosition = new Vector3(-699f, -245f, 0f);

    [SerializeField] OnScreenStick onScreenStick;
    private CanvasGroup joystickCanvasGroup;

    void Start()
    {
        if (joystickRoot != null)
        {
            // Đặt Joystick về đúng vị trí chuẩn dùng anchoredPosition3D cho đúng UI Inspector
            joystickRoot.anchoredPosition3D = defaultLocalPosition;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (joystickRoot == null) return;

        // 2. Di chuyển tâm Joystick tới đúng vị trí ngón tay vừa chạm,
        // sử dụng RectTransformUtility để đảm bảo chuẩn xác với mọi độ phân giải (Canvas Scaler)
        RectTransform parentRect = joystickRoot.parent as RectTransform;
        if (parentRect != null)
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                joystickRoot.localPosition = localPoint;
            }
        }
        else
        {
            joystickRoot.position = eventData.position; // Fallback nếu không có parent
        }

        // 3. Truyền sự kiện xuống cho OnScreenStick xử lý tiếp (dính chặt ngón tay)
        if (onScreenStick != null)
        {
            onScreenStick.OnPointerDown(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (onScreenStick != null)
        {
            onScreenStick.OnDrag(eventData);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (joystickRoot == null) return;

        // Ngừng kéo
        if (onScreenStick != null)
        {
            onScreenStick.OnPointerUp(eventData);
        }

        // Luôn trả Joystick về vị trí mặc định ban đầu.
        // Dùng anchoredPosition3D để chính xác với thông số Pos X, Pos Y, Pos Z trong Unity Inspector
        joystickRoot.anchoredPosition3D = defaultLocalPosition;
    }
}
