using System.Linq;
using System.Text;
using NUnit.Framework;
using Prototypes.NeonPong.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Prototypes.NeonPong.Tests
{
    public class NeonPongSceneBuilderTests
    {
        const string TempScene = "Assets/NeonPongBuilderTest_Temp.unity";
        const float Tol = 0.001f;

        NeonPongSettings settings;

        // The tests only read the scene, so it is built once for the whole fixture.
        [OneTimeSetUp]
        public void BuildOnce()
        {
            NeonPongSceneBuilder.Build(TempScene);
            settings = AssetDatabase.LoadAssetAtPath<NeonPongSettings>(NeonPongSceneBuilder.SettingsPath);
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(TempScene);
        }

        static GameObject Find(string name)
        {
            var go = GameObject.Find(name);
            Assert.IsNotNull(go, $"missing object '{name}'");
            return go;
        }

        [Test]
        public void The_settings_asset_exists_after_a_build()
        {
            Assert.IsNotNull(settings);
        }

        [Test]
        public void Every_part_of_the_game_is_in_the_scene()
        {
            foreach (var name in new[]
            {
                "Table", "Rail Top", "Rail Bottom", "End Line Left", "End Line Right",
                "Paddle Left", "Paddle Right", "Ball", "Main Camera", "Global Volume",
                "Game", "Score Left", "Score Right", "Message", "Wash",
                "Ball Trail", "Ball Light", "Bursts", "Audio",
            })
            {
                Find(name);
            }
        }

        [Test]
        public void Models_sit_where_the_rules_expect()
        {
            var left = Find("Paddle Left").transform.position;
            var right = Find("Paddle Right").transform.position;
            Assert.AreEqual(-settings.paddleX, left.x, Tol);
            Assert.AreEqual(settings.paddleX, right.x, Tol);
            Assert.AreEqual(0f, left.y, Tol);
            Assert.AreEqual(0f, left.z, Tol);

            var ball = Find("Ball").transform.position;
            Assert.AreEqual(0f, ball.x, Tol);
            Assert.AreEqual(settings.ballRadius, ball.y, Tol);
            Assert.AreEqual(0f, ball.z, Tol);

            // Rails rest on the table, with their inner face on the play-area edge.
            var top = Find("Rail Top").transform.position;
            var bottom = Find("Rail Bottom").transform.position;
            Assert.AreEqual(settings.halfWidth + 0.15f, top.z, Tol);
            Assert.AreEqual(-(settings.halfWidth + 0.15f), bottom.z, Tol);
            Assert.AreEqual(0f, top.y, Tol);

            Assert.AreEqual(settings.halfLength, Find("End Line Right").transform.position.x, Tol);
            Assert.AreEqual(-settings.halfLength, Find("End Line Left").transform.position.x, Tol);
        }

        [Test]
        public void Every_renderer_has_real_materials_on_the_URP_Lit_shader()
        {
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
                .Where(r => r is MeshRenderer).ToArray();
            Assert.GreaterOrEqual(renderers.Length, 8);
            foreach (var r in renderers)
            {
                Assert.Greater(r.sharedMaterials.Length, 0, r.name);
                foreach (var m in r.sharedMaterials)
                {
                    Assert.IsNotNull(m, r.name);
                    Assert.AreEqual("Universal Render Pipeline/Lit", m.shader.name, r.name);
                    Assert.IsTrue(AssetDatabase.Contains(m), $"{r.name}: material is not a saved asset");
                }
            }
        }

        [Test]
        public void Paddles_use_their_player_colours()
        {
            var left = Find("Paddle Left").GetComponentInChildren<Renderer>().sharedMaterial;
            var right = Find("Paddle Right").GetComponentInChildren<Renderer>().sharedMaterial;
            Assert.AreNotEqual(left, right);
            AssertColor(settings.player1Color, left.GetColor("_BaseColor"));
            AssertColor(settings.player2Color, right.GetColor("_BaseColor"));
        }

        static void AssertColor(Color expected, Color actual)
        {
            Assert.AreEqual(expected.r, actual.r, 0.01f);
            Assert.AreEqual(expected.g, actual.g, 0.01f);
            Assert.AreEqual(expected.b, actual.b, 0.01f);
        }

        [Test]
        public void The_camera_is_set_up_from_the_settings_with_post_processing_on()
        {
            var go = Find("Main Camera");
            Assert.AreEqual("MainCamera", go.tag);
            var cam = go.GetComponent<Camera>();
            Assert.AreEqual(settings.cameraFieldOfView, cam.fieldOfView, Tol);
            Assert.AreEqual(settings.cameraPitch, cam.transform.eulerAngles.x, 0.01f);
            Assert.AreEqual(settings.cameraDistance, cam.transform.position.magnitude, 0.01f);
            Assert.IsTrue(go.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing);
        }

        [Test]
        public void The_volume_has_bloom_vignette_and_neutral_tonemapping_and_reaches_the_camera()
        {
            var volume = Find("Global Volume").GetComponent<Volume>();
            Assert.IsTrue(volume.isGlobal);
            Assert.IsNotNull(volume.sharedProfile);
            Assert.IsTrue(AssetDatabase.Contains(volume.sharedProfile), "profile must be a saved asset");

            Assert.IsTrue(volume.sharedProfile.TryGet(out Bloom bloom));
            Assert.IsTrue(bloom.active);
            Assert.Greater(bloom.intensity.value, 0f);
            Assert.IsTrue(bloom.intensity.overrideState);
            Assert.AreEqual(settings.bloomIntensity, bloom.intensity.value, Tol);
            Assert.AreEqual(settings.bloomThreshold, bloom.threshold.value, Tol);

            Assert.IsTrue(volume.sharedProfile.TryGet(out Vignette vignette));
            Assert.AreEqual(settings.vignetteIntensity, vignette.intensity.value, Tol);

            Assert.IsTrue(volume.sharedProfile.TryGet(out Tonemapping tone));
            Assert.AreEqual(TonemappingMode.Neutral, tone.mode.value);

            var cam = Find("Main Camera").GetComponent<UniversalAdditionalCameraData>();
            Assert.AreNotEqual(0, cam.volumeLayerMask.value & (1 << volume.gameObject.layer),
                "the camera's Volume Mask must include the Volume's layer");
        }

        [Test]
        public void The_game_and_view_are_wired_up()
        {
            var game = Find("Game").GetComponent<NeonPongGame>();
            var view = Find("Game").GetComponent<NeonPongView>();
            Assert.AreEqual(settings, game.settings);
            Assert.IsNotNull(view.game);
            Assert.IsNotNull(view.leftPaddle);
            Assert.IsNotNull(view.rightPaddle);
            Assert.IsNotNull(view.ball);
            Assert.IsNotNull(view.scoreLeft);
            Assert.IsNotNull(view.scoreRight);
            Assert.IsNotNull(view.message);
        }

        [Test]
        public void The_ball_has_a_trail_and_a_light_driven_by_the_settings()
        {
            var ball = Find("Ball");
            var trail = ball.GetComponentInChildren<TrailRenderer>();
            Assert.IsNotNull(trail);
            Assert.IsTrue(AssetDatabase.Contains(trail.sharedMaterial));
            var light = ball.GetComponentInChildren<Light>();
            Assert.AreEqual(LightType.Point, light.type);
            Assert.AreEqual(settings.lightRange, light.range, Tol);

            var view = Find("Game").GetComponent<NeonPongView>();
            Assert.AreEqual(trail, view.trail);
            Assert.AreEqual(light, view.ballLight);
            Assert.AreEqual(2, view.rails.Length);
            Assert.IsNotNull(view.wash);
        }

        [Test]
        public void The_camera_rig_the_bursts_and_the_audio_are_wired_to_the_game()
        {
            var game = Find("Game").GetComponent<NeonPongGame>();
            Assert.AreEqual(game, Find("Main Camera").GetComponent<NeonPongCameraRig>().game);

            var bursts = Find("Bursts").GetComponent<NeonPongBursts>();
            Assert.AreEqual(game, bursts.game);
            Assert.IsNotNull(bursts.system);
            Assert.IsTrue(AssetDatabase.Contains(bursts.GetComponent<ParticleSystemRenderer>().sharedMaterial));

            var audio = Find("Audio").GetComponent<NeonPongAudio>();
            Assert.AreEqual(game, audio.game);
            Assert.IsNotNull(audio.sfx);
            Assert.IsNotNull(audio.paddleSfx);
            Assert.IsNotNull(audio.musicSource);
            Assert.IsTrue(audio.musicSource.loop);
            Assert.IsNotNull(audio.music);
            Assert.AreEqual(settings.masterVolume * settings.musicVolume, audio.musicSource.volume, Tol);
            foreach (var clip in new[] { audio.wall, audio.paddle, audio.serve, audio.point, audio.win, audio.restart })
                Assert.IsNotNull(clip);
        }

        [Test]
        public void Score_text_uses_the_player_colours_and_a_neutral_message()
        {
            AssertColor(settings.player1Color, Find("Score Left").GetComponent<Text>().color);
            AssertColor(settings.player2Color, Find("Score Right").GetComponent<Text>().color);
            AssertColor(settings.textColor, Find("Message").GetComponent<Text>().color);
        }

        [Test]
        public void Building_twice_gives_the_same_content()
        {
            string first = Describe(SceneManager.GetActiveScene());
            NeonPongSceneBuilder.Build(TempScene);
            string second = Describe(SceneManager.GetActiveScene());
            Assert.AreEqual(first, second);
        }

        [Test]
        public void The_committed_scene_matches_what_the_builder_makes_now()
        {
            string fresh = Describe(SceneManager.GetActiveScene());
            string committed;
            try
            {
                EditorSceneManager.OpenScene(NeonPongSceneBuilder.ScenePath, OpenSceneMode.Single);
                committed = Describe(SceneManager.GetActiveScene());
            }
            finally
            {
                EditorSceneManager.OpenScene(TempScene, OpenSceneMode.Single); // back to the fixture's scene
            }
            Assert.AreEqual(fresh, committed,
                "NeonPong.unity is out of date. Run Prototypes > NeonPong > Build Scene and commit the result.");
        }

        // A readable dump of the scene: objects, transforms, components, meshes and materials.
        // The scene file text differs on every save (new object IDs), so content is compared.
        static string Describe(Scene scene)
        {
            var sb = new StringBuilder();
            foreach (var root in scene.GetRootGameObjects().OrderBy(g => g.name))
                DescribeObject(sb, root, 0);
            return sb.ToString();
        }

        static void DescribeObject(StringBuilder sb, GameObject go, int depth)
        {
            var t = go.transform;
            // A canvas and the things on it are laid out from the screen size, so compare their
            // anchors and sizes, which the builder sets, and not the positions Unity works out.
            bool isCanvas = go.GetComponent<Canvas>() != null;
            var rect = t as RectTransform;
            string placement = isCanvas
                ? "(canvas, sized by the screen)"
                : rect != null
                    ? $"anchors={rect.anchorMin:F2}{rect.anchorMax:F2} anchored={rect.anchoredPosition:F1} size={rect.sizeDelta:F1}"
                    : $"pos={t.localPosition:F3} rot={t.localEulerAngles:F2} scale={t.localScale:F3}";
            sb.Append(' ', depth * 2)
              .Append($"{go.name} layer={go.layer} tag={go.tag} active={go.activeSelf} {placement}\n");
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null || c is Transform) continue;
                sb.Append(' ', depth * 2 + 1).Append("- ").Append(c.GetType().Name);
                if (c is MeshFilter mf && mf.sharedMesh != null) sb.Append($" mesh={mf.sharedMesh.name}");
                if (c is Renderer r)
                    sb.Append(" mats=").Append(string.Join(",", r.sharedMaterials.Select(m => m ? m.name : "null")));
                if (c is Text text) sb.Append($" text='{text.text}' size={text.fontSize} color={text.color}");
                if (c is Camera cam) sb.Append($" fov={cam.fieldOfView:F2} clear={cam.clearFlags} bg={cam.backgroundColor}");
                if (c is Volume v)
                    sb.Append($" global={v.isGlobal} overrides=")
                      .Append(string.Join(",", v.sharedProfile.components.Select(o => o.GetType().Name)));
                if (c is NeonPongGame g) sb.Append($" settings={(g.settings ? g.settings.name : "null")}");
                sb.Append('\n');
            }
            for (int i = 0; i < t.childCount; i++)
                DescribeObject(sb, t.GetChild(i).gameObject, depth + 1);
        }
    }
}
