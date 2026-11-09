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
        internal const string TextureFolder = Root + "/Art/Textures";
        public const string SurfaceShaderName = "Prototypes/MedievalChess/Lit";

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

            var bone = Surface("Bone", settings.boneColor, 0.25f, 0f, settings, "grain", world: false);
            var cinnabar = Surface("Cinnabar", settings.cinnabarColor, 0.25f, 0f, settings, "grain", world: false);
            var iron = Surface("Iron", settings.ironColor, 0.45f, 0f, settings, "grain", world: false);
            var trim = Surface("Trim", settings.trimColor, 0.55f, 0.25f, settings, "grain", world: false);
            BuildBoard(Surface("Tile Light", settings.lightSquareColor, 0.15f, 0f, settings, "limestone", world: true),
                Surface("Tile Dark", settings.darkSquareColor, 0.15f, 0f, settings, "slate", world: true),
                Surface("Floor", settings.floorColor, 0.1f, 0f, settings, "flagstone", world: true));
            var cam = BuildCamera(settings);
            BuildLight(settings);
            var label = BuildLabel();
            BuildController(settings, bone, cinnabar, iron, trim, cam, label);

            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            return scene;
        }

        // --- assets ---

        /// <summary>Asset names have no spaces (Unity's command line tools trip on them): "Tile Light" is MedievalChessTileLight.</summary>
        internal static string MaterialName(string name) => "MedievalChess" + name.Replace(" ", "");

        internal static string MaterialPath(string name) => $"{MaterialFolder}/{MaterialName(name)}.mat";

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

        internal static Material Material(string name, Color color, float smoothness, float metallic = 0f)
        {
            string path = MaterialPath(name);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("URP Lit shader not found.");
                mat = new Material(shader) { name = MaterialName(name) };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>
        /// A material with the game's surface shader. A world-space one (board, frame, floor) projects its texture from
        /// the world so no square repeats its neighbour. An object-space one (the pieces) lays the grain on the piece itself.
        /// The strengths come from the settings (look.surface). A missing texture file just gives a flat colour.
        /// </summary>
        internal static Material Surface(string name, Color color, float smoothness, float metallic, MedievalChessSettings settings,
            string texture, bool world, bool rotate = false)
        {
            var shader = Shader.Find(SurfaceShaderName);
            if (shader == null) throw new InvalidOperationException($"Shader {SurfaceShaderName} not found. Is Shaders/MedievalLit.shader imported?");
            string path = MaterialPath(name);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = MaterialName(name) };
                AssetDatabase.CreateAsset(mat, path);
            }
            if (mat.shader != shader) mat.shader = shader;

            var look = settings.look.surface;
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/{texture}_albedo.png"));
            mat.SetTexture("_NormalMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/{texture}_normal.png"));
            mat.SetFloat("_WorldMapping", world ? 1f : 0f);
            mat.SetFloat("_MapRotate", rotate ? 1f : 0f);
            mat.SetFloat("_TextureStrength", world ? look.textureStrength : look.pieceGrain);
            mat.SetFloat("_NormalStrength", world ? look.normalStrength : 0f);
            mat.SetFloat("_MapScale", world ? 1f / Mathf.Max(look.mapScale, 0.01f) : look.pieceGrainScale);
            mat.SetFloat("_AoStrength", look.occlusionStrength);
            mat.SetColor("_EdgeColor", look.dissolveEdgeColor);
            mat.SetFloat("_EdgeWidth", look.dissolveEdgeWidth);
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

        static void BuildController(MedievalChessSettings settings, Material bone, Material cinnabar, Material iron, Material trim, Camera cam, Text label)
        {
            var go = new GameObject("Duel");
            var duel = go.AddComponent<DuelController>();
            duel.settings = settings;
            duel.bone = bone;
            duel.cinnabar = cinnabar;
            duel.iron = iron;
            duel.trim = trim;
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
