using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Wakes sleeping LevelTest enemies one at a time using randomized delays and
/// a randomized enemy order.
/// </summary>
public sealed class PrototypeEnemyWakeDirector : MonoBehaviour
{
    [SerializeField] private float minimumWakeDelay = 20f;
    [SerializeField] private float maximumWakeDelay = 35f;

    private readonly List<PrototypeEnemy> enemies = new();
    private readonly List<PrototypeEnemy> sleepingEnemies = new();
    private System.Random random;
    private float nextWakeTime;
    private float pausedAt = -1f;

    private void OnDisable()
    {
        if (random != null) pausedAt = Time.time;
    }

    private void OnEnable()
    {
        if (random != null && pausedAt >= 0f)
        {
            nextWakeTime += Time.time - pausedAt;
            pausedAt = -1f;
        }
    }

    public void Initialize(IReadOnlyList<PrototypeEnemy> spawnedEnemies, int randomSeed)
    {
        enemies.Clear();
        enemies.AddRange(spawnedEnemies);
        random = new System.Random(randomSeed);
        nextWakeTime = Time.time + NextWakeDelay();
    }

    private void Update()
    {
        if (PrototypeGameUI.IsGamePaused || random == null || Time.time < nextWakeTime)
        {
            return;
        }

        sleepingEnemies.Clear();
        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.IsAlive && !enemy.IsAwake)
            {
                sleepingEnemies.Add(enemy);
            }
        }

        if (sleepingEnemies.Count == 0)
        {
            enabled = false;
            return;
        }

        var selected = sleepingEnemies[random.Next(sleepingEnemies.Count)];
        selected.WakeUp();
        nextWakeTime = Time.time + NextWakeDelay();
    }

    private float NextWakeDelay()
    {
        return Mathf.Lerp(minimumWakeDelay, maximumWakeDelay, (float)random.NextDouble());
    }
}
