using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Prototypes.PixelInvaders.Tests
{
    public class SurfaceAndModelTests
    {
        const string ShaderPath = "Assets/Prototypes/PixelInvaders/Shaders/PixelInvadersLit.shader";
        const string TexturesFolder = "Assets/Prototypes/PixelInvaders/Art/Textures";
        const string ExportsFolder = "Assets/Prototypes/PixelInvaders/Art/Exports";

        [Test]
        public void Shader_exists_compiles_without_errors_and_is_supported()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Assert.IsNotNull(shader, $"Missing shader at {ShaderPath}");
            Assert.IsTrue(shader.isSupported, "PixelInvadersLit.shader must be supported on target pipeline");

            var messages = ShaderUtil.GetShaderMessages(shader);
            var errors = messages.Where(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();
            Assert.IsEmpty(errors, "Shader has compile errors: " + string.Join("; ", errors.Select(e => e.message)));
        }

        [Test]
        public void Brushed_metal_textures_are_power_of_two_tileable_and_have_valid_normals()
        {
            string albedoPath = $"{TexturesFolder}/metal_deck_albedo.png";
            string normalPath = $"{TexturesFolder}/metal_deck_normal.png";

            var albedoImporter = (TextureImporter)AssetImporter.GetAtPath(albedoPath);
            var normalImporter = (TextureImporter)AssetImporter.GetAtPath(normalPath);
            Assert.IsNotNull(albedoImporter);
            Assert.IsNotNull(normalImporter);
            Assert.AreEqual(TextureImporterType.Default, albedoImporter.textureType);
            Assert.AreEqual(TextureImporterType.NormalMap, normalImporter.textureType);
            Assert.AreEqual(TextureWrapMode.Repeat, albedoImporter.wrapMode);
            Assert.AreEqual(TextureWrapMode.Repeat, normalImporter.wrapMode);

            var albedo = LoadRawPng(albedoPath);
            var normal = LoadRawPng(normalPath);

            try
            {
                Assert.AreEqual(512, albedo.width);
                Assert.AreEqual(512, albedo.height);
                Assert.AreEqual(512, normal.width);
                Assert.AreEqual(512, normal.height);

                // Check non-flat standard deviation on albedo
                Color[] ap = albedo.GetPixels();
                float mean = ap.Average(c => c.grayscale);
                float variance = ap.Average(c => (c.grayscale - mean) * (c.grayscale - mean));
                Assert.Greater(Mathf.Sqrt(variance), 0.02f, "Albedo texture must not be flat");

                // Check seamless tiling: wrap step at x=0/511 is comparable to interior steps
                float seamDiff = 0f;
                float interiorDiff = 0f;
                int w = albedo.width;
                for (int y = 0; y < albedo.height; y++)
                {
                    seamDiff += Mathf.Abs(ap[y * w].grayscale - ap[y * w + (w - 1)].grayscale);
                    interiorDiff += Mathf.Abs(ap[y * w + 100].grayscale - ap[y * w + 101].grayscale);
                }
                seamDiff /= albedo.height;
                interiorDiff /= albedo.height;
                Assert.Less(seamDiff, interiorDiff * 2.5f + 0.02f, "Albedo seam jump is too large");

                // Check normal map vectors point mostly out (+Z)
                Color[] np = normal.GetPixels();
                float avgZ = np.Average(c => c.b * 2f - 1f);
                Assert.Greater(avgZ, 0.85f, "Normal map vectors must point mostly outward (+Z)");
            }
            finally
            {
                Object.DestroyImmediate(albedo);
                Object.DestroyImmediate(normal);
            }
        }

        [TestCase("trench_deck")]
        [TestCase("trench_rail")]
        [TestCase("trench_markers")]
        public void Exported_trench_models_stay_within_budget_and_have_baked_vertex_ao(string modelName)
        {
            string path = $"{ExportsFolder}/{modelName}.glb";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, $"Missing exported model at {path}");

            var filters = prefab.GetComponentsInChildren<MeshFilter>(true);
            Assert.IsNotEmpty(filters, $"{modelName} has no MeshFilter");

            int totalTris = 0;
            foreach (var mf in filters)
            {
                var mesh = mf.sharedMesh;
                Assert.IsNotNull(mesh);
                totalTris += mesh.triangles.Length / 3;
                var colors = mesh.colors;
                Assert.AreEqual(mesh.vertexCount, colors.Length, $"{modelName} must have vertex-colour AO per vertex");
                float minAo = colors.Min(c => c.r);
                float maxAo = colors.Max(c => c.r);
                Assert.Less(minAo, maxAo - 0.04f, $"{modelName} vertex AO must vary between crevices and open faces");
            }
            Assert.LessOrEqual(totalTris, 4000, $"{modelName} triangle count exceeds budget");
        }

        static Texture2D LoadRawPng(string path)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(path));
            return tex;
        }
    }
}
