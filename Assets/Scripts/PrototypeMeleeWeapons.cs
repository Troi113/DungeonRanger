using UnityEngine;

/// <summary>Prototype real-time weapon values inspired by the Pixel Dungeon melee roster.</summary>
public static class PrototypeMeleeWeapons
{
    public readonly struct WeaponData
    {
        public readonly string Id;
        public readonly string Name;
        public readonly int Damage;
        /// <summary>Reach in dungeon tiles.</summary>
        public readonly float Range;
        public readonly float Cooldown;
        public readonly Color Color;
        /// <summary>Strength needed to use it well (see PrototypeStrength).</summary>
        public readonly int Strength;

        public WeaponData(string id, string name, int damage, float range, float cooldown, Color color, int strength = 0)
        {
            Strength = strength;
            Id = id;
            Name = name;
            Damage = damage;
            Range = range;
            Cooldown = cooldown;
            Color = color;
        }
    }

    private static readonly WeaponData[] Weapons =
    {
        // Only weapons with a model (see PrototypeItemVisuals). "knife" is shown as the Dagger.
        new("knife", "Dagger", 1, 1f, 0.3f, new Color(0.72f, 0.73f, 0.76f), 10),
        new("sword", "Sword", 2, 1f, 0.45f, new Color(0.8f, 0.82f, 0.85f), 12),
        new("axe", "Axe", 3, 1f, 0.65f, new Color(0.54f, 0.5f, 0.46f), 13),
        new("spear", "Spear", 2, 2f, 0.8f, new Color(0.63f, 0.47f, 0.29f), 12)
    };

    public static int Count => Weapons.Length;
    public static WeaponData Get(int index) => Weapons[index];

    public static bool TryGet(string id, out WeaponData weapon)
    {
        foreach (var candidate in Weapons)
        {
            if (candidate.Id != id) continue;
            weapon = candidate;
            return true;
        }
        weapon = default;
        return false;
    }
}
