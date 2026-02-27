using UnityEngine;

public class HP : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHP = 100f;
    [SerializeField] private float currentHP;

    void Start()
    {
        currentHP = maxHP;
    }

    // Hàm nhận damage từ bên ngoài
    public void TakeDamage(float damage)
    {
        currentHP -= damage;

        Debug.Log(gameObject.name + " HP: " + currentHP);

        if (currentHP <= 0f)
        {
            Die();
        }
    }
    public void Heal(float amount)
    {
        currentHP += amount;

        if (currentHP > maxHP)
            currentHP = maxHP;

        Debug.Log(gameObject.name + " HP sau khi hồi: " + currentHP);
    }

    void Die()
    {
        Debug.Log(gameObject.name + " Destroyed!");
        Destroy(gameObject);
    }
}
