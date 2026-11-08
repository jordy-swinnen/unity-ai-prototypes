using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Prototypes.Pong.Editor
{
    /// <summary>
    /// Builds the Pong scene from nothing: the models exported by Blender, a camera, a light,
    /// the game objects and the score text. Nobody edits the scene by hand. Run it again
    /// and you get the same scene.
    /// </summary>
    public static class PongSceneBuilder
    {
        public const string RootFolder = "Assets/Prototypes/Pong";
        public const string ScenePath = RootFolder + "/Scenes/Pong.unity";
        public const string SettingsPath = RootFolder + "/Settings/PongSettings.asset";
        const string ExportFolder = RootFolder + "/Art/Exports/";

        [MenuItem("Prototypes/Pong/Build Scene")]
        public static void BuildScene()
        {
            Build(ScenePath);
        }

        public static void Build(string scenePath)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Load the settings after NewScene. Creating a scene unloads unused assets, and a
            // settings object loaded earlier would then be an empty reference in the saved scene.
            PongSettings settings = EnsureSettings();

            CreateCamera();
            CreateLight();

            // Heights: the table top is y = 0. The models are centred on their own origin.
            float wallZ = settings.fieldHalfWidth + 0.25f;
            PlaceModel("table", "Table", new Vector3(0f, -0.2f, 0f));
            PlaceModel("wall", "WallTop", new Vector3(0f, 0.3f, wallZ));
            PlaceModel("wall", "WallBottom", new Vector3(0f, 0.3f, -wallZ));
            Transform paddle1 = PlaceModel("paddle", "Paddle1", new Vector3(-settings.paddleX, 0.25f, 0f));
            Transform paddle2 = PlaceModel("paddle", "Paddle2", new Vector3(settings.paddleX, 0.25f, 0f));
            Transform ball = PlaceModel("ball", "Ball", new Vector3(0f, settings.ballRadius, 0f));

            var gameObject = new GameObject("PongGame");
            var game = gameObject.AddComponent<PongGame>();
            SetReference(game, "settings", settings);

            Transform canvas = CreateCanvas();
            // Scores sit on the green table, not on the light walls, so the white text stays readable.
            Text score1 = CreateText("Score1", canvas, new Vector2(-220f, -200f), 110, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f));
            Text score2 = CreateText("Score2", canvas, new Vector2(220f, -200f), 110, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f));
            Text message = CreateText("Message", canvas, new Vector2(0f, 0f), 64, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));

            var view = gameObject.AddComponent<PongView>();
            SetReference(view, "game", game);
            SetReference(view, "ball", ball);
            SetReference(view, "paddle1", paddle1);
            SetReference(view, "paddle2", paddle2);
            SetReference(view, "score1Text", score1);
            SetReference(view, "score2Text", score2);
            SetReference(view, "messageText", message);

            Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
            if (!EditorSceneManager.SaveScene(scene, scenePath))
            {
                throw new InvalidOperationException($"Could not save the scene to {scenePath}.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static PongSettings EnsureSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PongSettings>(SettingsPath);
            if (settings != null)
            {
                return settings;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
            settings = ScriptableObject.CreateInstance<PongSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        static Transform PlaceModel(string model, string objectName, Vector3 position)
        {
            string path = $"{ExportFolder}{model}.glb";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"Model not found or not imported: {path}. Run the Blender script first.");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            instance.name = objectName;
            instance.transform.position = position;
            return instance.transform;
        }

        static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 40f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 60f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.05f, 0.07f);
            cameraObject.AddComponent<AudioListener>();
            // Straight down at the table. Screen up is world +z, screen right is world +x.
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 18f, 0f), Quaternion.Euler(90f, 0f, 0f));
        }

        static void CreateLight()
        {
            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        static Transform CreateCanvas()
        {
            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            return canvasObject.transform;
        }

        static Text CreateText(string objectName, Transform parent, Vector2 position, int fontSize, TextAnchor alignment, Vector2 anchor)
        {
            var textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            var rect = (RectTransform)textObject.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(900f, 220f);

            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = string.Empty;
            return text;
        }

        static void SetReference(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException($"{target.GetType().Name} has no serialized field '{field}'.");
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
