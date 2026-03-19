using System;
using UnityEngine;

public class HP : MonoBehaviour
{
    [SerializeField] private bool enableDebugLogs = false;
    [Header("Health Settings")]
    public float maxHP = 100f;
    [SerializeField] private float currentHP;

    public event Action<float, float> OnHealthChanged;
    public event Action OnDied;

    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;

    void Start()
    {
        currentHP = maxHP;
        NotifyHealthChanged();
    }

    void OnEnable()
    {
        currentHP = maxHP;
        NotifyHealthChanged();
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f)
            return;

        currentHP = Mathf.Max(0f, currentHP - damage);
        LogDebug(gameObject.name + " HP: " + currentHP);
        NotifyHealthChanged();

        if (currentHP <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (amount <= 0f)
            return;

        currentHP = Mathf.Min(maxHP, currentHP + amount);
        LogDebug(gameObject.name + " HP sau khi hoi: " + currentHP);
        NotifyHealthChanged();
    }

    private void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    void Die()
    {
        LogDebug(gameObject.name + " Destroyed!");
        OnDied?.Invoke();
        if (gameObject.CompareTag("Player"))
        {
            Time.timeScale = 0f; // Pause the game
            return ; 
        }
        PooledEnemy pooledEnemy = GetComponent<PooledEnemy>();
        if (pooledEnemy != null)
        {
            EnemyPool.Despawn(gameObject);
            return;
        }

        Destroy(gameObject);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log(message);
        }
    }
}
