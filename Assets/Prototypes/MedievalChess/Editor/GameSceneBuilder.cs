using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
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
            RenderSettings.ambientLight = new Color(0.07f, 0.065f, 0.06f);
            RenderSettings.fog = false;

            var bone = DuelSceneBuilder.Material("Bone", settings.boneColor, 0.25f);
            var cinnabar = DuelSceneBuilder.Material("Cinnabar", settings.cinnabarColor, 0.25f);
            var iron = DuelSceneBuilder.Material("Iron", settings.ironColor, 0.45f);

            BuildBoard(settings);
            var cam = BuildCamera(settings);
            BuildLight(settings);
            var highlights = BuildHighlights(settings);
            var ui = BuildUi(settings);
            BuildEventSystem();
            BuildGame(settings, bone, cinnabar, iron, ui, highlights, cam);

            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            return scene;
        }

        // --- the board ---

        static void BuildBoard(MedievalChessSettings s)
        {
            var light = DuelSceneBuilder.Material("Tile Light", s.lightSquareColor, 0.15f);
            var dark = DuelSceneBuilder.Material("Tile Dark", s.darkSquareColor, 0.15f);
            var frame = DuelSceneBuilder.Material("Frame", s.frameColor, 0.2f);
            var floor = DuelSceneBuilder.Material("Floor", s.floorColor, 0.1f);

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
            foreach (var (name, position, size) in new[]
            {
                ("Frame North", new Vector3(0f, -0.1f, 4.35f), new Vector3(9.4f, 0.3f, 0.7f)),
                ("Frame South", new Vector3(0f, -0.1f, -4.35f), new Vector3(9.4f, 0.3f, 0.7f)),
                ("Frame East", new Vector3(4.35f, -0.1f, 0f), new Vector3(0.7f, 0.3f, 8f)),
                ("Frame West", new Vector3(-4.35f, -0.1f, 0f), new Vector3(0.7f, 0.3f, 8f)),
            })
                DuelSceneBuilder.Box(name, position, size, frame).transform.SetParent(edge.transform, true);

            DuelSceneBuilder.Box("Floor", new Vector3(0f, -0.35f, 0f), new Vector3(24f, 0.2f, 24f), floor);

            // the trays where captured pieces stand
            float x = s.game.trayX;
            DuelSceneBuilder.Box("Tray Left", new Vector3(-x, -0.05f, 0f), new Vector3(1.4f, 0.1f, 8f), frame);
            DuelSceneBuilder.Box("Tray Right", new Vector3(x, -0.05f, 0f), new Vector3(1.4f, 0.1f, 8f), frame);
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
            light.color = s.keyLightColor;
            light.intensity = 1.4f;
            light.shadows = LightShadows.Soft;
            go.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
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
            string path = $"{DuelSceneBuilder.MaterialFolder}/MedievalChess Ring {name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) throw new InvalidOperationException("URP Unlit shader not found.");
                mat = new Material(shader) { name = $"MedievalChess Ring {name}" };
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

        // --- the game itself ---

        static void BuildGame(MedievalChessSettings s, Material bone, Material cinnabar, Material iron, UiView ui, HighlightView highlights, Camera cam)
        {
            var go = new GameObject("Game");
            var controller = go.AddComponent<GameController>();
            controller.settings = s;
            controller.bone = bone;
            controller.cinnabar = cinnabar;
            controller.iron = iron;
            controller.ui = ui;
            controller.highlights = highlights;
            controller.cameraRig = cam.GetComponent<GameCameraRig>();

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
