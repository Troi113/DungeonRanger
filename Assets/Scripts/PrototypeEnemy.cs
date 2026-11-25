using UnityEngine;

/// <summary>
/// Minimal stationary target used to test the ranger's attacks.
/// </summary>
public sealed class PrototypeEnemy : MonoBehaviour
{
    [SerializeField] private int health = 3;

    public bool IsAlive => health > 0 && this != null;

    public void TakeDamage(int damage)
    {
        if (!IsAlive)
        {
            return;
        }

        health -= damage;
        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }
}
