using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shallow sewer water patches on a floor, after Pixel Dungeon's SewerBossLevel water
/// (Patch.generate(0.5, 5): random fill, then smoothing). Water is drawn as translucent
/// green-blue tiles under the fog layer. Goo heals while standing in it.
/// The active floor's water is registered while its floor object is enabled.
/// </summary>
public sealed class PrototypeSewerWater : MonoBehaviour
{
    private static PrototypeSewerWater active;
    private readonly HashSet<Vector2Int> cells = new();

    public static bool IsWater(Vector3 worldPosition) =>
        active != null && active.cells.Contains(PrototypeLevelGenerator.WorldToGrid(worldPosition));

    /// <summary>Generates water on walkable cells, keeping the given cells (stairs, spawns) dry.</summary>
    public static PrototypeSewerWater Create(Transform floorRoot, IReadOnlyCollection<Vector2Int> walkable, int seed,
        float fill, int smoothing, IEnumerable<Vector2Int> keepDry)
    {
        var water = floorRoot.gameObject.AddComponent<PrototypeSewerWater>();
        var random = new System.Random(seed + 9191);
        var walkableSet = new HashSet<Vector2Int>(walkable);
        var wet = new HashSet<Vector2Int>();
        foreach (var cell in walkableSet) if (random.NextDouble() < fill) wet.Add(cell);

        // Smoothing: a cell becomes wet when most of its 3x3 neighborhood is wet (like Patch.generate).
        for (var pass = 0; pass < smoothing; pass++)
        {
            var next = new HashSet<Vector2Int>();
            foreach (var cell in walkableSet)
            {
                var count = 0;
                var total = 0;
                for (var dx = -1; dx <= 1; dx++)
                for (var dy = -1; dy <= 1; dy++)
                {
                    var neighbor = cell + new Vector2Int(dx, dy);
                    if (!walkableSet.Contains(neighbor)) continue;
                    total++;
                    if (wet.Contains(neighbor)) count++;
                }
                if (count * 2 > total) next.Add(cell);
            }
            wet = next;
        }
        foreach (var cell in keepDry) wet.Remove(cell);

        var material = PrototypeBootstrap.CreateMaterial("Sewer Water", new Color(0.22f, 0.42f, 0.36f));
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.85f);
        var root = new GameObject("Level_Water").transform;
        root.SetParent(floorRoot, false);
        foreach (var cell in wet)
        {
            var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = $"Water_{cell.x}_{cell.y}";
            Object.Destroy(tile.GetComponent<Collider>());
            tile.transform.SetParent(root, false);
            tile.transform.position = PrototypeLevelGenerator.GridToWorld(cell, 0.04f);
            tile.transform.localScale = new Vector3(PrototypeLevelGenerator.TileSize, 0.04f, PrototypeLevelGenerator.TileSize);
            tile.GetComponent<Renderer>().sharedMaterial = material;
            water.cells.Add(cell);
        }
        active = water;
        return water;
    }

    private void OnEnable()
    {
        if (cells.Count > 0) active = this;
    }

    private void OnDisable()
    {
        if (active == this) active = null;
    }
}
