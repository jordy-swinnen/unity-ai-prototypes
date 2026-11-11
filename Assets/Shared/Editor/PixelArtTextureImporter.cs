using UnityEditor;
using UnityEngine;

namespace Shared.Editor
{
    /// <summary>
    /// Keeps pixel art sharp. Every texture under an Art/Sprites folder (written by an Aseprite script,
    /// see Tools/aseprite) is imported as a Sprite with Point filtering, no compression and no mipmaps.
    /// The settings are applied on every import, so a change made by hand in the Inspector does not stick.
    /// A new texture comes in as one Single sprite (Unity would otherwise pick Multiple, which gives no
    /// sprite until it is sliced). That mode is set on the first import only, so a prototype can later
    /// switch a sheet to Multiple and slice it from the sheet's JSON without this rule undoing it.
    /// </summary>
    public sealed class PixelArtTextureImporter : AssetPostprocessor
    {
        const string SpritesFolder = "/Art/Sprites/";

        /// <summary>True for an asset path such as Assets/Prototypes/Demo/Art/Sprites/hero.png.</summary>
        public static bool IsSpritePath(string assetPath)
        {
            return !string.IsNullOrEmpty(assetPath) && assetPath.Contains(SpritesFolder);
        }

        void OnPreprocessTexture()
        {
            if (!IsSpritePath(assetPath))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            if (importer.importSettingsMissing)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
            }
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
        }
    }
}
