using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Minimal sleeping target used to test the ranger's attacks.
/// </summary>
public sealed class PrototypeEnemy : MonoBehaviour
{
    [SerializeField] private int health = 3;
    [SerializeField] private Color sleepingColor = new(0.18f, 0.22f, 0.3f);
    [SerializeField] private Color awakeColor = new(0.85f, 0.12f, 0.1f);
    [SerializeField] private float patrolSpeed = 3.5f;
    [SerializeField] private float chaseSpeed = 4.5f;
    [SerializeField] private int sightRangeCells = 8;
    [SerializeField] private float repathInterval = 0.35f;

    private Renderer enemyRenderer;
    private PrototypePlayerMovement player;
    private List<Vector2Int> walkableCells;
    private HashSet<Vector2Int> walkableLookup;
    private readonly List<Vector2Int> path = new();
    private System.Random random;
    private Vector2Int patrolTarget;
    private int pathIndex;
    private float nextRepathTime;

    public bool IsAlive => health > 0 && this != null;
    public bool IsAwake { get; private set; }

    private void Awake()
    {
        enemyRenderer = GetComponent<Renderer>();
        ApplySleepVisual();
    }

    public void Initialize(
        PrototypePlayerMovement targetPlayer,
        IReadOnlyList<Vector2Int> generatedWalkableCells,
        int randomSeed)
    {
        player = targetPlayer;
        walkableCells = new List<Vector2Int>(generatedWalkableCells);
        walkableLookup = new HashSet<Vector2Int>(walkableCells);
        random = new System.Random(randomSeed);
        ChoosePatrolTarget();
    }

    private void Update()
    {
        if (!IsAlive || !IsAwake || player == null || walkableCells == null || walkableCells.Count == 0)
        {
            return;
        }

        var sightRangeWorld = sightRangeCells * PrototypeLevelGenerator.TileSize;
        var distanceToPlayer = player.transform.position - transform.position;
        var canSeePlayer = distanceToPlayer.sqrMagnitude <= sightRangeWorld * sightRangeWorld;

        if (canSeePlayer)
        {
            if (Time.time >= nextRepathTime)
            {
                RebuildPath(
                    PrototypeLevelGenerator.WorldToGrid(transform.position),
                    PrototypeLevelGenerator.WorldToGrid(player.transform.position));
                nextRepathTime = Time.time + repathInterval;
            }

            FollowPath(chaseSpeed);
            return;
        }

        if (pathIndex >= path.Count)
        {
            ChoosePatrolTarget();
        }

        FollowPath(patrolSpeed);
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

    private void ChoosePatrolTarget()
    {
        if (random == null || walkableCells == null || walkableCells.Count == 0)
        {
            return;
        }

        patrolTarget = walkableCells[random.Next(walkableCells.Count)];
        RebuildPath(PrototypeLevelGenerator.WorldToGrid(transform.position), patrolTarget);
    }

    private void FollowPath(float speed)
    {
        if (pathIndex >= path.Count)
        {
            return;
        }

        var waypoint = PrototypeLevelGenerator.GridToWorld(path[pathIndex], transform.position.y);
        var offset = waypoint - transform.position;
        offset.y = 0f;

        if (offset.sqrMagnitude <= 0.25f)
        {
            pathIndex++;
            return;
        }

        var direction = offset.normalized;
        transform.position = Vector3.MoveTowards(transform.position, waypoint, speed * Time.deltaTime);
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    private void RebuildPath(Vector2Int start, Vector2Int target)
    {
        path.Clear();
        pathIndex = 0;

        if (walkableLookup == null || walkableLookup.Count == 0)
        {
            return;
        }

        start = FindNearestWalkable(start);
        target = FindNearestWalkable(target);

        var frontier = new Queue<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        frontier.Enqueue(start);
        cameFrom[start] = start;

        var directions = new[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            if (current == target)
            {
                break;
            }

            foreach (var direction in directions)
            {
                var next = current + direction;
                if (!walkableLookup.Contains(next) || cameFrom.ContainsKey(next))
                {
                    continue;
                }

                frontier.Enqueue(next);
                cameFrom[next] = current;
            }
        }

        if (!cameFrom.ContainsKey(target))
        {
            return;
        }

        var reversePath = new List<Vector2Int>();
        var step = target;
        while (step != start)
        {
            reversePath.Add(step);
            step = cameFrom[step];
        }

        reversePath.Reverse();
        path.AddRange(reversePath);
    }

    private Vector2Int FindNearestWalkable(Vector2Int requested)
    {
        if (walkableLookup.Contains(requested))
        {
            return requested;
        }

        var nearest = walkableCells[0];
        var nearestDistance = (nearest - requested).sqrMagnitude;
        foreach (var cell in walkableCells)
        {
            var distance = (cell - requested).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearest = cell;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private void ApplySleepVisual()
    {
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = sleepingColor;
        }
    }
}
