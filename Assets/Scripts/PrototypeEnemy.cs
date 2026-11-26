using System.Collections.Generic;
using System;
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
    [SerializeField] private float attackRange = 2.4f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private int minAttackDamage = 1;
    [SerializeField] private int maxAttackDamage = 1;
    [Tooltip("Hits on this enemy are reduced by a random 0..defense, never below 1.")]
    [SerializeField] private int defense;
    [SerializeField] private float hitSlowDuration = 1.5f;
    [SerializeField, Range(0f, 1f)] private float hitSpeedMultiplier = 0.5f;

    private Renderer enemyRenderer;
    private PrototypePlayerMovement player;
    private List<Vector2Int> walkableCells;
    private HashSet<Vector2Int> walkableLookup;
    private HashSet<long> blockedEdges;
    private readonly List<Vector2Int> path = new();
    private System.Random random;
    private Vector2Int patrolTarget;
    private int pathIndex;
    private float nextRepathTime;
    private float nextAttackTime;
    private bool noticedPlayer;
    private Vector2Int lastSeenPlayer;
    private float slowRemaining;
    private bool isBoss;
    private PrototypeGooCharge charge;
    private int maxHealth;
    private bool hasTakenDamage;
    private PrototypeMobs.MobData mob;
    private bool hasMobData;
    public event Action Defeated;

    public bool IsAlive => health > 0 && this != null;
    public bool IsAwake { get; private set; }
    public bool IsSlowed => slowRemaining > 0f;
    public int CurrentHealth => health;
    public int MaxHealth => maxHealth;
    public bool HasTakenDamage => hasTakenDamage;
    public string MobName => hasMobData ? mob.Name : "Enemy";

    private void Awake()
    {
        enemyRenderer = GetComponent<Renderer>();
        health = Mathf.Max(1, health);
        maxHealth = health;
        ApplySleepVisual();
        gameObject.AddComponent<PrototypeEnemyHealthBar>();
    }

    public void Initialize(
        PrototypePlayerMovement targetPlayer,
        IReadOnlyList<Vector2Int> generatedWalkableCells,
        int randomSeed,
        IReadOnlyCollection<long> blockedRoomEdges = null)
    {
        player = targetPlayer;
        walkableCells = new List<Vector2Int>(generatedWalkableCells);
        walkableLookup = new HashSet<Vector2Int>(walkableCells);
        blockedEdges = blockedRoomEdges != null ? new HashSet<long>(blockedRoomEdges) : new HashSet<long>();
        random = new System.Random(randomSeed);
        ChoosePatrolTarget();
    }

    private void Update()
    {
        if (PrototypeGameUI.IsGamePaused || !IsAlive || player == null || walkableCells == null || walkableCells.Count == 0)
        {
            return;
        }

        if (slowRemaining > 0f) slowRemaining = Mathf.Max(0f, slowRemaining - Time.deltaTime);
        if (charge != null && charge.IsActive) return;

        var sightRangeWorld = sightRangeCells * PrototypeLevelGenerator.TileSize;
        var distanceToPlayer = player.transform.position - transform.position;
        var canSeePlayer = distanceToPlayer.sqrMagnitude <= sightRangeWorld * sightRangeWorld && HasClearSight(distanceToPlayer);

        if (canSeePlayer)
        {
            WakeUp();
            noticedPlayer = true;
            lastSeenPlayer = PrototypeLevelGenerator.WorldToGrid(player.transform.position);
            if (charge != null && charge.TryStart()) return;
            var planarDistance = distanceToPlayer;
            planarDistance.y = 0f;
            if (planarDistance.magnitude <= attackRange)
            {
                AttackPlayer();
                return;
            }

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

        // Sleeping enemies wait for the staggered wake director, a hit, or
        // direct sight of the player. Randomly awakened enemies patrol until
        // they notice the player; waking alone does not reveal the player.
        if (!IsAwake) return;

        if (noticedPlayer)
        {
            if (Time.time >= nextRepathTime && pathIndex >= path.Count)
            {
                RebuildPath(PrototypeLevelGenerator.WorldToGrid(transform.position), lastSeenPlayer);
                nextRepathTime = Time.time + repathInterval;
            }
            if (PrototypeLevelGenerator.WorldToGrid(transform.position) == lastSeenPlayer)
                noticedPlayer = false;
            else
            {
                FollowPath(chaseSpeed);
                return;
            }
        }

        if (pathIndex >= path.Count)
        {
            ChoosePatrolTarget();
        }

        FollowPath(patrolSpeed);
    }

    public void TakeDamage(int damage, bool slowOnHit = false)
    {
        if (!IsAlive || damage <= 0)
        {
            return;
        }

        WakeUp();
        if (defense > 0) damage = Mathf.Max(1, damage - UnityEngine.Random.Range(0, defense + 1));
        if (slowOnHit && !isBoss && damage > 0) slowRemaining = hitSlowDuration;
        if (player != null)
        {
            noticedPlayer = true;
            lastSeenPlayer = PrototypeLevelGenerator.WorldToGrid(player.transform.position);
            RebuildPath(PrototypeLevelGenerator.WorldToGrid(transform.position), lastSeenPlayer);
        }
        health = (int)Math.Max(0L, (long)health - damage);
        hasTakenDamage = true;
        if (health <= 0)
        {
            Defeated?.Invoke();
            Destroy(gameObject);
        }
    }

    /// <summary>Applies a regular mob's stats; call right after Initialize.</summary>
    public void Configure(PrototypeMobs.MobData data)
    {
        ApplyMobData(data);
        health = maxHealth = Mathf.Max(1, data.Health);
        hasTakenDamage = false;
        awakeColor = data.Color;
        sleepingColor = data.Color * 0.4f;
        sleepingColor.a = 1f;
        if (!IsAwake) ApplySleepVisual();
    }

    private void ApplyMobData(PrototypeMobs.MobData data)
    {
        mob = data;
        hasMobData = true;
        minAttackDamage = data.MinDamage;
        maxAttackDamage = Mathf.Max(data.MinDamage, data.MaxDamage);
        defense = Mathf.Max(0, data.Defense);
    }

    public void ConfigureBoss(int bossHealth)
    {
        ApplyMobData(PrototypeMobs.Goo);
        isBoss = true;
        health = Mathf.Max(1, bossHealth);
        maxHealth = health;
        hasTakenDamage = false;
        awakeColor = new Color(0.2f, 0.75f, 0.35f);
        patrolSpeed = 2.5f;
        chaseSpeed = 3f;
        attackRange = 3f;
        // Charge damage stays fixed at 2; normal contact attacks roll Goo's damage range.
        attackDamage = 2;
        attackCooldown = 1.8f;
        WakeUp();
        charge = gameObject.AddComponent<PrototypeGooCharge>();
        charge.Initialize(player, chaseSpeed, attackDamage);
    }

    private bool HasClearSight(Vector3 offset)
    {
        var distance = offset.magnitude;
        if (distance < 0.01f) return true;
        foreach (var hit in Physics.RaycastAll(transform.position, offset / distance, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform.IsChildOf(player.transform)) continue;
            if (hit.collider.name.StartsWith("Wall_") || hit.collider.name == "Door_Panel") return false;
        }
        return true;
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

    public void AlertTo(PrototypePlayerMovement target)
    {
        if (!IsAlive || target == null) return;
        player = target;
        WakeUp();
        noticedPlayer = true;
        lastSeenPlayer = PrototypeLevelGenerator.WorldToGrid(target.transform.position);
        RebuildPath(PrototypeLevelGenerator.WorldToGrid(transform.position), lastSeenPlayer);
    }

    private void AttackPlayer()
    {
        if (Time.time < nextAttackTime || player == null)
        {
            return;
        }

        nextAttackTime = Time.time + attackCooldown;
        var stats = player.GetComponent<PrototypePlayerStats>();
        if (stats == null) return;
        var dealt = stats.TakeAttackDamage(PrototypeMobs.RollDamage(minAttackDamage, maxAttackDamage));
        if (dealt > 0 && hasMobData && mob.HasOnHitEffect && UnityEngine.Random.value < mob.OnHitChance)
            player.GetComponent<PrototypeStatusEffects>()?.Apply(mob.OnHitEffect, mob.OnHitDuration);
    }

    public void SetVisible(bool visible)
    {
        if (enemyRenderer != null)
        {
            enemyRenderer.enabled = visible;
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
        var actualSpeed = IsSlowed ? speed * hitSpeedMultiplier : speed;
        transform.position = Vector3.MoveTowards(transform.position, waypoint, actualSpeed * Time.deltaTime);
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
                if (!walkableLookup.Contains(next) || cameFrom.ContainsKey(next) ||
                    blockedEdges.Contains(PrototypeLevelGenerator.EdgeKey(current, next)))
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
