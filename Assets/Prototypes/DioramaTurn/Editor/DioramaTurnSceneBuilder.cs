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

namespace Prototypes.DioramaTurn.Editor
{
    /// <summary>
    /// Builds the game scene: camera rig, light, the preview room, Pip, the star, ribbons, HUD, input and the
    /// controller. Menu: Prototypes > DioramaTurn > Build Scene. Scenes and materials are generated, never
    /// edited by hand, and running it twice gives the same content.
    /// </summary>
    public static class DioramaTurnSceneBuilder
    {
        const string Root = "Assets/Prototypes/DioramaTurn";
        public const string ScenePath = Root + "/Scenes/DioramaTurn.unity";
        public const string SettingsPath = Root + "/Settings/DioramaTurnSettings.asset";
        const string MaterialFolder = Root + "/Materials";
        const string SpritePath = Root + "/Art/Sprites/pip_sheet.png";
        const string SpriteJsonPath = Root + "/Art/Sprites/pip_sheet.json";

        [MenuItem("Prototypes/DioramaTurn/Build Scene")]
        public static void BuildDefault()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build(ScenePath);
        }

        public static Scene Build(string scenePath)
        {
            EnsureFolder(Root + "/Settings");
            EnsureFolder(Root + "/Scenes");
            EnsureFolder(MaterialFolder);
            EnsureFolder(Root + "/Art/Sprites");
            EnsureFolder(Root + "/Shaders");

            CreateSettingsIfMissing();
            int rendererIndex = RendererSetup.Ensure();

            // Load every asset after NewScene: it unloads unused assets (pitfall 2).
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var settings = AssetDatabase.LoadAssetAtPath<DioramaTurnSettings>(SettingsPath);
            if (settings == null) throw new InvalidOperationException($"missing settings at {SettingsPath}");

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = settings.light.fillColour;

            var blockMaterials = new[]
            {
                Surface(settings, "Block Tomato", "#FF5A4F", 0.45f),
                Surface(settings, "Block Sun", "#FFC83D", 0.45f),
                Surface(settings, "Block Mint", "#4CCB9B", 0.45f),
                Surface(settings, "Block Sky", "#4C9BFF", 0.45f),
            };
            var startMaterial = Surface(settings, "Start Mint", "#4CCB9B", 0.45f);
            var goalMaterial = Surface(settings, "Goal Blue", "#4C9BFF", 0.45f);
            var woodAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/Textures/wood_albedo.png");
            var woodNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/Textures/wood_normal.png");
            var plinthMaterial = Surface(settings, "Plinth Wood", "#C98B5A", 0.2f, woodAlbedo, woodNormal, world: true);
            var bridgeMaterial = Surface(settings, "Plank Wood", "#E8B67A", 0.3f, woodAlbedo, woodNormal, world: true);
            var cursorMaterial = Unlit("Cursor", "#3B2A4D", 0.55f);
            var ribbonMaterial = Unlit("Ribbon", "#FFF6B0", 1f);
            var spriteMaterial = SpriteMaterial();

            var plinthModel = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Art/Exports/plinth.glb");
            var blockModel = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Art/Exports/block.glb");
            var pillarModel = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Art/Exports/pillar.glb");
            var goalModel = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Art/Exports/goal_pedestal.glb");
            var plankTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/Sprites/plank.png");
            var starTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/Sprites/star_sheet.png");
            var starJson = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Art/Sprites/star_sheet.json");

            var pipTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(SpritePath);
            var pipJson = AssetDatabase.LoadAssetAtPath<TextAsset>(SpriteJsonPath);

            var rig = BuildCamera(settings, rendererIndex, out var camera);
            BuildLight(settings, rig);
            var room = BuildRoom(settings, blockMaterials, startMaterial, goalMaterial, plinthMaterial, bridgeMaterial,
                cursorMaterial, plinthModel, blockModel, goalModel, pillarModel, spriteMaterial, plankTexture);
            var ribbons = BuildRibbons(settings, room, ribbonMaterial);
            var pip = BuildPip(settings, room, spriteMaterial, pipTexture, pipJson);
            var star = BuildStar(settings, rig, spriteMaterial, starTexture, starJson);
            var hud = BuildHud(settings);
            BuildVolume(settings);
            ApplyTiltSettings(settings);
            BuildEventSystem();
            var game = BuildGame(settings, rig, room, pip, star, ribbons, hud);

            // A preview of room 0 in view 0, so the scene is not empty before Play (Start rebuilds it).
            var preview = Rooms.Build(0);
            var previewSim = new DioramaSimulation(preview, settings.ToTiming());
            room.Build(preview);
            room.ShowCursor(previewSim.Room.Start);
            star.Place(preview.Goal);
            ribbons.ShowJoins(previewSim.ScreenJoins(0));
            pip.SnapTo(previewSim);
            rig.SnapToView(0);

            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            return scene;
        }

