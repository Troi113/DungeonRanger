using UnityEngine;

/// <summary>
/// Natural healing, after Pixel Dungeon's Regeneration buff: while the ranger is not starving and
/// hurt, 1 health returns every `secondsPerHealth` of game time. Like PD, only a starving
/// ranger stops regenerating. Pausing (timeScale 0) stops the timer.
/// </summary>
[RequireComponent(typeof(PrototypePlayerStats), typeof(PrototypeHunger))]
public sealed class PrototypeRegeneration : MonoBehaviour
{
    [SerializeField] private float secondsPerHealth = 5f;

    private PrototypePlayerStats stats;
    private PrototypeHunger hunger;
    private float timer;

    private void Awake()
    {
        stats = GetComponent<PrototypePlayerStats>();
        hunger = GetComponent<PrototypeHunger>();
    }

    private void Update()
    {
        if (stats.IsDead || stats.CurrentHealth >= stats.MaxHealth || hunger.IsStarving)
        {
            timer = 0f;
            return;
        }

        timer += Time.deltaTime;
        if (timer < secondsPerHealth) return;
        timer -= secondsPerHealth;
        stats.Heal(1);
    }
}
