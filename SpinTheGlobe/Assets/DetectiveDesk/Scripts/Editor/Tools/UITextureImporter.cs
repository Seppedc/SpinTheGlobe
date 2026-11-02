using UnityEditor;

namespace DetectiveDesk.Editor
{
    /// <summary>
    /// Import settings for the runtime UI assets in Art/UI/Resources/UI:
    /// sprites crisp, uncompressed, no mipmaps; fonts dynamic with OS fallbacks for symbols the serif fonts lack (★ ▸).
    /// </summary>
    public sealed class UITextureImporter : AssetPostprocessor
    {
        const string Folder = "Assets/DetectiveDesk/Art/UI/Resources/UI/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
        }

        void OnPreprocessAsset()
        {
            if (!assetPath.StartsWith(Folder) || !(assetImporter is TrueTypeFontImporter font)) return;
            font.fontTextureCase = FontTextureCase.Dynamic;
            font.includeFontData = true;
            font.fontNames = new[] { "Segoe UI Symbol", "Segoe UI", "Arial" };
        }
    }
}
