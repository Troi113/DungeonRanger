using UnityEditor;
using UnityEngine;

/// <summary>
/// Import settings for the third-party Rat Pack: mesh only (no camera, lights, animation,
/// or materials) and a point-filtered palette texture so the flat low-poly colors stay clean.
/// Editor-only; materials are assigned at runtime by PrototypeMobModels.
/// </summary>
public sealed class RatPackImporter : AssetPostprocessor
{
    private const string RatPackRoot = "Assets/ThirdParty/Resources/RatPack/";

    public override uint GetVersion() => 1;

    private void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(RatPackRoot)) return;
        var importer = (ModelImporter)assetImporter;
        importer.importCameras = false;
        importer.importLights = false;
        importer.importAnimation = false;
        importer.animationType = ModelImporterAnimationType.None;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.addCollider = false;
    }

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(RatPackRoot)) return;
        var importer = (TextureImporter)assetImporter;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
    }
}
