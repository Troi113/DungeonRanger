using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Presentation only: dresses a generated floor with KayKit Dungeon Pack models in a sewer
/// style. Each Floor_* cube and Wall_* cube keeps its collider and logic but has its renderer
/// hidden and a fitted model placed over it. Floors mix plain, rocky, and grate tiles; walls
/// mix plain, cracked, and broken pieces; some walls get a torch with a warm light.
/// Choices use a seeded random so a floor looks the same every time it is built.
/// </summary>
public static class PrototypeDungeonDressing
{
    private const string Folder = "KayKit/Dungeon/";
    private const string TexturePath = Folder + "dungeon_texture";
    // A slight green-grey tint for damp sewer stone.
    private static readonly Color SewerTint = new(0.78f, 0.86f, 0.78f);
    private static readonly string[] FloorPieces = { "floor_tile_large", "floor_tile_large_rocks", "floor_tile_big_grate", "floor_tile_grate" };
    private static readonly float[] FloorWeights = { 0.62f, 0.18f, 0.1f, 0.1f };
    private static readonly string[] WallPieces = { "wall", "wall_cracked", "wall_broken" };
    private static readonly float[] WallWeights = { 0.75f, 0.2f, 0.05f };
    private const float TorchChance = 0.08f;
    private const float WallDepth = 0.6f;

    public static void Apply(Transform floorRoot, int seed)
    {
        if (PrototypeKayKit.Prefab(Folder + "wall") == null) return;
        var random = new System.Random(seed + 4242);
        var material = PrototypeKayKit.TextureMaterial(TexturePath, SewerTint);
        var floorCells = new HashSet<Vector2Int>();
        var floors = new List<Transform>();
        var walls = new List<Transform>();
        foreach (var child in floorRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.StartsWith("Floor_")) { floors.Add(child); floorCells.Add(PrototypeLevelGenerator.WorldToGrid(child.position)); }
            else if (child.name.StartsWith("Wall_")) walls.Add(child);
        }

        var dressing = new GameObject("Level_Dressing").transform;
        dressing.SetParent(floorRoot, false);
        foreach (var floor in floors) DressFloor(floor, dressing, material, random);
        foreach (var wall in walls) DressWall(wall, dressing, material, random, floorCells);
    }

    private static void DressFloor(Transform floor, Transform parent, Material material, System.Random random)
    {
        var path = Folder + FloorPieces[Pick(FloorWeights, random)];
        var prefab = PrototypeKayKit.Prefab(path);
        if (prefab == null) return;
        HideCube(floor);
        var bounds = PrototypeKayKit.LocalBounds(path, prefab);
        var size = Mathf.Max(bounds.size.x, bounds.size.z);
        var scale = size > 0.001f ? floor.lossyScale.x / size : 1f;
        var piece = PrototypeKayKit.Spawn(prefab, parent, material).transform;
        piece.localScale = Vector3.one * scale;
        piece.rotation = Quaternion.Euler(0f, 90f * random.Next(4), 0f);
        // Top of the tile sits at the floor cube's top (y = 0).
        var top = floor.position.y + floor.lossyScale.y * 0.5f;
        var center = piece.rotation * new Vector3(bounds.center.x, 0f, bounds.center.z) * scale;
        piece.position = new Vector3(floor.position.x - center.x, top - bounds.max.y * scale, floor.position.z - center.z);
    }

    private static void DressWall(Transform wall, Transform parent, Material material, System.Random random, HashSet<Vector2Int> floorCells)
    {
        var path = Folder + WallPieces[Pick(WallWeights, random)];
        var prefab = PrototypeKayKit.Prefab(path);
        if (prefab == null) return;
        HideCube(wall);
        var bounds = PrototypeKayKit.LocalBounds(path, prefab);
        var cube = wall.lossyScale;
        var alongZ = cube.z > cube.x;
        var length = alongZ ? cube.z : cube.x;
        // Model length runs along its wider horizontal axis; turn it to match the wall cube.
        var modelAlongX = bounds.size.x >= bounds.size.z;
        var yaw = (alongZ == modelAlongX) ? 90f : 0f;
        var modelLength = modelAlongX ? bounds.size.x : bounds.size.z;
        var modelDepth = modelAlongX ? bounds.size.z : bounds.size.x;
        var piece = PrototypeKayKit.Spawn(prefab, parent, material).transform;
        var lengthScale = modelLength > 0.001f ? length / modelLength : 1f;
        var heightScale = bounds.size.y > 0.001f ? cube.y / bounds.size.y : 1f;
        var depthScale = modelDepth > 0.001f ? WallDepth / modelDepth : 1f;
        piece.localScale = modelAlongX ? new Vector3(lengthScale, heightScale, depthScale) : new Vector3(depthScale, heightScale, lengthScale);
        piece.rotation = Quaternion.Euler(0f, yaw, 0f);
        var bottom = wall.position.y - cube.y * 0.5f;
        var center = piece.rotation * Vector3.Scale(new Vector3(bounds.center.x, 0f, bounds.center.z), piece.localScale);
        piece.position = new Vector3(wall.position.x - center.x, bottom - bounds.min.y * heightScale, wall.position.z - center.z);

        if (random.NextDouble() < TorchChance) AddTorch(wall, parent, material, alongZ, floorCells);
    }

    /// <summary>Mounts a torch on the side of the wall that faces a floor cell, with a warm light.</summary>
    private static void AddTorch(Transform wall, Transform parent, Material material, bool alongZ, HashSet<Vector2Int> floorCells)
    {
        var prefab = PrototypeKayKit.Prefab(Folder + "torch_mounted");
        if (prefab == null) return;
        var normal = alongZ ? Vector3.right : Vector3.forward;
        var half = PrototypeLevelGenerator.TileSize * 0.5f;
        if (!floorCells.Contains(PrototypeLevelGenerator.WorldToGrid(wall.position + normal * half))) normal = -normal;
        if (!floorCells.Contains(PrototypeLevelGenerator.WorldToGrid(wall.position + normal * half))) return;

        var torch = PrototypeKayKit.Spawn(prefab, parent, material).transform;
        torch.rotation = Quaternion.LookRotation(normal);
        torch.position = new Vector3(wall.position.x, 1.6f, wall.position.z) + normal * (WallDepth * 0.5f);

        var light = new GameObject("Torch_Light").AddComponent<Light>();
        light.transform.SetParent(torch, false);
        light.transform.position = torch.position + normal * 0.6f + Vector3.up * 0.4f;
        light.type = LightType.Point;
        light.color = new Color(1f, 0.62f, 0.3f);
        light.range = 9f;
        light.intensity = 2.2f;
        light.shadows = LightShadows.None;
    }

    private static void HideCube(Transform cube)
    {
        var renderer = cube.GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = false;
    }

    private static int Pick(float[] weights, System.Random random)
    {
        var roll = (float)random.NextDouble();
        for (var i = 0; i < weights.Length; i++)
        {
            if (roll < weights[i]) return i;
            roll -= weights[i];
        }
        return 0;
    }
}
