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
        return true;
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
            case PrototypeTrapType.ToxicGas:
                statuses?.Apply(PrototypeStatusEffectType.Toxic, 6f);
                break;
            case PrototypeTrapType.Fire:
                stats.TakeDamage(3);
                break;
            case PrototypeTrapType.ParalyticGas:
                statuses?.Apply(PrototypeStatusEffectType.Paralyzed, 4f);
                break;
            case PrototypeTrapType.PoisonDart:
                statuses?.Apply(PrototypeStatusEffectType.Poisoned, 6f);
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

        foreach (var renderer in visualRenderers)
        {
            renderer.enabled = visible;
            if (activated)
            {
                renderer.material.color = Color.Lerp(trapColor, Color.white, 0.35f);
            }
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
