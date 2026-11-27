using UnityEngine;

/// <summary>
/// Straight arrow with swept collision so fast shots cannot skip enemies or walls.
/// Shots at an enemy within one block of the archer can miss: the arrow flies past that enemy.
/// </summary>
public sealed class PrototypeProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private int damage;
    private float remainingDistance;
    private Vector3 origin;
    private float pointBlankMissChance;
    private PrototypeEnemy missed;

    public void Initialize(Vector3 initialDirection, float projectileSpeed, int projectileDamage, float range,
        Vector3 shooterPosition = default, float missChanceWithinOneBlock = 0f)
    {
        origin = shooterPosition;
        pointBlankMissChance = missChanceWithinOneBlock;
        direction = initialDirection.normalized;
        speed = projectileSpeed;
        damage = projectileDamage;
        remainingDistance = range;
    }

    private void Update()
    {
        if (PrototypeGameUI.IsGamePaused) return;
        var distance = Mathf.Min(speed * Time.deltaTime, remainingDistance);
        var closestDistance = float.PositiveInfinity;
        PrototypeEnemy target = null;
        var hitObstacle = false;
        foreach (var collider in Physics.OverlapSphere(transform.position, 0.15f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (collider.name.StartsWith("Wall_") || collider.name == "Door_Panel")
            {
                target = null;
                hitObstacle = true;
                closestDistance = 0f;
                break;
            }
            var enemy = collider.GetComponentInParent<PrototypeEnemy>();
            if (enemy != null && enemy.IsAlive && enemy != missed) { target = enemy; closestDistance = 0f; }
        }
        foreach (var hit in Physics.SphereCastAll(transform.position, 0.15f, direction, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.distance >= closestDistance) continue;
            var enemy = hit.collider.GetComponentInParent<PrototypeEnemy>();
            if (enemy == missed) enemy = null;
            var wall = hit.collider.name.StartsWith("Wall_") || hit.collider.name == "Door_Panel";
            if ((enemy == null || !enemy.IsAlive) && !wall) continue;
            closestDistance = hit.distance;
            target = enemy;
            hitObstacle = wall && enemy == null;
        }

        if (target != null && RollPointBlankMiss(target))
        {
            // Too close to aim properly: the arrow flies past this enemy and keeps going.
            missed = target;
            PrototypeGameLog.Add($"Your arrow misses the {target.DisplayName} at point-blank range.");
            target = null;
            PrototypeGameUI.ShowNotice("Your arrow misses at point-blank range.");
        }

        if (target != null)
        {
            target.TakeDamage(damage, true);
            FindAnyObjectByType<PrototypeTalents>()?.OnBowHit(target);
            PrototypeGameFeel.Sparks(transform.position, new Color(0.8f, 0.95f, 1f), 8);
            PrototypeGameFeel.Shake(0.1f, 0.1f);
        }
        if (target == null && hitObstacle) PrototypeGameLog.Add("Your arrow hits a wall.");
        if (target != null || hitObstacle) { Destroy(gameObject); return; }
        transform.position += direction * distance;
        remainingDistance -= distance;
        if (remainingDistance <= 0f) Destroy(gameObject);
    }

    /// <summary>True if the enemy is within one block of where the arrow was shot and the miss roll succeeds.</summary>
    private bool RollPointBlankMiss(PrototypeEnemy enemy)
    {
        if (pointBlankMissChance <= 0f) return false;
        var collider = enemy.GetComponent<Collider>();
        var closest = collider != null ? collider.ClosestPoint(origin) : enemy.transform.position;
        var offset = closest - origin;
        offset.y = 0f;
        return offset.magnitude <= PrototypeLevelGenerator.TileSize && Random.value < pointBlankMissChance;
    }
}
