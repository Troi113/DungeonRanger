using UnityEditor;
using UnityEngine;

/// <summary>
/// Import settings for the KayKit packs (CC0, Kay Lousberg) under Resources/KayKit:
/// models import without materials (assigned at runtime from the pack textures);
/// character and animation FBX files use Legacy animation so Rig_Medium clips can play
/// on the Ranger through an Animation component. Editor-only.
/// </summary>
public sealed class KayKitImporter : AssetPostprocessor
{
    private const string Root = "Assets/ThirdParty/Resources/KayKit/";

    public override uint GetVersion() => 1;

    private void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(Root)) return;
        var importer = (ModelImporter)assetImporter;
        importer.importCameras = false;
        importer.importLights = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.addCollider = false;
        var isCharacter = assetPath.Contains("/Characters/");
        importer.animationType = isCharacter ? ModelImporterAnimationType.Legacy : ModelImporterAnimationType.None;
        importer.importAnimation = isCharacter;
    }

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Root)) return;
        var importer = (TextureImporter)assetImporter;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
    }
}
