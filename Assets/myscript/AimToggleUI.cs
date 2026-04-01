using UnityEngine;
using UnityEngine.UI;

public class AimToggleUI : MonoBehaviour
{
    [Header("Settings")]
    public Button toggleButton;
    public GameObject rightJoystickUI; // Optional: Kéo On-Screen Joystick xoay nòng (bên phải) vào đây để tự động ẩn/hiện

    void Start()
    {
        if (toggleButton == null)
            toggleButton = GetComponent<Button>();

        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(OnToggleClicked);
        }

        UpdateButtonUI();
    }

    void OnToggleClicked()
    {
        if (SaveSystem.Data != null)
        {
            // Đảo ngược trạng thái Mode
            SaveSystem.Data.useJoystickAim = !SaveSystem.Data.useJoystickAim;
            SaveSystem.Save(); // Lưu trạng thái
            UpdateButtonUI();
        }
    }

    void UpdateButtonUI()
    {
        if (SaveSystem.Data == null) return;

        bool useJoystick = SaveSystem.Data.useJoystickAim;

        // Cập nhật text của Button
        string modeText = useJoystick ? "Aim: Joystick" : "Aim: Touch";
        
        // Cố gắng tìm component Text của Legacy UI hoặc TextMeshPro, nên script này rất dễ dàng tích hợp
        Text legacyText = GetComponentInChildren<Text>();
        if (legacyText != null)
        {
            legacyText.text = modeText;
        }

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
        // Sử dụng reflection để gọi qua TMP nếu có tồn tại tránh lỗi biên dịch khi không import TMPro từ đầu
        var tmpText = GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.text = modeText;
        }
#endif

        // Từ tự ẩn hiện Joystick nếu bạn đã kéo thả GameObject tương ứng vào Inspector
        if (rightJoystickUI != null)
        {
            rightJoystickUI.SetActive(useJoystick);
        }
    }
}
