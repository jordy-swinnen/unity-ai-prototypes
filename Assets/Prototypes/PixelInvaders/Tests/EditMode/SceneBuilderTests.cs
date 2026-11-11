using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Prototypes.PixelInvaders.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Prototypes.PixelInvaders.Tests
{
    public class SceneBuilderTests
    {
        const string TempScenePath = "Assets/Prototypes/PixelInvaders/Scenes/TempBuilderTest.unity";

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(TempScenePath))
            {
                AssetDatabase.DeleteAsset(TempScenePath);
            }
        }

        [Test]
        public void BuildScene_creates_deterministic_scene_with_all_required_views_and_references()
        {
            PixelInvadersSceneBuilder.BuildScene(TempScenePath);
            string snapshot1 = CaptureSceneSnapshot();

            PixelInvadersSceneBuilder.BuildScene(TempScenePath);
            string snapshot2 = CaptureSceneSnapshot();

            Assert.AreEqual(snapshot1, snapshot2, "Building the scene twice must produce identical scene content");

            var game = Object.FindAnyObjectByType<PixelInvadersGame>();
            Assert.IsNotNull(game, "Scene must contain PixelInvadersGame");
            Assert.IsNotNull(game.settings, "PixelInvadersGame.settings must be assigned");
            Assert.IsNotNull(game.view, "PixelInvadersGame.view must be assigned");
            Assert.IsNotNull(game.audioDirector, "PixelInvadersGame.audioDirector must be assigned");

            var view = game.view;
            Assert.IsNotNull(view.mainCamera);
            Assert.AreEqual(55, view.invaderQuads.Length);
            Assert.AreEqual(4, view.bunkerViews.Length);
            Assert.IsNotNull(view.cannonQuad);
            Assert.IsNotNull(view.ufoQuad);
            Assert.IsNotNull(view.capsuleQuad);
            Assert.IsNotNull(view.scoreText);
            Assert.IsNotNull(view.highScoreText);
            Assert.IsNotNull(view.waveText);

            // Re-open the real scene so the Editor stays on the prototype's main scene
            if (File.Exists(PixelInvadersSceneBuilder.ScenePath))
            {
                EditorSceneManager.OpenScene(PixelInvadersSceneBuilder.ScenePath, OpenSceneMode.Single);
            }
        }

        static string CaptureSceneSnapshot()
        {
            var sb = new StringBuilder();
            var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OrderBy(GetHierarchyPath);
            foreach (var t in transforms)
            {
                Vector3 p = t.localPosition;
                Vector3 r = t.localEulerAngles;
                Vector3 s = t.localScale;
                sb.AppendFormat("{0}|active={1}|pos=({2:F3},{3:F3},{4:F3})|rot=({5:F1},{6:F1},{7:F1})|scale=({8:F3},{9:F3},{10:F3})\n",
                    GetHierarchyPath(t), t.gameObject.activeSelf, p.x, p.y, p.z, r.x, r.y, r.z, s.x, s.y, s.z);
            }
            return sb.ToString();
        }

        static string GetHierarchyPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}
