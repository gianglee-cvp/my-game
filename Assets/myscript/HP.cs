using System;
using UnityEngine;

public class HP : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHP = 100f;
    [SerializeField] private float currentHP;

    public event Action<float, float> OnHealthChanged;

    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;

    void Start()
    {
        currentHP = maxHP;
        NotifyHealthChanged();
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f)
            return;

        currentHP = Mathf.Max(0f, currentHP - damage);
        Debug.Log(gameObject.name + " HP: " + currentHP);
        NotifyHealthChanged();

        if (currentHP <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (amount <= 0f)
            return;

        currentHP = Mathf.Min(maxHP, currentHP + amount);
        Debug.Log(gameObject.name + " HP sau khi hoi: " + currentHP);
        NotifyHealthChanged();
    }

    private void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    void Die()
    {
        Debug.Log(gameObject.name + " Destroyed!");
        Destroy(gameObject);
    }
}
