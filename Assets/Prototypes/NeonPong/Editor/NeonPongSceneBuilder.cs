using System;
using System.Collections.Generic;
using System.IO;
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

            var rails = BuildTable(mats, settings);
            var leftPaddle = Place("paddle", "Paddle Left", new Vector3(-settings.paddleX, 0f, 0f), PaddleMap(mats.paddleLeft, mats));
            var rightPaddle = Place("paddle", "Paddle Right", new Vector3(settings.paddleX, 0f, 0f), PaddleMap(mats.paddleRight, mats));
            var ball = Place("ball", "Ball", new Vector3(0f, settings.ballRadius, 0f), PaddleMap(mats.ball, mats));
            var trail = BuildTrail(ball, mats, settings);
            var ballLight = BuildBallLight(ball, settings);
            var camera = BuildCamera(settings);
            BuildVolume(profile);
            var ui = BuildUi(settings);
            var game = BuildGame(settings, leftPaddle, rightPaddle, ball, rails, trail, ballLight, ui);

            camera.AddComponent<NeonPongCameraRig>().game = game;
            BuildBursts(game, mats, settings);
            BuildAudio(game);

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
            public Material body, grid, rail, paddleLeft, paddleRight, ball, spark, trail;
        }

        static Materials BuildMaterials(NeonPongSettings s)
        {
            return new Materials
            {
                body = Material("Table Body", s.baseColor, Color.black, 0f, 0.15f),
                grid = Material("Table Grid", s.gridColor, s.gridColor, s.gridEmission, 0.1f),
                rail = Material("Rail", s.gridColor, s.gridColor, s.railEmission, 0.1f),
                paddleLeft = Material("Paddle Left", s.player1Color, s.player1Color, s.paddleEmission, 0.2f),
                paddleRight = Material("Paddle Right", s.player2Color, s.player2Color, s.paddleEmission, 0.2f),
                ball = Material("Ball", s.ballColor, s.ballColor, s.ballEmission, 0.3f),
                spark = Additive("Spark", Color.white * s.burstEmission),
                trail = Additive("Trail", Color.white * s.trailEmission),
            };
        }

        /// <summary>Materials are named NeonPong plus the name without spaces (NeonPongTableBody), as AGENTS.md asks.</summary>
        static string MaterialPath(string name) => $"{MaterialFolder}/NeonPong{name.Replace(" ", "")}.mat";

        /// <summary>
        /// A URP Lit material. Emission is the colour times an intensity, worked out in linear light
        /// (so the colour stays saturated), then converted back because SetColor converts it again.
        /// </summary>
        static Material Material(string name, Color baseColor, Color emissionColor, float intensity, float smoothness)
        {
            string path = MaterialPath(name);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("URP Lit shader not found.");
                mat = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", smoothness);
            var emission = (emissionColor.linear * intensity).gamma;
            mat.SetColor("_EmissionColor", emission);
            if (intensity > 0f && emission.maxColorComponent > 0f) mat.EnableKeyword("_EMISSION");
            else mat.DisableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>An unlit, additive particle material. The colour is a brightness multiplier (HDR).</summary>
        static Material Additive(string name, Color tint)
        {
            string path = MaterialPath(name);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null) throw new InvalidOperationException("URP Particles/Unlit shader not found.");
                mat = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(mat, path);
            }
            // Transparent surface, additive blend, no depth write.
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 2f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.One);
            mat.SetFloat("_DstBlend", (float)BlendMode.One);
            mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", (float)BlendMode.One);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetColor("_BaseColor", tint);
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

            // Reuse the overrides that are already in the profile, so a rebuild keeps the same
            // file content (new objects get new IDs, and the asset would change on every build).
            var bloom = GetOrAdd<Bloom>(profile);
            bloom.threshold.Override(s.bloomThreshold);
            bloom.intensity.Override(s.bloomIntensity);
            bloom.scatter.Override(s.bloomScatter);

            var vignette = GetOrAdd<Vignette>(profile);
            vignette.intensity.Override(s.vignetteIntensity);
            vignette.smoothness.Override(s.vignetteSmoothness);

            var tone = GetOrAdd<Tonemapping>(profile);
            tone.mode.Override(TonemappingMode.Neutral);

            EditorUtility.SetDirty(profile);
            return profile;
        }

        static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet(out T existing)) return existing;
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

        static Transform[] BuildTable(Materials mats, NeonPongSettings s)
        {
            var map = PaddleMap(mats.body, mats);
            Place("table", "Table", Vector3.zero, map);
            var top = Place("rail", "Rail Top", new Vector3(0f, 0f, s.halfWidth + RailHalf), map);
            var bottom = Place("rail", "Rail Bottom", new Vector3(0f, 0f, -(s.halfWidth + RailHalf)), map);
            Place("endline", "End Line Right", new Vector3(s.halfLength, 0f, 0f), map);
            Place("endline", "End Line Left", new Vector3(-s.halfLength, 0f, 0f), map);
            return new[] { top.transform, bottom.transform };
        }

        static TrailRenderer BuildTrail(GameObject ball, Materials mats, NeonPongSettings s)
        {
            var go = new GameObject("Ball Trail");
            go.transform.SetParent(ball.transform, false);
            var trail = go.AddComponent<TrailRenderer>();
            trail.sharedMaterial = mats.trail;
            trail.time = s.trailTimeMin;
            trail.widthMultiplier = s.trailWidth;
            trail.minVertexDistance = 0.05f;
            trail.alignment = LineAlignment.TransformZ; // faces up, so it reads from the camera above
            trail.numCapVertices = 4;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = false;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(s.ballColor, 0f), new GradientColorKey(s.ballColor, 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
            // Lie flat: the trail's facing axis is the transform's z, so point it up.
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            return trail;
        }

        static Light BuildBallLight(GameObject ball, NeonPongSettings s)
        {
            var go = new GameObject("Ball Light");
            go.transform.SetParent(ball.transform, false);
            go.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = s.ballColor;
            light.intensity = s.lightIntensity;
            light.range = s.lightRange;
            light.shadows = LightShadows.None;
            return light;
        }

        static void BuildBursts(NeonPongGame game, Materials mats, NeonPongSettings s)
        {
            var go = new GameObject("Bursts");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = s.burstLifetime;
            main.startSize = s.burstSize;
            main.gravityModifier = 0f;
            main.maxParticles = 400;
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));
            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLife.color = fade;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = mats.spark;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            var bursts = go.AddComponent<NeonPongBursts>();
            bursts.game = game;
            bursts.system = ps;
        }

        static void BuildAudio(NeonPongGame game)
        {
            var go = new GameObject("Audio");
            var audio = go.AddComponent<NeonPongAudio>();
            audio.game = game;
            audio.sfx = NewSource(go, "Sfx");
            audio.paddleSfx = NewSource(go, "Paddle Sfx");
            audio.wall = Clip("wall");
            audio.paddle = Clip("paddle");
            audio.serve = Clip("serve");
            audio.point = Clip("point");
            audio.win = Clip("win");
            audio.restart = Clip("restart");
            audio.musicSource = NewSource(go, "Music");
            audio.music = Clip("music");
            audio.musicSource.clip = audio.music;
            audio.musicSource.loop = true;
            audio.musicSource.volume = Mathf.Clamp01(game.settings.masterVolume * game.settings.musicVolume);
        }

        static AudioSource NewSource(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        static AudioClip Clip(string name)
        {
            string path = $"{Root}/Audio/{name}.wav";
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
                throw new InvalidOperationException($"Missing sound: {path}. Run Tools/audio/NeonPong/make_sounds.py first.");
            return clip;
        }

        static GameObject BuildCamera(NeonPongSettings s)
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
            return go;
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
            public Image wash;
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

            // The wash goes first so the text draws on top of it.
            var washGo = new GameObject("Wash");
            washGo.transform.SetParent(canvasGo.transform, false);
            var washRect = washGo.AddComponent<RectTransform>();
            washRect.anchorMin = Vector2.zero;
            washRect.anchorMax = Vector2.one;
            washRect.offsetMin = washRect.offsetMax = Vector2.zero;
            var wash = washGo.AddComponent<Image>();
            wash.color = new Color(1f, 1f, 1f, 0f);
            wash.raycastTarget = false;

            return new Ui
            {
                wash = wash,
                left = MakeText(canvasGo.transform, "Score Left", s.player1Color, s.scoreFontSize, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 1f), new Vector2(-s.scoreOffsetX, s.scoreOffsetY), new Vector2(300f, 200f)),
                right = MakeText(canvasGo.transform, "Score Right", s.player2Color, s.scoreFontSize, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 1f), new Vector2(s.scoreOffsetX, s.scoreOffsetY), new Vector2(300f, 200f)),
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

        static NeonPongGame BuildGame(NeonPongSettings s, GameObject leftPaddle, GameObject rightPaddle,
            GameObject ball, Transform[] rails, TrailRenderer trail, Light ballLight, Ui ui)
        {
            var go = new GameObject("Game");
            var game = go.AddComponent<NeonPongGame>();
            game.settings = s;
            var view = go.AddComponent<NeonPongView>();
            view.game = game;
            view.leftPaddle = leftPaddle.transform;
            view.rightPaddle = rightPaddle.transform;
            view.ball = ball.transform;
            view.rails = rails;
            view.trail = trail;
            view.ballLight = ballLight;
            view.scoreLeft = ui.left;
            view.scoreRight = ui.right;
            view.message = ui.message;
            view.wash = ui.wash;
            return game;
        }
    }
}
