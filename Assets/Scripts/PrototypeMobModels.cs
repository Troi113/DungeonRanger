using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Presentation only: builds 3D models for mobs from the third-party Rat Pack
/// (Assets/ThirdParty/Resources/RatPack). The pack's FBX holds three rats; the palette
/// texture colors them (Rat1 white, Rat2 grey, Rat3 brown). The source rats face -Z,
/// so they are turned to face +Z, the direction enemies move.
/// </summary>
public static class PrototypeMobModels
{
    private const string PackPath = "RatPack/RatPack";
    private const string PalettePath = "RatPack/LPoly-Pallete_Texture";

    private static GameObject pack;
    private static Material paletteMaterial;
    private static bool loadAttempted;
    private static readonly List<Renderer> RendererBuffer = new();

    /// <summary>
    /// Creates the named model as a child of an unscaled parent, scaled so the model is `length`
    /// world units long, centered under the parent and standing at local height `floorY`.
    /// Returns null if the pack or model is missing.
    /// </summary>
    public static GameObject Create(string modelName, Transform parent, float length, float floorY)
    {
        if (string.IsNullOrEmpty(modelName) || !Load()) return null;
        var source = pack.transform.Find(modelName);
        if (source == null)
        {
            Debug.LogWarning($"Rat Pack model '{modelName}' not found.");
            return null;
        }

        var holder = new GameObject($"Model_{modelName}");
        holder.transform.SetParent(parent, false);
        var model = Object.Instantiate(source.gameObject, holder.transform, false);
        model.name = modelName;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        model.GetComponentsInChildren(true, RendererBuffer);
        foreach (var renderer in RendererBuffer)
        {
            renderer.sharedMaterial = paletteMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        // Normalize size and placement from the mesh bounds measured in the holder's space.
        var bounds = LocalBounds(holder.transform, RendererBuffer);
        var longest = Mathf.Max(bounds.size.x, bounds.size.z);
        var scale = longest > 0.0001f ? length / longest : 1f;
        model.transform.localScale = Vector3.one * scale;
        model.transform.localPosition = new Vector3(-bounds.center.x * scale, floorY - bounds.min.y * scale, -bounds.center.z * scale);
        return holder;
    }

    private static bool Load()
    {
        if (loadAttempted) return pack != null;
        loadAttempted = true;
        pack = Resources.Load<GameObject>(PackPath);
        if (pack == null)
        {
            Debug.LogWarning($"Rat Pack not found at Resources/{PackPath}; enemies use plain shapes.");
            return false;
        }

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        paletteMaterial = new Material(shader) { name = "Rat Pack Palette" };
        var palette = Resources.Load<Texture2D>(PalettePath);
        if (palette != null)
        {
            paletteMaterial.mainTexture = palette;
            if (paletteMaterial.HasProperty("_BaseMap")) paletteMaterial.SetTexture("_BaseMap", palette);
        }
        if (paletteMaterial.HasProperty("_Smoothness")) paletteMaterial.SetFloat("_Smoothness", 0.1f);
        return true;
    }

    private static Bounds LocalBounds(Transform space, List<Renderer> renderers)
    {
        var hasBounds = false;
        var result = new Bounds();
        foreach (var renderer in renderers)
        {
            if (renderer is not MeshRenderer || !renderer.TryGetComponent<MeshFilter>(out var filter) || filter.sharedMesh == null) continue;
            var meshBounds = filter.sharedMesh.bounds;
            var toSpace = space.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            for (var i = 0; i < 8; i++)
            {
                var corner = meshBounds.center + Vector3.Scale(meshBounds.extents,
                    new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                var point = toSpace.MultiplyPoint3x4(corner);
                if (!hasBounds) { result = new Bounds(point, Vector3.zero); hasBounds = true; }
                else result.Encapsulate(point);
            }
        }
        return result;
    }
}
