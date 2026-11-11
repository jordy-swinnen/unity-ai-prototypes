using System.Linq;
using System.Text;
using NUnit.Framework;
using Prototypes.DioramaTurn.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Prototypes.DioramaTurn.Tests
{
    public class DioramaTurnSceneBuilderTests
    {
        const string TempScene = "Assets/DioramaTurnSceneTest_Temp.unity";
        const float Tol = 0.01f;

        DioramaTurnSettings settings;

        [OneTimeSetUp]
        public void BuildOnce()
        {
            DioramaTurnSceneBuilder.Build(TempScene);
            settings = AssetDatabase.LoadAssetAtPath<DioramaTurnSettings>(DioramaTurnSceneBuilder.SettingsPath);
            Assert.IsNotNull(settings);
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(TempScene);
        }

        static GameObject Find(string name)
        {
            var found = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name == name);
            Assert.IsNotNull(found, $"missing object '{name}'");
            return found.gameObject;
        }

        [Test]
        public void The_camera_is_orthographic_at_the_true_isometric_angle_and_pixel_scale()
        {
            var go = Find("Main Camera");
            Assert.AreEqual("MainCamera", go.tag);
            var camera = go.GetComponent<Camera>();
            Assert.IsTrue(camera.orthographic);

            float scale = PixelScale.Scale(settings.camera.referenceHeight, settings.camera.pixelsPerUnit,
                settings.camera.roomExtentUnits, settings.camera.maxScale);
            Assert.AreEqual(PixelScale.OrthoSize(settings.camera.referenceHeight, settings.camera.pixelsPerUnit, (int)scale),
                camera.orthographicSize, Tol);
            Assert.AreEqual(settings.camera.pitchDeg, go.transform.eulerAngles.x, 0.01f);
            Assert.AreEqual(settings.camera.baseYawDeg, go.transform.eulerAngles.y, 0.01f);
            Assert.AreEqual(settings.camera.distance, Vector3.Distance(go.transform.position, CameraRig.Focus), 0.05f);

            var rig = go.GetComponent<CameraRig>();
            Assert.IsNotNull(rig);
            Assert.AreEqual(settings, rig.settings);

            var data = go.GetComponent<UniversalAdditionalCameraData>();
            Assert.IsNotNull(data);
            Assert.AreEqual(AntialiasingMode.SubpixelMorphologicalAntiAliasing, data.antialiasing);
        }

        [Test]
        public void The_key_light_is_directional_with_soft_shadows_and_rotates_with_the_rig()
        {
            var light = Find("Key Light").GetComponent<Light>();
            Assert.AreEqual(LightType.Directional, light.type);
            Assert.AreEqual(LightShadows.Soft, light.shadows);
            Assert.AreEqual(settings.light.keyIntensity, light.intensity, Tol);
            Assert.AreEqual(settings.light.keyColour.r, light.color.r, 0.01f);
            Assert.AreSame(Find("Key Light").GetComponent<Light>(), Find("Main Camera").GetComponent<CameraRig>().keyLight);
        }

        [Test]
        public void The_preview_room_has_its_pieces_on_their_cells()
        {
            var room = Rooms.Build(0);
            foreach (var piece in room.Pieces)
            {
                string prefix = piece.Kind == PieceKind.Start ? "Start" : piece.Kind == PieceKind.Goal ? "Goal" : "Block";
                var go = Find($"{prefix} {piece.Cell}");
                var expected = RoomView.CellCentre(piece.Cell);
                Assert.AreEqual(expected.x, go.transform.position.x, Tol, go.name);
                Assert.AreEqual(expected.y, go.transform.position.y, Tol, go.name);
                Assert.AreEqual(expected.z, go.transform.position.z, Tol, go.name);
                Assert.AreEqual(Vector3.one, go.transform.localScale);
            }
            var plinth = Find("Plinth");
            Assert.AreEqual(-1.1f, plinth.transform.position.y, Tol, "the model's origin is its bottom");
            Assert.AreEqual(Vector3.one, plinth.transform.localScale);
        }

        [Test]
        public void The_room_uses_the_surface_shader_and_the_plinth_carries_the_wood_textures()
        {
            var plinthMaterial = Find("Plinth").GetComponentInChildren<Renderer>().sharedMaterial;
            Assert.AreEqual(DioramaTurnSceneBuilder.SurfaceShaderName, plinthMaterial.shader.name);
            Assert.IsNotNull(plinthMaterial.GetTexture("_BaseMap"), "the plinth has the wood albedo");
            Assert.IsNotNull(plinthMaterial.GetTexture("_NormalMap"), "the plinth has the wood normal");
            Assert.AreEqual(1f, plinthMaterial.GetFloat("_WorldMapping"), "the plinth maps in world space");

            var start = Rooms.Build(0).Start;
            var blockMaterial = Find($"Start {start}").GetComponentInChildren<Renderer>().sharedMaterial;
            Assert.AreEqual(DioramaTurnSceneBuilder.SurfaceShaderName, blockMaterial.shader.name);
            Assert.AreEqual(0f, blockMaterial.GetFloat("_TextureStrength"), "blocks are flat painted colour");
            Assert.Greater(blockMaterial.GetFloat("_AoStrength"), 0f, "the baked occlusion is on");
        }

        [Test]
        public void The_post_volume_holds_bloom_vignette_grading_and_neutral_tonemapping()
        {
            var go = Find("Post Volume");
            var volume = go.GetComponent<UnityEngine.Rendering.Volume>();
            Assert.IsNotNull(volume);
            Assert.IsTrue(volume.isGlobal);
            Assert.IsNotNull(volume.sharedProfile);
            Assert.IsTrue(volume.sharedProfile.TryGet(out UnityEngine.Rendering.Universal.Bloom bloom));
            Assert.AreEqual(settings.post.bloomIntensity, bloom.intensity.value, Tol);
            Assert.IsTrue(volume.sharedProfile.TryGet(out UnityEngine.Rendering.Universal.Vignette vignette));
            Assert.AreEqual(settings.post.vignette, vignette.intensity.value, Tol);
            Assert.IsTrue(volume.sharedProfile.TryGet(out UnityEngine.Rendering.Universal.ColorAdjustments colour));
            Assert.AreEqual(settings.post.saturation, colour.saturation.value, Tol);
            Assert.IsTrue(volume.sharedProfile.TryGet(out UnityEngine.Rendering.Universal.Tonemapping tonemapping));
            Assert.AreEqual(UnityEngine.Rendering.Universal.TonemappingMode.Neutral, tonemapping.mode.value);

            var camera = Find("Main Camera").GetComponent<Camera>();
            Assert.IsTrue(Find("Main Camera").GetComponent<UniversalAdditionalCameraData>().renderPostProcessing);
        }

        [Test]
        public void Pip_carries_the_parsed_sheet_and_stands_on_the_start_cell()
        {
            var quad = Find("Pip").GetComponent<SpriteQuad>();
            Assert.IsNotNull(quad);
            Assert.IsNotNull(quad.sheet, "the sheet texture must be serialised so it survives a scene load");
            Assert.AreEqual("pip_sheet", quad.sheet.name);
            quad.ShowFrame(0); // binds and parses the json at first use
            Assert.IsNotNull(quad.Frames);
            Assert.AreEqual(14, quad.Frames.FrameCount);

            var start = Rooms.Build(0).Start;
            var pip = Find("Pip").transform;
            Assert.AreEqual(RoomView.TopFace(start).x, pip.position.x, 0.6f);
            Assert.AreEqual(RoomView.TopFace(start).y, pip.position.y, 0.01f);
        }

        [Test]
        public void The_star_sits_above_the_goal_cell()
        {
            var star = Find("Star").GetComponent<StarView>();
            Assert.IsNotNull(star);
            Assert.IsNotNull(star.quad, "the star is a sprite quad");
            var goal = Rooms.Build(0).Goal;
            var expected = RoomView.TopFace(goal) + Vector3.up * 0.4f;
            Assert.AreEqual(expected.y, star.quad.transform.position.y, Tol);
            Assert.AreEqual(expected.x, star.quad.transform.position.x, Tol);
        }

        [Test]
        public void The_hud_has_the_title_playing_complete_and_finish_panels_and_buttons()
        {
            foreach (var name in new[]
            {
                "Title Panel", "Playing Panel", "Complete Panel", "Finish Panel",
                "Button Turn Left", "Button Turn Right", "Button Restart", "Button Next", "Button Play Again",
                "Room Name", "Turn Count", "Hints", "Complete Text",
            })
                Find(name);

            var hud = Find("Canvas").GetComponent<Hud>();
            Assert.IsNotNull(hud);
            Assert.IsNotNull(hud.turnLeftButton);
            Assert.IsNotNull(hud.turnRightButton);
            Assert.IsNotNull(hud.restartButton);
            Assert.IsNotNull(hud.nextButton);
            Assert.IsNotNull(hud.playAgainButton);
            Assert.IsNotNull(hud.roomNameText);
            Assert.IsNotNull(hud.turnText);
            Assert.IsTrue(Find("Title Panel").activeSelf, "a fresh scene opens on the title card");
            Assert.IsFalse(Find("Complete Panel").activeSelf);
            Assert.IsFalse(Find("Finish Panel").activeSelf);

            var es = Find("EventSystem");
            Assert.IsNotNull(es.GetComponent<EventSystem>());
            Assert.IsNotNull(es.GetComponent<InputSystemUIInputModule>());
        }

        [Test]
        public void The_game_is_wired_to_everything_it_needs()
        {
            var game = Find("Game").GetComponent<DioramaTurnGame>();
            Assert.IsNotNull(game);
            Assert.AreEqual(settings, game.settings);
            Assert.IsNotNull(game.rig);
            Assert.IsNotNull(game.room);
            Assert.IsNotNull(game.pip);
            Assert.IsNotNull(game.star);
            Assert.IsNotNull(game.ribbons);
            Assert.IsNotNull(game.hud);
            Assert.IsNotNull(game.input);
            Assert.IsNotNull(game.audio);
            Assert.IsNotNull(game.audio.source);
            Assert.AreEqual(game, game.input.game);
            Assert.AreSame(game.rig, game.pip.rig);
            Assert.AreSame(game.room, game.pip.room);
            Assert.AreSame(game.room, game.ribbons.room);
        }

        [Test]
        public void Building_twice_gives_the_same_content()
        {
            string first = Describe(SceneManager.GetActiveScene());
            DioramaTurnSceneBuilder.Build(TempScene);
            Assert.AreEqual(first, Describe(SceneManager.GetActiveScene()));
        }

        [Test]
        public void The_committed_scene_matches_what_the_builder_makes_now()
        {
            string fresh = Describe(SceneManager.GetActiveScene());
            string committed;
            try
            {
                EditorSceneManager.OpenScene(DioramaTurnSceneBuilder.ScenePath, OpenSceneMode.Single);
                committed = Describe(SceneManager.GetActiveScene());
            }
            finally
            {
                EditorSceneManager.OpenScene(TempScene, OpenSceneMode.Single);
            }
            Assert.AreEqual(fresh, committed,
                "DioramaTurn.unity is out of date. Run Prototypes > DioramaTurn > Build Scene and commit the result.");
        }

        static string Describe(Scene scene)
        {
            var sb = new StringBuilder();
            foreach (var root in scene.GetRootGameObjects().OrderBy(g => g.name)) DescribeObject(sb, root, 0);
            return sb.ToString();
        }

        static void DescribeObject(StringBuilder sb, GameObject go, int depth)
        {
            var t = go.transform;
            var rect = t as RectTransform;
            bool isCanvas = go.GetComponent<Canvas>() != null;
            string placement = isCanvas
                ? "(canvas, sized by the screen)"
                : rect != null
                    ? $"anchors={rect.anchorMin:F2}{rect.anchorMax:F2} anchored={rect.anchoredPosition:F1} size={rect.sizeDelta:F1}"
                    : $"pos={t.localPosition:F3} rot={t.localEulerAngles:F2} scale={t.localScale:F3}";
            sb.Append(' ', depth * 2).Append($"{go.name} layer={go.layer} tag={go.tag} active={go.activeSelf} {placement}\n");
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null || c is Transform) continue;
                sb.Append(' ', depth * 2 + 1).Append("- ").Append(c.GetType().Name);
                if (c is Renderer r) sb.Append(" mats=").Append(string.Join(",", r.sharedMaterials.Select(m => m ? m.name : "null")));
                if (c is Text text) sb.Append($" text='{text.text}' size={text.fontSize}");
                if (c is Image image) sb.Append($" color={image.color}");
                if (c is Camera cam) sb.Append($" ortho={cam.orthographic} size={cam.orthographicSize:F3} bg={cam.backgroundColor}");
                if (c is Light l) sb.Append($" light={l.type} shadows={l.shadows} color={l.color}");
                if (c is SpriteQuad q) sb.Append($" sheet={(q.sheet ? q.sheet.name : "null")} size={q.size}");
                if (c is DioramaTurnGame g) sb.Append($" settings={(g.settings ? g.settings.name : "null")}");
                if (c is CameraRig rig) sb.Append($" rigSettings={(rig.settings ? rig.settings.name : "null")}");
                sb.Append('\n');
            }
            for (int i = 0; i < t.childCount; i++) DescribeObject(sb, t.GetChild(i).gameObject, depth + 1);
        }
    }
}
