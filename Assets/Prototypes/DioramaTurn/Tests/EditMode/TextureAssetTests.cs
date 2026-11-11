using System.IO;
using System.Linq;
using NUnit.Framework;
using Prototypes.DioramaTurn.Editor;
using UnityEditor;
using UnityEngine;

namespace Prototypes.DioramaTurn.Tests
{
    public class TextureAssetTests
    {
        const string ShaderName = DioramaTurnSceneBuilder.SurfaceShaderName;
        const string TextureFolder = "Assets/Prototypes/DioramaTurn/Art/Textures";
        const string ModelFolder = "Assets/Prototypes/DioramaTurn/Art/Exports";

        [Test]
        public void The_shader_compiles_and_has_the_properties_the_code_sets()
        {
            var shader = Shader.Find(ShaderName);
            Assert.IsNotNull(shader, $"no shader named {ShaderName}");
            var errors = ShaderUtil.GetShaderMessages(shader)
                .Where(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)
                .ToArray();
            Assert.IsEmpty(errors, string.Join("\n", errors.Select(m => $"{m.file}:{m.line} {m.message}")));
            Assert.IsTrue(shader.isSupported);
            foreach (var property in new[]
                     {
                         "_BaseColor", "_BaseMap", "_NormalMap", "_TextureStrength", "_NormalStrength",
                         "_MapScale", "_WorldMapping", "_Metallic", "_Smoothness", "_AoStrength",
                     })
                Assert.GreaterOrEqual(shader.FindPropertyIndex(property), 0, $"the shader has no property {property}");
        }

        static Texture2D Read(string file)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), TextureFolder, file);
            Assert.IsTrue(File.Exists(path), $"{file} is missing. Run Tools/textures/DioramaTurn/make_textures.py");
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            Assert.IsTrue(ImageConversion.LoadImage(texture, File.ReadAllBytes(path)));
            return texture;
        }

        [Test]
        public void The_wood_textures_are_square_512_and_not_flat()
        {
            foreach (var file in new[] { "wood_albedo.png", "wood_normal.png" })
            {
                var texture = Read(file);
                Assert.AreEqual(512, texture.width, file);
                Assert.AreEqual(512, texture.height, file);
                var pixels = texture.GetPixels32();
                double mean = pixels.Average(p => p.r / 255.0);
                double sd = System.Math.Sqrt(pixels.Average(p => System.Math.Pow(p.r / 255.0 - mean, 2)));
                Assert.Greater(sd, 0.01, $"{file} is nearly flat");
            }
        }

        [Test]
        public void The_wood_texture_imports_as_repeating_srgb_and_the_normal_as_a_normal_map()
        {
            var albedo = (TextureImporter)AssetImporter.GetAtPath(TextureFolder + "/wood_albedo.png");
            var normal = (TextureImporter)AssetImporter.GetAtPath(TextureFolder + "/wood_normal.png");
            Assert.IsNotNull(albedo);
            Assert.IsNotNull(normal);
            Assert.AreEqual(TextureImporterType.Default, albedo.textureType);
            Assert.IsTrue(albedo.sRGBTexture);
            Assert.AreEqual(TextureWrapMode.Repeat, albedo.wrapMode);
            Assert.IsTrue(albedo.mipmapEnabled);
            Assert.AreEqual(TextureImporterType.NormalMap, normal.textureType);
            Assert.AreEqual(TextureWrapMode.Repeat, normal.wrapMode);
        }

        [Test]
        public void Every_room_model_carries_baked_occlusion_that_varies()
        {
            foreach (var name in new[] { "plinth", "block", "pillar", "goal_pedestal", "bridge" })
            {
                string path = $"{ModelFolder}/{name}.glb";
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.IsNotNull(model, $"{name}.glb is missing. Run Tools/blender/DioramaTurn/make_room_models.py");
                var mesh = model.GetComponentInChildren<MeshFilter>().sharedMesh;
                Assert.IsNotNull(mesh, $"{name} has no mesh");
                var colours = mesh.colors;
                Assert.AreEqual(mesh.vertexCount, colours.Length, $"{name}: one colour per vertex");
                float min = colours.Min(c => c.r);
                float max = colours.Max(c => c.r);
                // A convex cube is open almost everywhere; the plinth and pedestal have real crevices.
                float required = name == "block" ? 0.02f : 0.15f;
                Assert.Greater(max - min, required, $"{name}: the occlusion does not vary (all {min:F2}..{max:F2})");
            }
        }
    }
}
