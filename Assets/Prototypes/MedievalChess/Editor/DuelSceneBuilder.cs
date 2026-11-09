using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Prototypes.MedievalChess.Editor
{
    /// <summary>
    /// Builds the duel scene: a bare board, two pieces' worth of models, a camera, a key light and a
    /// label. Menu: Prototypes > MedievalChess > Build Duel Scene. Scenes are generated, never edited
    /// by hand, and running it twice gives the same content.
    /// </summary>
    public static class DuelSceneBuilder
    {
        const string Root = "Assets/Prototypes/MedievalChess";
        public const string ScenePath = Root + "/Scenes/Duel.unity";
        public const string SettingsPath = Root + "/Settings/MedievalChessSettings.asset";
        internal const string MaterialFolder = Root + "/Materials";
        internal const string ModelFolder = Root + "/Art/Exports";

        [MenuItem("Prototypes/MedievalChess/Build Duel Scene")]
        public static void BuildDefault()
        {
            // Build replaces the open scene, so offer to save any unsaved changes first.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build(ScenePath);
        }

        public static Scene Build(string scenePath)
        {
            EnsureFolder(Root + "/Settings");
            EnsureFolder(Root + "/Scenes");
            EnsureFolder(MaterialFolder);
            CreateSettingsIfMissing();

            // Load every asset after NewScene: it unloads unused assets, and a reference loaded before
            // it can come back empty.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var settings = AssetDatabase.LoadAssetAtPath<MedievalChessSettings>(SettingsPath);

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.07f, 0.065f, 0.06f);
            RenderSettings.fog = false;

            var bone = Material("Bone", settings.boneColor, 0.25f);
            var cinnabar = Material("Cinnabar", settings.cinnabarColor, 0.25f);
            var iron = Material("Iron", settings.ironColor, 0.45f);
            BuildBoard(Material("Tile Light", settings.lightSquareColor, 0.15f), Material("Tile Dark", settings.darkSquareColor, 0.15f),
                Material("Floor", settings.floorColor, 0.1f));
            var cam = BuildCamera(settings);
            BuildLight(settings);
            var label = BuildLabel();
            BuildController(settings, bone, cinnabar, iron, cam, label);

            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            return scene;
        }

        // --- assets ---

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }

        internal static void CreateSettingsIfMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<MedievalChessSettings>(SettingsPath) != null) return;
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<MedievalChessSettings>(), SettingsPath);
            AssetDatabase.SaveAssets();
        }

        internal static Material Material(string name, Color color, float smoothness)
        {
            string path = $"{MaterialFolder}/MedievalChess {name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("URP Lit shader not found.");
                mat = new Material(shader) { name = $"MedievalChess {name}" };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // --- scene objects ---

        static void BuildBoard(Material light, Material dark, Material floor)
        {
            // A strip of the board: 7 by 3 squares, each 1 m, top face at y = 0.
            for (int x = -3; x <= 3; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    var tile = Box($"Tile {x} {z}", new Vector3(x, -0.06f, z), new Vector3(1f, 0.12f, 1f), (x + z) % 2 == 0 ? light : dark);
                    tile.transform.localScale = new Vector3(1f, 0.12f, 1f);
                }
            }
            Box("Floor", new Vector3(0f, -0.22f, 0f), new Vector3(20f, 0.2f, 20f), floor);
        }

        internal static GameObject Box(string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        static Camera BuildCamera(MedievalChessSettings s)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.05f, 0.04f);
            cam.fieldOfView = s.duelCameraFieldOfView;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 100f;
            go.AddComponent<AudioListener>();

            // Pitched down around x, looking at the middle of the duel from the near side (negative z).
            var rotation = Quaternion.Euler(s.duelCameraPitch, 0f, 0f);
            var focus = new Vector3(0f, 0.35f, 0f);
            go.transform.rotation = rotation;
            go.transform.position = focus - (rotation * Vector3.forward) * s.duelCameraDistance;
            return cam;
        }

        static void BuildLight(MedievalChessSettings s)
        {
            var go = new GameObject("Key Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = s.keyLightColor;
            light.intensity = 1.4f;
            light.shadows = LightShadows.Soft;
            go.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
        }

        static Text BuildLabel()
        {
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var go = new GameObject("Label");
            go.transform.SetParent(canvasGo.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(30f, 24f);
            rt.sizeDelta = new Vector2(1500f, 90f);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 28;
            text.alignment = TextAnchor.LowerLeft;
            text.color = new Color32(0xE9, 0xDF, 0xC5, 0xFF);
            text.text = "Medieval Chess duel";
            text.raycastTarget = false;
            return text;
        }

        static void BuildController(MedievalChessSettings settings, Material bone, Material cinnabar, Material iron, Camera cam, Text label)
        {
            var go = new GameObject("Duel");
            var duel = go.AddComponent<DuelController>();
            duel.settings = settings;
            duel.bone = bone;
            duel.cinnabar = cinnabar;
            duel.iron = iron;
            duel.cam = cam;
            duel.label = label;
            var types = (PieceType[])Enum.GetValues(typeof(PieceType));
            duel.piecePrefabs = new GameObject[types.Length];
            foreach (var type in types)
            {
                string path = $"{ModelFolder}/{type.ToString().ToLowerInvariant()}.glb";
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null) throw new InvalidOperationException($"Model not imported: {path}. Run Tools/blender/MedievalChess/make_pieces.py");
                duel.piecePrefabs[(int)type] = model;
            }
        }
    }
}
