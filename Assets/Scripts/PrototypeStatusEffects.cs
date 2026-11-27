using System.Collections.Generic;
using UnityEngine;

public enum PrototypeStatusEffectType
{
    Poisoned,
    Toxic,
    Paralyzed,
    Gripped,
    Bleeding,
    Oozed,
    Slowed,
    Invisible,
    Burning,
    Barkskin,
    Crippled
}

/// <summary>
/// The ranger's buffs and debuffs, after Pixel Dungeon's Buff classes. Each acts once per
/// turn (1 second here):
///   Burning   (8 turns)  1-2 damage per turn (toned down from PD's 1-4); each turn a small
///                        chance (half the equip-curse chance) to burn up a random unequipped
///                        scroll; can go out early (more likely at low health); water puts it out.
///   Bleeding  (level)    each turn the level becomes random(level/2 .. level-1) and deals that
///                        much damage; ends at 0.
///   Poisoned  (left)     deals (left / 3) + 1 per turn, then left - 1; ends at 0.
///   Toxic, Oozed         1 damage per turn while they last.
///   Crippled  (10)       moving at half speed.
///   Slowed    (10)       moving and attacking at half speed.
///   Paralyzed (10)       cannot move or act.  Gripped: cannot move.
///   Invisible (15)       enemies cannot see you.  Barkskin: extra armor (talent).
/// Hunger's Hungry / Starving debuffs live in PrototypeHunger and show alongside these.
/// </summary>
[RequireComponent(typeof(PrototypePlayerStats))]
public sealed class PrototypeStatusEffects : MonoBehaviour
{
    // Pixel Dungeon durations, in turns (seconds).
    public const float BurningDuration = 8f;
    public const float CrippleDuration = 10f;
    public const float SlowDuration = 10f;
    public const float ParalysisDuration = 10f;
    public const float InvisibilityDuration = 15f;

    [SerializeField] private float turnSeconds = 1f;
    [SerializeField] private int burnDamageMin = 1;
    [SerializeField] private int burnDamageMax = 2;
    [Tooltip("Chance per burning turn to destroy one carried scroll (half of the 15% equip-curse chance).")]
    [SerializeField, Range(0f, 1f)] private float scrollBurnChance = 0.075f;

    private sealed class Effect
    {
        public float Expires;   // for timed (flavour) buffs
        public float Left;      // Burning / Poisoned turns left
        public int Level;       // Bleeding level
    }

    private readonly Dictionary<PrototypeStatusEffectType, Effect> effects = new();
    private readonly List<PrototypeStatusEffectType> scratch = new();
    private PrototypePlayerStats stats;
    private float nextTurn;

    public bool IsMovementBlocked => Has(PrototypeStatusEffectType.Paralyzed) || Has(PrototypeStatusEffectType.Gripped);
    public bool IsParalyzed => Has(PrototypeStatusEffectType.Paralyzed);

    /// <summary>Movement speed factor from Cripple and Slow (each halves).</summary>
    public float MoveSpeedMultiplier =>
        (Has(PrototypeStatusEffectType.Crippled) ? 0.5f : 1f) * (Has(PrototypeStatusEffectType.Slowed) ? 0.5f : 1f);

    /// <summary>Attack delay factor from Slow (everything takes twice as long).</summary>
    public float ActionDelayMultiplier => Has(PrototypeStatusEffectType.Slowed) ? 2f : 1f;

    private void Awake() => stats = GetComponent<PrototypePlayerStats>();

    // ---------------- Applying ----------------

    /// <summary>A timed buff; re-applying keeps the longer remaining time (Buff.prolong).</summary>
    public void Apply(PrototypeStatusEffectType type, float duration)
    {
        switch (type)
        {
            case PrototypeStatusEffectType.Burning: Ignite(); return;
            case PrototypeStatusEffectType.Poisoned: Poison(duration); return;
            case PrototypeStatusEffectType.Bleeding: Bleed(Mathf.Max(1, Mathf.RoundToInt(duration))); return;
        }
        LogGained(type);
        var effect = Get(type);
        effect.Expires = Mathf.Max(effect.Expires, Time.time + Mathf.Max(0.1f, duration));
        StartTurns();
    }

    /// <summary>Logs an effect the moment it starts (not every time it is refreshed).</summary>
    private void LogGained(PrototypeStatusEffectType type)
    {
        if (Has(type)) return;
        PrototypeGameLog.Add(type switch
        {
            PrototypeStatusEffectType.Burning => "You catch fire!",
            PrototypeStatusEffectType.Poisoned => "You are poisoned.",
            PrototypeStatusEffectType.Toxic => "You breathe in toxic gas.",
            PrototypeStatusEffectType.Paralyzed => "You are paralyzed!",
            PrototypeStatusEffectType.Gripped => "You are gripped and cannot move.",
            PrototypeStatusEffectType.Bleeding => "You start bleeding.",
            PrototypeStatusEffectType.Oozed => "Caustic ooze covers you.",
            PrototypeStatusEffectType.Slowed => "You are slowed.",
            PrototypeStatusEffectType.Invisible => "You turn invisible.",
            PrototypeStatusEffectType.Barkskin => "Your skin hardens like bark.",
            PrototypeStatusEffectType.Crippled => "You are crippled.",
            _ => $"You are affected by {type}."
        });
    }

    /// <summary>Burning.reignite: sets the burn back to its full 8 turns.</summary>
    public void Ignite()
    {
        LogGained(PrototypeStatusEffectType.Burning);
        Get(PrototypeStatusEffectType.Burning).Left = BurningDuration;
        StartTurns();
    }

