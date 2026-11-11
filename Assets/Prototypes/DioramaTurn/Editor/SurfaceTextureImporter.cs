using UnityEditor;
using UnityEngine;

namespace Prototypes.DioramaTurn.Editor
{
    /// <summary>
    /// Import rule for the generated wood textures: normal maps are normal maps, the rest is sRGB,
    /// everything repeats and mipmaps, so the plinth holds up at a distance.
    /// </summary>
    public sealed class SurfaceTextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Art/Textures/")) return;

            var importer = (TextureImporter)assetImporter;
            bool normal = assetPath.EndsWith("_normal.png");
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
