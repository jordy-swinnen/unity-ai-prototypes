using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Prototypes.NeonPong.Editor
{
    /// <summary>
    /// Builds the Neon Pong scene from nothing: Prototypes > NeonPong > Build Scene.
    /// Scenes are generated, never edited by hand. Running it twice gives the same content.
    /// </summary>
    public static class NeonPongSceneBuilder
    {
        const string Root = "Assets/Prototypes/NeonPong";
        public const string ScenePath = Root + "/Scenes/NeonPong.unity";
        public const string SettingsPath = Root + "/Settings/NeonPongSettings.asset";
        const string ProfilePath = Root + "/Settings/NeonPongVolumeProfile.asset";
        const string MaterialFolder = Root + "/Materials";
        const string ModelFolder = Root + "/Art/Exports";

        // Half the rail thickness (the rail model is 0.3 wide): its inner face sits on the play-area edge.
        const float RailHalf = 0.15f;

        [MenuItem("Prototypes/NeonPong/Build Scene")]
        public static void BuildDefault()
        {
            Build(ScenePath);
        }

        public static Scene Build(string scenePath)
        {
            EnsureFolder(Root + "/Settings");
            EnsureFolder(Root + "/Scenes");
            EnsureFolder(MaterialFolder);

            CreateSettingsIfMissing();

            // Load every asset after NewScene: it unloads unused assets, and a reference
            // loaded before it can come back empty.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var settings = AssetDatabase.LoadAssetAtPath<NeonPongSettings>(SettingsPath);

            var mats = BuildMaterials(settings);
            var profile = BuildProfile(settings);

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.02f, 0.025f, 0.05f);
            RenderSettings.fog = false;

            BuildTable(mats, settings);
            var leftPaddle = Place("paddle", "Paddle Left", new Vector3(-settings.paddleX, 0f, 0f), PaddleMap(mats.paddleLeft, mats));
            var rightPaddle = Place("paddle", "Paddle Right", new Vector3(settings.paddleX, 0f, 0f), PaddleMap(mats.paddleRight, mats));
            var ball = Place("ball", "Ball", new Vector3(0f, settings.ballRadius, 0f), PaddleMap(mats.ball, mats));
            BuildCamera(settings);
            BuildVolume(profile);
            var ui = BuildUi(settings);
            BuildGame(settings, leftPaddle, rightPaddle, ball, ui);

            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            return scene;
        }

        // --- assets ---

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }

        static void CreateSettingsIfMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<NeonPongSettings>(SettingsPath) != null) return;
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<NeonPongSettings>(), SettingsPath);
            AssetDatabase.SaveAssets();
        }

        class Materials
        {
            public Material body, grid, rail, paddleLeft, paddleRight, ball;
        }

        static Materials BuildMaterials(NeonPongSettings s)
        {
            return new Materials
            {
                body = Material("Table Body", s.baseColor, Color.black, 0.15f),
                grid = Material("Table Grid", s.gridColor, s.gridColor * s.gridEmission, 0.1f),
                rail = Material("Rail", s.gridColor, s.gridColor * s.railEmission, 0.1f),
                paddleLeft = Material("Paddle Left", s.player1Color, s.player1Color * s.paddleEmission, 0.2f),
                paddleRight = Material("Paddle Right", s.player2Color, s.player2Color * s.paddleEmission, 0.2f),
                ball = Material("Ball", s.ballColor, s.ballColor * s.ballEmission, 0.3f),
            };
        }

        static Material Material(string name, Color baseColor, Color emission, float smoothness)
        {
            string path = $"{MaterialFolder}/NeonPong {name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("URP Lit shader not found.");
                mat = new Material(shader) { name = $"NeonPong {name}" };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetColor("_EmissionColor", emission);
            if (emission.maxColorComponent > 0f) mat.EnableKeyword("_EMISSION");
            else mat.DisableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static VolumeProfile BuildProfile(NeonPongSettings s)
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            // Rebuild the overrides so the profile always matches the settings.
            foreach (var old in new List<VolumeComponent>(profile.components))
            {
                AssetDatabase.RemoveObjectFromAsset(old);
                UnityEngine.Object.DestroyImmediate(old, true);
            }
            profile.components.Clear();

            var bloom = Add<Bloom>(profile);
            bloom.threshold.Override(s.bloomThreshold);
            bloom.intensity.Override(s.bloomIntensity);
            bloom.scatter.Override(s.bloomScatter);

            var vignette = Add<Vignette>(profile);
            vignette.intensity.Override(s.vignetteIntensity);
            vignette.smoothness.Override(0.4f);

            var tone = Add<Tonemapping>(profile);
            tone.mode.Override(TonemappingMode.Neutral);

            EditorUtility.SetDirty(profile);
            return profile;
        }

        static T Add<T>(VolumeProfile profile) where T : VolumeComponent
        {
            var component = profile.Add<T>(false);
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        // --- scene objects ---

        static GameObject Place(string model, string name, Vector3 position, Dictionary<string, Material> map)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}/{model}.glb");
            if (asset == null) throw new InvalidOperationException($"Model not imported: {model}.glb");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            go.name = name;
            go.transform.position = position;
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
            {
                var slots = r.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    string imported = slots[i] != null ? slots[i].name : "(none)";
                    if (!map.TryGetValue(imported, out var mat))
                        throw new InvalidOperationException($"{model}.glb: no material mapped for '{imported}'.");
                    slots[i] = mat;
                }
                r.sharedMaterials = slots;
            }
            return go;
        }

        static Dictionary<string, Material> PaddleMap(Material own, Materials m)
        {
            // The models name their materials; each one maps to a URP Lit material asset.
            return new Dictionary<string, Material>
            {
                { "body", m.body }, { "grid", m.grid }, { "rail", m.rail },
                { "paddle", own }, { "ball", own },
            };
        }

        static void BuildTable(Materials mats, NeonPongSettings s)
        {
            var map = PaddleMap(mats.body, mats);
            Place("table", "Table", Vector3.zero, map);
            Place("rail", "Rail Top", new Vector3(0f, 0f, s.halfWidth + RailHalf), map);
            Place("rail", "Rail Bottom", new Vector3(0f, 0f, -(s.halfWidth + RailHalf)), map);
            Place("endline", "End Line Right", new Vector3(s.halfLength, 0f, 0f), map);
            Place("endline", "End Line Left", new Vector3(-s.halfLength, 0f, 0f), map);
        }

        static void BuildCamera(NeonPongSettings s)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = s.baseColor;
            cam.fieldOfView = s.cameraFieldOfView;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 100f;
            go.AddComponent<AudioListener>();

            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.volumeLayerMask = ~0;

            // Pitched down around x, looking at the table centre from the near side (negative z).
            var rotation = Quaternion.Euler(s.cameraPitch, 0f, 0f);
            go.transform.rotation = rotation;
            go.transform.position = -(rotation * Vector3.forward) * s.cameraDistance;
        }

        static void BuildVolume(VolumeProfile profile)
        {
            var go = new GameObject("Global Volume");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        class Ui
        {
            public Text left, right, message;
        }

        static Ui BuildUi(NeonPongSettings s)
        {
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return new Ui
            {
                left = MakeText(canvasGo.transform, "Score Left", s.player1Color, 140, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 1f), new Vector2(-260f, -110f), new Vector2(300f, 200f)),
                right = MakeText(canvasGo.transform, "Score Right", s.player2Color, 140, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 1f), new Vector2(260f, -110f), new Vector2(300f, 200f)),
                message = MakeText(canvasGo.transform, "Message", s.textColor, 90, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 360f)),
            };
        }

        static Text MakeText(Transform parent, string name, Color color, int size, TextAnchor align,
            Vector2 anchor, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = dimensions;
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = align;
            text.color = color;
            text.text = name == "Message" ? string.Empty : "0";
            text.raycastTarget = false;
            return text;
        }

        static void BuildGame(NeonPongSettings s, GameObject leftPaddle, GameObject rightPaddle,
            GameObject ball, Ui ui)
        {
            var go = new GameObject("Game");
            var game = go.AddComponent<NeonPongGame>();
            game.settings = s;
            var view = go.AddComponent<NeonPongView>();
            view.game = game;
            view.leftPaddle = leftPaddle.transform;
            view.rightPaddle = rightPaddle.transform;
            view.ball = ball.transform;
            view.scoreLeft = ui.left;
            view.scoreRight = ui.right;
            view.message = ui.message;
        }
    }
}
