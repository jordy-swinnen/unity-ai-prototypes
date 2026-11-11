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
                Surface("Block Tomato", "#FF5A4F", 0.45f),
                Surface("Block Sun", "#FFC83D", 0.45f),
                Surface("Block Mint", "#4CCB9B", 0.45f),
                Surface("Block Sky", "#4C9BFF", 0.45f),
            };
            var startMaterial = Surface("Start Mint", "#4CCB9B", 0.45f);
            var goalMaterial = Surface("Goal Blue", "#4C9BFF", 0.45f);
            var plinthMaterial = Surface("Plinth Wood", "#C98B5A", 0.2f);
            var bridgeMaterial = Surface("Plank Wood", "#E8B67A", 0.3f);
            var cursorMaterial = Unlit("Cursor", "#3B2A4D", 0.55f);
            var ribbonMaterial = Unlit("Ribbon", "#FFF6B0", 1f);
            var starMaterial = Emissive("Goal Star", "#FFD93B");
            var spriteMaterial = SpriteMaterial();

            var pipTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(SpritePath);
            var pipJson = AssetDatabase.LoadAssetAtPath<TextAsset>(SpriteJsonPath);

            var rig = BuildCamera(settings, rendererIndex, out var camera);
            BuildLight(settings, rig);
            var room = BuildRoom(settings, blockMaterials, startMaterial, goalMaterial, plinthMaterial, bridgeMaterial, cursorMaterial);
            var ribbons = BuildRibbons(settings, room, ribbonMaterial);
            var pip = BuildPip(settings, room, spriteMaterial, pipTexture, pipJson);
            var star = BuildStar(settings, starMaterial);
            var hud = BuildHud(settings);
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
            Material plinth, Material bridge, Material cursor)
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

        static StarView BuildStar(DioramaTurnSettings settings, Material material)
        {
            var go = new GameObject("Star");
            var shape = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shape.name = "Star Shape";
            shape.transform.SetParent(go.transform, false);
            shape.transform.localScale = Vector3.one * 0.45f;
            shape.transform.localRotation = Quaternion.Euler(0f, 45f, 45f);
            UnityEngine.Object.DestroyImmediate(shape.GetComponent<Collider>());
            shape.GetComponent<Renderer>().sharedMaterial = material;

            var view = go.AddComponent<StarView>();
            view.star = go.transform;
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

        static Material Surface(string name, string hex, float smoothness)
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
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", 0f);
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
