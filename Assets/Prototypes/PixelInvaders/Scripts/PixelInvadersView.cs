using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Prototypes.PixelInvaders
{
    [Serializable]
    public struct TransientSpriteFx
    {
        public Vector2 Position;
        public int StartFrame;
        public int EndFrame;
        public float Elapsed;
        public float FrameDuration;
        public float Scale;
    }

    [Serializable]
    public struct ScorePopupFx
    {
        public Vector3 WorldPos;
        public string Label;
        public Color Color;
        public float Elapsed;
        public float Duration;
    }

    /// <summary>
    /// Synchronizes the 2.5D scene objects (55 invader quads with white hit-flash frames, 4 bunker quads,
    /// cannon, UFO, capsule, projectiles, dynamic point lights, explosion sprite pool, floating score popups,
    /// Aseprite HUD badges, camera rig and uGUI HUD) with the simulation.
    /// </summary>
    public sealed class PixelInvadersView : MonoBehaviour
    {
        public PixelInvadersSettings settings;
        public Camera mainCamera;
        public Light keyLight;

        public SpriteQuad[] invaderQuads = Array.Empty<SpriteQuad>();
        public BunkerTextureView[] bunkerViews = Array.Empty<BunkerTextureView>();
        public SpriteQuad cannonQuad;
        public SpriteQuad muzzleFlashQuad;
        public SpriteQuad ufoQuad;
        public Light ufoPointLight;
        public SpriteQuad capsuleQuad;

        public SpriteQuad[] projectilePool = Array.Empty<SpriteQuad>();
        public Light[] projectileLights = Array.Empty<Light>();
        public SpriteQuad[] fxPool = Array.Empty<SpriteQuad>();

        public Text scoreText;
        public Text highScoreText;
        public Text waveText;
        public Text livesText;
        public Text powerupText;
        public Text statusOverlayText;

        public Texture2D hudSheet;
        public TextAsset hudSheetJson;
        public RawImage crownIcon;
        public RawImage[] lifeIcons = Array.Empty<RawImage>();
        public RawImage powerupBadgeIcon;
        public Text[] scorePopupPool = Array.Empty<Text>();

        readonly List<TransientSpriteFx> activeFx = new List<TransientSpriteFx>();
        readonly List<ScorePopupFx> activePopups = new List<ScorePopupFx>();
        readonly float[] invaderHitFlashTimers = new float[55];
        SheetFrames parsedHudFrames;
        float recoilTimer;
        float muzzleFlashTimer;
        float cannonShatterTimer;
        float visualTime;

        public int ActiveInvaderQuadCount
        {
            get
            {
                int count = 0;
                if (invaderQuads == null) return 0;
                for (int i = 0; i < invaderQuads.Length; i++)
                {
                    if (invaderQuads[i] != null && invaderQuads[i].gameObject.activeSelf) count++;
                }
                return count;
            }
        }

        public int ActiveExplosionCount => activeFx.Count;
        public int ActivePopupCount => activePopups.Count;

        public void HandleEvent(GameEvent ev)
        {
            switch (ev.Kind)
            {
                case GameEventKind.ShotFired:
                    recoilTimer = 0.10f;
                    muzzleFlashTimer = 0.055f;
                    break;
                case GameEventKind.InvaderKilled:
                    int hitIndex = FindInvaderIndexAt(ev.Position);
                    if (hitIndex >= 0 && hitIndex < invaderHitFlashTimers.Length &&
                        (settings == null || settings.feel.effectStrength > 0f))
                    {
                        invaderHitFlashTimers[hitIndex] = 0.055f;
                    }
                    SpawnFx(ev.Position, startFrame: 6, endFrame: 8, frameDuration: 0.065f, scale: 0.92f);
                    Color rankColor = ev.Rank == InvaderRank.Scout
                        ? new Color(1.0f, 0.28f, 0.41f)
                        : (ev.Rank == InvaderRank.Guard ? new Color(0.72f, 0.36f, 1.0f) : new Color(0.22f, 0.85f, 0.45f));
                    SpawnPopup(ev.Position, $"+{ev.Value}", rankColor);
                    break;
                case GameEventKind.UfoDestroyed:
                    SpawnFx(ev.Position, startFrame: 9, endFrame: 12, frameDuration: 0.075f, scale: 1.35f);
                    SpawnPopup(ev.Position, $"+{ev.Value}", new Color(1.0f, 0.82f, 0.28f));
                    break;
                case GameEventKind.PowerupCollected:
                    string pLabel = ev.Powerup == PowerupKind.RapidFire
                        ? "RAPID!"
                        : (ev.Powerup == PowerupKind.TwinShot ? "TWIN SHOT!" : "SHIELD REPAIRED!");
                    Color pCol = ev.Powerup == PowerupKind.RapidFire
                        ? new Color(1.0f, 0.82f, 0.28f)
                        : (ev.Powerup == PowerupKind.TwinShot ? new Color(0.17f, 0.91f, 0.96f) : new Color(0.22f, 0.85f, 0.45f));
                    SpawnPopup(ev.Position, pLabel, pCol);
                    break;
                case GameEventKind.BunkerEroded:
                case GameEventKind.ShotMissed:
                case GameEventKind.ShotsCancelled:
                    SpawnFx(ev.Position, startFrame: 13, endFrame: 14, frameDuration: 0.05f, scale: 0.65f);
                    break;
                case GameEventKind.CannonDestroyed:
                    cannonShatterTimer = 0f;
                    break;
            }
        }

        int FindInvaderIndexAt(Vector2 pos)
        {
            if (invaderQuads == null) return -1;
            int best = -1;
            float bestDist = 0.25f;
            for (int i = 0; i < invaderQuads.Length; i++)
            {
                if (invaderQuads[i] == null) continue;
                Vector3 lp = invaderQuads[i].transform.localPosition;
                float d = Vector2.Distance(new Vector2(lp.x, lp.z), pos);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = i;
                }
            }
            return best;
        }

        void SpawnFx(Vector2 pos, int startFrame, int endFrame, float frameDuration, float scale)
        {
            if (settings != null && settings.feel.effectStrength <= 0f) return;
            activeFx.Add(new TransientSpriteFx
            {
                Position = pos,
                StartFrame = startFrame,
                EndFrame = endFrame,
                Elapsed = 0f,
                FrameDuration = frameDuration,
                Scale = scale,
            });
        }

        void SpawnPopup(Vector2 pos, string label, Color color)
        {
            if (settings != null && settings.feel.effectStrength <= 0f) return;
            activePopups.Add(new ScorePopupFx
            {
                WorldPos = new Vector3(pos.x, 0.45f, pos.y),
                Label = label,
                Color = color,
                Elapsed = 0f,
                Duration = 0.55f,
            });
        }

        public void Sync(PixelInvadersSimulation sim, float dt, Vector2 cameraShakeOffset)
        {
            if (sim == null) return;
            visualTime += dt;
            if (recoilTimer > 0f) recoilTimer = Mathf.Max(0f, recoilTimer - dt);
            if (muzzleFlashTimer > 0f) muzzleFlashTimer = Mathf.Max(0f, muzzleFlashTimer - dt);
            if (sim.Phase == GamePhase.Respawning) cannonShatterTimer += dt;
            for (int i = 0; i < invaderHitFlashTimers.Length; i++)
            {
                if (invaderHitFlashTimers[i] > 0f)
                {
                    invaderHitFlashTimers[i] = Mathf.Max(0f, invaderHitFlashTimers[i] - dt);
                }
            }

            SyncCamera(cameraShakeOffset);
            SyncInvaders(sim);
            SyncBunkers(sim);
            SyncCannon(sim);
            SyncUfoAndCapsule(sim);
            SyncProjectiles(sim);
            SyncTransientFx(dt);
            SyncScorePopups(dt);
            SyncHud(sim);
        }

        void SyncCamera(Vector2 shakeOffset)
        {
            if (mainCamera == null || settings == null) return;
            float pitchRad = settings.camera.pitchDeg * Mathf.Deg2Rad;
            float dist = settings.camera.distance;
            Vector3 lookTarget = new Vector3(0f, 0f, settings.camera.lookAtZ);
            Vector3 basePos = lookTarget + new Vector3(0f, Mathf.Sin(pitchRad) * dist, -Mathf.Cos(pitchRad) * dist);
            float eff = Mathf.Clamp01(settings.feel.effectStrength);
            Vector3 shakenPos = basePos + new Vector3(shakeOffset.x * eff, 0f, shakeOffset.y * eff);
            mainCamera.transform.position = shakenPos;
            mainCamera.transform.rotation = Quaternion.Euler(settings.camera.pitchDeg, 0f, 0f);
            mainCamera.fieldOfView = settings.camera.fovDeg;
        }

        void SyncInvaders(PixelInvadersSimulation sim)
        {
            var list = sim.Invaders;
            int count = Mathf.Min(invaderQuads.Length, list.Count);
            for (int i = 0; i < count; i++)
            {
                var quad = invaderQuads[i];
                if (quad == null) continue;
                var inv = list[i];
                int baseFrame = inv.Rank == InvaderRank.Scout ? 0 : (inv.Rank == InvaderRank.Guard ? 3 : 6);

                if (!inv.Alive)
                {
                    if (i < invaderHitFlashTimers.Length && invaderHitFlashTimers[i] > 0f)
                    {
                        if (!quad.gameObject.activeSelf) quad.gameObject.SetActive(true);
                        quad.PlaceOnDeck(inv.Position.x, inv.Position.y, 0.025f);
                        quad.ShowFrame(baseFrame + 2); // White hit-flash frame in invaders_sheet
                    }
                    else if (quad.gameObject.activeSelf)
                    {
                        quad.gameObject.SetActive(false);
                    }
                    continue;
                }
                if (!quad.gameObject.activeSelf) quad.gameObject.SetActive(true);
                quad.PlaceOnDeck(inv.Position.x, inv.Position.y, 0.02f);
                quad.ShowFrame(baseFrame + (sim.PoseFrame & 1));
            }
        }

        void SyncBunkers(PixelInvadersSimulation sim)
        {
            int count = Mathf.Min(bunkerViews.Length, sim.Bunkers.Count);
            for (int i = 0; i < count; i++)
            {
                if (bunkerViews[i] != null) bunkerViews[i].Sync(sim.Bunkers[i]);
            }
        }

        void SyncCannon(PixelInvadersSimulation sim)
        {
            if (cannonQuad == null) return;
            float eff = settings != null ? Mathf.Clamp01(settings.feel.effectStrength) : 1f;
            float recoilZ = (recoilTimer > 0f ? -0.08f * (recoilTimer / 0.10f) : 0f) * eff;
            cannonQuad.PlaceOnDeck(sim.CannonX, sim.CannonZ + recoilZ, 0.02f);

            if (sim.Phase == GamePhase.Respawning)
            {
                int shatterIdx = Mathf.Clamp(4 + Mathf.FloorToInt(cannonShatterTimer / 0.15f), 4, 7);
                cannonQuad.ShowFrame(shatterIdx);
                cannonQuad.transform.localRotation = Quaternion.Euler(35f, 0f, 0f);
            }
            else
            {
                int frame = recoilTimer > 0f
                    ? 3
                    : (sim.CannonMoveVelocity < -0.5f ? 1 : (sim.CannonMoveVelocity > 0.5f ? 2 : 0));
                cannonQuad.ShowFrame(frame);
                float tiltZ = Mathf.Clamp(-sim.CannonMoveVelocity * 0.55f, -4f, 4f) * eff;
                cannonQuad.transform.localRotation = Quaternion.Euler(35f, 0f, tiltZ);
            }

            if (muzzleFlashQuad != null)
            {
                bool showFlash = muzzleFlashTimer > 0f && eff > 0f && sim.Phase == GamePhase.Playing;
                if (muzzleFlashQuad.gameObject.activeSelf != showFlash)
                {
                    muzzleFlashQuad.gameObject.SetActive(showFlash);
                }
                if (showFlash)
                {
                    muzzleFlashQuad.PlaceOnDeck(sim.CannonX, sim.CannonZ + 0.45f, 0.22f);
                    muzzleFlashQuad.ShowFrame(10);
                }
            }
        }

        void SyncUfoAndCapsule(PixelInvadersSimulation sim)
        {
            if (ufoQuad != null)
            {
                bool active = sim.ActiveUfo.Active;
                if (ufoQuad.gameObject.activeSelf != active) ufoQuad.gameObject.SetActive(active);
                if (active)
                {
                    ufoQuad.PlaceOnDeck(sim.ActiveUfo.Position.x, sim.ActiveUfo.Position.y, 0.02f);
                    int spinFrame = 9 + (Mathf.FloorToInt(visualTime * 8f) & 1);
                    ufoQuad.ShowFrame(spinFrame);
                }
                if (ufoPointLight != null)
                {
                    ufoPointLight.enabled = active;
                    if (active)
                    {
                        ufoPointLight.transform.position = new Vector3(
                            sim.ActiveUfo.Position.x, 0.55f, sim.ActiveUfo.Position.y);
                    }
                }
            }

            if (capsuleQuad != null)
            {
                bool active = sim.ActiveCapsule.Active;
                if (capsuleQuad.gameObject.activeSelf != active) capsuleQuad.gameObject.SetActive(active);
                if (active)
                {
                    capsuleQuad.PlaceOnDeck(sim.ActiveCapsule.Position.x, sim.ActiveCapsule.Position.y, 0.28f);
                    int pulse = Mathf.FloorToInt(visualTime * 8f) & 1;
                    int baseIdx = sim.ActiveCapsule.Kind == PowerupKind.RapidFire
                        ? 0
                        : (sim.ActiveCapsule.Kind == PowerupKind.TwinShot ? 2 : 4);
                    capsuleQuad.ShowFrame(baseIdx + pulse);
                }
            }
        }

        void SyncProjectiles(PixelInvadersSimulation sim)
        {
            int used = 0;
            int poolLen = projectilePool != null ? projectilePool.Length : 0;

            for (int i = 0; i < sim.PlayerShots.Count && used < poolLen; i++)
            {
                var shot = sim.PlayerShots[i];
                var quad = projectilePool[used];
                if (quad != null)
                {
                    if (!quad.gameObject.activeSelf) quad.gameObject.SetActive(true);
                    quad.PlaceOnDeck(shot.Position.x, shot.Position.y, 0.20f);
                    quad.ShowFrame(shot.TwinBolt ? 9 : 8);
                }
                if (projectileLights != null && used < projectileLights.Length && projectileLights[used] != null)
                {
                    var lt = projectileLights[used];
                    lt.enabled = true;
                    lt.color = new Color(0.17f, 0.91f, 0.96f);
                    lt.transform.position = new Vector3(shot.Position.x, 0.35f, shot.Position.y);
                }
                used++;
            }

            int alienAnim = Mathf.FloorToInt(visualTime * 12f) & 1;
            for (int i = 0; i < sim.AlienShots.Count && used < poolLen; i++)
            {
                var shot = sim.AlienShots[i];
                var quad = projectilePool[used];
                if (quad != null)
                {
                    if (!quad.gameObject.activeSelf) quad.gameObject.SetActive(true);
                    quad.PlaceOnDeck(shot.Position.x, shot.Position.y, 0.20f);
                    quad.ShowFrame(11 + alienAnim);
                }
                if (projectileLights != null && used < projectileLights.Length && projectileLights[used] != null)
                {
                    var lt = projectileLights[used];
                    lt.enabled = true;
                    lt.color = new Color(1.0f, 0.28f, 0.41f);
                    lt.transform.position = new Vector3(shot.Position.x, 0.35f, shot.Position.y);
                }
                used++;
            }

            for (int i = used; i < poolLen; i++)
            {
                if (projectilePool[i] != null && projectilePool[i].gameObject.activeSelf)
                {
                    projectilePool[i].gameObject.SetActive(false);
                }
                if (projectileLights != null && i < projectileLights.Length && projectileLights[i] != null)
                {
                    projectileLights[i].enabled = false;
                }
            }
        }

        void SyncTransientFx(float dt)
        {
            for (int i = activeFx.Count - 1; i >= 0; i--)
            {
                var fx = activeFx[i];
                fx.Elapsed += dt;
                int totalFrames = fx.EndFrame - fx.StartFrame + 1;
                if (fx.Elapsed >= totalFrames * fx.FrameDuration)
                {
                    activeFx.RemoveAt(i);
                }
                else
                {
                    activeFx[i] = fx;
                }
            }

            if (fxPool == null) return;
            for (int i = 0; i < fxPool.Length; i++)
            {
                var quad = fxPool[i];
                if (quad == null) continue;
                if (i < activeFx.Count)
                {
                    var fx = activeFx[i];
                    if (!quad.gameObject.activeSelf) quad.gameObject.SetActive(true);
                    quad.PlaceOnDeck(fx.Position.x, fx.Position.y, 0.30f);
                    quad.SetSize(new Vector2(fx.Scale, fx.Scale));
                    int frame = Mathf.Clamp(
                        fx.StartFrame + Mathf.FloorToInt(fx.Elapsed / fx.FrameDuration),
                        fx.StartFrame,
                        fx.EndFrame);
                    quad.ShowFrame(frame);
                }
                else if (quad.gameObject.activeSelf)
                {
                    quad.gameObject.SetActive(false);
                }
            }
        }

        void SyncScorePopups(float dt)
        {
            for (int i = activePopups.Count - 1; i >= 0; i--)
            {
                var p = activePopups[i];
                p.Elapsed += dt;
                p.WorldPos.z += 0.65f * dt;
                if (p.Elapsed >= p.Duration)
                {
                    activePopups.RemoveAt(i);
                }
                else
                {
                    activePopups[i] = p;
                }
            }

            if (scorePopupPool == null || mainCamera == null) return;
            for (int i = 0; i < scorePopupPool.Length; i++)
            {
                var txt = scorePopupPool[i];
                if (txt == null) continue;
                if (i < activePopups.Count)
                {
                    var p = activePopups[i];
                    if (!txt.gameObject.activeSelf) txt.gameObject.SetActive(true);
                    txt.text = p.Label;
                    float alpha = 1f - Mathf.Clamp01(p.Elapsed / p.Duration);
                    Color c = p.Color;
                    c.a = alpha;
                    txt.color = c;
                    Vector3 vp = mainCamera.WorldToViewportPoint(p.WorldPos);
                    var rt = txt.rectTransform;
                    rt.anchorMin = new Vector2(vp.x, vp.y);
                    rt.anchorMax = new Vector2(vp.x, vp.y);
                    rt.anchoredPosition = Vector2.zero;
                }
                else if (txt.gameObject.activeSelf)
                {
                    txt.gameObject.SetActive(false);
                }
            }
        }

        void SyncHud(PixelInvadersSimulation sim)
        {
            EnsureHudSheetParsed();

            if (scoreText != null) scoreText.text = $"SCORE {sim.Score:D4}";
            if (highScoreText != null) highScoreText.text = $"HI {sim.HighScore:D4}";
            if (waveText != null) waveText.text = $"WAVE {sim.Wave}";
            if (livesText != null) livesText.text = $"LIVES {sim.Lives}";

            if (crownIcon != null) ApplyHudFrame(crownIcon, 2); // crown_icon in bunker_and_hud_sheet

            if (lifeIcons != null)
            {
                for (int i = 0; i < lifeIcons.Length; i++)
                {
                    if (lifeIcons[i] != null)
                    {
                        lifeIcons[i].enabled = i < sim.Lives;
                        ApplyHudFrame(lifeIcons[i], 1); // life_icon in bunker_and_hud_sheet
                    }
                }
            }

            if (powerupText != null)
            {
                if (sim.ActivePowerup != PowerupKind.None && sim.PowerupTimeLeft > 0f)
                {
                    string label = sim.ActivePowerup == PowerupKind.RapidFire ? "RAPID FIRE" : "TWIN SHOT";
                    powerupText.text = $"{label} {sim.PowerupTimeLeft:0.0}s";
                    if (powerupBadgeIcon != null)
                    {
                        powerupBadgeIcon.enabled = true;
                        int badgeFrame = sim.ActivePowerup == PowerupKind.RapidFire ? 3 : 4;
                        ApplyHudFrame(powerupBadgeIcon, badgeFrame);
                    }
                }
                else
                {
                    powerupText.text = "";
                    if (powerupBadgeIcon != null) powerupBadgeIcon.enabled = false;
                }
            }

            if (statusOverlayText != null)
            {
                if (sim.Phase == GamePhase.GameOver)
                {
                    statusOverlayText.text = "GAME OVER\nPRESS SPACE TO RESTART";
                }
                else if (sim.Phase == GamePhase.WaveClear)
                {
                    statusOverlayText.text = $"WAVE {sim.Wave} CLEARED!";
                }
                else
                {
                    statusOverlayText.text = "";
                }
            }
        }

        void EnsureHudSheetParsed()
        {
            if (parsedHudFrames == null && hudSheetJson != null && !string.IsNullOrEmpty(hudSheetJson.text))
            {
                parsedHudFrames = SheetFrames.Parse(hudSheetJson.text);
            }
        }

        void ApplyHudFrame(RawImage img, int frameIndex)
        {
            if (img == null || hudSheet == null || parsedHudFrames == null) return;
            img.texture = hudSheet;
            parsedHudFrames.Uv(frameIndex, out Vector2 scale, out Vector2 offset);
            img.uvRect = new Rect(offset.x, offset.y, scale.x, scale.y);
        }
    }
}
