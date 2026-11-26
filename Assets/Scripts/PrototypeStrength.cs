using UnityEngine;

/// <summary>
/// Strength rules, after Pixel Dungeon: each weapon and armor needs some Strength, and each
/// upgrade lowers that need by 1. A weapon you are too weak for attacks slower (+25% delay per
/// missing point); extra Strength adds 0..excess bonus damage per hit. Armor you are too weak
/// for slows your movement (divided by 1.3 per missing point).
/// </summary>
public static class PrototypeStrength
{
    /// <summary>Strength the item needs, or 0 if it has no requirement.</summary>
    public static int Required(PrototypeInventoryItem item)
    {
        if (item == null) return 0;
        if (PrototypeMeleeWeapons.TryGet(item.ItemId, out var weapon)) return Mathf.Max(1, weapon.Strength - item.UpgradeLevel);
        if (PrototypeArmors.TryGet(item.ItemId, out var armor)) return Mathf.Max(1, armor.Strength - item.UpgradeLevel);
        return 0;
    }

    /// <summary>Missing Strength (0 when strong enough).</summary>
    public static int Shortfall(PrototypeInventoryItem item, int strength) => Mathf.Max(0, Required(item) - strength);

    /// <summary>Extra Strength beyond the requirement (0 when not strong enough).</summary>
    public static int Excess(PrototypeInventoryItem item, int strength) =>
        Required(item) == 0 ? 0 : Mathf.Max(0, strength - Required(item));

    public static float WeaponDelayMultiplier(int shortfall) => 1f + 0.25f * shortfall;

    public static float ArmorSpeedMultiplier(int shortfall) => Mathf.Pow(1.3f, -shortfall);
}
