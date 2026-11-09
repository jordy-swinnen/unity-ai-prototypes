using System.Linq;
using System.Text;
using NUnit.Framework;
using Prototypes.MedievalChess.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Prototypes.MedievalChess.Tests
{
    public class GameSceneBuilderTests
    {
        const string TempScene = "Assets/MedievalChessGameTest_Temp.unity";
        const float Tol = 0.001f;

        MedievalChessSettings settings;

        // The tests only read the scene, so it is built once for the whole fixture.
        [OneTimeSetUp]
        public void BuildOnce()
        {
            GameSceneBuilder.Build(TempScene);
            settings = AssetDatabase.LoadAssetAtPath<MedievalChessSettings>(DuelSceneBuilder.SettingsPath);
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(TempScene);
        }

        // Includes objects that are switched off (the panels start hidden).
        static GameObject Find(string name)
        {
            var found = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name == name);
            Assert.IsNotNull(found, $"missing object '{name}'");
            return found.gameObject;
        }

        [Test]
        public void There_are_sixty_four_squares_in_two_colours_on_the_squares_the_rules_use()
        {
            var tiles = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.StartsWith("Tile ")).ToArray();
            Assert.AreEqual(64, tiles.Length);
            for (int sq = 0; sq < 64; sq++)
            {
                var tile = Find($"Tile {Squares.Name(sq)}").transform;
                var c = BoardGeometry.Centre(sq);
                Assert.AreEqual(c.x, tile.position.x, Tol);
                Assert.AreEqual(c.z, tile.position.z, Tol);
                Assert.AreEqual(Squares.IsDark(sq) ? settings.darkSquareColor.r : settings.lightSquareColor.r,
                    tile.GetComponent<Renderer>().sharedMaterial.GetColor("_BaseColor").r, 0.01f, Squares.Name(sq));
            }
            Assert.AreEqual(2, tiles.Select(t => t.GetComponent<Renderer>().sharedMaterial).Distinct().Count());
            Assert.AreEqual(Squares.IsDark(0), Find("Tile a1").GetComponent<Renderer>().sharedMaterial.name.EndsWith("Dark"), "a1 is a dark square");
        }

        [Test]
        public void The_frame_the_floor_and_the_two_trays_are_there()
        {
            foreach (var n in new[] { "Frame North", "Frame South", "Frame East", "Frame West", "Floor", "Tray Left", "Tray Right" }) Find(n);
            Assert.AreEqual(-settings.game.trayX, Find("Tray Left").transform.position.x, Tol);
            Assert.AreEqual(settings.game.trayX, Find("Tray Right").transform.position.x, Tol);
        }

        [Test]
        public void The_camera_looks_down_at_the_board_from_the_settings_angle()
        {
            var go = Find("Main Camera");
            Assert.AreEqual("MainCamera", go.tag);
            var cam = go.GetComponent<Camera>();
            Assert.AreEqual(settings.game.cameraFieldOfView, cam.fieldOfView, Tol);
            Assert.AreEqual(settings.game.cameraPitch, cam.transform.eulerAngles.x, 0.01f);
            Assert.Less(cam.transform.position.z, 0f, "behind the bone side");
            Assert.AreEqual(settings.game.cameraDistance, (cam.transform.position - new Vector3(0f, settings.game.cameraFocusHeight, 0f)).magnitude, 0.01f);
            Assert.IsNotNull(go.GetComponent<GameCameraRig>());
        }

        [Test]
        public void A_key_light_casts_soft_shadows()
        {
            var light = Find("Key Light").GetComponent<Light>();
            Assert.AreEqual(LightType.Directional, light.type);
            Assert.AreEqual(LightShadows.Soft, light.shadows);
        }

        [Test]
        public void The_highlight_rings_have_a_template_and_six_saved_materials()
        {
            var view = Find("Highlights").GetComponent<HighlightView>();
            Assert.IsNotNull(view.ringTemplate);
            Assert.IsFalse(view.ringTemplate.activeSelf, "the template is a pattern, not a ring");
            Assert.IsNull(view.ringTemplate.GetComponent<Collider>());
            var mats = new[] { view.selectionMaterial, view.moveMaterial, view.captureMaterial, view.lastMoveMaterial, view.checkMaterial, view.hoverMaterial };
            foreach (var m in mats)
            {
                Assert.IsNotNull(m);
                Assert.IsTrue(AssetDatabase.Contains(m));
            }
            Assert.AreEqual(6, mats.Distinct().Count());
        }

        [Test]
        public void Every_menu_button_and_panel_exists()
        {
            foreach (var n in new[]
            {
                "Start Panel", "HUD", "Promotion Panel", "Result Panel",
                "Button Squire", "Button Knight", "Button Marshal", "Button White", "Button Black", "Button Random",
                "Button Full", "Button Quick", "Button Off", "Button Start", "Button Resign", "Button Menu",
                "Promote Queen", "Promote Rook", "Promote Bishop", "Promote Knight", "Button Play Again", "Button Change Difficulty",
                "Status Text", "Result Text", "Difficulty Description",
            })
            {
                Find(n);
            }
        }

        [Test]
        public void The_menus_can_be_clicked_with_the_input_system()
        {
            var es = Find("EventSystem");
            Assert.IsNotNull(es.GetComponent<EventSystem>());
            Assert.IsNotNull(es.GetComponent<InputSystemUIInputModule>());
            Assert.IsNotNull(Find("Canvas").GetComponent<GraphicRaycaster>());
        }

        [Test]
        public void The_controller_is_wired_to_everything_it_needs()
        {
            var game = Find("Game");
            var controller = game.GetComponent<GameController>();
            Assert.AreEqual(settings, controller.settings);
            Assert.AreEqual(6, controller.piecePrefabs.Length);
            foreach (var type in System.Enum.GetValues(typeof(PieceType)).Cast<PieceType>())
                StringAssert.AreEqualIgnoringCase(type.ToString(), controller.piecePrefabs[(int)type].name);
            foreach (var m in new[] { controller.bone, controller.cinnabar, controller.iron, controller.trim })
            {
                Assert.IsNotNull(m);
                Assert.AreEqual("Universal Render Pipeline/Lit", m.shader.name);
            }
            Assert.IsNotNull(controller.ui);
            Assert.IsNotNull(controller.highlights);
            Assert.IsNotNull(controller.cameraRig);
            var ui = controller.ui;
            Assert.AreEqual(3, ui.difficultyButtons.Length);
            Assert.AreEqual(3, ui.sideButtons.Length);
            Assert.AreEqual(3, ui.speedButtons.Length);
            Assert.AreEqual(4, ui.promotionButtons.Length);
            Assert.IsNotNull(ui.startButton);

            var input = game.GetComponent<InputController>();
            Assert.AreEqual(controller, input.controller);
            Assert.AreEqual(Camera.main, input.cam);
        }

        [Test]
        public void Building_twice_gives_the_same_content()
        {
            string first = Describe(SceneManager.GetActiveScene());
            GameSceneBuilder.Build(TempScene);
            Assert.AreEqual(first, Describe(SceneManager.GetActiveScene()));
        }

        [Test]
        public void The_committed_scene_matches_what_the_builder_makes_now()
        {
            string fresh = Describe(SceneManager.GetActiveScene());
            string committed;
            try
            {
                EditorSceneManager.OpenScene(GameSceneBuilder.ScenePath, OpenSceneMode.Single);
                committed = Describe(SceneManager.GetActiveScene());
            }
            finally
            {
                EditorSceneManager.OpenScene(TempScene, OpenSceneMode.Single);
            }
            Assert.AreEqual(fresh, committed,
                "MedievalChess.unity is out of date. Run Prototypes > MedievalChess > Build Scene and commit the result.");
        }

        // A readable dump of the scene. The scene file text changes on every save, so content is compared.
        static string Describe(Scene scene)
        {
            var sb = new StringBuilder();
            foreach (var root in scene.GetRootGameObjects().OrderBy(g => g.name)) DescribeObject(sb, root, 0);
            return sb.ToString();
        }

        static void DescribeObject(StringBuilder sb, GameObject go, int depth)
        {
            var t = go.transform;
            bool isCanvas = go.GetComponent<Canvas>() != null;
            var rect = t as RectTransform;
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
                if (c is Camera cam) sb.Append($" fov={cam.fieldOfView:F2} bg={cam.backgroundColor}");
                if (c is Light l) sb.Append($" light={l.type} shadows={l.shadows} color={l.color}");
                if (c is GameController g)
                    sb.Append($" settings={(g.settings ? g.settings.name : "null")} prefabs={string.Join(",", g.piecePrefabs.Select(p => p ? p.name : "null"))}");
                sb.Append('\n');
            }
            for (int i = 0; i < t.childCount; i++) DescribeObject(sb, t.GetChild(i).gameObject, depth + 1);
        }
    }
}
