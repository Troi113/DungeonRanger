using System;
using UnityEngine;

/// <summary>
/// Player health foundation for the live gameplay prototype.
/// </summary>
public sealed class PrototypePlayerStats : MonoBehaviour
{
    [SerializeField] private int maxHealth = 20;
    private PrototypeInventory inventory;
    private System.Random protectionRandom;

    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    public event Action<int, int> HealthChanged;
    public event Action Died;

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        CurrentHealth = maxHealth;
        inventory = GetComponent<PrototypeInventory>();
        InitializeProtectionRandom(PrototypeLevelGenerator.Seed + 271);
    }

    public void InitializeProtectionRandom(int seed) => protectionRandom = new System.Random(seed);

    public void TakePhysicalDamage(int amount)
    {
        if (IsDead || amount <= 0) return;
        if (inventory == null) inventory = GetComponent<PrototypeInventory>();
        var protection = PrototypeArmor.Protection(inventory != null ? inventory.GetSlot(1) : null);
        var expectedBlocked = amount * protection;
        var blocked = Mathf.FloorToInt(expectedBlocked);
        if (protectionRandom.NextDouble() < expectedBlocked - blocked) blocked++;
        TakeDamage(Mathf.Max(0, amount - blocked));
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
