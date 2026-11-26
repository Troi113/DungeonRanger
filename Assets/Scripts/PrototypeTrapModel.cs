using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Presentation only: the low-poly dungeon trap (designed in the project's three.js trap
/// customizer) rebuilt in code. Three material slots like the original: Stone_Base (octagonal
/// stone discs), Iron_Trim (ring segments, corner brackets with spikes, emitter claws), and
/// Rune_Accent (rune strips, four small crystals and the center crystal). Rune parts are named
/// "Rune_*" so PrototypeTrap can color and light them per trap type and state.
/// Sizes are in the original model's units (about 2.1 wide); `scale` fits it to a tile.
/// three.js is right-handed, so z positions and y rotations are mirrored for Unity.
/// </summary>
public static class PrototypeTrapModel
{
    private static Material stone;
    private static Material iron;
    private static readonly Dictionary<string, Mesh> Meshes = new();

    public static void Build(Transform parent, Material rune, float scale)
    {
        EnsureMaterials();
        var root = new GameObject("Trap_Model").transform;
        root.SetParent(parent, false);
        root.localScale = Vector3.one * scale;
        const float eighth = Mathf.PI / 8f;

        // Slot 0: Stone_Base - three stacked octagonal discs.
        Add(root, "Stone_Base", Frustum(0.94f, 1.05f, 0.16f, 8), stone, 0f, 0.08f, 0f, eighth);
        Add(root, "Stone_Step", Frustum(0.74f, 0.80f, 0.08f, 8), stone, 0f, 0.19f, 0f, eighth);
        Add(root, "Stone_Core", Frustum(0.28f, 0.34f, 0.10f, 8), stone, 0f, 0.27f, 0f, eighth);

        // Slot 1: Iron_Trim - octagonal ring segments.
        for (var i = 0; i < 8; i++)
        {
            var a = i * Mathf.PI * 2f / 8f + eighth;
            Add(root, "Iron_Ring", Box(0.58f, 0.05f, 0.07f), iron, Mathf.Cos(a) * 0.74f, 0.24f, Mathf.Sin(a) * 0.74f, -a + Mathf.PI / 2f);
        }
        // Four corner brackets, each with a spike.
        for (var i = 0; i < 4; i++)
        {
            var a = i * Mathf.PI * 2f / 4f + Mathf.PI / 4f;
            float bx = 0.88f * Mathf.Cos(a), bz = 0.88f * Mathf.Sin(a);
            Add(root, "Iron_Bracket", Box(0.22f, 0.18f, 0.14f), iron, bx, 0.15f, bz, -a);
            Add(root, "Iron_Spike", Frustum(0f, 0.05f, 0.10f, 4), iron, bx, 0.29f, bz, Mathf.PI / 4f);
        }
        // Four emitter claws around the center.
        for (var i = 0; i < 4; i++)
        {
            var a = i * Mathf.PI * 2f / 4f;
            Add(root, "Iron_Claw", Box(0.06f, 0.22f, 0.05f), iron, 0.23f * Mathf.Cos(a), 0.40f, 0.23f * Mathf.Sin(a), -a);
        }

        // Slot 2: Rune_Accent - rune strips, four small crystals, the center crystal.
        for (var i = 0; i < 8; i++)
        {
            var a = i * Mathf.PI * 2f / 8f + eighth;
            Add(root, "Rune_Strip", Box(0.49f, 0.03f, 0.085f), rune, Mathf.Cos(a) * 0.625f, 0.245f, Mathf.Sin(a) * 0.625f, -a + Mathf.PI / 2f);
        }
        for (var i = 0; i < 4; i++)
        {
            var a = i * Mathf.PI * 2f / 4f;
            Add(root, "Rune_Gem", Octahedron(0.065f), rune, 0.625f * Mathf.Cos(a), 0.30f, 0.625f * Mathf.Sin(a), a);
        }
        var core = Add(root, "Rune_Core", Octahedron(0.18f), rune, 0f, 0.52f, 0f, 0f);
        core.localScale = new Vector3(0.85f, 1.35f, 0.85f);
    }

    private static Transform Add(Transform root, string name, Mesh mesh, Material material, float x, float y, float z, float rotationY)
    {
        var part = new GameObject(name);
        part.transform.SetParent(root, false);
        part.transform.localPosition = new Vector3(x, y, -z);
        // Mirroring z also reverses the turning direction around y.
        part.transform.localRotation = Quaternion.Euler(0f, -rotationY * Mathf.Rad2Deg, 0f);
        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = part.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        return part.transform;
    }

    private static void EnsureMaterials()
    {
        if (stone != null) return;
        stone = PrototypeBootstrap.CreateMaterial("Trap Stone", new Color(0x48 / 255f, 0x45 / 255f, 0x4a / 255f));
        if (stone.HasProperty("_Smoothness")) stone.SetFloat("_Smoothness", 0.12f);
        iron = PrototypeBootstrap.CreateMaterial("Trap Iron", new Color(0x38 / 255f, 0x3d / 255f, 0x45 / 255f));
        if (iron.HasProperty("_Metallic")) iron.SetFloat("_Metallic", 0.85f);
        if (iron.HasProperty("_Smoothness")) iron.SetFloat("_Smoothness", 0.58f);
    }

    // ---------------- Flat-shaded meshes (three.js geometry equivalents) ----------------

    /// <summary>CylinderGeometry(radiusTop, radiusBottom, height, sides), centered; radiusTop 0 = cone.</summary>
    private static Mesh Frustum(float top, float bottom, float height, int sides)
    {
        var key = $"F{top}_{bottom}_{height}_{sides}";
        if (Meshes.TryGetValue(key, out var cached)) return cached;
        var tris = new List<Vector3>();
        float h = height * 0.5f;
        for (var i = 0; i < sides; i++)
        {
            // three.js starts its segments at +z and winds around; mirror z for Unity.
            float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
            Vector3 b0 = Ring(a0, bottom, -h), b1 = Ring(a1, bottom, -h), t0 = Ring(a0, top, h), t1 = Ring(a1, top, h);
            Quad(tris, b0, b1, t1, t0);
            Tri(tris, new Vector3(0f, h, 0f), t1, t0);
            Tri(tris, new Vector3(0f, -h, 0f), b0, b1);
        }
        return Meshes[key] = Build(key, tris);
    }

    private static Vector3 Ring(float angle, float radius, float y) => new(Mathf.Sin(angle) * radius, y, -Mathf.Cos(angle) * radius);

    private static Mesh Box(float x, float y, float z)
    {
        var key = $"B{x}_{y}_{z}";
        if (Meshes.TryGetValue(key, out var cached)) return cached;
        float hx = x * 0.5f, hy = y * 0.5f, hz = z * 0.5f;
        var c = new Vector3[8];
        for (var i = 0; i < 8; i++) c[i] = new Vector3((i & 1) == 0 ? -hx : hx, (i & 2) == 0 ? -hy : hy, (i & 4) == 0 ? -hz : hz);
        var tris = new List<Vector3>();
        Quad(tris, c[0], c[2], c[3], c[1]); // back (-z)
        Quad(tris, c[4], c[5], c[7], c[6]); // front (+z)
        Quad(tris, c[0], c[4], c[6], c[2]); // left
        Quad(tris, c[1], c[3], c[7], c[5]); // right
        Quad(tris, c[2], c[6], c[7], c[3]); // top
        Quad(tris, c[0], c[1], c[5], c[4]); // bottom
        return Meshes[key] = Build(key, tris);
    }

    private static Mesh Octahedron(float radius)
    {
        var key = $"O{radius}";
        if (Meshes.TryGetValue(key, out var cached)) return cached;
        Vector3 up = Vector3.up * radius, down = Vector3.down * radius;
        Vector3[] ring = { Vector3.right * radius, Vector3.forward * radius, Vector3.left * radius, Vector3.back * radius };
        var tris = new List<Vector3>();
        for (var i = 0; i < 4; i++)
        {
            var a = ring[i];
            var b = ring[(i + 1) % 4];
            Tri(tris, up, b, a);
            Tri(tris, down, a, b);
        }
        return Meshes[key] = Build(key, tris);
    }

    private static void Tri(List<Vector3> tris, Vector3 a, Vector3 b, Vector3 c) { tris.Add(a); tris.Add(b); tris.Add(c); }

    private static void Quad(List<Vector3> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        Tri(tris, a, b, c);
        Tri(tris, a, c, d);
    }

    /// <summary>Unshared vertices per triangle give flat shading (like three.js flatShading).</summary>
    private static Mesh Build(string name, List<Vector3> tris)
    {
        var mesh = new Mesh { name = name };
        mesh.SetVertices(tris);
        var indices = new int[tris.Count];
        for (var i = 0; i < indices.Length; i++) indices[i] = i;
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        // Faces built with either winding are fixed below so every face points outward.
        FixWinding(mesh);
        return mesh;
    }

    /// <summary>Flips any triangle whose normal points toward the mesh center (all parts are convex).</summary>
    private static void FixWinding(Mesh mesh)
    {
        var vertices = mesh.vertices;
        var triangles = mesh.triangles;
        var center = mesh.bounds.center;
        for (var i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a = vertices[triangles[i]], b = vertices[triangles[i + 1]], c = vertices[triangles[i + 2]];
            var normal = Vector3.Cross(b - a, c - a);
            if (Vector3.Dot(normal, (a + b + c) / 3f - center) < 0f) (triangles[i + 1], triangles[i + 2]) = (triangles[i + 2], triangles[i + 1]);
        }
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
}
