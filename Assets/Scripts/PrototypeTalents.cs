using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Huntress tier-1 talents from Shattered Pixel Dungeon, adapted to this game (no tall grass or
/// thrown weapons). One talent point per level from level 2 to 5; each talent has 2 ranks.
///   Nature's Bounty         - kills can drop a berry: 30% / 50% chance, up to 2 / 4 per floor.
///   Survivalist's Intuition - unidentified gear identifies itself after 60 s carried / on pickup.
///   Followup Strike         - after a bow hit, the next melee hit on that enemy within 4 s
///                             deals +1 / +2 damage.
///   Nature's Aid            - eating or drinking grants Barkskin: +2 armor for 5 s / +3 for 8 s.
/// </summary>
[RequireComponent(typeof(PrototypePlayerStats))]
public sealed class PrototypeTalents : MonoBehaviour
{
    public enum Talent { NaturesBounty, SurvivalistsIntuition, FollowupStrike, NaturesAid }

    public const int MaxRank = 2;
    private const int FirstPointLevel = 2;
    private const int LastPointLevel = 5;

    public static readonly string[] Names = { "Nature's Bounty", "Survivalist's Intuition", "Followup Strike", "Nature's Aid" };

    [SerializeField] private float intuitionSeconds = 60f;
    [SerializeField] private float followupWindow = 4f;

    private readonly int[] ranks = new int[4];
    private readonly Dictionary<PrototypeInventoryItem, float> carried = new();
    private PrototypePlayerStats stats;
    private PrototypeInventory inventory;
    private PrototypeEnemy followupTarget;
    private float followupUntil;
    private float nextIntuitionCheck;
    private int berriesThisFloor;
    private int berryFloor = -1;

    public int Rank(Talent talent) => ranks[(int)talent];
    public IReadOnlyList<int> Ranks => ranks;

    public int PointsEarned => Mathf.Clamp(stats.Level - FirstPointLevel + 1, 0, LastPointLevel - FirstPointLevel + 1);
    public int PointsSpent { get { var total = 0; foreach (var r in ranks) total += r; return total; } }
    public int PointsAvailable => Mathf.Max(0, PointsEarned - PointsSpent);

    private void Awake()
    {
        stats = GetComponent<PrototypePlayerStats>();
        inventory = GetComponent<PrototypeInventory>();
    }

    public bool TrySpend(Talent talent)
    {
        if (PointsAvailable <= 0 || ranks[(int)talent] >= MaxRank) return false;
        ranks[(int)talent]++;
        return true;
    }

    public void RestoreRanks(IList<int> saved)
    {
        if (saved == null) return;
        for (var i = 0; i < ranks.Length && i < saved.Count; i++) ranks[i] = Mathf.Clamp(saved[i], 0, MaxRank);
    }

    public static string Describe(Talent talent, int rank) => talent switch
    {
        Talent.NaturesBounty => rank >= 2 ? "Kills drop a berry 50% of the time, up to 4 per floor." : "Kills drop a berry 30% of the time, up to 2 per floor. Berries heal 2 and ease hunger.",
        Talent.SurvivalistsIntuition => rank >= 2 ? "Unidentified gear is identified as soon as you pick it up." : "Unidentified gear identifies itself after you carry it for 60 seconds.",
        Talent.FollowupStrike => rank >= 2 ? "After a bow hit, your next melee hit on that enemy within 4 s deals +2 damage." : "After a bow hit, your next melee hit on that enemy within 4 s deals +1 damage.",
        Talent.NaturesAid => rank >= 2 ? "Eating or drinking grants Barkskin: +3 armor for 8 seconds." : "Eating or drinking grants Barkskin: +2 armor for 5 seconds.",
        _ => ""
    };

    // ---------------- Effects ----------------

    /// <summary>Nature's Bounty: called when an enemy dies; may drop a berry there.</summary>
    public void OnEnemyKilled(Vector3 position, Transform floor, int floorNumber)
    {
        var rank = Rank(Talent.NaturesBounty);
        if (rank == 0) return;
        if (berryFloor != floorNumber) { berryFloor = floorNumber; berriesThisFloor = 0; }
        if (berriesThisFloor >= (rank >= 2 ? 4 : 2) || Random.value >= (rank >= 2 ? 0.5f : 0.3f)) return;
        berriesThisFloor++;
        PrototypeItemPickup.CreateForItem(floor, new Vector3(position.x, 0f, position.z), new PrototypeInventoryItem(PrototypeItemCatalog.Berry));
    }

    /// <summary>Followup Strike: remember the enemy the bow just hit.</summary>
    public void OnBowHit(PrototypeEnemy enemy)
    {
        if (Rank(Talent.FollowupStrike) == 0) return;
        followupTarget = enemy;
        followupUntil = Time.time + followupWindow;
    }

    /// <summary>Followup Strike: bonus melee damage against the marked enemy (used once).</summary>
    public int TakeFollowupBonus(PrototypeEnemy enemy)
    {
        if (enemy == null || enemy != followupTarget || Time.time > followupUntil) return 0;
        followupTarget = null;
        return Rank(Talent.FollowupStrike);
    }

    /// <summary>Nature's Aid: eating or drinking grants Barkskin.</summary>
    public void OnConsumed()
    {
        var rank = Rank(Talent.NaturesAid);
        if (rank == 0) return;
        GetComponent<PrototypeStatusEffects>()?.Apply(PrototypeStatusEffectType.Barkskin, rank >= 2 ? 8f : 5f);
    }

    /// <summary>Extra armor absorption while Barkskin is active.</summary>
    public int BarkskinArmor()
    {
        var statuses = GetComponent<PrototypeStatusEffects>();
        if (statuses == null || !statuses.Has(PrototypeStatusEffectType.Barkskin)) return 0;
        return Rank(Talent.NaturesAid) >= 2 ? 3 : 2;
    }

    /// <summary>Survivalist's Intuition: identify carried gear over time (checked once a second).</summary>
    private void Update()
    {
        var rank = Rank(Talent.SurvivalistsIntuition);
        if (rank == 0 || inventory == null || PrototypeGameUI.IsGamePaused || Time.time < nextIntuitionCheck) return;
        nextIntuitionCheck = Time.time + 1f;
        for (var i = 0; i < inventory.Capacity; i++)
        {
            var item = inventory.GetSlot(i);
            if (item == null || item.IsIdentified || item.EquipmentSlot == PrototypeEquipmentSlot.None) continue;
            if (!carried.TryGetValue(item, out var since)) { carried[item] = since = Time.time; }
            if (rank >= 2 || Time.time - since >= intuitionSeconds)
            {
                item.IsIdentified = true;
                carried.Remove(item);
                PrototypeGameUI.ShowNotice("Your intuition tells you more about your gear.");
            }
        }
    }
}
