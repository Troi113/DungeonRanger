using UnityEditor;
using UnityEngine;

/// <summary>
/// Imports PNGs under Assets/ThirdParty as crisp, uncompressed pixel-art UI sprites.
/// Editor-only; it does not run in builds. Source PNG files are never modified.
/// </summary>
public sealed class PixelArtTextureImporter : AssetPostprocessor
{
    private const string PixelArtRoot = "Assets/ThirdParty/";

    // Bump when the import rules change so Unity reimports the affected textures.
    public override uint GetVersion() => 3;

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(PixelArtRoot) || !assetPath.EndsWith(".png")) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.sRGBTexture = true;
        // PrototypeUITheme reads the pixels once to correct IMGUI colors in Linear color space.
        importer.isReadable = true;
    }
}
