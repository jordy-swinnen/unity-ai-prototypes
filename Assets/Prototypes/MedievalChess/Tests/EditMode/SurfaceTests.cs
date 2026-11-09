using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Prototypes.MedievalChess.Tests
{
    /// <summary>
    /// The surface pass: the shader compiles, the generated textures tile and are not flat, the exported
    /// models carry baked occlusion, and the victim's dissolve reaches the renderer.
    /// </summary>
    public class SurfaceTests
    {
        const string ShaderName = "Prototypes/MedievalChess/Lit";
        const string TextureFolder = "Assets/Prototypes/MedievalChess/Art/Textures";
        const string ModelFolder = "Assets/Prototypes/MedievalChess/Art/Exports";

        public static readonly string[] Surfaces = { "slate", "limestone", "wood", "flagstone", "grain" };

        // --- the shader ---

        [Test]
        public void The_shader_exists_compiles_and_runs_on_this_machine()
        {
            var shader = Shader.Find(ShaderName);
            Assert.IsNotNull(shader, $"No shader named {ShaderName}");
            var errors = ShaderUtil.GetShaderMessages(shader).Where(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();
            Assert.IsEmpty(errors, string.Join("\n", errors.Select(m => $"{m.file}:{m.line} {m.message}")));
            Assert.IsTrue(ShaderUtil.IsPassCompiled(new Material(shader), 0) || shader.isSupported);
            Assert.IsTrue(shader.isSupported);
        }

        [Test]
        public void The_shader_has_the_properties_the_code_sets()
        {
            var shader = Shader.Find(ShaderName);
            Assert.IsNotNull(shader);
            foreach (var p in new[] { "_BaseColor", "_BaseMap", "_NormalMap", "_Dissolve", "_EdgeColor", "_AoStrength", "_TextureStrength", "_NormalStrength", "_MapScale", "_WorldMapping" })
                Assert.GreaterOrEqual(shader.FindPropertyIndex(p), 0, $"the shader has no property {p}");
        }

        // --- the generated textures ---

        static Texture2D Read(string file)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), TextureFolder, file);
            Assert.IsTrue(File.Exists(path), $"{file} is missing. Run Tools/textures/MedievalChess/make_textures.py");
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            Assert.IsTrue(ImageConversion.LoadImage(tex, File.ReadAllBytes(path)));
            return tex;
        }

        [TestCaseSource(nameof(Surfaces))]
        public void Every_texture_is_square_a_power_of_two_and_not_flat(string name)
        {
            var tex = Read($"{name}_albedo.png");
            Assert.AreEqual(tex.width, tex.height);
            Assert.IsTrue(Mathf.IsPowerOfTwo(tex.width), "power of two");
            var px = tex.GetPixels32();
            double mean = px.Average(p => p.r / 255.0);
            double sd = System.Math.Sqrt(px.Average(p => System.Math.Pow(p.r / 255.0 - mean, 2)));
            Assert.Greater(sd, 0.02, $"{name} is nearly flat");
        }

        [TestCaseSource(nameof(Surfaces))]
        public void Every_texture_tiles_without_a_seam(string name)
        {
            foreach (var kind in new[] { "albedo", "normal" })
            {
                var tex = Read($"{name}_{kind}.png");
                int n = tex.width;
                var px = tex.GetPixels32();
                // The average step between neighbouring columns (or rows) across the whole image.
                float Step(int a, int b, bool columns)
                {
                    float sum = 0f;
                    for (int i = 0; i < n; i++)
                    {
                        var p = columns ? px[i * n + a] : px[a * n + i];
                        var q = columns ? px[i * n + b] : px[b * n + i];
                        sum += Mathf.Abs(p.r - q.r) + Mathf.Abs(p.g - q.g) + Mathf.Abs(p.b - q.b);
                    }
                    return sum / n;
                }
                foreach (bool columns in new[] { true, false })
                {
                    float mean = 0f, max = 0f;
                    for (int k = 0; k < n - 1; k++)
                    {
                        float step = Step(k, k + 1, columns);
                        mean += step / (n - 1);
                        max = Mathf.Max(max, step);
                    }
                    float seam = Step(n - 1, 0, columns);
                    // A seam must look like the texture's own joins: either an ordinary step, or no bigger than its
                    // biggest designed edge (the gap between wood planks). Noise that does not wrap fails both.
                    Assert.IsTrue(seam <= mean * 1.6f + 3f || seam <= max,
                        $"{name}_{kind} shows a seam across {(columns ? "columns" : "rows")} (seam {seam:F1}, mean step {mean:F1}, biggest step {max:F1})");
                }
            }
        }

        [TestCaseSource(nameof(Surfaces))]
        public void Normal_maps_hold_unit_vectors_that_mostly_point_out(string name)
        {
            var tex = Read($"{name}_normal.png");
            var px = tex.GetPixels32();
            double z = 0, len = 0;
            foreach (var p in px)
            {
                var v = new Vector3(p.r / 127.5f - 1f, p.g / 127.5f - 1f, p.b / 127.5f - 1f);
                z += v.z;
                len += v.magnitude;
            }
            Assert.Greater(z / px.Length, 0.7, "a normal map points mostly out of the surface");
            Assert.AreEqual(1.0, len / px.Length, 0.05);
        }

        [TestCaseSource(nameof(Surfaces))]
        public void Textures_import_with_the_right_colour_space(string name)
        {
            var normal = (TextureImporter)AssetImporter.GetAtPath($"{TextureFolder}/{name}_normal.png");
            var albedo = (TextureImporter)AssetImporter.GetAtPath($"{TextureFolder}/{name}_albedo.png");
            Assert.IsNotNull(normal);
            Assert.IsNotNull(albedo);
            Assert.AreEqual(TextureImporterType.NormalMap, normal.textureType);
            Assert.IsTrue(albedo.sRGBTexture);
            Assert.AreEqual(TextureWrapMode.Repeat, albedo.wrapMode);
        }

        // --- baked occlusion ---

        [TestCase("pawn")]
        [TestCase("knight")]
        [TestCase("bishop")]
        [TestCase("rook")]
        [TestCase("queen")]
        [TestCase("king")]
        public void Every_model_carries_baked_occlusion_that_varies(string name)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}/{name}.glb");
            Assert.IsNotNull(go);
            int total = 0, dark = 0, bright = 0;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = mf.sharedMesh;
                var colors = mesh.colors32;
                Assert.AreEqual(mesh.vertexCount, colors.Length, $"{name}/{mf.name} has no vertex colours (occlusion). Run make_pieces.py");
                foreach (var c in colors)
                {
                    total++;
                    if (c.r < 215) dark++;
                    if (c.r > 240) bright++;
                }
            }
            Assert.Greater(dark / (float)total, 0.03f, $"{name}: almost nothing is occluded");
            Assert.Greater(bright / (float)total, 0.10f, $"{name}: almost nothing is open to the light");
            Assert.Less(dark / (float)total, 0.9f, $"{name}: nearly everything is dark");
        }

        // --- the dissolve reaches the renderer ---

        [Test]
        public void The_actor_passes_the_dissolve_to_its_renderers_and_clears_it_at_zero()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var mat = new Material(Shader.Find(ShaderName));
                var actor = PieceActor.Bind(go, PieceType.Pawn, mat, mat);
                actor.Place(Vector3.zero, 0f);
                var block = new MaterialPropertyBlock();
                var r = go.GetComponent<Renderer>();

                actor.Apply(new ActorFrame { Dissolve = 0.5f });
                r.GetPropertyBlock(block);
                Assert.AreEqual(0.5f, block.GetFloat("_Dissolve"), 1e-4f);

                actor.Apply(new ActorFrame { Dissolve = 0f });
                r.GetPropertyBlock(block);
                Assert.AreEqual(0f, block.GetFloat("_Dissolve"), 1e-4f);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void The_surface_settings_start_in_a_sane_range()
        {
            var s = new LookSettings().surface;
            Assert.That(s.textureStrength, Is.InRange(0f, 1f));
            Assert.That(s.occlusionStrength, Is.InRange(0f, 1f));
            Assert.That(s.normalStrength, Is.InRange(0f, 2f));
            Assert.That(s.pieceGrain, Is.InRange(0f, 1f));
            Assert.Greater(new FightSettings().dissolveTime, 0.1f);
            Assert.LessOrEqual(new FightSettings().dissolveTime, new FightSettings().collapseTime);
        }
    }
}
