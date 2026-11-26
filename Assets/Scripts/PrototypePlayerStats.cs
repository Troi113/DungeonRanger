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

    private PrototypeInventory inventory;

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        CurrentHealth = maxHealth;
    }

    /// <summary>Enemy attack damage; equipped armor absorbs a random 0..max amount per hit.</summary>
    public void TakeAttackDamage(int amount)
    {
        if (inventory == null) inventory = GetComponent<PrototypeInventory>();
        var armor = inventory != null ? inventory.GetSlot(1) : null;
        var maxAbsorb = PrototypeArmors.MaxAbsorb(armor);
        TakeDamage(amount - UnityEngine.Random.Range(0, maxAbsorb + 1));
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
