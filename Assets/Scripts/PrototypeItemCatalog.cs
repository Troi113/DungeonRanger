using UnityEngine;

/// <summary>Display name and world color for any prototype item id (used for dropped, thrown, and restored items).</summary>
public static class PrototypeItemCatalog
{
    public static void Describe(string itemId, out string name, out Color color)
    {
        if (PrototypeMeleeWeapons.TryGet(itemId, out var weapon))
        {
            name = weapon.Name;
            color = weapon.Color;
            return;
        }

        if (PrototypeArmors.TryGet(itemId, out var armor))
        {
            name = armor.Name;
            color = armor.Color;
            return;
        }

        if (PrototypeScrolls.IsScroll(itemId))
        {
            name = "Scroll";
            color = new Color(0.9f, 0.82f, 0.57f);
            return;
        }

        switch (itemId)
        {
            case "health_potion":
                name = "Potion of Healing";
                color = new Color(0.9f, 0.15f, 0.2f);
                return;
            case "strength_potion":
                name = "Potion of Strength";
                color = new Color(0.25f, 0.45f, 0.95f);
                return;
            case "food":
                name = "Food Ration";
                color = new Color(0.72f, 0.48f, 0.2f);
                return;
            case "key":
                name = "Key";
                color = new Color(1f, 0.82f, 0.1f);
                return;
            default:
                name = string.IsNullOrEmpty(itemId) ? "Item" : itemId.Replace('_', ' ');
                color = new Color(0.8f, 0.8f, 0.8f);
                return;
        }
    }

    /// <summary>The ranger's permanent bow cannot leave the inventory.</summary>
    public static bool CanLeaveInventory(string itemId) => itemId != "ranger_bow";
}
