using UnityEngine;

/// <summary>
/// Presentation only: a wooden plank door built from simple boxes - five planks of slightly
/// different shades and heights, two iron bands across them, and an iron ring handle on each
/// side. It replaces the look of the door's plain panel cube (the cube keeps its collider).
/// Burning (PrototypeDoor) chars the planks and makes them fall.
/// </summary>
public static class PrototypeDoorModel
{
    public const int PlankCount = 5;

    private static readonly Color[] WoodShades =
    {
        new(0.42f, 0.27f, 0.15f), new(0.37f, 0.23f, 0.12f), new(0.46f, 0.3f, 0.17f)
    };
    private static Material[] wood;
    private static Material iron;

    /// <summary>
    /// Builds the door under `doorRoot` at the panel's place and size. The model's local x runs
    /// across the doorway; `widthAlongZ` turns it for doors in walls running along z.
    /// </summary>
    public static Transform Build(Transform doorRoot, Transform panel, bool widthAlongZ, int seed)
    {
        EnsureMaterials();
        var scale = panel.localScale;
        var width = widthAlongZ ? scale.z : scale.x;
        var height = scale.y;
        var thickness = Mathf.Min(scale.x, scale.z);

        var model = new GameObject("Door_Model").transform;
        model.SetParent(doorRoot, false);
        model.localPosition = panel.localPosition;
        model.localRotation = Quaternion.Euler(0f, widthAlongZ ? 90f : 0f, 0f);

        var random = new System.Random(seed);
        var plankWidth = width / PlankCount;
        for (var i = 0; i < PlankCount; i++)
        {
            // Slightly uneven tops, like hand-cut planks.
            var plankHeight = height - (float)random.NextDouble() * 0.12f;
            var x = -width * 0.5f + plankWidth * (i + 0.5f);
            Box(model, $"Plank_{i}", new Vector3(x, (plankHeight - height) * 0.5f, 0f),
                new Vector3(plankWidth - 0.03f, plankHeight, thickness * 0.8f), wood[random.Next(wood.Length)]);
        }
        foreach (var y in new[] { height * 0.3f, -height * 0.3f })
            Box(model, "Iron_Band", new Vector3(0f, y, 0f), new Vector3(width * 0.98f, 0.16f, thickness * 0.8f + 0.06f), iron);
        foreach (var side in new[] { -1f, 1f })
            Box(model, "Iron_Handle", new Vector3(width * 0.3f, -height * 0.05f, side * (thickness * 0.4f + 0.06f)), new Vector3(0.18f, 0.22f, 0.06f), iron);
        return model;
    }

    private static void Box(Transform parent, string name, Vector3 position, Vector3 size, Material material)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        Object.Destroy(box.GetComponent<Collider>());
        box.transform.SetParent(parent, false);
        box.transform.localPosition = position;
        box.transform.localScale = size;
        box.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void EnsureMaterials()
    {
        if (iron != null) return;
        wood = new Material[WoodShades.Length];
        for (var i = 0; i < wood.Length; i++) wood[i] = PrototypeBootstrap.CreateMaterial($"Door Wood {i}", WoodShades[i]);
        iron = PrototypeBootstrap.CreateMaterial("Door Iron", new Color(0.22f, 0.23f, 0.25f));
        if (iron.HasProperty("_Metallic")) iron.SetFloat("_Metallic", 0.8f);
        if (iron.HasProperty("_Smoothness")) iron.SetFloat("_Smoothness", 0.45f);
    }
}
