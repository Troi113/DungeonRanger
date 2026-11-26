using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Presentation-only IMGUI helpers that draw the prototype UI with the
/// Free Medieval Fantasy UI Pack (Kibyra). Textures load once and are cached.
/// Pack layout: Assets/ThirdParty/Resources/FreeMedievalFantasyUIPack/.
/// </summary>
public static class PrototypeUITheme
{
    private const string PackRoot = "FreeMedievalFantasyUIPack/";
    private const float FrameScale = 3f;

    public static readonly Color Parchment = new(0.97f, 0.9f, 0.72f);
    public static readonly Color Gold = new(1f, 0.82f, 0.38f);
    public static readonly Color Muted = new(0.75f, 0.68f, 0.55f);
    public static readonly Color Danger = new(1f, 0.42f, 0.32f);
    public static readonly Color Warning = new(1f, 0.68f, 0.3f);
    private static readonly Color Shadow = new(0f, 0f, 0f, 0.85f);
    private static readonly Color HighlightTint = new(1f, 0.85f, 0.45f);
    private static readonly Color HoverTint = new(1f, 0.93f, 0.78f);

    private static readonly Dictionary<string, Texture2D> Cache = new();
    private static readonly Dictionary<int, GUIStyle> Styles = new();
    private static readonly GUIContent SharedContent = new();

    // Frame sources: (texture, opaque bounds in pixels from the top-left, 9-slice border).
    private static readonly RectInt PanelBounds = new(3, 3, 26, 25);
    private static readonly RectInt PlankBounds = new(4, 3, 24, 25);
    private static readonly RectInt IronBounds = new(4, 4, 24, 24);

    // ---------------- Texture lookup ----------------

    /// <summary>set 1-3 = "UI Icons", "UI Icons 2", "UI Icons 3"; index is the 1-based, row-major sheet position.</summary>
    public static Texture2D Icon(int set, int index)
    {
        var folder = set == 1 ? "UI Icons" : $"UI Icons {set}";
        return Load($"{PackRoot}{folder}/icon{index}");
    }

    public static Texture2D MenuButton(int index) => Load($"{PackRoot}UI Main Menu Icons/mainmenuicon{index}");

    private static Texture2D Load(string path)
    {
        if (Cache.TryGetValue(path, out var texture)) return texture;
        texture = Resources.Load<Texture2D>(path);
        if (texture == null) Debug.LogWarning($"UI theme texture missing: Resources/{path}");
        else if (QualitySettings.activeColorSpace == ColorSpace.Linear) texture = ToLinearCopy(texture);
        Cache[path] = texture;
        return texture;
    }

    /// <summary>
    /// IMGUI in a Linear color-space project does not sRGB-decode textures but does encode
    /// its output, which washes the art out. A one-time copy with linearized pixels
    /// (made when first loaded, then cached) draws with the original PNG colors.
    /// </summary>
    private static Texture2D ToLinearCopy(Texture2D source)
    {
        if (!source.isReadable)
        {
            Debug.LogWarning($"UI theme texture '{source.name}' is not readable; colors may look washed out.");
            return source;
        }
        var pixels = source.GetPixels();
        for (var i = 0; i < pixels.Length; i++) pixels[i] = pixels[i].linear;
        var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, false)
        {
            name = source.name + " (UI linear)",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        copy.SetPixels(pixels);
        copy.Apply(false, false);
        return copy;
    }

    public static Texture2D Heart => Icon(3, 1);
    public static Texture2D BrokenHeart => Icon(3, 2);
    public static Texture2D Bread => Icon(3, 41);
    public static Texture2D Castle => Icon(3, 62);
    public static Texture2D Skull => Icon(3, 57);
    public static Texture2D Bag => Icon(3, 33);
    public static Texture2D Star => Icon(3, 25);
    public static Texture2D Crown => Icon(3, 63);
    public static Texture2D Ring => Icon(3, 64);
    public static Texture2D Trophy => Icon(1, 53);
    public static Texture2D Pause => Icon(1, 7);
    public static Texture2D Check => Icon(1, 8);
    public static Texture2D Cross => Icon(1, 2);
    public static Texture2D Bow => Icon(3, 11);
    public static Texture2D Sword => Icon(3, 9);
    public static Texture2D Chainmail => Icon(1, 24);
    public static Texture2D Exclamation => Icon(3, 50);

    /// <summary>Approximate icon for each prototype item id; null when none fits.</summary>
    public static Texture2D ItemIcon(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        if (PrototypeScrolls.IsScroll(itemId)) return Icon(3, 32);
        return itemId switch
        {
            "ranger_bow" => Bow,
            "health_potion" => Icon(3, 17),
            "food" => Bread,
            "key" => Icon(3, 29),
            "knuckleduster" => Icon(3, 14),
            "knife" => Icon(3, 10),
            "sword" => Sword,
            "sickle" => Icon(2, 38),
            "axe" => Icon(1, 18),
            "spear" => Icon(3, 16),
            "cloth_armor" => Icon(3, 36),
            "leather_armor" => Icon(1, 20),
            "mail_armor" => Chainmail,
            _ => null
        };
    }

    /// <summary>Faded placeholder shown in an empty equipment slot.</summary>
    public static Texture2D EquipmentPlaceholder(int column) => column switch
    {
        0 => Sword,
        1 => Chainmail,
        _ => Ring
    };

    // ---------------- Drawing ----------------

    /// <summary>Virtual screen size after BeginScaledGui; use instead of Screen.width/height.</summary>
    public static float Width { get; private set; } = 1280f;
    public static float Height { get; private set; } = 720f;

