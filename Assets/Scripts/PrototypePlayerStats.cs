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
    /// <summary>Experience level (Pixel Dungeon: each level needs 5 + 5 x level experience and gives +5 max health).</summary>
    public int Level { get; private set; } = 1;
    public int Experience { get; private set; }
    public int ExperienceToLevel => 5 + Level * 5;
    public bool IsDead => CurrentHealth <= 0;

    public event Action<int, int> HealthChanged;
    public event Action Died;

    private PrototypeInventory inventory;
    private PrototypeTalents talents => GetComponent<PrototypeTalents>();

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
        var maxAbsorb = PrototypeArmors.MaxAbsorb(armor) + (talents != null ? talents.BarkskinArmor() : 0);
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
        PrototypeCombatLog.Add($"You take {amount} damage ({CurrentHealth}/{MaxHealth}).");
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);

        if (IsDead)
        {
            PrototypeCombatLog.Add("You die...");
            Died?.Invoke();
        }
    }

    public void AddStrength(int amount) => Strength += amount;

    /// <summary>Adds experience; each level gained raises max health by 5 and heals 5.</summary>
    public int EarnExperience(int amount)
    {
        if (IsDead || amount <= 0) return 0;
        Experience += amount;
        var gained = 0;
        while (Experience >= ExperienceToLevel)
        {
            Experience -= ExperienceToLevel;
            Level++;
            gained++;
            maxHealth += 5;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + 5);
        }
        if (gained > 0) HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        return gained;
    }

    /// <summary>Restores level, experience, and max health from a saved game.</summary>
    public void RestoreLevel(int level, int experience, int savedMaxHealth)
    {
        Level = Mathf.Max(1, level);
        Experience = Mathf.Max(0, experience);
        if (savedMaxHealth > 0) maxHealth = savedMaxHealth;
    }

    /// <summary>Full heal (Potion of Healing).</summary>
    public void HealFully() => Heal(MaxHealth);

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
