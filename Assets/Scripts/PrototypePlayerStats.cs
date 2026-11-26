using System;
using UnityEngine;

/// <summary>
/// Player health foundation for the live gameplay prototype.
/// </summary>
public sealed class PrototypePlayerStats : MonoBehaviour
{
    [SerializeField] private int maxHealth = 20;
    [Tooltip("Pixel Dungeon heroes start with 10 Strength.")]
    [SerializeField] private int startingStrength = 10;

    public int MaxHealth => maxHealth;
    /// <summary>Strength: equipment needs enough of it (see PrototypeStrength); Potions of Strength raise it.</summary>
    public int Strength { get; private set; }
    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    public event Action<int, int> HealthChanged;
    public event Action Died;

    private PrototypeInventory inventory;

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        CurrentHealth = maxHealth;
        Strength = startingStrength;
    }

    /// <summary>Enemy attack damage; equipped armor absorbs a random 0..max amount per hit. Returns damage dealt.</summary>
    public int TakeAttackDamage(int amount)
    {
        if (inventory == null) inventory = GetComponent<PrototypeInventory>();
        var armor = inventory != null ? inventory.GetSlot(1) : null;
        var maxAbsorb = PrototypeArmors.MaxAbsorb(armor);
        var before = CurrentHealth;
        TakeDamage(amount - UnityEngine.Random.Range(0, maxAbsorb + 1));
        return before - CurrentHealth;
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

    public void AddStrength(int amount) => Strength += amount;

    public void RestoreStrength(int value) => Strength = Mathf.Max(1, value);

    /// <summary>Sets health directly when restoring a saved game.</summary>
    public void RestoreHealth(int value)
    {
        CurrentHealth = Mathf.Clamp(value, 1, MaxHealth);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
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
