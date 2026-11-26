using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Potions, after Pixel Dungeon's Potion classes. Each run gives every potion a random color
/// ("crimson potion"); a potion is identified when drunk (Liquid Flame also when it shatters in
/// view) or with a Scroll of Identify. Drinking applies the effect to the ranger; a thrown potion
/// shatters where it lands: Liquid Flame bursts into fire, the others splash harmlessly.
///   Healing       - restores all health, cures poison, toxic gas, and bleeding.
///   Strength      - +1 Strength, permanently.
///   Experience    - gains enough experience for the next level.
///   Invisibility  - enemies cannot see the ranger for a while; attacking ends it.
///   Liquid Flame  - fire on the spot (drinking sets fire under the ranger!).
/// </summary>
[RequireComponent(typeof(PrototypePlayerStats))]
public sealed class PrototypePotions : MonoBehaviour
{
    public const string Healing = "health_potion";
    public const string Strength = "strength_potion";
    public const string Experience = "potion_experience";
    public const string Invisibility = "potion_invisibility";
    public const string LiquidFlame = "potion_liquid_flame";

    [SerializeField] private float invisibilitySeconds = 15f;

    private static readonly string[] Ids = { Healing, Strength, Experience, Invisibility, LiquidFlame };
    private static readonly string[] Names = { "Potion of Healing", "Potion of Strength", "Potion of Experience", "Potion of Invisibility", "Potion of Liquid Flame" };
    private static readonly string[] Colors = { "turquoise", "crimson", "azure", "jade", "golden", "magenta", "charcoal", "ivory", "amber", "bistre", "indigo", "silver" };
    private static readonly Color[] Tints =
    {
        new(0.25f, 0.85f, 0.8f), new(0.75f, 0.08f, 0.15f), new(0.2f, 0.45f, 0.95f), new(0.2f, 0.65f, 0.35f),
        new(1f, 0.8f, 0.2f), new(0.85f, 0.2f, 0.75f), new(0.18f, 0.18f, 0.2f), new(0.95f, 0.93f, 0.85f),
        new(0.95f, 0.6f, 0.15f), new(0.45f, 0.32f, 0.2f), new(0.3f, 0.2f, 0.6f), new(0.75f, 0.78f, 0.82f)
    };

    /// <summary>The ranger's potion knowledge for the current run (also used to color potion pickups).</summary>
    public static PrototypePotions Current { get; private set; }

    private readonly Dictionary<string, int> colorIndex = new();
    private readonly HashSet<string> known = new();
    private PrototypePlayerStats stats;

    public static bool IsPotion(string id) => Array.IndexOf(Ids, id) >= 0;
    public static bool IsHarmful(string id) => id == LiquidFlame;
    public IEnumerable<string> KnownPotions => known;

    private void Awake()
    {
        stats = GetComponent<PrototypePlayerStats>();
        Current = this;
        Initialize(PrototypeLevelGenerator.Seed);
    }

    private void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    /// <summary>Assigns this run's random colors (same seed, same colors).</summary>
    public void Initialize(int runSeed)
    {
        Current = this;
        known.Clear();
        colorIndex.Clear();
        var random = new System.Random(runSeed + 7331);
        var available = new List<int>();
        for (var i = 0; i < Colors.Length; i++) available.Add(i);
        foreach (var id in Ids)
        {
            var pick = random.Next(available.Count);
            colorIndex[id] = available[pick];
            available.RemoveAt(pick);
        }
    }

    public void RestoreKnown(IEnumerable<string> ids)
    {
        foreach (var id in ids) if (IsPotion(id)) known.Add(id);
    }

    public bool IsKnown(string id) => known.Contains(id);
    public void SetKnown(string id) { if (IsPotion(id)) known.Add(id); }

    public Color TintOf(string id) => colorIndex.TryGetValue(id, out var i) ? Tints[i] : Color.white;

    public string NameOf(string id)
    {
        var index = Array.IndexOf(Ids, id);
        if (index < 0) return id;
        return IsKnown(id) ? Names[index] : $"{Colors[colorIndex[id]]} potion";
    }

    public string Description(string id)
    {
        if (!IsKnown(id))
            return $"This flask contains a swirling {Colors[colorIndex[id]]} liquid. Who knows what it will do when drunk or thrown?";
        return id switch
        {
            Healing => "Instantly restores you to full health and cures poison and bleeding.",
            Strength => "Permanently increases your Strength by 1.",
            Experience => "Instantly raises your experience level.",
            Invisibility => "Makes you invisible for a while: enemies cannot see you. Attacking ends it.",
            LiquidFlame => "Bursts violently into flame when exposed to air. Best thrown at enemies.",
            _ => ""
        };
    }

    /// <summary>Applies a potion to the ranger and identifies it. Returns a log message.</summary>
    public string Drink(string id)
    {
        SetKnown(id);
        var statuses = GetComponent<PrototypeStatusEffects>();
        switch (id)
        {
            case Healing:
                stats.HealFully();
                statuses?.Cure(PrototypeStatusEffectType.Poisoned, PrototypeStatusEffectType.Toxic, PrototypeStatusEffectType.Bleeding);
                return "Your wounds heal completely.";
            case Strength:
                stats.AddStrength(1);
                return $"Newfound strength surges through your body. (STR {stats.Strength})";
            case Experience:
                stats.EarnExperience(stats.ExperienceToLevel - stats.Experience);
                return $"You feel more experienced! (Level {stats.Level})";
            case Invisibility:
                statuses?.Apply(PrototypeStatusEffectType.Invisible, invisibilitySeconds);
                return "You see your hands turn invisible!";
            case LiquidFlame:
                PrototypeFireArea.Ignite(transform.parent, transform.position);
                return "The potion bursts into flame around you!";
            default:
                return "";
        }
    }

    /// <summary>A potion breaking at a point (thrown). Returns a log message.</summary>
    public string Shatter(string id, Transform floor, Vector3 position)
    {
        if (id == LiquidFlame)
        {
            SetKnown(id);
            PrototypeFireArea.Ignite(floor, position);
            return "The flask shatters and bursts into flame!";
        }
        PrototypeGameFeel.Sparks(new Vector3(position.x, 0.6f, position.z), TintOf(id), 12);
        return $"The flask shatters and {Colors[colorIndex[id]]} liquid splashes harmlessly.";
    }
}
