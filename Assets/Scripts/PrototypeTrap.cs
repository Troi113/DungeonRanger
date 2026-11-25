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
    private PrototypeTrapType trapType;
    private bool isHidden;
    private Renderer[] visualRenderers;
    private Color trapColor;

    public bool IsActivated { get; private set; }
    public bool IsHidden => isHidden && !IsActivated;
    public PrototypeTrapType TrapType => trapType;

    public void Initialize(PrototypeTrapType type, bool hidden, Color color, Transform visualRoot)
    {
        trapType = type;
        isHidden = hidden;
        trapColor = color;
        visualRenderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        SetVisualState(!hidden, false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsActivated || other.GetComponentInParent<PrototypePlayerStats>() == null)
        {
            return;
        }

        Activate(other.GetComponentInParent<PrototypePlayerStats>());
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
