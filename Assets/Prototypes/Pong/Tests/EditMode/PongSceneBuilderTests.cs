using System.Linq;
using System.Text;
using NUnit.Framework;
using Prototypes.Pong.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Prototypes.Pong.Tests
{
    public class PongSceneBuilderTests
    {
        // The test builds into its own scene so the real Pong.unity is not rewritten.
        const string TempScenePath = PongSceneBuilder.RootFolder + "/Scenes/__BuilderTest.unity";
        const float Tolerance = 0.01f;

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TempScenePath);
        }

        [Test]
        public void Build_ContainsEveryPartOfTheGame()
        {
            PongSceneBuilder.Build(TempScenePath);

            string[] names = SceneObjectNames();
            foreach (string expected in new[]
            {
                "Main Camera", "Directional Light", "Table", "WallTop", "WallBottom",
                "Paddle1", "Paddle2", "Ball", "PongGame", "Canvas", "Score1", "Score2", "Message",
            })
            {
                Assert.Contains(expected, names);
            }

            var game = Object.FindFirstObjectByType<PongGame>();
            Assert.IsNotNull(game);
            Assert.IsNotNull(game.Settings);
            Assert.IsNotNull(game.GetComponent<PongView>());
        }

        [Test]
        public void Build_PutsTheModelsWhereTheRulesExpectThem()
        {
            PongSceneBuilder.Build(TempScenePath);
            var settings = AssetDatabase.LoadAssetAtPath<PongSettings>(PongSceneBuilder.SettingsPath);

            Bounds table = BoundsOf("Table");
            Assert.AreEqual(0f, table.max.y, Tolerance, "table top is the y = 0 plane");
            Assert.AreEqual(settings.fieldHalfLength * 2f, table.size.x, Tolerance);
            Assert.AreEqual(settings.fieldHalfWidth * 2f, table.size.z, Tolerance);

            Assert.AreEqual(settings.fieldHalfWidth, BoundsOf("WallTop").min.z, Tolerance, "inner face of the top wall");
            Assert.AreEqual(-settings.fieldHalfWidth, BoundsOf("WallBottom").max.z, Tolerance, "inner face of the bottom wall");

            Assert.AreEqual(-settings.paddleX, BoundsOf("Paddle1").center.x, Tolerance);
            Assert.AreEqual(settings.paddleX, BoundsOf("Paddle2").center.x, Tolerance);
            Assert.AreEqual(settings.paddleHalfLength * 2f, BoundsOf("Paddle1").size.z, Tolerance);
            Assert.AreEqual(settings.paddleHalfThickness * 2f, BoundsOf("Paddle1").size.x, Tolerance);

            Bounds ball = BoundsOf("Ball");
            Assert.AreEqual(0f, ball.min.y, Tolerance, "ball rests on the table");
            Assert.AreEqual(settings.ballRadius * 2f, ball.size.x, Tolerance);
        }

        [Test]
        public void Build_Twice_GivesTheSameScene()
        {
            PongSceneBuilder.Build(TempScenePath);
            string first = Describe(SceneManager.GetActiveScene());

            PongSceneBuilder.Build(TempScenePath);
            string second = Describe(SceneManager.GetActiveScene());

            Assert.IsNotEmpty(first);
            Assert.AreEqual(first, second);
        }

        static string[] SceneObjectNames()
        {
            return Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(t => t.name)
                .ToArray();
        }

        static Bounds BoundsOf(string objectName)
        {
            GameObject target = GameObject.Find(objectName);
            Assert.IsNotNull(target, objectName);
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            Assert.IsNotEmpty(renderers, $"{objectName} has no renderer");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        // A text dump of the whole scene: every object, its transform, its components and what
        // they point at. Unity writes new object IDs on every save, so scene files differ
        // byte for byte even when the scenes are the same. This compares the content instead.
        static string Describe(Scene scene)
        {
            var text = new StringBuilder();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                DescribeObject(root.transform, text, string.Empty);
            }

            return text.ToString();
        }

        static void DescribeObject(Transform target, StringBuilder text, string parentPath)
        {
            string path = $"{parentPath}/{target.name}";
            text.AppendLine($"{path} active={target.gameObject.activeSelf} pos={target.localPosition:F3} rot={target.localEulerAngles:F3} scale={target.localScale:F3}");

            foreach (Component component in target.GetComponents<Component>())
            {
                if (component == null)
                {
                    text.AppendLine($"  missing component on {path}");
                    continue;
                }

                text.AppendLine($"  {component.GetType().Name}");
                var serialized = new SerializedObject(component);
                SerializedProperty property = serialized.GetIterator();
                while (property.NextVisible(true))
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.propertyPath != "m_Script")
                    {
                        string targetName = property.objectReferenceValue != null ? property.objectReferenceValue.name : "null";
                        text.AppendLine($"    {property.propertyPath} -> {targetName}");
                    }
                }
            }

            foreach (Transform child in target)
            {
                DescribeObject(child, text, path);
            }
        }
    }
}
