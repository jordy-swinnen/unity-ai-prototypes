using UnityEditor;
using UnityEngine;

namespace Prototypes.PixelInvaders.Editor
{
    /// <summary>
    /// Import rule for generated brushed-metal textures under PixelInvaders/Art/Textures/:
    /// normal maps are NormalMap, albedo maps are sRGB, Repeat, Trilinear with mipmaps.
    /// </summary>
    public sealed class SurfaceTextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            string normalized = assetPath.Replace('\\', '/');
            if (!normalized.Contains("/PixelInvaders/Art/Textures/")) return;

            var importer = (TextureImporter)assetImporter;
            bool normal = normalized.EndsWith("_normal.png");
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normal;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.mipmapEnabled = true;
            importer.anisoLevel = 2;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }
    }
}
