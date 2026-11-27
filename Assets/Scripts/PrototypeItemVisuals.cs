using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Presentation only: 3D models for items on the floor, in flight and in the ranger's hand, from
/// the Poly Pizza models in Resources/Items (see Docs/CREDITS.md): sword, dagger (the knife),
/// axe, spear, potion bottle (glass, cork, and liquid tinted with the potion's color for this
/// run), bread loaf (food ration) and key. The models are stored upright (+Y), longest side 1,
/// resting on y = 0, with the weapons' grips at the bottom. Returns null for items without a
/// model; callers then use their simple shapes.
/// </summary>
public static class PrototypeItemVisuals
{
    private const string Folder = "Items/";
    // The potion model's liquid and glass materials (from its MTL).
    private const string PotionLiquid = "mat2";
    private const string PotionGlass = "mat24";

    // Floor pickups are drawn twice their in-flight size so they stand out.
    private const float FloorScale = 2f;

    private static readonly Dictionary<string, Material> Materials = new();
    private static readonly List<Renderer> RendererBuffer = new();
    private static readonly List<Material> MaterialBuffer = new();

    /// <summary>Model path for an item id, or null.</summary>
    public static string ModelPath(string itemId)
    {
        switch (itemId)
        {
            case "sword": return Folder + "Sword/sword";
            case "knife": return Folder + "Dagger/dagger";
            case "axe": return Folder + "Axe/axe";
            case "spear": return Folder + "Spear/spear";
            case "food": return Folder + "Loaf/loaf";
            case "key": return Folder + "Key/key";
        }
        return PrototypePotions.IsPotion(itemId) ? Folder + "Potion/potion" : null;
    }

    /// <summary>Length (world units) of a weapon held in the hand, and how far up from its bottom the grip is.</summary>
    public static bool HeldSize(string itemId, out float length, out float grip)
    {
        switch (itemId)
        {
            case "sword": length = 1.25f; grip = 0.14f; return true;
            case "knife": length = 0.75f; grip = 0.24f; return true;
            case "axe": length = 1.05f; grip = 0.15f; return true;
            case "spear": length = 2.7f; grip = 0.3f; return true;
        }
        length = grip = 0f;
        return false;
    }

    /// <summary>A weapon model held in the hand: upright along the hand's +Y, grip at the origin.</summary>
    /// The returned object's origin is the grip, so scaling its Y stretches the weapon from the hand.
    public static GameObject CreateHeld(string itemId, Transform hand)
    {
        if (!HeldSize(itemId, out var length, out var grip)) return null;
        var pivot = new GameObject($"Held_{itemId}");
        pivot.transform.SetParent(hand, false);
        var holder = Create(itemId, pivot.transform, length);
        if (holder == null)
        {
            Object.Destroy(pivot);
            return null;
        }
        holder.transform.localPosition = Vector3.down * (grip * length);
        holder.transform.localRotation = Quaternion.identity;
        return pivot;
    }

    /// <summary>Longest side of the model on the floor and in flight.</summary>
    public static float DisplaySize(string itemId)
    {
        if (PrototypePotions.IsPotion(itemId)) return 0.8f;
        return itemId switch { "food" => 1f, "key" => 0.9f, "knife" => 1.1f, "spear" => 2.2f, _ => 1.5f };
    }

    /// <summary>
    /// A floor pickup's model: potions stand upright, everything else lies on its side, turned
    /// to a fixed angle per item so the floor does not look uniform. Rests just above the floor.
    /// </summary>
    public static GameObject CreateOnFloor(string itemId, Transform parent)
    {
        var holder = Create(itemId, parent, DisplaySize(itemId) * FloorScale, true);
        if (holder == null) return null;
        var upright = PrototypePotions.IsPotion(itemId) || itemId == "food";
        var turn = 0;
        foreach (var c in itemId) turn = (turn * 31 + c) % 360;
        holder.transform.localRotation = Quaternion.Euler(0f, turn, 0f) * (upright ? Quaternion.identity : Quaternion.Euler(0f, 0f, 90f));
        // Lift so the lowest point sits on the floor.
        var lowest = float.MaxValue;
        holder.GetComponentsInChildren(true, RendererBuffer);
        foreach (var renderer in RendererBuffer) lowest = Mathf.Min(lowest, renderer.bounds.min.y);
        if (lowest < float.MaxValue) holder.transform.position += Vector3.up * (parent.position.y + 0.03f - lowest);
        return holder;
    }

    /// <summary>
    /// Creates the item's model under parent. Its longest side becomes `size`; it is centered on
    /// x/z and rests on y = 0 (or, when `centered`, is centered on all axes, for tumbling).
    /// </summary>
    public static GameObject Create(string itemId, Transform parent, float size, bool centered = false)
    {
        var path = ModelPath(itemId);
        if (path == null) return null;
        var prefab = PrototypeKayKit.Prefab(path);
        if (prefab == null) return null;
        var holder = new GameObject($"Model_{itemId}").transform;
        holder.SetParent(parent, false);
        var model = Object.Instantiate(prefab, holder, false).transform;
        ApplyMaterials(model.gameObject, itemId);
        var bounds = PrototypeKayKit.LocalBounds(path, prefab);
        var longest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        var scale = longest > 0.0001f ? size / longest : 1f;
        model.localScale = Vector3.one * scale;
        var baseY = centered ? bounds.center.y : bounds.min.y;
        model.localPosition = new Vector3(-bounds.center.x * scale, -baseY * scale, -bounds.center.z * scale);
        return holder.gameObject;
    }

    /// <summary>Swaps the imported materials for shared runtime ones (tinting potion liquid, see-through glass).</summary>
    private static void ApplyMaterials(GameObject model, string itemId)
    {
        var isPotion = PrototypePotions.IsPotion(itemId);
        model.GetComponentsInChildren(true, RendererBuffer);
        foreach (var renderer in RendererBuffer)
        {
            renderer.GetSharedMaterials(MaterialBuffer);
            var materials = new Material[MaterialBuffer.Count];
            for (var i = 0; i < materials.Length; i++)
            {
                var source = MaterialBuffer[i];
                var name = source != null ? source.name : "";
                var color = source == null ? Color.grey
                    : source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor")
                    : source.HasProperty("_Color") ? source.color : Color.grey;
                if (isPotion && IsNamed(name, PotionLiquid) && PrototypePotions.Current != null)
                {
                    color = PrototypePotions.Current.TintOf(itemId);
                    materials[i] = Shared($"liquid {color}", color, false, 0.6f);
                }
                else if (isPotion && IsNamed(name, PotionGlass))
                {
                    materials[i] = Shared("glass", new Color(0.85f, 0.92f, 0.95f, 0.35f), true, 0.9f);
                }
                else
                {
                    materials[i] = Shared($"{color}", color, false, 0.15f);
                }
            }
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }
    }

    // Imported material names may carry a suffix such as " (Instance)".
    private static bool IsNamed(string name, string material) => name == material || name.StartsWith(material + " ");

    private static Material Shared(string key, Color color, bool transparent, float smoothness)
    {
        if (Materials.TryGetValue(key, out var material)) return material;
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        material = new Material(shader) { name = $"Item {key}" };
        material.SetColor("_BaseColor", color);
        material.color = color;
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (transparent)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = 3000;
        }
        Materials[key] = material;
        return material;
    }
}
