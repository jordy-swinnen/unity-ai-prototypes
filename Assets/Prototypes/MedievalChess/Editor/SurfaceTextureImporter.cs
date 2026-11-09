using UnityEditor;
using UnityEngine;

namespace Prototypes.MedievalChess.Editor
{
    /// <summary>
    /// Imports the generated surface textures the way the shader expects: "_normal" files as normal maps, the rest
    /// as colour, all repeating with mipmaps and anisotropic filtering. Runs on import, so a new texture from
    /// Tools/textures/MedievalChess/make_textures.py needs no clicks.
    /// </summary>
    public sealed class SurfaceTextureImporter : AssetPostprocessor
    {
        const string Folder = "Assets/Prototypes/MedievalChess/Art/Textures/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var importer = (TextureImporter)assetImporter;
            bool normal = assetPath.EndsWith("_normal.png");
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normal;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 8;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }
    }
}