    /// <summary>
    /// Scales all following IMGUI drawing (and mouse input) so layouts designed for a
    /// ~800px-tall screen fit small windows and stay readable on large ones.
    /// </summary>
    public static void BeginScaledGui(float referenceHeight = 800f)
    {
        var scale = Mathf.Clamp(Screen.height / referenceHeight, 0.5f, 2f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        Width = Screen.width / scale;
        Height = Screen.height / scale;
    }

    /// <summary>
    /// IMGUI treats colors as linear in a Linear color-space project, so authored
    /// (sRGB) colors are converted here to show as written.
    /// </summary>
    private static Color Ui(Color color) =>
        QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color;

    public static void Fill(Rect rect, Color color)
    {
        var old = GUI.color;
        GUI.color = Ui(color);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    public static void DrawPanel(Rect rect) => DrawNineSlice(rect, Icon(2, 58), PanelBounds, 7, Color.white);

    public static void DrawSlot(Rect rect, bool equipment, bool selected)
    {
        var tint = selected ? HighlightTint : Color.white;
        if (equipment) DrawNineSlice(rect, Icon(2, 51), IronBounds, 6, tint);
        else DrawNineSlice(rect, Icon(2, 58), PanelBounds, 7, tint);
        if (selected) DrawOutline(rect, Gold, 3f);
    }

    public static void DrawIcon(Rect rect, Texture2D texture, float alpha = 1f)
    {
        if (texture == null) return;
        var old = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
        GUI.color = old;
    }

    /// <summary>Wood-plank button. Returns true when clicked.</summary>
    public static bool Button(Rect rect, string label, bool selected = false, bool enabled = true)
    {
        var hovered = enabled && rect.Contains(Event.current.mousePosition);
        var tint = !enabled ? new Color(0.55f, 0.55f, 0.55f) : selected ? HighlightTint : hovered ? HoverTint : Color.white;
        DrawNineSlice(rect, Icon(2, 59), PlankBounds, 5, tint);
        if (selected) DrawOutline(rect, Gold, 2f);
        Label(rect, label, 16, selected ? Gold : Parchment, TextAnchor.MiddleCenter, true);
        return enabled && GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    /// <summary>Picture button (for the 64×32 main-menu art). Returns true when clicked.</summary>
    public static bool ImageButton(Rect rect, Texture2D texture)
    {
        var hovered = rect.Contains(Event.current.mousePosition);
        var old = GUI.color;
        GUI.color = hovered ? Ui(HoverTint) : Color.white;
        if (texture != null) GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
        GUI.color = old;
        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    public static void Label(Rect rect, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft, bool bold = false, bool wrap = false)
    {
        if (string.IsNullOrEmpty(text)) return;
        var style = GetStyle(size, anchor, bold, wrap);
        SharedContent.text = text;
        style.normal.textColor = Shadow;
        GUI.Label(new Rect(rect.x + 1f, rect.y + 2f, rect.width, rect.height), SharedContent, style);
        style.normal.textColor = Ui(color);
        GUI.Label(rect, SharedContent, style);
    }

    public static void DrawBar(Rect rect, float fraction, Color fill)
    {
        Fill(rect, new Color(0.08f, 0.05f, 0.04f, 0.95f));
        var inner = new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f);
        Fill(new Rect(inner.x, inner.y, inner.width * Mathf.Clamp01(fraction), inner.height), fill);
        DrawOutline(rect, new Color(0.35f, 0.22f, 0.12f), 2f);
    }

    public static void DrawOutline(Rect rect, Color color, float thickness)
    {
        Fill(new Rect(rect.x, rect.y, rect.width, thickness), color);
        Fill(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        Fill(new Rect(rect.x, rect.y, thickness, rect.height), color);
        Fill(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    private static GUIStyle GetStyle(int size, TextAnchor anchor, bool bold, bool wrap)
    {
        var key = size * 100 + (int)anchor * 4 + (bold ? 2 : 0) + (wrap ? 1 : 0);
        if (Styles.TryGetValue(key, out var style)) return style;
        style = new GUIStyle
        {
            fontSize = size,
            alignment = anchor,
            fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
            wordWrap = wrap,
            clipping = TextClipping.Clip
        };
        Styles[key] = style;
        return style;
    }

    /// <summary>
    /// Draws a pixel-art frame stretched to any size: corners stay a fixed size,
    /// edges and center stretch. Source rects are in pixels from the texture's top-left.
    /// </summary>
    private static void DrawNineSlice(Rect rect, Texture2D texture, RectInt source, int border, Color tint)
    {
        if (texture == null)
        {
            Fill(rect, new Color(0.18f, 0.11f, 0.07f, 0.95f));
            return;
        }

        var old = GUI.color;
        GUI.color = Ui(tint);
        var b = Mathf.Min(border * FrameScale, rect.width * 0.5f, rect.height * 0.5f);
        float[] xs = { rect.x, rect.x + b, rect.xMax - b, rect.xMax };
        float[] ys = { rect.y, rect.y + b, rect.yMax - b, rect.yMax };
        int[] sx = { source.x, source.x + border, source.xMax - border, source.xMax };
        int[] sy = { source.y, source.y + border, source.yMax - border, source.yMax };
        float w = texture.width, h = texture.height;
        for (var row = 0; row < 3; row++)
        for (var col = 0; col < 3; col++)
        {
            var dest = Rect.MinMaxRect(xs[col], ys[row], xs[col + 1], ys[row + 1]);
            if (dest.width <= 0f || dest.height <= 0f) continue;
            // Unity UVs start at the bottom-left; source rows are counted from the top.
            var uv = Rect.MinMaxRect(sx[col] / w, 1f - sy[row + 1] / h, sx[col + 1] / w, 1f - sy[row] / h);
            GUI.DrawTextureWithTexCoords(dest, texture, uv, true);
        }
        GUI.color = old;
    }
}
