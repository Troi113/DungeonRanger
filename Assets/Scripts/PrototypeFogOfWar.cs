using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime fog for LevelTest. Walls and door frames remain visible as the map
/// outline, while floor contents and enemies are hidden outside the hero's
/// current visibility region.
/// </summary>
public sealed class PrototypeFogOfWar : MonoBehaviour
{
    [SerializeField] private int visibilityRadiusCells = 9;
    [SerializeField] private float refreshInterval = 0.1f;
    [SerializeField] private Color invisibleColor = new(0.01f, 0.01f, 0.015f);
    [SerializeField] private Color visitedColor = new(0.07f, 0.08f, 0.1f);
    [SerializeField] private Color mappedColor = new(0.2f, 0.11f, 0.06f);

    private readonly struct DoorEdge : IEquatable<DoorEdge>
    {
        public readonly Vector2Int Cell;
        public readonly Vector2Int Direction;

        public DoorEdge(Vector2Int cell, Vector2Int direction)
        {
            Cell = cell;
            Direction = direction;
        }

        public bool Equals(DoorEdge other) => Cell == other.Cell && Direction == other.Direction;
        public override bool Equals(object obj) => obj is DoorEdge other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Cell, Direction);
    }

    private PrototypePlayerMovement player;
    private HashSet<Vector2Int> walkableCells;
    private List<Vector2Int> teleportCells;
    private HashSet<long> blockedEdges;
    private readonly HashSet<Vector2Int> visibleCells = new();
    private readonly HashSet<Vector2Int> visitedCells = new();
    private readonly HashSet<Vector2Int> mappedCells = new();
    private readonly Dictionary<Vector2Int, Renderer> fogTiles = new();
    private readonly Dictionary<DoorEdge, PrototypeDoor> doors = new();
    private readonly List<PrototypeEnemy> enemies = new();
    private Material invisibleMaterial;
    private Material visitedMaterial;
    private Material mappedMaterial;
    private float nextRefreshTime;

    public void Initialize(
        PrototypePlayerMovement targetPlayer,
        IReadOnlyList<Vector2Int> generatedWalkableCells,
        Transform levelRoot,
        IReadOnlyCollection<long> blockedRoomEdges = null)
    {
        player = targetPlayer;
        walkableCells = new HashSet<Vector2Int>(generatedWalkableCells);
        teleportCells = new List<Vector2Int>(generatedWalkableCells);
        blockedEdges = blockedRoomEdges != null ? new HashSet<long>(blockedRoomEdges) : new HashSet<long>();
        enemies.AddRange(FindObjectsByType<PrototypeEnemy>(FindObjectsInactive.Exclude));

        foreach (var door in levelRoot.GetComponentsInChildren<PrototypeDoor>(true))
        {
            var edge = new DoorEdge(door.Cell, door.Direction);
            var reverseEdge = new DoorEdge(door.Cell + door.Direction, -door.Direction);
            doors[edge] = door;
            doors[reverseEdge] = door;
        }

        CreateFogMaterials();
        BuildFogTiles(levelRoot);
        RefreshVisibility();
    }

    private void Update()
    {
        if (player == null || walkableCells == null || Time.time < nextRefreshTime)
        {
            return;
        }

        RefreshVisibility();
    }

    public void RefreshNow()
    {
        if (player != null && walkableCells != null) RefreshVisibility();
    }

    private void BuildFogTiles(Transform levelRoot)
    {
        var fogRoot = new GameObject("Level_FogOfWar").transform;
        fogRoot.SetParent(levelRoot, false);

        foreach (var cell in walkableCells)
        {
            var fogTile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fogTile.name = $"Fog_{cell.x}_{cell.y}";
            fogTile.transform.SetParent(fogRoot, false);
            fogTile.transform.position = PrototypeLevelGenerator.GridToWorld(cell, 0.12f);
            fogTile.transform.localScale = new Vector3(
                PrototypeLevelGenerator.TileSize,
                0.05f,
                PrototypeLevelGenerator.TileSize);
            var renderer = fogTile.GetComponent<Renderer>();
            renderer.sharedMaterial = invisibleMaterial;
            var collider = fogTile.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            fogTiles[cell] = renderer;
        }
    }

    private void RefreshVisibility()
    {
        nextRefreshTime = Time.time + refreshInterval;
        visibleCells.Clear();

        var start = FindNearestWalkable(PrototypeLevelGenerator.WorldToGrid(player.transform.position));
        var radiusSquared = visibilityRadiusCells * visibilityRadiusCells;

        // Test every floor cell against a circular radius and a grid line of
        // sight. This prevents a nearby corridor from revealing an entire
        // room simply because its path distance is short enough.
        foreach (var cell in walkableCells)
        {
            var offsetFromHero = cell - start;
            if (offsetFromHero.sqrMagnitude <= radiusSquared && HasLineOfSight(start, cell))
            {
                visibleCells.Add(cell);
            }
        }

        visitedCells.UnionWith(visibleCells);

        foreach (var pair in fogTiles)
        {
            var cell = pair.Key;
            var renderer = pair.Value;
            if (visibleCells.Contains(cell))
            {
                renderer.enabled = false;
            }
            else
            {
                renderer.enabled = true;
                renderer.sharedMaterial = visitedCells.Contains(cell)
                    ? visitedMaterial
                    : mappedCells.Contains(cell)
                        ? mappedMaterial
                        : invisibleMaterial;
            }
        }

        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.IsAlive)
            {
                enemy.SetVisible(visibleCells.Contains(PrototypeLevelGenerator.WorldToGrid(enemy.transform.position)));
            }
        }
    }

    /// <summary>
    /// Marks cells as known by a future map item or mapping effect. Mapped
    /// cells use a separate state from cells the hero has physically visited.
    /// </summary>
    public void RevealMappedCells(IEnumerable<Vector2Int> cells)
    {
        foreach (var cell in cells)
        {
            if (walkableCells.Contains(cell))
            {
                mappedCells.Add(cell);
            }
        }

        RefreshVisibility();
    }

    public void RevealEntireMap()
    {
        if (walkableCells == null) return;
        RevealMappedCells(walkableCells);
        foreach (var trap in GetComponentsInChildren<PrototypeTrap>(true)) trap.Reveal();
    }

    public bool TryTeleportPlayer(System.Random random)
    {
        if (player == null || teleportCells == null || teleportCells.Count == 0) return false;
        var startingIndex = random.Next(teleportCells.Count);
        var currentCell = PrototypeLevelGenerator.WorldToGrid(player.transform.position);
        for (var i = 0; i < teleportCells.Count; i++)
        {
            var cell = teleportCells[(startingIndex + i) % teleportCells.Count];
            if (cell == currentCell) continue;
            var position = PrototypeLevelGenerator.GridToWorld(cell, 1f);
            var safe = true;
            foreach (var collider in Physics.OverlapSphere(position, 0.7f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            {
                if (collider.GetComponentInParent<PrototypeEnemy>() != null ||
                    collider.GetComponentInParent<PrototypeTrap>() != null ||
                    collider.GetComponentInParent<PrototypeDoor>() != null ||
                    collider.name.StartsWith("Wall_"))
                {
                    safe = false;
                    break;
                }
            }
            if (!safe) continue;
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = position;
            controller.enabled = true;
            Physics.SyncTransforms();
            RefreshNow();
            return true;
        }
        return false;
    }

    private bool HasLineOfSight(Vector2Int start, Vector2Int target)
    {
        if (start == target)
        {
            return true;
        }

        var current = start;
        var delta = target - start;
        var deltaX = Mathf.Abs(delta.x);
        var deltaY = Mathf.Abs(delta.y);
        var stepX = delta.x >= 0 ? 1 : -1;
        var stepY = delta.y >= 0 ? 1 : -1;
        var error = deltaX - deltaY;

        while (current != target)
        {
            var previous = current;
            var doubleError = error * 2;
            var movedX = false;
            var movedY = false;

            if (doubleError > -deltaY)
            {
                error -= deltaY;
                current.x += stepX;
                movedX = true;
            }

            if (doubleError < deltaX)
            {
                error += deltaX;
                current.y += stepY;
                movedY = true;
            }

            if (!walkableCells.Contains(current))
            {
                return false;
            }

            if (movedX && movedY)
            {
                var horizontal = new Vector2Int(stepX, 0);
                var vertical = new Vector2Int(0, stepY);
                var horizontalCell = previous + horizontal;
                var verticalCell = previous + vertical;

                if (!walkableCells.Contains(horizontalCell) ||
                    !walkableCells.Contains(verticalCell) ||
                    IsBlockedByClosedDoor(previous, horizontal) ||
                    IsBlockedByClosedDoor(previous, vertical) ||
                    IsBlockedByClosedDoor(horizontalCell, vertical) ||
                    IsBlockedByClosedDoor(verticalCell, horizontal))
                {
                    return false;
                }
            }
            else if (IsBlockedByClosedDoor(previous, current - previous))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsBlockedByClosedDoor(Vector2Int cell, Vector2Int direction)
    {
        if (blockedEdges.Contains(PrototypeLevelGenerator.EdgeKey(cell, cell + direction))) return true;
        var edge = new DoorEdge(cell, direction);
        return doors.TryGetValue(edge, out var door) && !door.IsOpen;
    }

    private Vector2Int FindNearestWalkable(Vector2Int requested)
    {
        if (walkableCells.Contains(requested))
        {
            return requested;
        }

        var nearest = Vector2Int.zero;
        var nearestDistance = float.MaxValue;
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

    private void CreateFogMaterials()
    {
        invisibleMaterial = CreateMaterial("Level Fog Invisible Material", invisibleColor);
        visitedMaterial = CreateMaterial("Level Fog Visited Material", visitedColor);
        mappedMaterial = CreateMaterial("Level Fog Mapped Material", mappedColor);
    }

    private static Material CreateMaterial(string materialName, Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader)
        {
            name = materialName,
            color = color
        };
        material.SetFloat("_Smoothness", 0f);
        return material;
    }
}