        static void CreateSettingsIfMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<DioramaTurnSettings>(SettingsPath) != null) return;
            var settings = ScriptableObject.CreateInstance<DioramaTurnSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
        }

        static CameraRig BuildCamera(DioramaTurnSettings settings, int rendererIndex, out Camera camera)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = settings.light.backdropColour;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 100f;
            go.AddComponent<AudioListener>();

            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.Medium;
            data.renderPostProcessing = true;
            data.SetRenderer(rendererIndex);

            var rig = go.AddComponent<CameraRig>();
            rig.settings = settings;
            rig.camera = camera;
            return rig;
        }

        static void BuildLight(DioramaTurnSettings settings, CameraRig rig)
        {
            var go = new GameObject("Key Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = settings.light.keyColour;
            light.intensity = settings.light.keyIntensity;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = settings.light.shadowStrength;
            rig.keyLight = light;
        }

        static RoomView BuildRoom(DioramaTurnSettings settings, Material[] blocks, Material start, Material goal,
            Material plinth, Material bridge, Material cursor, GameObject plinthModel, GameObject blockModel,
            GameObject goalModel, GameObject pillarModel, Material spriteMaterial, Texture2D plankTexture)
        {
            var go = new GameObject("Room");
            var view = go.AddComponent<RoomView>();
            view.settings = settings;
            view.blockMaterials = blocks;
            view.startMaterial = start;
            view.goalMaterial = goal;
            view.plinthMaterial = plinth;
            view.bridgeMaterial = bridge;
            view.cursorMaterial = cursor;
            view.plinthModel = plinthModel;
            view.blockModel = blockModel;
            view.goalModel = goalModel;
            view.pillarModel = pillarModel;
            view.bridgeSpriteMaterial = spriteMaterial;
            view.bridgeTexture = plankTexture;
            view.bridgeJson = null; // a single-image sprite: the whole texture is the frame
            return view;
        }

        static LinkRibbons BuildRibbons(DioramaTurnSettings settings, RoomView room, Material ribbon)
        {
            var go = new GameObject("Ribbons");
            var ribbons = go.AddComponent<LinkRibbons>();
            ribbons.settings = settings;
            ribbons.room = room;
            ribbons.ribbonMaterial = ribbon;
            return ribbons;
        }

        static PipView BuildPip(DioramaTurnSettings settings, RoomView room, Material sprite, Texture2D texture, TextAsset json)
        {
            var go = new GameObject("Pip");
            var quad = go.AddComponent<SpriteQuad>();
            quad.SetSheet(sprite, texture, json, new Vector2(1f, 1f));
            var view = go.AddComponent<PipView>();
            view.settings = settings;
            view.quad = quad;
            view.room = room;
            return view;
        }

        static StarView BuildStar(DioramaTurnSettings settings, CameraRig rig, Material sprite, Texture2D texture, TextAsset json)
        {
            var go = new GameObject("Star");
            var quad = go.AddComponent<SpriteQuad>();
            quad.SetSheet(sprite, texture, json, new Vector2(1f, 1f));
            var view = go.AddComponent<StarView>();
            view.quad = quad;
            view.rig = rig;
            return view;
        }

        static DioramaTurnGame BuildGame(DioramaTurnSettings settings, CameraRig rig, RoomView room, PipView pip,
            StarView star, LinkRibbons ribbons, Hud hud)
        {
            var go = new GameObject("Game");
            var game = go.AddComponent<DioramaTurnGame>();
            game.settings = settings;
            game.rig = rig;
            game.room = room;
            game.pip = pip;
            game.star = star;
            game.ribbons = ribbons;
            game.hud = hud;
            var input = go.AddComponent<DioramaInput>();
            input.game = game;
            game.input = input;
            pip.rig = rig;
            return game;
        }

        static void BuildEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        /// <summary>The post volume: Bloom, Vignette, warm grading and Neutral tonemapping.</summary>
        static void BuildVolume(DioramaTurnSettings settings)
        {
            string path = Root + "/Settings/DioramaTurnVolumeProfile.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            if (!profile.TryGet(out Bloom bloom)) bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(settings.post.bloomIntensity);
            bloom.threshold.Override(settings.post.bloomThreshold);
            if (!profile.TryGet(out Vignette vignette)) vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(settings.post.vignette);
            vignette.smoothness.Override(0.5f);
            if (!profile.TryGet(out ColorAdjustments colour)) colour = profile.Add<ColorAdjustments>(true);
            colour.saturation.Override(settings.post.saturation);
            colour.contrast.Override(settings.post.contrast);
            if (!profile.TryGet(out Tonemapping tonemapping)) tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.Neutral);
            EditorUtility.SetDirty(profile);

            var go = new GameObject("Post Volume");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = profile;
        }

        /// <summary>Pushes the tilt-shift band and blur from the settings onto the feature's material.</summary>
        static void ApplyTiltSettings(DioramaTurnSettings settings)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(RendererSetup.MaterialPath);
            if (material == null) return;
            material.SetFloat("_BandCentre", settings.post.tiltBandCentre);
            material.SetFloat("_BandHalfHeight", settings.post.tiltBandHalfHeight);
            material.SetFloat("_BlurStrength", settings.post.tiltBlur);
            EditorUtility.SetDirty(material);
        }

        // --- HUD ---

        static Hud BuildHud(DioramaTurnSettings settings)
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var hud = canvasGo.AddComponent<Hud>();
            var card = CardColour();
            var ink = InkColour();

            // title
            var title = Panel(canvasGo.transform, "Title Panel", card);
            Label(title, "Title", "DIORAMATURN", 120, new Vector2(0f, 220f), new Vector2(1400f, 160f), ink);
            Label(title, "Subtitle", "Turn the room until the pieces line up.\nWalk Pip to the star.", 44, new Vector2(0f, 40f), new Vector2(1200f, 140f), ink);
            Label(title, "Title Hint", "Click or press Enter to begin", 36, new Vector2(0f, -180f), new Vector2(1000f, 70f), ink);

            // playing
            var playing = new GameObject("Playing Panel", typeof(RectTransform));
            playing.transform.SetParent(canvasGo.transform, false);
            Stretch(playing.GetComponent<RectTransform>());
            hud.roomNameText = Label(playing.transform, "Room Name", string.Empty, 46, new Vector2(0f, -60f), new Vector2(900f, 70f), ink, top: true);
            hud.turnText = Label(playing.transform, "Turn Count", string.Empty, 34, new Vector2(0f, -120f), new Vector2(500f, 60f), ink, top: true);
            hud.turnLeftButton = MakeButton(playing.transform, "Button Turn Left", "Turn left", new Vector2(160f, -70f), new Vector2(220f, 90f), topLeft: true);
            hud.turnRightButton = MakeButton(playing.transform, "Button Turn Right", "Turn right", new Vector2(-160f, -70f), new Vector2(220f, 90f), topRight: true);
            hud.restartButton = MakeButton(playing.transform, "Button Restart", "Restart", new Vector2(-160f, -180f), new Vector2(220f, 80f), topRight: true);
            hud.hintText = Label(playing.transform, "Hints", "Q / E turn   -   arrows move   -   Enter confirm   -   R restart", 28,
                new Vector2(0f, 50f), new Vector2(1400f, 60f), ink, bottom: true);

            // complete
            var complete = Panel(canvasGo.transform, "Complete Panel", card, new Vector2(760f, 380f));
            hud.completeText = Label(complete, "Complete Text", string.Empty, 52, new Vector2(0f, 60f), new Vector2(700f, 200f), ink);
            hud.nextButton = MakeButton(complete, "Button Next", "Next room", new Vector2(0f, -110f), new Vector2(360f, 100f));

            // finish
            var finish = Panel(canvasGo.transform, "Finish Panel", card, new Vector2(760f, 360f));
            Label(finish, "Finish Title", "All three rooms done!", 60, new Vector2(0f, 70f), new Vector2(700f, 100f), ink);
            hud.playAgainButton = MakeButton(finish, "Button Play Again", "Play again", new Vector2(0f, -90f), new Vector2(360f, 100f));

            hud.titlePanel = title.gameObject;
            hud.playingPanel = playing;
            hud.completePanel = complete.gameObject;
            hud.finishPanel = finish.gameObject;
            hud.ShowTitle();
            playing.SetActive(false);
            complete.gameObject.SetActive(false);
            finish.gameObject.SetActive(false);
            return hud;
        }

        static Color CardColour()
        {
            ColorUtility.TryParseHtmlString("#FFF6E4", out var colour);
            return colour;
        }

        static Color InkColour()
        {
            ColorUtility.TryParseHtmlString("#3B2A4D", out var colour);
            return colour;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static Transform Panel(Transform parent, string name, Color colour, Vector2? size = null)
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
            go.GetComponent<Image>().color = colour;
            return go.transform;
        }

        static Text Label(Transform parent, string name, string text, int size, Vector2 position, Vector2 dimensions,
            Color colour, bool top = false, bool bottom = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = top ? new Vector2(0.5f, 1f) : bottom ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = dimensions;
            var label = go.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = colour;
            label.text = text;
            label.raycastTarget = false;
            return label;
        }

        static Button MakeButton(Transform parent, string name, string label, Vector2 position, Vector2 dimensions,
            bool topRight = false, bool topLeft = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = topRight ? new Vector2(1f, 1f) : topLeft ? new Vector2(0f, 1f) : new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = dimensions;
            go.GetComponent<Image>().color = InkColour();
            var button = go.GetComponent<Button>();

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            Stretch(textRt);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 30;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = CardColour();
            text.text = label;
            text.raycastTarget = false;
            return button;
        }

        // --- materials ---

        public const string SurfaceShaderName = "Prototypes/DioramaTurn/Lit";

        static Material Surface(DioramaTurnSettings settings, string name, string hex, float smoothness,
            Texture2D albedo = null, Texture2D normal = null, bool world = false)
        {
            ColorUtility.TryParseHtmlString(hex, out var colour);
            string path = $"{MaterialFolder}/{name.Replace(" ", string.Empty)}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(SurfaceShaderName)) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = Shader.Find(SurfaceShaderName);
            material.SetColor("_BaseColor", colour);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_AoStrength", settings.look.occlusion);
            material.SetFloat("_TextureStrength", albedo != null ? settings.look.woodTexture : 0f);
            material.SetFloat("_NormalStrength", normal != null ? settings.look.woodNormal : 0f);
            material.SetFloat("_MapScale", settings.look.woodScale);
            material.SetFloat("_WorldMapping", world ? 1f : 0f);
            if (albedo != null) material.SetTexture("_BaseMap", albedo);
            if (normal != null) material.SetTexture("_NormalMap", normal);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material Unlit(string name, string hex, float alpha)
        {
            ColorUtility.TryParseHtmlString(hex, out var colour);
            colour.a = alpha;
            string path = $"{MaterialFolder}/{name.Replace(" ", string.Empty)}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", colour);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material Emissive(string name, string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var colour);
            string path = $"{MaterialFolder}/{name.Replace(" ", string.Empty)}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", colour);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", colour * 1.6f);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material SpriteMaterial()
        {
            string path = MaterialFolder + "/DioramaTurnSprite.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "DioramaTurnSprite" };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", 0.5f);
            material.SetFloat("_Cull", 0f);
            material.SetFloat("_Smoothness", 0f);
            material.SetFloat("_Metallic", 0f);
            material.EnableKeyword("_ALPHATEST_ON");
            EditorUtility.SetDirty(material);
            return material;
        }

        static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
