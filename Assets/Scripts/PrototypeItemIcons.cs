using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Presentation only: inventory icons rendered from the same 3D models the items use on the
/// map (PrototypeItemVisuals models, or the colored block for items without a model), so the
/// UI and the world match. Each icon is rendered once by a hidden camera on its own layer
/// (potions once per color) and cached. Rendering happens in LateUpdate, outside the GUI pass;
/// until an icon is ready, Get returns null and the caller shows its old icon for a frame.
/// </summary>
public sealed class PrototypeItemIcons : MonoBehaviour
{
    private const int Layer = 31;
    private const int Size = 128;
    private static readonly Vector3 StagePosition = new(0f, -1000f, 0f);

    private static PrototypeItemIcons instance;
    private static readonly Dictionary<string, Texture2D> Icons = new();
    private static readonly HashSet<string> Pending = new();
    private static readonly List<Renderer> RendererBuffer = new();

    private Camera stageCamera;
    private RenderTexture target;
    private Material blockMaterial;

    /// <summary>The rendered icon for an item, or null while it is being made.</summary>
    public static Texture2D Get(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        var key = Key(itemId);
        if (Icons.TryGetValue(key, out var icon)) return icon;
        if (instance == null)
        {
            var host = new GameObject("Item_Icon_Stage");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<PrototypeItemIcons>();
        }
        Pending.Add(itemId);
        return null;
    }

    // Potions are rendered per run color, so the key includes the tint.
    private static string Key(string itemId) =>
        PrototypePotions.IsPotion(itemId) && PrototypePotions.Current != null ? $"{itemId}|{PrototypePotions.Current.TintOf(itemId)}" : itemId;

    private void Awake()
    {
        transform.position = StagePosition;
        target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32) { name = "Item Icon Target", antiAliasing = 1 };
        stageCamera = new GameObject("Icon_Camera").AddComponent<Camera>();
        stageCamera.transform.SetParent(transform, false);
        stageCamera.enabled = false;
        stageCamera.orthographic = true;
        stageCamera.cullingMask = 1 << Layer;
        stageCamera.clearFlags = CameraClearFlags.SolidColor;
        stageCamera.backgroundColor = new Color(1f, 0f, 1f, 0f);
        stageCamera.allowHDR = false;
        stageCamera.allowMSAA = false;
        stageCamera.nearClipPlane = 0.01f;
        stageCamera.farClipPlane = 20f;
        stageCamera.targetTexture = target;

        var key = new GameObject("Icon_Key_Light").AddComponent<Light>();
        key.transform.SetParent(transform, false);
        key.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
        key.type = LightType.Directional;
        key.intensity = 1.6f;
        key.cullingMask = 1 << Layer;
        var fill = new GameObject("Icon_Fill_Light").AddComponent<Light>();
        fill.transform.SetParent(transform, false);
        fill.transform.rotation = Quaternion.Euler(10f, 150f, 0f);
        fill.type = LightType.Directional;
        fill.intensity = 0.6f;
        fill.cullingMask = 1 << Layer;
    }

    private void LateUpdate()
    {
        if (Pending.Count == 0) return;
        // A few per frame keeps opening the inventory smooth.
        var done = 0;
        var ids = new List<string>(Pending);
        foreach (var itemId in ids)
        {
            Pending.Remove(itemId);
            var key = Key(itemId);
            if (!Icons.ContainsKey(key)) Icons[key] = Render(itemId);
            if (++done >= 4) break;
        }
    }

    private Texture2D Render(string itemId)
    {
        var holder = new GameObject($"Icon_{itemId}").transform;
        holder.SetParent(transform, false);
        var model = PrototypeItemVisuals.Create(itemId, holder, 1f, true);
        var upright = PrototypePotions.IsPotion(itemId) || itemId == "food";
        if (model == null) BuildBlock(itemId, holder);
        // Long items (weapons, key) lie diagonally like classic item icons; potions and bread stand in a 3/4 view.
        holder.localRotation = model != null && !upright
            ? Quaternion.Euler(0f, 0f, -45f) * Quaternion.Euler(0f, 25f, 0f)
            : Quaternion.Euler(12f, 35f, 0f);
        foreach (var t in holder.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layer;

        // Frame the model.
        holder.GetComponentsInChildren(true, RendererBuffer);
        var bounds = new Bounds(holder.position, Vector3.zero);
        var first = true;
        foreach (var renderer in RendererBuffer)
        {
            if (first) { bounds = renderer.bounds; first = false; }
            else bounds.Encapsulate(renderer.bounds);
        }
        stageCamera.transform.position = bounds.center - Vector3.forward * 5f;
        stageCamera.transform.rotation = Quaternion.identity;
        stageCamera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y) * 1.12f + 0.01f;

        stageCamera.Render();
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        var read = new Texture2D(Size, Size, TextureFormat.RGBA32, false, false);
        read.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
        read.Apply(false, false);
        RenderTexture.active = previous;
        DestroyImmediate(holder.gameObject);

        // Clear the background (works whether or not the pipeline keeps alpha), then match the
        // UI's color handling: IMGUI in Linear space needs linearized pixels (see PrototypeUITheme).
        var pixels = read.GetPixels();
        var linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
        for (var i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            var isBackground = p.a < 0.02f || (p.r > 0.97f && p.g < 0.03f && p.b > 0.97f);
            if (isBackground) { pixels[i] = Color.clear; continue; }
            p.a = 1f;
            pixels[i] = linear ? p.linear : p;
        }
        read.SetPixels(pixels);
        read.filterMode = FilterMode.Bilinear;
        read.wrapMode = TextureWrapMode.Clamp;
        read.name = $"Item Icon {itemId}";
        read.Apply(false, true);
        return read;
    }

    /// <summary>The colored block the item uses on the floor when it has no model (scrolls are flat).</summary>
    private void BuildBlock(string itemId, Transform holder)
    {
        PrototypeItemCatalog.Describe(itemId, out _, out var color);
        var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        DestroyImmediate(block.GetComponent<Collider>());
        block.transform.SetParent(holder, false);
        block.transform.localScale = PrototypeScrolls.IsScroll(itemId) ? new Vector3(0.8f, 0.4f, 1.1f) : new Vector3(0.7f, 1.1f, 0.7f);
        if (blockMaterial == null) blockMaterial = PrototypeBootstrap.CreateMaterial("Icon Block", Color.white);
        var renderer = block.GetComponent<Renderer>();
        renderer.sharedMaterial = blockMaterial;
        var block_ = new MaterialPropertyBlock();
        block_.SetColor("_BaseColor", color);
        renderer.SetPropertyBlock(block_);
    }
}
