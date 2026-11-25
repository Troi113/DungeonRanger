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
    [SerializeField] private Color fogColor = new(0.025f, 0.03f, 0.05f);

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
    private readonly HashSet<Vector2Int> visibleCells = new();
    private readonly HashSet<Vector2Int> discoveredCells = new();
    private readonly Dictionary<Vector2Int, GameObject> fogTiles = new();
    private readonly Dictionary<DoorEdge, PrototypeDoor> doors = new();
    private readonly List<PrototypeEnemy> enemies = new();
    private float nextRefreshTime;

    public void Initialize(
        PrototypePlayerMovement targetPlayer,
        IReadOnlyList<Vector2Int> generatedWalkableCells,
        Transform levelRoot)
    {
        player = targetPlayer;
        walkableCells = new HashSet<Vector2Int>(generatedWalkableCells);
        enemies.AddRange(FindObjectsByType<PrototypeEnemy>(FindObjectsInactive.Exclude));

        foreach (var door in levelRoot.GetComponentsInChildren<PrototypeDoor>(true))
        {
            var edge = new DoorEdge(door.Cell, door.Direction);
            var reverseEdge = new DoorEdge(door.Cell + door.Direction, -door.Direction);
            doors[edge] = door;
            doors[reverseEdge] = door;
        }

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

    private void BuildFogTiles(Transform levelRoot)
    {
        var fogRoot = new GameObject("Level_FogOfWar").transform;
        fogRoot.SetParent(levelRoot, false);
        var material = CreateMaterial("Level Fog Material", fogColor);

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
            fogTile.GetComponent<Renderer>().sharedMaterial = material;
            var collider = fogTile.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            fogTiles[cell] = fogTile;
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

        discoveredCells.UnionWith(visibleCells);

        foreach (var pair in fogTiles)
        {
            pair.Value.SetActive(!visibleCells.Contains(pair.Key));
        }

        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.IsAlive)
            {
                enemy.SetVisible(visibleCells.Contains(PrototypeLevelGenerator.WorldToGrid(enemy.transform.position)));
            }
        }
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