    /// <summary>Poison.set: poison for this many turns (keeps the stronger poison).</summary>
    public void Poison(float turns)
    {
        LogGained(PrototypeStatusEffectType.Poisoned);
        var effect = Get(PrototypeStatusEffectType.Poisoned);
        effect.Left = Mathf.Max(effect.Left, turns);
        StartTurns();
    }

    /// <summary>Bleeding.set: bleeding at this level (usually the damage of the hit).</summary>
    public void Bleed(int level)
    {
        LogGained(PrototypeStatusEffectType.Bleeding);
        var effect = Get(PrototypeStatusEffectType.Bleeding);
        effect.Level = Mathf.Max(effect.Level, level);
        StartTurns();
    }

    /// <summary>Ends the given effects immediately (cures, or attacking ending invisibility).</summary>
    public void Cure(params PrototypeStatusEffectType[] types)
    {
        foreach (var type in types) effects.Remove(type);
    }

    public bool Has(PrototypeStatusEffectType type)
    {
        if (!effects.TryGetValue(type, out var effect)) return false;
        return type switch
        {
            PrototypeStatusEffectType.Burning or PrototypeStatusEffectType.Poisoned => effect.Left > 0f,
            PrototypeStatusEffectType.Bleeding => effect.Level > 0,
            _ => Time.time < effect.Expires
        };
    }

    /// <summary>Seconds (turns) left, for the buff indicator; Bleeding returns its level.</summary>
    public float Remaining(PrototypeStatusEffectType type)
    {
        if (!effects.TryGetValue(type, out var effect)) return 0f;
        return type switch
        {
            PrototypeStatusEffectType.Burning or PrototypeStatusEffectType.Poisoned => effect.Left,
            PrototypeStatusEffectType.Bleeding => effect.Level,
            _ => Mathf.Max(0f, effect.Expires - Time.time)
        };
    }

    /// <summary>Currently active effects (filled into the given list, for the HUD).</summary>
    public void GetActive(List<PrototypeStatusEffectType> into)
    {
        into.Clear();
        foreach (var pair in effects) if (Has(pair.Key)) into.Add(pair.Key);
    }

    private Effect Get(PrototypeStatusEffectType type)
    {
        if (!effects.TryGetValue(type, out var effect)) effects[type] = effect = new Effect();
        return effect;
    }

    private void StartTurns()
    {
        if (nextTurn <= Time.time) nextTurn = Time.time + turnSeconds;
    }

    // ---------------- Turns ----------------

    private void Update()
    {
        if (stats.IsDead || Time.time < nextTurn) return;
        nextTurn = Time.time + turnSeconds;

        scratch.Clear();
        scratch.AddRange(effects.Keys);
        foreach (var type in scratch)
        {
            if (!effects.TryGetValue(type, out var effect)) continue;
            switch (type)
            {
                case PrototypeStatusEffectType.Burning: ActBurning(effect); break;
                case PrototypeStatusEffectType.Bleeding: ActBleeding(effect); break;
                case PrototypeStatusEffectType.Poisoned: ActPoison(effect); break;
                case PrototypeStatusEffectType.Toxic:
                case PrototypeStatusEffectType.Oozed:
                    if (!Has(type)) break;
                    PrototypeGameLog.Add(type == PrototypeStatusEffectType.Toxic ? "Toxic gas burns your lungs." : "The ooze stings you.");
                    stats.TakeDamage(1);
                    break;
            }
            if (!Has(type)) effects.Remove(type);
            if (stats.IsDead) return;
        }
    }

    private void ActBurning(Effect effect)
    {
        if (effect.Left <= 0f) return;
        var damage = Random.Range(burnDamageMin, burnDamageMax + 1);
        PrototypeGameLog.Add($"You burn for {damage} damage.");
        stats.TakeDamage(damage);
        if (Random.value < scrollBurnChance) BurnRandomScroll();
        effect.Left -= 1f;
        // Pixel Dungeon: may go out early (likelier when hurt), and water douses it.
        var chanceToKeepBurning = (2f + (float)stats.CurrentHealth / stats.MaxHealth) / 3f;
        if (Random.value > chanceToKeepBurning || PrototypeSewerWater.IsWater(transform.position)) effect.Left = 0f;
        if (effect.Left <= 0f) PrototypeGameLog.Add("The flames on you go out.");
    }

    private void ActBleeding(Effect effect)
    {
        effect.Level = Random.Range(effect.Level / 2, effect.Level);
        if (effect.Level > 0)
        {
            PrototypeGameLog.Add("You are bleeding.");
            stats.TakeDamage(effect.Level);
        }
    }

    private void ActPoison(Effect effect)
    {
        if (effect.Left <= 0f) return;
        PrototypeGameLog.Add("Poison courses through you.");
        stats.TakeDamage((int)(effect.Left / 3f) + 1);
        effect.Left -= 1f;
    }

    /// <summary>Burning destroys one scroll from a random unequipped scroll stack (Pixel Dungeon).</summary>
    private void BurnRandomScroll()
    {
        var inventory = GetComponent<PrototypeInventory>();
        if (inventory == null) return;
        var found = 0;
        var slot = -1;
        for (var i = 4; i < inventory.Capacity; i++)
        {
            var candidate = inventory.GetSlot(i);
            if (candidate == null || !PrototypeScrolls.IsScroll(candidate.ItemId)) continue;
            if (Random.Range(0, ++found) == 0) slot = i;
        }
        if (slot < 0) return;
        var item = inventory.GetSlot(slot);
        var name = GetComponent<PrototypeScrolls>()?.DisplayName(new PrototypeInventoryItem(item.ItemId)) ?? "A scroll";
        inventory.ConsumeOneAt(slot);
        PrototypeGameUI.ShowNotice($"{name} burns up!");
        PrototypeGameLog.Add($"The flames destroy your {name}.");
    }
}
