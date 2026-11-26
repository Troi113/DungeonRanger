using UnityEngine;

/// <summary>Prototype protection values for the five ordinary armor tiers.</summary>
public static class PrototypeArmor
{
    public readonly struct ArmorData
    {
        public readonly string Id;
        public readonly string Name;
        public readonly int Tier;
        public readonly Color Color;

        public ArmorData(string id, string name, int tier, Color color)
        {
            Id = id;
            Name = name;
            Tier = tier;
            Color = color;
        }
    }

    private static readonly ArmorData[] Armors =
    {
        new("cloth_armor", "Cloth Armor", 1, new Color(0.76f, 0.72f, 0.61f)),
        new("leather_armor", "Leather Armor", 2, new Color(0.55f, 0.32f, 0.17f)),
        new("mail_armor", "Mail Armor", 3, new Color(0.54f, 0.56f, 0.6f)),
        new("scale_armor", "Scale Armor", 4, new Color(0.42f, 0.51f, 0.47f)),
        new("plate_armor", "Plate Armor", 5, new Color(0.7f, 0.72f, 0.77f))
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

    public static float Protection(PrototypeInventoryItem item)
    {
        if (item == null || item.EquipmentSlot != PrototypeEquipmentSlot.Armor || !TryGet(item.ItemId, out var armor)) return 0f;
        return Mathf.Clamp(armor.Tier * 0.1f + item.UpgradeLevel * 0.05f, 0f, 0.75f);
    }
}
