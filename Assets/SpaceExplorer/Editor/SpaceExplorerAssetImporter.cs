using UnityEditor;
using UnityEngine;

// Applies consistent sprite settings only to this lab's artwork.
public sealed class SpaceExplorerAssetImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/SpaceExplorer/") || !assetPath.EndsWith(".png")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.spritePixelsPerUnit = assetPath.Contains("/Backgrounds/") ? 100 : 256;
        if (assetPath.Contains("/UI/"))
            importer.spriteBorder = new Vector4(12, 12, 12, 12);
    }

    [MenuItem("Tools/Space Explorer/Reimport Artwork")]
    private static void ReimportArtwork()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/SpaceExplorer" }))
            AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
    }
}
