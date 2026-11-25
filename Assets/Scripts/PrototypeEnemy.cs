using UnityEngine;

/// <summary>
/// Minimal sleeping target used to test the ranger's attacks.
/// </summary>
public sealed class PrototypeEnemy : MonoBehaviour
{
    [SerializeField] private int health = 3;
    [SerializeField] private Color sleepingColor = new(0.18f, 0.22f, 0.3f);
    [SerializeField] private Color awakeColor = new(0.85f, 0.12f, 0.1f);

    private Renderer enemyRenderer;

    public bool IsAlive => health > 0 && this != null;
    public bool IsAwake { get; private set; }

    private void Awake()
    {
        enemyRenderer = GetComponent<Renderer>();
        ApplySleepVisual();
    }

    public void TakeDamage(int damage)
    {
        if (!IsAlive)
        {
            return;
        }

        WakeUp();
        health -= damage;
        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }

    public void WakeUp()
    {
        if (IsAwake)
        {
            return;
        }

        IsAwake = true;
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = awakeColor;
        }
    }

    private void ApplySleepVisual()
    {
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = sleepingColor;
        }
    }
}
