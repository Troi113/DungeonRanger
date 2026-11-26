using UnityEngine;

/// <summary>Prototype armor tiers inspired by Pixel Dungeon's cloth, leather, and mail armor.</summary>
public static class PrototypeArmors
{
    public readonly struct ArmorData
    {
        public readonly string Id;
        public readonly string Name;
        public readonly int Tier;
        public readonly int SpawnWeight;
        public readonly Color Color;

        public ArmorData(string id, string name, int tier, int spawnWeight, Color color)
        {
            Id = id;
            Name = name;
            Tier = tier;
            SpawnWeight = spawnWeight;
            Color = color;
        }
    }

    private static readonly ArmorData[] Armors =
    {
        new("cloth_armor", "Cloth Armor", 1, 3, new Color(0.78f, 0.72f, 0.6f)),
        new("leather_armor", "Leather Armor", 2, 2, new Color(0.5f, 0.32f, 0.18f)),
        new("mail_armor", "Mail Armor", 3, 1, new Color(0.62f, 0.66f, 0.72f))
    };

    public static int Count => Armors.Length;
    public static ArmorData Get(int index) => Armors[index];

    public static bool TryGet(string id, out ArmorData armor)
    {
        foreach (var candidate in Armors)
        {
            if (candidate.Id != id) continue;
            armor = candidate;
            return true;
        }
        armor = default;
        return false;
    }

    /// <summary>Highest damage one hit can be reduced by; each upgrade adds one.</summary>
    public static int MaxAbsorb(PrototypeInventoryItem item)
    {
        if (item == null || !TryGet(item.ItemId, out var armor)) return 0;
        return Mathf.Max(0, armor.Tier + item.UpgradeLevel);
    }

    /// <summary>Weighted, seeded pick so cloth is most common and mail is rarest.</summary>
    public static ArmorData PickRandom(System.Random random)
    {
        var total = 0;
        foreach (var armor in Armors) total += armor.SpawnWeight;
        var roll = random.Next(total);
        foreach (var armor in Armors)
        {
            if (roll < armor.SpawnWeight) return armor;
            roll -= armor.SpawnWeight;
        }
        return Armors[0];
    }
}
