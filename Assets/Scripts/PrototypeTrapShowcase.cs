using UnityEngine;

/// <summary>
/// Places one visible, non-functional marker for each trap type identified in the
/// original Watabou Pixel Dungeon source overview.
/// </summary>
public sealed class PrototypeTrapShowcase
{
    private enum Glyph
    {
        Orb,
        Pill,
        Bar,
        Cross,
        Beacon,
        Bolt,
        Grasp,
        Ring
    }

    private readonly struct TrapDefinition
    {
        public readonly string Name;
        public readonly Color Color;
        public readonly Vector3 Position;
        public readonly Glyph Glyph;

        public TrapDefinition(string name, Color color, Vector3 position, Glyph glyph)
        {
            Name = name;
            Color = color;
            Position = position;
            Glyph = glyph;
        }
    }

    private static readonly TrapDefinition[] TrapDefinitions =
    {
        new("Fire", new Color(0.95f, 0.2f, 0.05f), new Vector3(-9f, 0f, -5f), Glyph.Orb),
        new("Poison", new Color(0.25f, 0.85f, 0.15f), new Vector3(-3f, 0f, -5f), Glyph.Pill),
        new("Toxic", new Color(0.05f, 0.65f, 0.45f), new Vector3(3f, 0f, -5f), Glyph.Bar),
        new("Paralytic", new Color(0.65f, 0.25f, 0.95f), new Vector3(9f, 0f, -5f), Glyph.Cross),
        new("Alarm", new Color(1f, 0.8f, 0.05f), new Vector3(-9f, 0f, 5f), Glyph.Beacon),
        new("Lightning", new Color(0.15f, 0.55f, 1f), new Vector3(-3f, 0f, 5f), Glyph.Bolt),
        new("Gripping", new Color(0.7f, 0.35f, 0.1f), new Vector3(3f, 0f, 5f), Glyph.Grasp),
        new("Summoning", new Color(0.95f, 0.15f, 0.7f), new Vector3(9f, 0f, 5f), Glyph.Ring)
    };

    public void Build(Transform root)
    {
        var showcaseRoot = new GameObject("TrapShowcase_VisualOnly");
        showcaseRoot.transform.SetParent(root, false);

        foreach (var definition in TrapDefinitions)
        {
            CreateTrapMarker(showcaseRoot.transform, definition);
        }
    }

    private static void CreateTrapMarker(Transform parent, TrapDefinition definition)
    {
        var trapRoot = new GameObject($"Trap_{definition.Name}");
        trapRoot.transform.SetParent(parent, false);
        trapRoot.transform.localPosition = definition.Position;

        var material = CreateMaterial($"Prototype {definition.Name} Trap Material", definition.Color);
        CreateVisualPrimitive("Marker_Base", PrimitiveType.Cube, trapRoot.transform, new Vector3(0f, 0.08f, 0f), new Vector3(1.5f, 0.12f, 1.5f), material);

        switch (definition.Glyph)
        {
            case Glyph.Orb:
                CreateVisualPrimitive("Fire_Orb", PrimitiveType.Sphere, trapRoot.transform, new Vector3(0f, 0.38f, 0f), new Vector3(0.55f, 0.75f, 0.55f), material);
                break;
            case Glyph.Pill:
                CreateVisualPrimitive("Poison_Pill", PrimitiveType.Capsule, trapRoot.transform, new Vector3(0f, 0.4f, 0f), new Vector3(0.45f, 0.5f, 0.45f), material);
                break;
            case Glyph.Bar:
                CreateVisualPrimitive("Toxic_Bar", PrimitiveType.Cube, trapRoot.transform, new Vector3(0f, 0.3f, 0f), new Vector3(0.9f, 0.25f, 0.35f), material);
                CreateVisualPrimitive("Toxic_Bar_Cross", PrimitiveType.Cube, trapRoot.transform, new Vector3(0f, 0.31f, 0f), new Vector3(0.35f, 0.27f, 0.9f), material);
                break;
            case Glyph.Cross:
                CreateVisualPrimitive("Paralytic_Cross_A", PrimitiveType.Cube, trapRoot.transform, new Vector3(0f, 0.28f, 0f), new Vector3(0.95f, 0.22f, 0.28f), material);
                CreateVisualPrimitive("Paralytic_Cross_B", PrimitiveType.Cube, trapRoot.transform, new Vector3(0f, 0.29f, 0f), new Vector3(0.28f, 0.24f, 0.95f), material);
                break;
            case Glyph.Beacon:
                CreateVisualPrimitive("Alarm_Beacon", PrimitiveType.Cylinder, trapRoot.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.32f, 0.5f, 0.32f), material);
                CreateVisualPrimitive("Alarm_Light", PrimitiveType.Sphere, trapRoot.transform, new Vector3(0f, 1.05f, 0f), new Vector3(0.4f, 0.4f, 0.4f), material);
                break;
            case Glyph.Bolt:
                var bolt = CreateVisualPrimitive("Lightning_Bolt", PrimitiveType.Cube, trapRoot.transform, new Vector3(0f, 0.32f, 0f), new Vector3(0.25f, 0.25f, 1.05f), material);
                bolt.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
                break;
            case Glyph.Grasp:
                CreateVisualPrimitive("Gripping_Claw_A", PrimitiveType.Capsule, trapRoot.transform, new Vector3(-0.32f, 0.3f, 0f), new Vector3(0.25f, 0.45f, 0.55f), material);
                CreateVisualPrimitive("Gripping_Claw_B", PrimitiveType.Capsule, trapRoot.transform, new Vector3(0.32f, 0.3f, 0f), new Vector3(0.25f, 0.45f, 0.55f), material);
                break;
            case Glyph.Ring:
                CreateVisualPrimitive("Summoning_Ring", PrimitiveType.Cylinder, trapRoot.transform, new Vector3(0f, 0.18f, 0f), new Vector3(0.65f, 0.08f, 0.65f), material);
                CreateVisualPrimitive("Summoning_Core", PrimitiveType.Sphere, trapRoot.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.35f, 0.35f, 0.35f), material);
                break;
        }
    }

    private static GameObject CreateVisualPrimitive(
        string objectName,
        PrimitiveType primitiveType,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        var visual = GameObject.CreatePrimitive(primitiveType);
        visual.name = objectName;
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = localPosition;
        visual.transform.localScale = localScale;
        visual.GetComponent<Renderer>().sharedMaterial = material;

        var collider = visual.GetComponent<Collider>();
        if (collider != null)
        {
            Object.Destroy(collider);
        }

        return visual;
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
