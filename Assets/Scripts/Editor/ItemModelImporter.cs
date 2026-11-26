using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

/// <summary>
/// Import settings for the item models under Resources/Items (Poly Pizza OBJ files, see
/// Docs/CREDITS.md): no cameras, lights, colliders or animation, and each OBJ material becomes a
/// URP Lit material carrying the MTL diffuse color (and opacity). PrototypeItemVisuals reads
/// those colors at runtime. Editor-only.
/// </summary>
public sealed class ItemModelImporter : AssetPostprocessor
{
    private const string Root = "Assets/ThirdParty/Resources/Items/";

    public override uint GetVersion() => 1;
    // Runs after the render pipeline's own material conversion, so these settings win.
    public override int GetPostprocessOrder() => 100;

    private void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(Root)) return;
        var importer = (ModelImporter)assetImporter;
        importer.importCameras = false;
        importer.importLights = false;
        importer.addCollider = false;
        importer.animationType = ModelImporterAnimationType.None;
        importer.importAnimation = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
    }

    private void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] animations)
    {
        if (!assetPath.StartsWith(Root)) return;
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader != null) material.shader = shader;
        var color = Color.white;
        if (description.TryGetProperty("DiffuseColor", out Vector4 diffuse)) color = new Color(diffuse.x, diffuse.y, diffuse.z, 1f);
        if (description.TryGetProperty("Opacity", out float opacity)) color.a = opacity;
        else if (description.TryGetProperty("TransparencyFactor", out float transparency)) color.a = 1f - transparency;
        material.SetColor("_BaseColor", color);
        material.color = color;
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.2f);
    }
}
