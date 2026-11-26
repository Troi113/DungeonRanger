using System.Collections.Generic;
using UnityEngine;

public enum PrototypeTrapType
{
    ToxicGas,
    Fire,
    ParalyticGas,
    PoisonDart,
    Alarm,
    Lightning,
    Gripping,
    Summoning
}

/// <summary>
/// A generated trap that may be hidden and activates once when the ranger
/// steps on it.
/// </summary>
public sealed class PrototypeTrap : MonoBehaviour
{
    // Traps on the active floor by grid cell; floors that are switched off unregister their traps.
    private static readonly Dictionary<Vector2Int, PrototypeTrap> ActiveByCell = new();

    [SerializeField, Range(0f, 1f)] private float spentShade = 0.3f;
    [SerializeField, Range(0f, 2f)] private float armedGlow = 0.6f;

    private Vector2Int cell;
    private PrototypeTrapType trapType;
    private bool isHidden;
    private Renderer[] visualRenderers;
    private Color trapColor;

    public bool IsActivated { get; private set; }
    public bool IsHidden => isHidden && !IsActivated;
    /// <summary>True once the trap's hiding was removed (revealed by mapping or activated).</summary>
    public bool WasRevealed => !isHidden;
    public PrototypeTrapType TrapType => trapType;

    public void Initialize(PrototypeTrapType type, bool hidden, Color color, Transform visualRoot)
    {
        trapType = type;
        isHidden = hidden;
        trapColor = color;
        visualRenderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        SetVisualState(!hidden, false);
    }

    public static bool TryGetAt(Vector2Int gridCell, out PrototypeTrap trap) => ActiveByCell.TryGetValue(gridCell, out trap);

