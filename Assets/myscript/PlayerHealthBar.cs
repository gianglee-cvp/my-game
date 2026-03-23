using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HP playerHP;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Image fillImage;

    [Header("Style")]
    [SerializeField] private Gradient fillGradient;

    private void Awake()
    {
        if (hpSlider == null)
            hpSlider = GetComponent<Slider>();
    }

    private void OnEnable()
    {
        TankSelector.OnPlayerTankSelected += OnPlayerTankSelected;
        
        // If player is already active, bind immediately
        if (TankSelector.ActivePlayer != null)
        {
            OnPlayerTankSelected(TankSelector.ActivePlayer);
        }
    }

    private void OnDisable()
    {
        TankSelector.OnPlayerTankSelected -= OnPlayerTankSelected;
        Unbind();
    }

    private void OnPlayerTankSelected(GameObject playerObj)
    {
        Unbind(); // Clean up old reference

        if (playerObj != null)
        {
            playerHP = playerObj.GetComponent<HP>();
            if (playerHP == null) playerHP = playerObj.GetComponentInChildren<HP>();
            if (playerHP == null) playerHP = playerObj.GetComponentInParent<HP>();

            Bind();
        }
    }

    private void Bind()
    {
        if (playerHP == null || hpSlider == null)
            return;

        playerHP.OnHealthChanged += HandleHealthChanged;
        HandleHealthChanged(playerHP.CurrentHP, playerHP.MaxHP);
    }

    private void Unbind()
    {
        if (playerHP != null)
            playerHP.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(float current, float max)
    {
        hpSlider.maxValue = max;
        hpSlider.value = current;

        if (fillImage != null && max > 0f)
            fillImage.color = fillGradient.Evaluate(current / max);
    }
}
