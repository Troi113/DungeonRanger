using UnityEngine;

/// <summary>Prototype real-time weapon values inspired by the Pixel Dungeon melee roster.</summary>
public static class PrototypeMeleeWeapons
{
    public readonly struct WeaponData
    {
        public readonly string Id;
        public readonly string Name;
        public readonly int Damage;
        public readonly float Range;
        public readonly float Cooldown;
        public readonly Color Color;

        public WeaponData(string id, string name, int damage, float range, float cooldown, Color color)
        {
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
        new("knuckleduster", "Knuckleduster", 1, 1.35f, 0.22f, new Color(0.58f, 0.59f, 0.62f)),
        new("knife", "Knife", 1, 1.6f, 0.3f, new Color(0.72f, 0.73f, 0.76f)),
        new("sword", "Sword", 2, 1.85f, 0.45f, new Color(0.8f, 0.82f, 0.85f)),
        new("sickle", "Sickle", 2, 1.65f, 0.4f, new Color(0.63f, 0.68f, 0.64f)),
        new("axe", "Axe", 3, 1.7f, 0.65f, new Color(0.54f, 0.5f, 0.46f)),
        new("spear", "Spear", 2, 2.5f, 0.6f, new Color(0.63f, 0.47f, 0.29f))
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
