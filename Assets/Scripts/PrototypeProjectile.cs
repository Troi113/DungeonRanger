using UnityEngine;

/// <summary>
/// Simple projectile that can travel toward one selected enemy or continue straight.
/// </summary>
public sealed class PrototypeProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private int damage;
    private float remainingLifetime;

    public void Initialize(Vector3 initialDirection, float projectileSpeed, int projectileDamage, float lifetime)
    {
        direction = initialDirection.normalized;
        speed = projectileSpeed;
        damage = projectileDamage;
        remainingLifetime = lifetime / projectileSpeed;
    }

    private void Update()
    {
        if (PrototypeGameUI.IsGamePaused) return;
        transform.position += direction * speed * Time.deltaTime;
        remainingLifetime -= Time.deltaTime;

        foreach (var enemy in FindObjectsByType<PrototypeEnemy>(FindObjectsInactive.Exclude))
        {
            if (enemy != null && enemy.IsAlive && enemy.GetComponent<Collider>() != null &&
                Vector3.Distance(transform.position, enemy.GetComponent<Collider>().ClosestPoint(transform.position)) <= 0.2f)
            {
                enemy.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }
        }

        if (remainingLifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
