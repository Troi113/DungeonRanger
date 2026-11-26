using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Presentation helpers for the KayKit packs (CC0, Kay Lousberg) in Resources/KayKit.
/// Loads prefabs once, gives them the pack texture, and measures their bounds for fitting.
/// </summary>
public static class PrototypeKayKit
{
    private static readonly Dictionary<string, GameObject> Prefabs = new();
    private static readonly Dictionary<string, Bounds> PrefabBounds = new();
    private static readonly Dictionary<string, Material> Materials = new();
    private static readonly List<Renderer> RendererBuffer = new();
    private static readonly List<MeshFilter> FilterBuffer = new();

    public static GameObject Prefab(string path)
    {
        if (Prefabs.TryGetValue(path, out var prefab)) return prefab;
        prefab = Resources.Load<GameObject>(path);
        if (prefab == null) Debug.LogWarning($"KayKit asset missing: Resources/{path}");
        Prefabs[path] = prefab;
        return prefab;
    }

    /// <summary>Lit material using a pack texture, optionally tinted; created once per texture/tint.</summary>
    public static Material TextureMaterial(string texturePath, Color tint)
    {
        var key = $"{texturePath}|{tint}";
        if (Materials.TryGetValue(key, out var material)) return material;
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        material = new Material(shader) { name = $"KayKit {texturePath}", color = tint };
        var texture = Resources.Load<Texture2D>(texturePath);
        if (texture != null)
        {
            material.mainTexture = texture;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
        }
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.05f);
        Materials[key] = material;
        return material;
    }

    /// <summary>Instantiates a prefab under parent and gives every renderer the material.</summary>
    public static GameObject Spawn(GameObject prefab, Transform parent, Material material)
    {
        var instance = Object.Instantiate(prefab, parent, false);
        instance.GetComponentsInChildren(true, RendererBuffer);
        foreach (var renderer in RendererBuffer) renderer.sharedMaterial = material;
        return instance;
    }

    /// <summary>Bounds of a prefab's meshes in its own root space (cached per path).</summary>
    public static Bounds LocalBounds(string path, GameObject prefab)
    {
        if (PrefabBounds.TryGetValue(path, out var cached)) return cached;
        var result = new Bounds();
        var hasBounds = false;
        prefab.GetComponentsInChildren(true, FilterBuffer);
        var toRoot = prefab.transform.worldToLocalMatrix;
        foreach (var filter in FilterBuffer)
        {
            if (filter.sharedMesh == null) continue;
            var mesh = filter.sharedMesh.bounds;
            var matrix = toRoot * filter.transform.localToWorldMatrix;
            for (var i = 0; i < 8; i++)
            {
                var corner = mesh.center + Vector3.Scale(mesh.extents,
                    new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                var point = matrix.MultiplyPoint3x4(corner);
                if (!hasBounds) { result = new Bounds(point, Vector3.zero); hasBounds = true; }
                else result.Encapsulate(point);
            }
        }
        PrefabBounds[path] = result;
        return result;
    }
}
