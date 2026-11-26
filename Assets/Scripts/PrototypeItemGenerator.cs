using System.Collections.Generic;

/// <summary>
/// Decides which items appear on a floor, after Pixel Dungeon's Level / RegularLevel.createItems /
/// Generator / Dungeon quotas:
/// - Every regular floor (1-4) gets one food ration.
/// - Quota items: 2 Potions of Strength and 3 Scrolls of Upgrade spread over floors 1-4
///   (PD guarantees them by depth through a catch-up quota; here the floors are picked once from
///   the run seed, so a floor always gets the same items, including after save/load).
/// - Random items: 3, plus one more for each 40% roll. Each picks a category by weight
///   (potion 50, scroll 40, weapon 15, armor 10) and that category's weight halves for the rest
///   of the floor, keeping drops varied. Strength and Upgrade never drop randomly.
/// The boss floor gets random items only.
/// </summary>
public static class PrototypeItemGenerator
{
    public const string HealthPotion = "health_potion";
    public const string StrengthPotion = "strength_potion";
    public const string Food = "food";
    public const string UpgradeScroll = "scroll_upgrade";

    private const int RegularFloors = 4;
    private const int StrengthPotionsPerRun = 2;
    private const int UpgradeScrollsPerRun = 3;

    private enum Category { Potion, Scroll, Weapon, Armor }
    private static readonly float[] CategoryWeights = { 50f, 40f, 15f, 10f };

    private static readonly string[] RandomPotions = { PrototypePotions.Healing, PrototypePotions.Experience, PrototypePotions.LiquidFlame, PrototypePotions.Invisibility };
    private static readonly float[] RandomPotionWeights = { 45f, 4f, 15f, 10f };

    // Random scrolls (PD-style weights); Upgrade comes only from the quota.
    private static readonly string[] RandomScrolls = { "scroll_identify", "scroll_remove_curse", "scroll_teleportation", "scroll_magic_mapping", "scroll_challenge" };
    private static readonly float[] RandomScrollWeights = { 30f, 15f, 10f, 10f, 8f };

    /// <summary>All items for one floor, guaranteed items first.</summary>
    public static List<PrototypeInventoryItem> PlanFloor(int runSeed, int floor, System.Random random)
    {
        var items = new List<PrototypeInventoryItem>();
        if (floor <= RegularFloors)
        {
            items.Add(new PrototypeInventoryItem(Food));
            if (QuotaFloors(runSeed, 101, StrengthPotionsPerRun).Contains(floor)) items.Add(new PrototypeInventoryItem(StrengthPotion));
            if (QuotaFloors(runSeed, 202, UpgradeScrollsPerRun).Contains(floor)) items.Add(new PrototypeInventoryItem(UpgradeScroll));
        }

        var count = 3;
        while (random.NextDouble() < 0.4) count++;
        var weights = (float[])CategoryWeights.Clone();
        for (var i = 0; i < count; i++)
        {
            var category = (Category)Pick(weights, random);
            weights[(int)category] *= 0.5f;
            items.Add(RandomItem(category, random));
        }
        return items;
    }

    /// <summary>A single random item (e.g. a chest reward), using the category weights.</summary>
    public static PrototypeInventoryItem RandomItem(System.Random random) =>
        RandomItem((Category)Pick(CategoryWeights, random), random);

    private static PrototypeInventoryItem RandomItem(Category category, System.Random random)
    {
        switch (category)
        {
            case Category.Potion:
                // PD weights among our potions; Strength comes only from the quota.
                return new PrototypeInventoryItem(RandomPotions[Pick(RandomPotionWeights, random)]);
            case Category.Scroll:
                return new PrototypeInventoryItem(RandomScrolls[Pick(RandomScrollWeights, random)]);
            case Category.Weapon:
                return WithEquipmentTraits(new PrototypeInventoryItem(PrototypeMeleeWeapons.Get(random.Next(PrototypeMeleeWeapons.Count)).Id,
                    PrototypeEquipmentSlot.Weapon), random);
            default:
                return WithEquipmentTraits(new PrototypeInventoryItem(PrototypeArmors.PickRandom(random).Id, PrototypeEquipmentSlot.Armor), random);
        }
    }

    /// <summary>
    /// PD-style equipment rolls: unidentified; 20% chance of an upgrade (+1, sometimes +2),
    /// 20% chance of a curse.
    /// </summary>
    private static PrototypeInventoryItem WithEquipmentTraits(PrototypeInventoryItem item, System.Random random)
    {
        item.IsIdentified = false;
        var roll = random.NextDouble();
        if (roll < 0.2) item.UpgradeLevel = random.Next(3) == 0 ? 2 : 1;
        else if (roll < 0.4) item.IsCursed = true;
        return item;
    }

    /// <summary>The floors (1-4) that receive one quota item each, chosen once per run.</summary>
    private static HashSet<int> QuotaFloors(int runSeed, int salt, int count)
    {
        var random = new System.Random(runSeed * 31 + salt);
        var floors = new List<int>();
        for (var f = 1; f <= RegularFloors; f++) floors.Add(f);
        var chosen = new HashSet<int>();
        while (chosen.Count < count && floors.Count > 0)
        {
            var index = random.Next(floors.Count);
            chosen.Add(floors[index]);
            floors.RemoveAt(index);
        }
        return chosen;
    }

    private static int Pick(float[] weights, System.Random random)
    {
        var total = 0f;
        foreach (var weight in weights) total += weight;
        var roll = (float)random.NextDouble() * total;
        for (var i = 0; i < weights.Length; i++)
        {
            if (roll < weights[i]) return i;
            roll -= weights[i];
        }
        return weights.Length - 1;
    }
}
