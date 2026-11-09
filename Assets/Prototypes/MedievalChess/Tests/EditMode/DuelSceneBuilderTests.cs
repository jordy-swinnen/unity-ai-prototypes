using System.Linq;
using System.Text;
using NUnit.Framework;
using Prototypes.MedievalChess.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Prototypes.MedievalChess.Tests
{
    public class DuelSceneBuilderTests
    {
        const string TempScene = "Assets/MedievalChessDuelTest_Temp.unity";
        const float Tol = 0.001f;

        MedievalChessSettings settings;

        // The tests only read the scene, so it is built once for the whole fixture.
        [OneTimeSetUp]
        public void BuildOnce()
        {
            DuelSceneBuilder.Build(TempScene);
            settings = AssetDatabase.LoadAssetAtPath<MedievalChessSettings>(DuelSceneBuilder.SettingsPath);
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
        public void The_settings_asset_exists()
        {
            Assert.IsNotNull(settings);
        }

        [Test]
        public void The_controller_is_wired_to_the_settings_models_and_materials()
        {
            var duel = Find("Duel").GetComponent<DuelController>();
            Assert.AreEqual(settings, duel.settings);
            Assert.AreEqual(6, duel.piecePrefabs.Length);
            foreach (var type in System.Enum.GetValues(typeof(PieceType)).Cast<PieceType>())
            {
                var prefab = duel.piecePrefabs[(int)type];
                Assert.IsNotNull(prefab, type.ToString());
                StringAssert.AreEqualIgnoringCase(type.ToString(), prefab.name);
            }
            foreach (var m in new[] { duel.bone, duel.cinnabar, duel.iron })
            {
                Assert.IsNotNull(m);
                Assert.AreEqual("Universal Render Pipeline/Lit", m.shader.name);
                Assert.IsTrue(AssetDatabase.Contains(m), "materials must be saved assets");
            }
            Assert.IsNotNull(duel.cam);
            Assert.IsNotNull(duel.label);
        }

        [Test]
        public void The_side_materials_use_the_palette()
        {
            var duel = Find("Duel").GetComponent<DuelController>();
            AssertColor(settings.boneColor, duel.bone.GetColor("_BaseColor"));
            AssertColor(settings.cinnabarColor, duel.cinnabar.GetColor("_BaseColor"));
            AssertColor(settings.ironColor, duel.iron.GetColor("_BaseColor"));
        }

        static void AssertColor(Color expected, Color actual)
        {
            Assert.AreEqual(expected.r, actual.r, 0.01f);
            Assert.AreEqual(expected.g, actual.g, 0.01f);
            Assert.AreEqual(expected.b, actual.b, 0.01f);
        }

        [Test]
        public void The_camera_looks_at_the_duel_from_the_settings_angle()
        {
            var go = Find("Main Camera");
            Assert.AreEqual("MainCamera", go.tag);
            var cam = go.GetComponent<Camera>();
            Assert.AreEqual(settings.duelCameraFieldOfView, cam.fieldOfView, Tol);
            Assert.AreEqual(settings.duelCameraPitch, cam.transform.eulerAngles.x, 0.01f);
        }

        [Test]
        public void A_key_light_casts_soft_shadows()
        {
            var light = Find("Key Light").GetComponent<Light>();
            Assert.AreEqual(LightType.Directional, light.type);
            Assert.AreEqual(LightShadows.Soft, light.shadows);
        }

        [Test]
        public void There_is_a_board_with_two_square_colours_under_the_duel()
        {
            var tiles = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.StartsWith("Tile ")).ToArray();
            Assert.GreaterOrEqual(tiles.Length, 12);
            Assert.AreEqual(2, tiles.Select(t => t.GetComponent<Renderer>().sharedMaterial).Distinct().Count());
        }

        [Test]
        public void Building_twice_gives_the_same_content()
        {
            string first = Describe(SceneManager.GetActiveScene());
            DuelSceneBuilder.Build(TempScene);
            Assert.AreEqual(first, Describe(SceneManager.GetActiveScene()));
        }

        [Test]
        public void The_committed_scene_matches_what_the_builder_makes_now()
        {
            string fresh = Describe(SceneManager.GetActiveScene());
            string committed;
            try
            {
                EditorSceneManager.OpenScene(DuelSceneBuilder.ScenePath, OpenSceneMode.Single);
                committed = Describe(SceneManager.GetActiveScene());
            }
            finally
            {
                EditorSceneManager.OpenScene(TempScene, OpenSceneMode.Single);
            }
            Assert.AreEqual(fresh, committed,
                "Duel.unity is out of date. Run Prototypes > MedievalChess > Build Duel Scene and commit the result.");
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
                if (c is MeshFilter mf && mf.sharedMesh != null) sb.Append($" mesh={mf.sharedMesh.name}");
                if (c is Renderer r) sb.Append(" mats=").Append(string.Join(",", r.sharedMaterials.Select(m => m ? m.name : "null")));
                if (c is Text text) sb.Append($" text='{text.text}' size={text.fontSize} color={text.color}");
                if (c is Camera cam) sb.Append($" fov={cam.fieldOfView:F2} clear={cam.clearFlags} bg={cam.backgroundColor}");
                if (c is Light l) sb.Append($" light={l.type} intensity={l.intensity:F2} shadows={l.shadows} color={l.color}");
                if (c is DuelController d)
                    sb.Append($" settings={(d.settings ? d.settings.name : "null")} prefabs={string.Join(",", d.piecePrefabs.Select(p => p ? p.name : "null"))}");
                sb.Append('\n');
            }
            for (int i = 0; i < t.childCount; i++) DescribeObject(sb, t.GetChild(i).gameObject, depth + 1);
        }
    }
}
