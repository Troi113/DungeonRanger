using System.Collections.Generic;
using UnityEngine;

public enum PrototypeStatusEffectType
{
    Poisoned,
    Toxic,
    Paralyzed,
    Gripped
}

/// <summary>
/// Small status-effect foundation for trap effects and future buffs/debuffs.
/// </summary>
[RequireComponent(typeof(PrototypePlayerStats))]
public sealed class PrototypeStatusEffects : MonoBehaviour
{
    [SerializeField] private float damageTickInterval = 1f;

    private readonly Dictionary<PrototypeStatusEffectType, float> expirationTimes = new();
    private PrototypePlayerStats stats;
    private float nextDamageTick;

    public bool IsMovementBlocked =>
        Has(PrototypeStatusEffectType.Paralyzed) || Has(PrototypeStatusEffectType.Gripped);

    private void Awake()
    {
        stats = GetComponent<PrototypePlayerStats>();
    }

    private void Update()
    {
        var expired = ListPool<PrototypeStatusEffectType>.Get();
        foreach (var pair in expirationTimes)
        {
            if (Time.time >= pair.Value)
            {
                expired.Add(pair.Key);
            }
        }

        foreach (var effect in expired)
        {
            expirationTimes.Remove(effect);
        }

        ListPool<PrototypeStatusEffectType>.Release(expired);

        if ((Has(PrototypeStatusEffectType.Poisoned) || Has(PrototypeStatusEffectType.Toxic)) &&
            Time.time >= nextDamageTick)
        {
            nextDamageTick = Time.time + damageTickInterval;
            stats.TakeDamage(1);
        }
    }

    public void Apply(PrototypeStatusEffectType effect, float duration)
    {
        expirationTimes[effect] = Mathf.Max(
            expirationTimes.TryGetValue(effect, out var currentExpiration) ? currentExpiration : 0f,
            Time.time + Mathf.Max(0.1f, duration));

        if (effect == PrototypeStatusEffectType.Poisoned || effect == PrototypeStatusEffectType.Toxic)
        {
            if (nextDamageTick <= Time.time)
            {
                nextDamageTick = Time.time + damageTickInterval;
            }
        }
    }

    public bool Has(PrototypeStatusEffectType effect)
    {
        return expirationTimes.TryGetValue(effect, out var expiration) && Time.time < expiration;
    }

    private static class ListPool<T>
    {
        private static readonly Stack<List<T>> Pool = new();

        public static List<T> Get()
        {
            return Pool.Count > 0 ? Pool.Pop() : new List<T>();
        }

        public static void Release(List<T> list)
        {
            list.Clear();
            Pool.Push(list);
        }
    }
}
