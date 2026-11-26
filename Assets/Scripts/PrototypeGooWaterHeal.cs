using UnityEngine;

/// <summary>Like Pixel Dungeon's Goo, heals 1 health every `secondsPerHealth` while standing in sewer water.</summary>
[RequireComponent(typeof(PrototypeEnemy))]
public sealed class PrototypeGooWaterHeal : MonoBehaviour
{
    [SerializeField] private float secondsPerHealth = 1.5f;

    private PrototypeEnemy enemy;
    private float timer;

    private void Awake() => enemy = GetComponent<PrototypeEnemy>();

    private void Update()
    {
        if (PrototypeGameUI.IsGamePaused || !enemy.IsAlive || enemy.CurrentHealth >= enemy.MaxHealth || !PrototypeSewerWater.IsWater(transform.position))
        {
            timer = 0f;
            return;
        }
        timer += Time.deltaTime;
        if (timer < secondsPerHealth) return;
        timer -= secondsPerHealth;
        enemy.Heal(1);
    }
}
