using UnityEngine;

public enum PrototypeHungerState
{
    Satiated,
    Hungry,
    Starving
}

/// <summary>
/// Hunger as a debuff, after Pixel Dungeon's Hunger buff (1 turn = 1 second of game time):
/// the hunger level rises by 2 per second (twice PD's pace); at 260 the ranger is Hungry, at 360 Starving.
/// While starving, every 10 seconds there is a 30% chance of 1 damage (never the last point
/// while paralyzed). Food lowers the level (a ration by 260). Paused time does not count.
/// </summary>
[RequireComponent(typeof(PrototypePlayerStats))]
public sealed class PrototypeHunger : MonoBehaviour
{
    public const float HungryLevel = 260f;
    public const float StarvingLevel = 360f;
    private const float Step = 10f;

    [SerializeField] private float levelPerSecond = 2f;

    private PrototypePlayerStats stats;
    private PrototypeStatusEffects statuses;
    private float level;
    private float stepTimer;

    public PrototypeHungerState State =>
        level >= StarvingLevel ? PrototypeHungerState.Starving : level >= HungryLevel ? PrototypeHungerState.Hungry : PrototypeHungerState.Satiated;

    public bool IsStarving => level >= StarvingLevel;

    /// <summary>Current hunger level (saved with the run).</summary>
    public float MovingTime => level;

    private void Awake()
    {
        stats = GetComponent<PrototypePlayerStats>();
        statuses = GetComponent<PrototypeStatusEffects>();
    }

    private void Update()
    {
        if (stats.IsDead) return;
        stepTimer += Time.deltaTime * levelPerSecond;
        if (stepTimer < Step) return;
        stepTimer -= Step;

        if (IsStarving)
        {
            var paralyzed = statuses != null && statuses.IsParalyzed;
            if (Random.value < 0.3f && (stats.CurrentHealth > 1 || !paralyzed))
            {
                PrototypeGameUI.ShowNotice("You are starving!");
                PrototypeGameLog.Add("Starvation gnaws at you.");
                stats.TakeDamage(1);
            }
            return;
        }

        var newLevel = level + Step;
        if (newLevel >= StarvingLevel) PrototypeGameUI.ShowNotice("You are starving!");
        else if (newLevel >= HungryLevel && level < HungryLevel) PrototypeGameUI.ShowNotice("You are hungry.");
        level = Mathf.Min(newLevel, StarvingLevel);
    }

    /// <summary>Hunger.satisfy: lowers the hunger level (food).</summary>
    public void Eat(float energy)
    {
        level = Mathf.Clamp(level - Mathf.Max(0f, energy), 0f, StarvingLevel);
    }

    public void RestoreMovingTime(float value) => level = Mathf.Clamp(value, 0f, StarvingLevel);
}
