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
    [SerializeField] private int visibilityRadiusCells = 12;
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
    private readonly Queue<Vector2Int> frontier = new();
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
        frontier.Clear();

        var start = FindNearestWalkable(PrototypeLevelGenerator.WorldToGrid(player.transform.position));
        visibleCells.Add(start);
        frontier.Enqueue(start);

        var radiusSquared = visibilityRadiusCells * visibilityRadiusCells;
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
            foreach (var direction in directions)
            {
                var next = current + direction;
                if (!walkableCells.Contains(next) || visibleCells.Contains(next))
                {
                    continue;
                }

                var offsetFromHero = next - start;
                if (offsetFromHero.sqrMagnitude > radiusSquared || IsBlockedByClosedDoor(current, direction))
                {
                    continue;
                }

                visibleCells.Add(next);
                frontier.Enqueue(next);
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
