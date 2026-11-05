using Alice.Core;
using UnityEditor;
using UnityEngine;

namespace Alice.Editor
{
    /// <summary>
    /// Import settings for character sprites, so nobody sets them by hand in the inspector:
    /// whole pixels per unit, point filter, no compression or mipmaps, pivot between the feet.
    /// </summary>
    public sealed class AliceCharacterArtPostprocessor : AssetPostprocessor
    {
        private const string CharactersFolder = "Assets/_Alice/Art/Characters/";
        // HUD icons are pixel art too and need the same crisp import; the UI ignores the pivot.
        private const string UiFolder = "Assets/_Alice/Art/UI/";
        // World props such as the rest armchair stand on the floor like characters and share their pivot.
        private const string WorldFolder = "Assets/_Alice/Art/World/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(CharactersFolder) && !assetPath.StartsWith(UiFolder)
                && !assetPath.StartsWith(WorldFolder))
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = WorldMetrics.PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
        }
    }
}
