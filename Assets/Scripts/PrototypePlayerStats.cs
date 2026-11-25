using System;
using UnityEngine;

/// <summary>
/// Player health foundation for the live gameplay prototype.
/// </summary>
public sealed class PrototypePlayerStats : MonoBehaviour
{
    [SerializeField] private int maxHealth = 20;

    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    public event Action<int, int> HealthChanged;
    public event Action Died;

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);

        if (IsDead)
        {
            Died?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0)
        {
            return;
        }

        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }
}