    /// <summary>
    /// Pixel Dungeon-style search: each hidden, unsprung trap in the square of `distance` cells
    /// around `center` (Chebyshev distance) is revealed with probability `chance`, but only on
    /// cells `isVisible` accepts. Returns how many were revealed.
    /// </summary>
    public static int SearchAround(Vector2Int center, int distance, float chance, System.Func<Vector2Int, bool> isVisible)
    {
        var found = 0;
        foreach (var pair in ActiveByCell)
        {
            var trap = pair.Value;
            var offset = pair.Key - center;
            if (trap == null || !trap.IsHidden || Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.y)) > distance) continue;
            if (isVisible != null && !isVisible(pair.Key)) continue;
            if (chance < 1f && Random.value >= chance) continue;
            trap.Reveal();
            found++;
        }
        return found;
    }

    private void OnEnable()
    {
        cell = PrototypeLevelGenerator.WorldToGrid(transform.position);
        ActiveByCell[cell] = this;
    }

    private void OnDisable()
    {
        if (ActiveByCell.TryGetValue(cell, out var registered) && registered == this) ActiveByCell.Remove(cell);
    }

    public void Reveal()
    {
        isHidden = false;
        SetVisualState(true, IsActivated);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsActivated || other.GetComponentInParent<PrototypePlayerStats>() == null)
        {
            return;
        }

        Activate(other.GetComponentInParent<PrototypePlayerStats>());
    }

    /// <summary>
    /// A thrown item landing on the trap sets it off. Effects that need the ranger to be
    /// standing on the trap do nothing; alarm and summoning still wake enemies.
    /// </summary>
    public bool ActivateByItem()
    {
        if (IsActivated) return false;
        IsActivated = true;
        SetVisualState(true, true);
        if (trapType == PrototypeTrapType.Alarm) WakeAllEnemies();
        else if (trapType == PrototypeTrapType.Summoning) WakeOneEnemy();
        else SeedBlob();
        return true;
    }

    /// <summary>
    /// Gas and fire traps release Pixel Dungeon blobs on their cell (amounts from PD's traps):
    /// toxic gas 300 + 20 x floor, paralytic gas 80 + 5 x floor, fire on the cell and its neighbors.
    /// Returns false when this trap type does not release a blob or no blob grid exists.
    /// </summary>
    private bool SeedBlob()
    {
        var blobs = PrototypeBlobs.Active;
        if (blobs == null) return false;
        var floor = FindAnyObjectByType<PrototypeDungeonRun>()?.Floor ?? 1;
        switch (trapType)
        {
            case PrototypeTrapType.ToxicGas: blobs.Seed(PrototypeBlobs.Kind.ToxicGas, transform.position, 300 + 20 * floor); return true;
            case PrototypeTrapType.ParalyticGas: blobs.Seed(PrototypeBlobs.Kind.ParalyticGas, transform.position, 80 + 5 * floor); return true;
            case PrototypeTrapType.Fire: blobs.SeedFireArea(transform.position, 4); return true;
            default: return false;
        }
    }

    /// <summary>Restores a saved trap state without triggering any effect.</summary>
    public void RestoreState(bool activated, bool revealed)
    {
        IsActivated = activated;
        if (revealed || activated) isHidden = false;
        SetVisualState(!isHidden, IsActivated);
    }

    private void Activate(PrototypePlayerStats stats)
    {
        IsActivated = true;
        SetVisualState(true, true);

        var statuses = stats.GetComponent<PrototypeStatusEffects>();
        switch (trapType)
        {
            // Gas and fire traps release spreading blobs; the old instant effects are fallbacks.
            case PrototypeTrapType.ToxicGas:
                if (!SeedBlob()) statuses?.Apply(PrototypeStatusEffectType.Toxic, 6f);
                break;
            case PrototypeTrapType.Fire:
                if (!SeedBlob()) stats.TakeDamage(3);
                break;
            case PrototypeTrapType.ParalyticGas:
                if (!SeedBlob()) statuses?.Apply(PrototypeStatusEffectType.Paralyzed, PrototypeStatusEffects.ParalysisDuration);
                break;
            case PrototypeTrapType.PoisonDart:
                // PD poison dart: poison for 4 + floor/2 turns.
                statuses?.Poison(4f + (FindAnyObjectByType<PrototypeDungeonRun>()?.Floor ?? 1) / 2f);
                break;
            case PrototypeTrapType.Alarm:
                WakeAllEnemies();
                break;
            case PrototypeTrapType.Lightning:
                stats.TakeDamage(4);
                break;
            case PrototypeTrapType.Gripping:
                statuses?.Apply(PrototypeStatusEffectType.Gripped, 5f);
                break;
            case PrototypeTrapType.Summoning:
                WakeOneEnemy();
                break;
        }
    }

    private void SetVisualState(bool visible, bool activated)
    {
        if (visualRenderers == null)
        {
            return;
        }

        // Armed traps' runes glow in their bright type color; spent traps' runes go dark and unlit.
        // Stone and iron parts keep their own materials.
        var color = activated ? trapColor * spentShade : trapColor;
        color.a = 1f;
        var glow = activated ? Color.black : trapColor * armedGlow;
        foreach (var renderer in visualRenderers)
        {
            renderer.enabled = visible;
            if (!renderer.name.StartsWith("Rune_") && renderer.name != "Trap_Plate") continue;
            var material = renderer.material;
            material.color = color;
            if (!material.HasProperty("_EmissionColor")) continue;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", glow);
        }
    }

    private static void WakeAllEnemies()
    {
        foreach (var enemy in FindObjectsByType<PrototypeEnemy>(FindObjectsInactive.Exclude))
        {
            if (enemy != null && enemy.IsAlive)
            {
                enemy.WakeUp();
            }
        }
    }

    private static void WakeOneEnemy()
    {
        foreach (var enemy in FindObjectsByType<PrototypeEnemy>(FindObjectsInactive.Exclude))
        {
            if (enemy != null && enemy.IsAlive && !enemy.IsAwake)
            {
                enemy.WakeUp();
                return;
            }
        }
    }
}
