using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Prototypes.MedievalChess.Editor
{
    /// <summary>
    /// Builds the game scene: the board, the pieces' models and materials, the trays, the camera, the light, the
    /// highlight rings, the menus and the controller. Menu: Prototypes > MedievalChess > Build Scene. Scenes are
    /// generated, never edited by hand, and running it twice gives the same content.
    /// </summary>
    public static class GameSceneBuilder
    {
        const string Root = "Assets/Prototypes/MedievalChess";
        public const string ScenePath = Root + "/Scenes/MedievalChess.unity";
        const string ProfilePath = Root + "/Settings/MedievalChessVolumeProfile.asset";

        [MenuItem("Prototypes/MedievalChess/Build Scene")]
        public static void BuildDefault()
        {
            // Build replaces the open scene, so offer to save any unsaved changes first.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build(ScenePath);
        }

        public static Scene Build(string scenePath)
        {
            DuelSceneBuilder.EnsureFolder(Root + "/Settings");
            DuelSceneBuilder.EnsureFolder(Root + "/Scenes");
            DuelSceneBuilder.EnsureFolder(DuelSceneBuilder.MaterialFolder);
            DuelSceneBuilder.CreateSettingsIfMissing();

            // Load every asset after NewScene: it unloads unused assets.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var settings = AssetDatabase.LoadAssetAtPath<MedievalChessSettings>(DuelSceneBuilder.SettingsPath);

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = settings.look.ambientColor;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = settings.look.fogColor;
            RenderSettings.fogDensity = settings.look.fogDensity;

            var bone = DuelSceneBuilder.Surface("Bone", settings.boneColor, 0.25f, 0f, settings, "grain", world: false);
            var cinnabar = DuelSceneBuilder.Surface("Cinnabar", settings.cinnabarColor, 0.25f, 0f, settings, "grain", world: false);
            var iron = DuelSceneBuilder.Surface("Iron", settings.ironColor, 0.45f, 0f, settings, "grain", world: false);
            var trim = DuelSceneBuilder.Surface("Trim", settings.trimColor, 0.55f, 0.25f, settings, "grain", world: false);

            BuildBoard(settings);
            var cam = BuildCamera(settings);
            BuildLight(settings);
            var torches = BuildTorches(settings);
            BuildVolume(settings);
            var sound = BuildAudio(settings);
            var effects = BuildEffects(settings);
            var highlights = BuildHighlights(settings);
            var ui = BuildUi(settings);
            BuildEventSystem();
            BuildGame(settings, bone, cinnabar, iron, trim, ui, highlights, cam, sound, effects, torches);

            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            return scene;
        }

        // --- the board ---

        static void BuildBoard(MedievalChessSettings s)
        {
            var light = DuelSceneBuilder.Surface("Tile Light", s.lightSquareColor, 0.15f, 0f, s, "limestone", world: true);
            var dark = DuelSceneBuilder.Surface("Tile Dark", s.darkSquareColor, 0.15f, 0f, s, "slate", world: true);
            // The frame's boards run along its length, so the east and west sides turn the wood a quarter.
            var frame = DuelSceneBuilder.Surface("Frame", s.frameColor, 0.2f, 0f, s, "wood", world: true);
            var frameSide = DuelSceneBuilder.Surface("Frame Side", s.frameColor, 0.2f, 0f, s, "wood", world: true, rotate: true);
            var floor = DuelSceneBuilder.Surface("Floor", s.floorColor, 0.1f, 0f, s, "flagstone", world: true);

            var board = new GameObject("Board");
            for (int sq = 0; sq < 64; sq++)
            {
                var c = BoardGeometry.Centre(sq);
                var tile = DuelSceneBuilder.Box($"Tile {Squares.Name(sq)}", new Vector3(c.x, -0.06f, c.z), new Vector3(1f, 0.12f, 1f),
                    Squares.IsDark(sq) ? dark : light);
                tile.transform.SetParent(board.transform, true);
            }

            // a carved border round the squares
            var edge = new GameObject("Frame");
            foreach (var (name, position, size, material) in new[]
            {
                ("Frame North", new Vector3(0f, -0.1f, 4.35f), new Vector3(9.4f, 0.3f, 0.7f), frame),
                ("Frame South", new Vector3(0f, -0.1f, -4.35f), new Vector3(9.4f, 0.3f, 0.7f), frame),
                ("Frame East", new Vector3(4.35f, -0.1f, 0f), new Vector3(0.7f, 0.3f, 8f), frameSide),
                ("Frame West", new Vector3(-4.35f, -0.1f, 0f), new Vector3(0.7f, 0.3f, 8f), frameSide),
            })
                DuelSceneBuilder.Box(name, position, size, material).transform.SetParent(edge.transform, true);

            DuelSceneBuilder.Box("Floor", new Vector3(0f, -0.35f, 0f), new Vector3(24f, 0.2f, 24f), floor);

            // the trays where captured pieces stand
            float x = s.game.trayX;
            DuelSceneBuilder.Box("Tray Left", new Vector3(-x, -0.05f, 0f), new Vector3(1.4f, 0.1f, 8f), frameSide);
            DuelSceneBuilder.Box("Tray Right", new Vector3(x, -0.05f, 0f), new Vector3(1.4f, 0.1f, 8f), frameSide);
        }

        static Camera BuildCamera(MedievalChessSettings s)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.05f, 0.04f);
            cam.fieldOfView = s.game.cameraFieldOfView;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 100f;
            go.AddComponent<AudioListener>();

            // behind the bone side, looking at the middle of the board (the game moves it for the black side)
            var centre = new Vector3(0f, s.game.cameraFocusHeight, 0f);
            var rotation = Quaternion.Euler(s.game.cameraPitch, 0f, 0f);
            go.transform.rotation = rotation;
            go.transform.position = centre - (rotation * Vector3.forward) * s.game.cameraDistance;

            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.volumeLayerMask = ~0;

            var rig = go.AddComponent<GameCameraRig>();
            rig.settings = s;
            rig.cam = cam;
            return cam;
        }

        static void BuildLight(MedievalChessSettings s)
        {
            var go = new GameObject("Key Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = s.look.keyLightColor;
            light.intensity = s.look.keyLightIntensity;
            light.shadows = LightShadows.Soft;
            go.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
        }

        // --- torches and post-processing ---

        static TorchFlicker[] BuildTorches(MedievalChessSettings s)
        {
            var flickers = new System.Collections.Generic.List<TorchFlicker>();
            var look = s.look;
            var stone = DuelSceneBuilder.Surface("Torch Stone", s.floorColor, 0.1f, 0f, s, "slate", world: true);
            var iron = DuelSceneBuilder.Surface("Iron", s.ironColor, 0.45f, 0f, s, "grain", world: false);
            var flameMaterial = Emissive("Flame", look.torchColor, look.flameEmission);

            var group = new GameObject("Torches");
            int index = 0;
            foreach (float sx in new[] { -1f, 1f })
            {
                foreach (float sz in new[] { -1f, 1f })
                {
                    var torch = new GameObject($"Torch {index + 1}");
                    torch.transform.SetParent(group.transform, false);
                    torch.transform.position = new Vector3(sx * look.torchX, 0f, sz * look.torchZ);

                    float h = look.torchHeight;
                    Child(DuelSceneBuilder.Box("Pillar", Vector3.zero, new Vector3(0.5f, h, 0.5f), stone), torch, new Vector3(0f, h * 0.5f, 0f));
                    Child(DuelSceneBuilder.Box("Pillar Cap", Vector3.zero, new Vector3(0.7f, 0.12f, 0.7f), stone), torch, new Vector3(0f, h + 0.06f, 0f));
                    var bowl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    bowl.name = "Bowl";
                    UnityEngine.Object.DestroyImmediate(bowl.GetComponent<Collider>());
                    bowl.GetComponent<Renderer>().sharedMaterial = iron;
                    bowl.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);
                    Child(bowl, torch, new Vector3(0f, h + 0.22f, 0f));

                    var flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    flame.name = "Flame";
                    UnityEngine.Object.DestroyImmediate(flame.GetComponent<Collider>());
                    flame.GetComponent<Renderer>().sharedMaterial = flameMaterial;
                    flame.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                    flame.transform.localScale = new Vector3(0.3f, 0.55f, 0.3f);
                    Child(flame, torch, new Vector3(0f, h + 0.55f, 0f));

                    var lightGo = new GameObject("Torch Light");
                    var light = lightGo.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.color = look.torchColor;
                    light.intensity = look.torchLightIntensity;
                    light.range = look.torchLightRange;
                    light.shadows = LightShadows.None;
                    Child(lightGo, torch, new Vector3(0f, h + 0.8f, 0f));

                    var flicker = torch.AddComponent<TorchFlicker>();
                    flicker.torchLight = light;
                    flicker.flame = flame.transform;
                    flicker.settings = s;
                    flicker.seed = index + 1;
                    flickers.Add(flicker);
                    index++;
                }
            }
            return flickers.ToArray();
        }

        static void Child(GameObject child, GameObject parent, Vector3 localPosition)
        {
            child.transform.SetParent(parent.transform, false);
            child.transform.localPosition = localPosition;
        }

        /// <summary>A material that glows. The colour is HDR: it is converted so the glow keeps its hue (see urp-effects).</summary>
        static Material Emissive(string name, Color color, float intensity)
        {
            var mat = DuelSceneBuilder.Material(name, Color.black, 0f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", (color.linear * intensity).gamma);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void BuildVolume(MedievalChessSettings s)
        {
            var look = s.look;
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            // Reuse the overrides already in the profile, so a rebuild keeps the same file content.
            var bloom = GetOrAdd<Bloom>(profile);
            bloom.threshold.Override(look.bloomThreshold);
            bloom.intensity.Override(look.bloomIntensity);
            bloom.scatter.Override(look.bloomScatter);

            var vignette = GetOrAdd<Vignette>(profile);
            vignette.intensity.Override(look.vignetteIntensity);
            vignette.smoothness.Override(look.vignetteSmoothness);

            var tone = GetOrAdd<Tonemapping>(profile);
            tone.mode.Override(look.tonemapping == 1 ? TonemappingMode.ACES : TonemappingMode.Neutral);

            var grade = GetOrAdd<ColorAdjustments>(profile);
            grade.postExposure.Override(look.postExposure);
            grade.contrast.Override(look.contrast);
            grade.saturation.Override(look.saturation);
            EditorUtility.SetDirty(profile);

            var go = new GameObject("Global Volume");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet(out T existing)) return existing;
            var component = profile.Add<T>(false);
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        // --- highlight rings ---

        static HighlightView BuildHighlights(MedievalChessSettings s)
        {
            var go = new GameObject("Highlights");
            var view = go.AddComponent<HighlightView>();
            view.settings = s;

            var template = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            template.name = "Ring Template";
            UnityEngine.Object.DestroyImmediate(template.GetComponent<Collider>());
            template.transform.SetParent(go.transform, false);
            template.SetActive(false);
            view.ringTemplate = template;

            var g = s.game;
            view.selectionMaterial = Ring("Selection", g.selectionColor);
            view.moveMaterial = Ring("Move", g.moveColor);
            view.captureMaterial = Ring("Capture", g.captureColor);
            view.lastMoveMaterial = Ring("Last Move", g.lastMoveColor);
            view.checkMaterial = Ring("Check", g.checkColor);
            view.hoverMaterial = Ring("Hover", g.hoverColor);
            return view;
        }

        static Material Ring(string name, Color color)
        {
            string path = DuelSceneBuilder.MaterialPath("Ring " + name);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) throw new InvalidOperationException("URP Unlit shader not found.");
                mat = new Material(shader) { name = DuelSceneBuilder.MaterialName("Ring " + name) };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // --- the menus ---

        static UiView BuildUi(MedievalChessSettings s)
        {
            var g = s.game;
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var view = canvasGo.AddComponent<UiView>();
            view.normalColor = g.buttonColor;
            view.selectedColor = g.buttonSelectedColor;
            var parent = canvasGo.transform;

            // the wash that fades in at the end of a game. It comes first, so every panel and button draws on top of it.
            var washGo = new GameObject("Wash", typeof(RectTransform), typeof(Image));
            washGo.transform.SetParent(parent, false);
            Stretch(washGo.GetComponent<RectTransform>());
            var washImage = washGo.GetComponent<Image>();
            washImage.color = Color.clear;
            washImage.raycastTarget = false;
            view.wash = washImage;

            // start screen
            var start = Panel(parent, "Start Panel", Vector2.zero, Vector2.one, g.panelColor);
            Label(start, "Title", "MEDIEVAL CHESS", 110, new Vector2(0, 380), new Vector2(1600, 140), g.textColor);
            Label(start, "Opponent Label", "Choose your opponent", 44, new Vector2(0, 270), new Vector2(1000, 60), g.textColor);
            view.difficultyButtons = new[]
            {
                MakeButton(start, "Button Squire", "Squire", new Vector2(-400, 170), new Vector2(360, 110), g),
                MakeButton(start, "Button Knight", "Knight", new Vector2(0, 170), new Vector2(360, 110), g),
                MakeButton(start, "Button Marshal", "Marshal", new Vector2(400, 170), new Vector2(360, 110), g),
            };
            view.difficultyDescription = Label(start, "Difficulty Description", string.Empty, 34, new Vector2(0, 60), new Vector2(1500, 80), g.textColor);
            Label(start, "Side Label", "Play as", 44, new Vector2(0, -30), new Vector2(600, 60), g.textColor);
            view.sideButtons = new[]
            {
                MakeButton(start, "Button White", "Bone", new Vector2(-300, -120), new Vector2(260, 90), g),
                MakeButton(start, "Button Black", "Cinnabar", new Vector2(0, -120), new Vector2(260, 90), g),
                MakeButton(start, "Button Random", "Random", new Vector2(300, -120), new Vector2(260, 90), g),
            };
            Label(start, "Speed Label", "Fights", 44, new Vector2(0, -230), new Vector2(600, 60), g.textColor);
            view.speedButtons = new[]
            {
                MakeButton(start, "Button Full", "Full", new Vector2(-240, -310), new Vector2(200, 80), g),
                MakeButton(start, "Button Quick", "Quick", new Vector2(0, -310), new Vector2(200, 80), g),
                MakeButton(start, "Button Off", "Off", new Vector2(240, -310), new Vector2(200, 80), g),
            };
            view.startButton = MakeButton(start, "Button Start", "BEGIN", new Vector2(0, -440), new Vector2(440, 120), g);
            view.startPanel = start.gameObject;

            // top bar
            var hud = new GameObject("HUD", typeof(RectTransform));
            hud.transform.SetParent(parent, false);
            Stretch(hud.GetComponent<RectTransform>());
            view.statusText = Label(hud.transform, "Status Text", string.Empty, 46, new Vector2(0, -60), new Vector2(1200, 80), g.textColor, top: true);
            view.resignButton = MakeButton(hud.transform, "Button Resign", "Resign", new Vector2(-140, -60), new Vector2(220, 80), g, topRight: true);
            view.menuButton = MakeButton(hud.transform, "Button Menu", "Menu", new Vector2(140, -60), new Vector2(220, 80), g, topLeft: true);
            view.hud = hud;

            // promotion choice
            var promotion = Panel(parent, "Promotion Panel", Vector2.zero, Vector2.zero, g.panelColor, new Vector2(1000, 320));
            Label(promotion, "Promotion Label", "Promote to", 52, new Vector2(0, 100), new Vector2(800, 70), g.textColor);
            view.promotionButtons = new[]
            {
                MakeButton(promotion, "Promote Queen", "Queen", new Vector2(-330, -30), new Vector2(200, 120), g),
                MakeButton(promotion, "Promote Rook", "Rook", new Vector2(-110, -30), new Vector2(200, 120), g),
                MakeButton(promotion, "Promote Bishop", "Bishop", new Vector2(110, -30), new Vector2(200, 120), g),
                MakeButton(promotion, "Promote Knight", "Knight", new Vector2(330, -30), new Vector2(200, 120), g),
            };
            view.promotionPanel = promotion.gameObject;

            // the result
            var result = Panel(parent, "Result Panel", Vector2.zero, Vector2.zero, g.panelColor, new Vector2(1100, 560));
            view.resultText = Label(result, "Result Text", string.Empty, 80, new Vector2(0, 100), new Vector2(1000, 280), g.textColor);
            view.playAgainButton = MakeButton(result, "Button Play Again", "Play again", new Vector2(-260, -190), new Vector2(440, 110), g);
            view.changeDifficultyButton = MakeButton(result, "Button Change Difficulty", "Change opponent", new Vector2(260, -190), new Vector2(440, 110), g);
            view.resultPanel = result.gameObject;

            hud.SetActive(false);
            promotion.gameObject.SetActive(false);
            result.gameObject.SetActive(false);
            return view;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static Transform Panel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color, Vector2? size = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            if (size.HasValue)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = size.Value;
                rt.anchoredPosition = Vector2.zero;
            }
            else Stretch(rt);
            var image = go.GetComponent<Image>();
            image.color = color;
            return go.transform;
        }

        static Text Label(Transform parent, string name, string text, int size, Vector2 position, Vector2 dimensions, Color color, bool top = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = top ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = dimensions;
            var t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = color;
            t.text = text;
            t.raycastTarget = false;
            return t;
        }

        static Button MakeButton(Transform parent, string name, string label, Vector2 position, Vector2 dimensions, GameLayoutSettings g,
            bool topRight = false, bool topLeft = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = topRight ? new Vector2(1f, 1f) : topLeft ? new Vector2(0f, 1f) : new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = dimensions;
            go.GetComponent<Image>().color = g.buttonColor;
            var text = Label(go.transform, "Text", label, Mathf.RoundToInt(dimensions.y * 0.42f), Vector2.zero, dimensions, g.textColor);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            return go.GetComponent<Button>();
        }

        static void BuildEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        // --- sound and particles ---

        static SoundBank BuildAudio(MedievalChessSettings s)
        {
            var go = new GameObject("Audio");
            var bank = go.AddComponent<SoundBank>();
            bank.settings = s;
            var ids = (SoundId[])Enum.GetValues(typeof(SoundId));
            bank.clips = new AudioClip[ids.Length];
            foreach (var id in ids)
            {
                string path = $"{Root}/Audio/{SoundIds.FileName(id)}.wav";
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) throw new InvalidOperationException($"Sound not imported: {path}. Run Tools/audio/MedievalChess/make_sounds.py");
                bank.clips[(int)id] = clip;
            }
            bank.sources = new AudioSource[6];
            for (int i = 0; i < bank.sources.Length; i++)
            {
                var child = new GameObject($"Source {i + 1}");
                child.transform.SetParent(go.transform, false);
                var source = child.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                bank.sources[i] = source;
            }
            var musicGo = new GameObject("Music");
            musicGo.transform.SetParent(go.transform, false);
            var musicSource = musicGo.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Root}/Audio/music.wav");
            musicSource.volume = Mathf.Clamp01(s.feel.masterVolume * s.feel.musicVolume);
            bank.musicSource = musicSource;
            bank.musicClip = musicSource.clip;
            return bank;
        }

        static EffectsView BuildEffects(MedievalChessSettings s)
        {
            var go = new GameObject("Effects");
            var view = go.AddComponent<EffectsView>();
            view.settings = s;
            view.dust = BuildParticles(go.transform, "Dust", ParticleMaterial("Dust"), 0.25f, 0.4f);
            view.embers = BuildParticles(go.transform, "Embers", ParticleMaterial("Ember"), -0.05f, 0.3f);
            view.sparks = BuildParticles(go.transform, "Sparks", ParticleMaterial("Spark"), 0.6f, 0f);
            return view;
        }

        static ParticleSystem BuildParticles(Transform parent, string name, Material material, float gravity, float growth)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;
            main.maxParticles = 300;
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f - growth * 0.5f), new Keyframe(1f, growth > 0f ? 1f + growth * 2f : 0f)));
            var colour = ps.colorOverLifetime;
            colour.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.4f), new GradientAlphaKey(0f, 1f) });
            colour.color = fade;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }

        /// <summary>An unlit, alpha-blended particle material: soft glows for the embers and sparks, and dust.</summary>
        static Material ParticleMaterial(string name)
        {
            string path = DuelSceneBuilder.MaterialPath("Particle " + name);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null) throw new InvalidOperationException("URP Particles/Unlit shader not found.");
                mat = new Material(shader) { name = DuelSceneBuilder.MaterialName("Particle " + name) };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.SetColor("_BaseColor", Color.white);
            var soft = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
            if (soft != null) mat.SetTexture("_BaseMap", soft);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // --- the game itself ---

        static void BuildGame(MedievalChessSettings s, Material bone, Material cinnabar, Material iron, Material trim, UiView ui, HighlightView highlights, Camera cam,
            SoundBank sound, EffectsView effects, TorchFlicker[] torches)
        {
            var go = new GameObject("Game");
            var controller = go.AddComponent<GameController>();
            controller.settings = s;
            controller.bone = bone;
            controller.cinnabar = cinnabar;
            controller.iron = iron;
            controller.trim = trim;
            controller.ui = ui;
            controller.highlights = highlights;
            controller.cameraRig = cam.GetComponent<GameCameraRig>();
            controller.sound = sound;
            controller.effects = effects;
            controller.torches = torches;

            var types = (PieceType[])Enum.GetValues(typeof(PieceType));
            controller.piecePrefabs = new GameObject[types.Length];
            foreach (var type in types)
            {
                string path = $"{DuelSceneBuilder.ModelFolder}/{type.ToString().ToLowerInvariant()}.glb";
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null) throw new InvalidOperationException($"Model not imported: {path}. Run Tools/blender/MedievalChess/make_pieces.py");
                controller.piecePrefabs[(int)type] = model;
            }

            var input = go.AddComponent<InputController>();
            input.cam = cam;
            input.controller = controller;
        }
    }
}
