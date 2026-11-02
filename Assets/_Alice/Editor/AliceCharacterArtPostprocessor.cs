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

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(CharactersFolder))
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
