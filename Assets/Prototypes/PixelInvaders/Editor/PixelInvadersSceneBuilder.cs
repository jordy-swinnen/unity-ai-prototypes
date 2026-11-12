using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Prototypes.PixelInvaders.Editor
{
    /// <summary>
    /// Builds the complete PixelInvaders scene from code via menu command Prototypes/PixelInvaders/Build Scene.
    /// Never edit the .unity file or generated materials by hand.
    /// </summary>
    public static class PixelInvadersSceneBuilder
    {
        public const string RootFolder = "Assets/Prototypes/PixelInvaders";
        public const string SettingsPath = RootFolder + "/Settings/PixelInvadersSettings.asset";
        public const string VolumeProfilePath = RootFolder + "/Settings/PixelInvadersVolumeProfile.asset";
        public const string MaterialsFolder = RootFolder + "/Materials";
        public const string ScenesFolder = RootFolder + "/Scenes";
        public const string ScenePath = ScenesFolder + "/PixelInvaders.unity";
        public const string SpritesFolder = RootFolder + "/Art/Sprites";
        public const string ExportsFolder = RootFolder + "/Art/Exports";
        public const string TexturesFolder = RootFolder + "/Art/Textures";
        public const string ShadersFolder = RootFolder + "/Shaders";
        public const string AudioFolder = RootFolder + "/Audio";

        [MenuItem("Prototypes/PixelInvaders/Build Scene")]
        public static void BuildMenu()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("PixelInvadersSceneBuilder: stop Play Mode before building the scene.");
                return;
            }
            BuildScene(ScenePath);
        }

        public static void BuildScene(string outputScenePath = ScenePath)
        {
            EnsureFolder(RootFolder, "Settings");
            EnsureFolder(RootFolder, "Materials");
            EnsureFolder(RootFolder, "Scenes");

            EnsureSettingsAsset();
            EnsureVolumeProfile();
            EnsureMaterials();
            AssetDatabase.SaveAssets();

            // Create the new scene first (Pitfall 2 & 20: NewScene unloads unused assets, so load assets AFTER NewScene)
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var settings = AssetDatabase.LoadAssetAtPath<PixelInvadersSettings>(SettingsPath);
            var volumeProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            var deckMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder + "/PixelInvadersTrenchDeck.mat");
            var railMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder + "/PixelInvadersTrenchRail.mat");
            var markerMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder + "/PixelInvadersTrenchMarkers.mat");
            var spriteLitMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder + "/PixelInvadersSpriteLit.mat");
            var spriteEmissiveMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder + "/PixelInvadersSpriteEmissive.mat");

            var invadersTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SpritesFolder + "/invaders_sheet.png");
            var invadersJson = AssetDatabase.LoadAssetAtPath<TextAsset>(SpritesFolder + "/invaders_sheet.json");
            var cannonTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SpritesFolder + "/cannon_and_shots_sheet.png");
            var cannonJson = AssetDatabase.LoadAssetAtPath<TextAsset>(SpritesFolder + "/cannon_and_shots_sheet.json");
            var fxTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SpritesFolder + "/powerups_and_fx_sheet.png");
            var fxJson = AssetDatabase.LoadAssetAtPath<TextAsset>(SpritesFolder + "/powerups_and_fx_sheet.json");
            var hudTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SpritesFolder + "/bunker_and_hud_sheet.png");
            var hudJson = AssetDatabase.LoadAssetAtPath<TextAsset>(SpritesFolder + "/bunker_and_hud_sheet.json");

            // 1. Camera & Lighting
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.035f, 0.047f, 0.071f, 1f); // #090C12 Void black
            cam.fieldOfView = settings.camera.fovDeg;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 60f;
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.None;

            float pitchRad = settings.camera.pitchDeg * Mathf.Deg2Rad;
            float dist = settings.camera.distance;
            Vector3 lookTarget = new Vector3(0f, 0f, settings.camera.lookAtZ);
            camGo.transform.position = lookTarget + new Vector3(0f, Mathf.Sin(pitchRad) * dist, -Mathf.Cos(pitchRad) * dist);
            camGo.transform.rotation = Quaternion.Euler(settings.camera.pitchDeg, 0f, 0f);

            var lightGo = new GameObject("Directional Light");
            var keyLight = lightGo.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(0.847f, 0.902f, 1.0f, 1f); // #D8E6FF
            keyLight.intensity = settings.look.keyLightIntensity;
            keyLight.shadows = LightShadows.Soft;
            keyLight.shadowStrength = 0.78f;
            lightGo.transform.rotation = Quaternion.Euler(62f, -18f, 0f);
            lightGo.AddComponent<UniversalAdditionalLightData>();

            var volumeGo = new GameObject("PostProcessVolume");
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = volumeProfile;

            // 2. 3D Command Trench Stage
            var trenchRoot = new GameObject("CommandTrench");
            BuildTrenchGeometry(trenchRoot.transform, deckMat, railMat, markerMat, settings);

            // 3. Game Root, View & Audio
            var gameRoot = new GameObject("PixelInvadersGame");
            var view = gameRoot.AddComponent<PixelInvadersView>();
            var audio = gameRoot.AddComponent<PixelInvadersAudio>();
            var game = gameRoot.AddComponent<PixelInvadersGame>();

            view.settings = settings;
            view.mainCamera = cam;
            view.keyLight = keyLight;
            view.hudSheet = hudTex;
            view.hudSheetJson = hudJson;

            audio.settings = settings;
            audio.sfxSource = CreateAudioSource(gameRoot.transform, "SfxSource", loop: false);
            audio.ufoLoopSource = CreateAudioSource(gameRoot.transform, "UfoLoopSource", loop: true);
            audio.musicSource = CreateAudioSource(gameRoot.transform, "MusicSource", loop: true);
            BindAudioClipsIfPresent(audio);
            if (audio.musicLoopClip != null)
            {
                audio.musicSource.clip = audio.musicLoopClip;
                audio.musicSource.playOnAwake = true;
                audio.musicSource.volume = settings.audio.masterVolume * settings.audio.musicVolume;
            }

            game.settings = settings;
            game.view = view;
            game.audioDirector = audio;

            // 4. Sprites Root (Invaders, Bunkers, Cannon, UFO, Capsule, Projectiles, FX)
            var spritesRoot = new GameObject("Sprites");
            spritesRoot.transform.SetParent(gameRoot.transform, false);

            var sim = new PixelInvadersSimulation(settings, seed: 1337);

            // 55 Invader Quads
            var invadersRoot = new GameObject("Invaders");
            invadersRoot.transform.SetParent(spritesRoot.transform, false);
            view.invaderQuads = new SpriteQuad[sim.Invaders.Count];
            for (int i = 0; i < sim.Invaders.Count; i++)
            {
                var inv = sim.Invaders[i];
                var q = CreateSpriteQuad(
                    invadersRoot.transform,
                    $"Invader_{inv.Row}_{inv.Col}",
                    spriteLitMat,
                    invadersTex,
                    invadersJson,
                    new Vector2(0.82f, 0.82f),
                    castShadows: true);
                q.PlaceOnDeck(inv.Position.x, inv.Position.y, 0.02f);
                int baseFrame = inv.Rank == InvaderRank.Scout ? 0 : (inv.Rank == InvaderRank.Guard ? 3 : 6);
                q.ShowFrame(baseFrame);
                view.invaderQuads[i] = q;
            }

            // 4 Bunkers
            var bunkersRoot = new GameObject("Bunkers");
            bunkersRoot.transform.SetParent(spritesRoot.transform, false);
            view.bunkerViews = new BunkerTextureView[sim.Bunkers.Count];
            for (int i = 0; i < sim.Bunkers.Count; i++)
            {
                var bGrid = sim.Bunkers[i];
                var bGo = new GameObject($"Bunker_{i}");
                bGo.transform.SetParent(bunkersRoot.transform, false);
                var mf = bGo.AddComponent<MeshFilter>();
                mf.sharedMesh = SpriteQuad.GetOrCreateQuadMesh();
                var mr = bGo.AddComponent<MeshRenderer>();
                mr.shadowCastingMode = ShadowCastingMode.On;
                mr.receiveShadows = true;
                var bView = bGo.AddComponent<BunkerTextureView>();
                bView.Initialize(spriteLitMat, new Vector2(bGrid.Width, bGrid.Height), bGrid.CenterX, bGrid.CenterZ, 35f);
                bView.Sync(bGrid);
                view.bunkerViews[i] = bView;
            }

            // Player Cannon & Muzzle Flash
            view.cannonQuad = CreateSpriteQuad(
                spritesRoot.transform,
                "PlayerCannon",
                spriteLitMat,
                cannonTex,
                cannonJson,
                new Vector2(0.95f, 0.95f),
                castShadows: true);
            view.cannonQuad.PlaceOnDeck(0f, settings.arena.cannonZ, 0.02f);
            view.cannonQuad.ShowFrame(0);

            view.muzzleFlashQuad = CreateSpriteQuad(
                spritesRoot.transform,
                "MuzzleFlash",
                spriteEmissiveMat,
                cannonTex,
                cannonJson,
                new Vector2(0.55f, 0.55f),
                castShadows: false);
            view.muzzleFlashQuad.ShowFrame(10);
            view.muzzleFlashQuad.gameObject.SetActive(false);

            // UFO & Point Light & Capsule
            view.ufoQuad = CreateSpriteQuad(
                spritesRoot.transform,
                "UfoSaucer",
                spriteLitMat,
                invadersTex,
                invadersJson,
                new Vector2(1.15f, 0.85f),
                castShadows: true);
            view.ufoQuad.PlaceOnDeck(0f, settings.arena.ufoZ, 0.02f);
            view.ufoQuad.ShowFrame(9);
            view.ufoQuad.gameObject.SetActive(false);

            var ufoLightGo = new GameObject("UfoPointLight");
            ufoLightGo.transform.SetParent(spritesRoot.transform, false);
            var ufoLight = ufoLightGo.AddComponent<Light>();
            ufoLight.type = LightType.Point;
            ufoLight.color = new Color(1.0f, 0.28f, 0.41f);
            ufoLight.intensity = settings.look.shotLightIntensity;
            ufoLight.range = 3.0f;
            ufoLight.shadows = LightShadows.None;
            ufoLight.enabled = false;
            ufoLightGo.AddComponent<UniversalAdditionalLightData>();
            view.ufoPointLight = ufoLight;

            view.capsuleQuad = CreateSpriteQuad(
                spritesRoot.transform,
                "PowerupCapsule",
                spriteLitMat,
                fxTex,
                fxJson,
                new Vector2(0.68f, 0.68f),
                castShadows: true);
            view.capsuleQuad.ShowFrame(0);
            view.capsuleQuad.gameObject.SetActive(false);

            // Projectile Pool (8 quads + 8 point lights)
            var projRoot = new GameObject("Projectiles");
            projRoot.transform.SetParent(spritesRoot.transform, false);
            view.projectilePool = new SpriteQuad[8];
            view.projectileLights = new Light[8];
            for (int i = 0; i < 8; i++)
            {
                var pq = CreateSpriteQuad(
                    projRoot.transform,
                    $"Projectile_{i}",
                    spriteEmissiveMat,
                    cannonTex,
                    cannonJson,
                    new Vector2(0.42f, 0.62f),
                    castShadows: false);
                pq.ShowFrame(8);
                pq.gameObject.SetActive(false);
                view.projectilePool[i] = pq;

                var plGo = new GameObject($"ProjectileLight_{i}");
                plGo.transform.SetParent(projRoot.transform, false);
                var pl = plGo.AddComponent<Light>();
                pl.type = LightType.Point;
                pl.intensity = settings.look.shotLightIntensity;
                pl.range = settings.look.shotLightRange;
                pl.shadows = LightShadows.None;
                pl.enabled = false;
                plGo.AddComponent<UniversalAdditionalLightData>();
                view.projectileLights[i] = pl;
            }

            // FX Pool (12 explosion/spark quads)
            var fxRoot = new GameObject("Effects");
            fxRoot.transform.SetParent(spritesRoot.transform, false);
            view.fxPool = new SpriteQuad[12];
            for (int i = 0; i < 12; i++)
            {
                var fq = CreateSpriteQuad(
                    fxRoot.transform,
                    $"Fx_{i}",
                    spriteEmissiveMat,
                    fxTex,
                    fxJson,
                    new Vector2(0.90f, 0.90f),
                    castShadows: false);
                fq.ShowFrame(6);
                fq.gameObject.SetActive(false);
                view.fxPool[i] = fq;
            }

            // 5. HUD Canvas (with Aseprite RawImage badges and ScorePopupPool)
            BuildHudCanvas(gameRoot.transform, view, hudTex, hudJson);
            view.Sync(sim, 0f, Vector2.zero);

            EditorSceneManager.SaveScene(scene, outputScenePath);
            AssetDatabase.Refresh();
        }

        static void BuildTrenchGeometry(
            Transform parent,
            Material deckMat,
            Material railMat,
            Material markerMat,
            PixelInvadersSettings settings)
        {
            var deckPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExportsFolder + "/trench_deck.glb");
            var railPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExportsFolder + "/trench_rail.glb");
            var markersPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExportsFolder + "/trench_markers.glb");

            if (deckPrefab != null)
            {
                var deckInst = (GameObject)PrefabUtility.InstantiatePrefab(deckPrefab, parent);
                deckInst.name = "TrenchDeck";
                deckInst.transform.localPosition = new Vector3(0f, 0f, 0.2f);
                AssignSharedMaterialRecursive(deckInst, deckMat);
            }
            else
            {
                var deckGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                deckGo.name = "TrenchDeck";
                deckGo.transform.SetParent(parent, false);
                Object.DestroyImmediate(deckGo.GetComponent<Collider>());
                deckGo.transform.localPosition = new Vector3(0f, -0.2f, 0.2f);
                deckGo.transform.localScale = new Vector3(15.0f, 0.4f, 13.0f);
                deckGo.GetComponent<MeshRenderer>().sharedMaterial = deckMat;
            }

            if (railPrefab != null)
            {
                var leftRail = (GameObject)PrefabUtility.InstantiatePrefab(railPrefab, parent);
                leftRail.name = "TrenchRailLeft";
                leftRail.transform.localPosition = new Vector3(-7.1f, 0f, 0.2f);
                AssignSharedMaterialRecursive(leftRail, railMat);

                var rightRail = (GameObject)PrefabUtility.InstantiatePrefab(railPrefab, parent);
                rightRail.name = "TrenchRailRight";
                rightRail.transform.localPosition = new Vector3(7.1f, 0f, 0.2f);
                rightRail.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                AssignSharedMaterialRecursive(rightRail, railMat);
            }
            else
            {
                CreateBox(parent, "TrenchRailLeft", new Vector3(-7.1f, 0.35f, 0.2f), new Vector3(0.8f, 0.75f, 13.0f), railMat);
                CreateBox(parent, "TrenchRailRight", new Vector3(7.1f, 0.35f, 0.2f), new Vector3(0.8f, 0.75f, 13.0f), railMat);
            }

            if (markersPrefab != null)
            {
                var markersInst = (GameObject)PrefabUtility.InstantiatePrefab(markersPrefab, parent);
                markersInst.name = "TrenchMarkers";
                markersInst.transform.localPosition = Vector3.zero;
                AssignSharedMaterialRecursive(markersInst, markerMat);
            }
            else
            {
                CreateBox(parent, "DefenseLineMarker", new Vector3(0f, 0.01f, settings.arena.defenseLineZ), new Vector3(13.4f, 0.02f, 0.06f), markerMat);
            }
        }

        static void AssignSharedMaterialRecursive(GameObject root, Material mat)
        {
            if (root == null || mat == null) return;
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.On;
                r.receiveShadows = true;
            }
        }

        static void CreateBox(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static SpriteQuad CreateSpriteQuad(
            Transform parent,
            string name,
            Material mat,
            Texture2D sheet,
            TextAsset json,
            Vector2 size,
            bool castShadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = SpriteQuad.GetOrCreateQuadMesh();
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            mr.receiveShadows = castShadows;
            var sq = go.AddComponent<SpriteQuad>();
            sq.Configure(mat, sheet, json, size, tiltXDeg: 35f);
            return sq;
        }

        static AudioSource CreateAudioSource(Transform parent, string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f;
            return src;
        }

        static void BindAudioClipsIfPresent(PixelInvadersAudio audio)
        {
            audio.stepNotes = new AudioClip[4];
            for (int i = 0; i < 4; i++)
            {
                audio.stepNotes[i] = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/step_{i}.wav");
            }
            audio.shootClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/shoot.wav");
            audio.twinShootClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/twin_shoot.wav");
            audio.alienShootClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/alien_shoot.wav");
            audio.alienPopClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/alien_pop.wav");
            audio.bunkerHitClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/bunker_hit.wav");
            audio.ufoLoopClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/ufo_loop.wav");
            audio.ufoBoomClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/ufo_boom.wav");
            audio.powerupClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/powerup.wav");
            audio.cannonDeathClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/cannon_death.wav");
            audio.waveClearClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/wave_clear.wav");
            audio.gameOverClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/game_over.wav");
            audio.musicLoopClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/music.wav");
        }

        static void BuildHudCanvas(Transform parent, PixelInvadersView view, Texture2D hudTex, TextAsset hudJson)
        {
            var canvasGo = new GameObject("HUDCanvas");
            canvasGo.transform.SetParent(parent, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            canvasGo.AddComponent<GraphicRaycaster>();

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Color phosphorWhite = new Color(0.941f, 0.965f, 1.0f, 1f);
            Color solarGold = new Color(1.0f, 0.820f, 0.278f, 1f);
            Color cyanTurret = new Color(0.173f, 0.910f, 0.961f, 1f);

            view.scoreText = CreateHudText(canvasGo.transform, "ScoreText", font, 24, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(28f, -18f), new Vector2(280f, 40f), phosphorWhite, "SCORE 0000");

            view.crownIcon = CreateHudRawImage(canvasGo.transform, "CrownIcon", hudTex,
                new Vector2(0.5f, 1f), new Vector2(-72f, -18f), new Vector2(28f, 28f));

            view.highScoreText = CreateHudText(canvasGo.transform, "HighScoreText", font, 24, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(12f, -18f), new Vector2(260f, 40f), solarGold, "HI 1000");

            view.waveText = CreateHudText(canvasGo.transform, "WaveText", font, 24, TextAnchor.UpperRight,
                new Vector2(1f, 1f), new Vector2(-28f, -18f), new Vector2(220f, 40f), phosphorWhite, "WAVE 1");

            view.livesText = CreateHudText(canvasGo.transform, "LivesText", font, 20, TextAnchor.LowerLeft,
                new Vector2(0f, 0f), new Vector2(28f, 18f), new Vector2(120f, 36f), cyanTurret, "LIVES 3");

            view.lifeIcons = new RawImage[3];
            for (int i = 0; i < 3; i++)
            {
                view.lifeIcons[i] = CreateHudRawImage(canvasGo.transform, $"LifeIcon_{i}", hudTex,
                    new Vector2(0f, 0f), new Vector2(124f + i * 32f, 22f), new Vector2(28f, 28f));
            }

            view.powerupBadgeIcon = CreateHudRawImage(canvasGo.transform, "PowerupBadgeIcon", hudTex,
                new Vector2(1f, 0f), new Vector2(-248f, 22f), new Vector2(28f, 28f));
            view.powerupBadgeIcon.enabled = false;

            view.powerupText = CreateHudText(canvasGo.transform, "PowerupText", font, 20, TextAnchor.LowerRight,
                new Vector2(1f, 0f), new Vector2(-28f, 18f), new Vector2(215f, 36f), solarGold, "");

            view.statusOverlayText = CreateHudText(canvasGo.transform, "StatusOverlayText", font, 34, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 140f), phosphorWhite, "");

            var popupRoot = new GameObject("ScorePopups");
            popupRoot.transform.SetParent(canvasGo.transform, false);
            var prt = popupRoot.AddComponent<RectTransform>();
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;
            view.scorePopupPool = new Text[8];
            for (int i = 0; i < 8; i++)
            {
                var pTxt = CreateHudText(popupRoot.transform, $"ScorePopup_{i}", font, 20, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 36f), solarGold, "");
                pTxt.gameObject.SetActive(false);
                view.scorePopupPool[i] = pTxt;
            }
        }

        static RawImage CreateHudRawImage(
            Transform parent,
            string name,
            Texture2D tex,
            Vector2 anchorPos,
            Vector2 anchoredOffset,
            Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorPos;
            rt.anchorMax = anchorPos;
            rt.pivot = anchorPos;
            rt.anchoredPosition = anchoredOffset;
            rt.sizeDelta = sizeDelta;
            var img = go.AddComponent<RawImage>();
            img.texture = tex;
            return img;
        }

        static Text CreateHudText(
            Transform parent,
            string name,
            Font font,
            int fontSize,
            TextAnchor anchor,
            Vector2 anchorPos,
            Vector2 anchoredOffset,
            Vector2 sizeDelta,
            Color color,
            string initialText)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorPos;
            rt.anchorMax = anchorPos;
            rt.pivot = anchorPos;
            rt.anchoredPosition = anchoredOffset;
            rt.sizeDelta = sizeDelta;

            var txt = go.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = fontSize;
            txt.alignment = anchor;
            txt.color = color;
            txt.text = initialText;
            return txt;
        }

        static void EnsureSettingsAsset()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PixelInvadersSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PixelInvadersSettings>();
                settings.Validate();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }
            else
            {
                settings.Validate();
                EditorUtility.SetDirty(settings);
            }
        }

        static void EnsureVolumeProfile()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PixelInvadersSettings>(SettingsPath);
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            }

            if (!profile.TryGet<Bloom>(out var bloom))
            {
                bloom = profile.Add<Bloom>(true);
            }
            bloom.threshold.overrideState = true;
            bloom.threshold.value = settings != null ? settings.look.bloomThreshold : 1.0f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = settings != null ? settings.look.bloomIntensity : 0.65f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.65f;

            if (!profile.TryGet<Vignette>(out var vignette))
            {
                vignette = profile.Add<Vignette>(true);
            }
            vignette.intensity.overrideState = true;
            vignette.intensity.value = settings != null ? settings.look.vignetteIntensity : 0.25f;

            if (!profile.TryGet<Tonemapping>(out var tonemapping))
            {
                tonemapping = profile.Add<Tonemapping>(true);
            }
            tonemapping.mode.overrideState = true;
            tonemapping.mode.value = TonemappingMode.Neutral;

            EditorUtility.SetDirty(profile);
        }

        static void EnsureMaterials()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PixelInvadersSettings>(SettingsPath);
            var customShader = AssetDatabase.LoadAssetAtPath<Shader>(ShadersFolder + "/PixelInvadersLit.shader");
            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            var urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
            var trenchShader = customShader != null ? customShader : urpLit;

            var albedoTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturesFolder + "/metal_deck_albedo.png");
            var normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturesFolder + "/metal_deck_normal.png");

            // 1. Trench Deck (#161E2B)
            var deckMat = LoadOrCreateMaterial(MaterialsFolder + "/PixelInvadersTrenchDeck.mat", trenchShader);
            ConfigureTrenchMaterial(deckMat, trenchShader, new Color(0.086f, 0.118f, 0.169f, 1f), 0.35f, albedoTex, normalTex, settings);

            // 2. Trench Rail (#2C394B)
            var railMat = LoadOrCreateMaterial(MaterialsFolder + "/PixelInvadersTrenchRail.mat", trenchShader);
            ConfigureTrenchMaterial(railMat, trenchShader, new Color(0.173f, 0.224f, 0.294f, 1f), 0.42f, albedoTex, normalTex, settings);

            // 3. Trench Markers (#D9822B warm amber + mild emission)
            var markerMat = LoadOrCreateMaterial(MaterialsFolder + "/PixelInvadersTrenchMarkers.mat", urpLit);
            markerMat.shader = urpLit;
            markerMat.SetColor("_BaseColor", new Color(0.851f, 0.510f, 0.169f, 1f));
            markerMat.SetFloat("_Smoothness", 0.25f);
            markerMat.EnableKeyword("_EMISSION");
            markerMat.SetColor("_EmissionColor", new Color(0.851f, 0.510f, 0.169f, 1f) * 1.35f);
            EditorUtility.SetDirty(markerMat);

            // 4. Sprite Lit (alpha-clipped URP Lit, Point filter, smoothness 0, casts shadows)
            var spriteLit = LoadOrCreateMaterial(MaterialsFolder + "/PixelInvadersSpriteLit.mat", urpLit);
            spriteLit.shader = urpLit;
            spriteLit.SetColor("_BaseColor", Color.white);
            spriteLit.SetFloat("_Smoothness", 0f);
            spriteLit.SetFloat("_SpecularHighlights", 0f);
            spriteLit.SetFloat("_EnvironmentReflections", 0f);
            spriteLit.SetFloat("_AlphaClip", 1f);
            spriteLit.SetFloat("_Cutoff", 0.5f);
            spriteLit.SetFloat("_Cull", 0f);
            spriteLit.EnableKeyword("_ALPHATEST_ON");
            spriteLit.renderQueue = (int)RenderQueue.AlphaTest;
            EditorUtility.SetDirty(spriteLit);

            // 5. Sprite Emissive (alpha-clipped URP Unlit with HDR > 1 tint for Bloom)
            var spriteEmissive = LoadOrCreateMaterial(MaterialsFolder + "/PixelInvadersSpriteEmissive.mat", urpUnlit);
            spriteEmissive.shader = urpUnlit;
            spriteEmissive.SetColor("_BaseColor", new Color(1.65f, 1.65f, 1.65f, 1f));
            spriteEmissive.SetFloat("_AlphaClip", 1f);
            spriteEmissive.SetFloat("_Cutoff", 0.5f);
            spriteEmissive.SetFloat("_Cull", 0f);
            spriteEmissive.EnableKeyword("_ALPHATEST_ON");
            spriteEmissive.renderQueue = (int)RenderQueue.AlphaTest;
            EditorUtility.SetDirty(spriteEmissive);
        }

        static void ConfigureTrenchMaterial(
            Material mat,
            Shader shader,
            Color baseColor,
            float smoothness,
            Texture2D albedoTex,
            Texture2D normalTex,
            PixelInvadersSettings settings)
        {
            mat.shader = shader;
            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", 0.35f);
            if (albedoTex != null) mat.SetTexture("_DetailAlbedo", albedoTex);
            if (normalTex != null) mat.SetTexture("_DetailNormal", normalTex);
            if (settings != null)
            {
                mat.SetFloat("_TextureStrength", settings.look.textureStrength);
                mat.SetFloat("_NormalStrength", settings.look.normalStrength);
                mat.SetFloat("_OcclusionStrength", settings.look.occlusionStrength);
            }
            EditorUtility.SetDirty(mat);
        }

        static Material LoadOrCreateMaterial(string path, Shader shader)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        static void EnsureFolder(string parent, string folderName)
        {
            string full = $"{parent}/{folderName}";
            if (!AssetDatabase.IsValidFolder(full))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
    }
}
