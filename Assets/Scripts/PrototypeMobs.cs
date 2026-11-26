using UnityEngine;

/// <summary>
/// Prototype mob stats. Values are scaled-down, real-time adaptations of Pixel Dungeon's
/// marsupial rat, albino rat, and Goo, sized for the ranger's 20 health and 1-3 damage weapons.
/// </summary>
public static class PrototypeMobs
{
    public readonly struct MobData
    {
        public readonly string Id;
        public readonly string Name;
        public readonly int Health;
        public readonly int MinDamage;
        public readonly int MaxDamage;
        /// <summary>Each hit on this mob is reduced by a random 0..Defense, never below 1.</summary>
        public readonly int Defense;
        public readonly Color Color;
        public readonly Vector3 Scale;
        public readonly bool HasOnHitEffect;
        public readonly PrototypeStatusEffectType OnHitEffect;
        public readonly float OnHitChance;
        public readonly float OnHitDuration;
        /// <summary>Rat Pack model name, or null to use a plain shape.</summary>
        public readonly string ModelName;

        public MobData(string id, string name, int health, int minDamage, int maxDamage, int defense, Color color, Vector3 scale,
            bool hasOnHitEffect = false, PrototypeStatusEffectType onHitEffect = default, float onHitChance = 0f, float onHitDuration = 0f,
            string modelName = null)
        {
            Id = id;
            Name = name;
            Health = health;
            MinDamage = minDamage;
            MaxDamage = maxDamage;
            Defense = defense;
            Color = color;
            Scale = scale;
            HasOnHitEffect = hasOnHitEffect;
            OnHitEffect = onHitEffect;
            OnHitChance = onHitChance;
            OnHitDuration = onHitDuration;
            ModelName = modelName;
        }
    }

    public static readonly MobData Rat = new("rat", "Rat", 4, 1, 2, 1,
        new Color(0.62f, 0.42f, 0.28f), new Vector3(0.9f, 1.6f, 1.4f), modelName: "Rat3");

    // Rarer, tougher rat whose bites can cause bleeding.
    public static readonly MobData AlbinoRat = new("albino_rat", "Albino Rat", 8, 1, 2, 1,
        new Color(0.95f, 0.9f, 0.9f), new Vector3(1f, 1.7f, 1.5f),
        true, PrototypeStatusEffectType.Bleeding, 0.5f, 3f, "Rat1");

    // Goo keeps its existing health, size, and charge; these add a damage range, defense, and ooze.
    public static readonly MobData Goo = new("goo", "Goo", 18, 1, 3, 1,
        new Color(0.2f, 0.75f, 0.35f), new Vector3(9f, 6f, 9f),
        true, PrototypeStatusEffectType.Oozed, 0.33f, 5f);

    /// <summary>Bell-curved roll like Pixel Dungeon's NormalIntRange: the average of two uniform rolls.</summary>
    public static int RollDamage(int min, int max)
    {
        if (max <= min) return min;
        return (Random.Range(min, max + 1) + Random.Range(min, max + 1)) / 2;
    }
}
