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

    /// <summary>
    /// Synchronizes the 2.5D scene objects (55 invader quads, 4 bunker quads, cannon, UFO, capsule,
    /// projectiles, dynamic point lights, explosion sprite pool, camera rig and uGUI HUD) with the simulation.
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
        public Image[] lifeIcons = Array.Empty<Image>();

        readonly List<TransientSpriteFx> activeFx = new List<TransientSpriteFx>();
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

        public void HandleEvent(GameEvent ev)
        {
            switch (ev.Kind)
            {
                case GameEventKind.ShotFired:
                    recoilTimer = 0.10f;
                    muzzleFlashTimer = 0.055f;
                    break;
                case GameEventKind.InvaderKilled:
                    // Frames 6..8 in powerups_and_fx_sheet are the 3-frame alien pixel burst
                    SpawnFx(ev.Position, startFrame: 6, endFrame: 8, frameDuration: 0.06f, scale: 0.90f);
                    break;
                case GameEventKind.UfoDestroyed:
                    // Frames 9..12 in powerups_and_fx_sheet are the 4-frame UFO burst
                    SpawnFx(ev.Position, startFrame: 9, endFrame: 12, frameDuration: 0.075f, scale: 1.35f);
                    break;
                case GameEventKind.BunkerEroded:
                case GameEventKind.ShotMissed:
                case GameEventKind.ShotsCancelled:
                    // Frames 13..14 in powerups_and_fx_sheet are the 2-frame impact spark
                    SpawnFx(ev.Position, startFrame: 13, endFrame: 14, frameDuration: 0.05f, scale: 0.65f);
                    break;
                case GameEventKind.CannonDestroyed:
                    cannonShatterTimer = 0f;
                    break;
            }
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

        public void Sync(PixelInvadersSimulation sim, float dt, Vector2 cameraShakeOffset)
        {
            if (sim == null) return;
            visualTime += dt;
            if (recoilTimer > 0f) recoilTimer = Mathf.Max(0f, recoilTimer - dt);
            if (muzzleFlashTimer > 0f) muzzleFlashTimer = Mathf.Max(0f, muzzleFlashTimer - dt);
            if (sim.Phase == GamePhase.Respawning) cannonShatterTimer += dt;

            SyncCamera(cameraShakeOffset);
            SyncInvaders(sim);
            SyncBunkers(sim);
            SyncCannon(sim);
            SyncUfoAndCapsule(sim);
            SyncProjectiles(sim);
            SyncTransientFx(dt);
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
                if (!inv.Alive)
                {
                    if (quad.gameObject.activeSelf) quad.gameObject.SetActive(false);
                    continue;
                }
                if (!quad.gameObject.activeSelf) quad.gameObject.SetActive(true);
                quad.PlaceOnDeck(inv.Position.x, inv.Position.y, 0.02f);

                // invaders_sheet: Scout 0..2, Guard 3..5, Heavy 6..8
                int baseFrame = inv.Rank == InvaderRank.Scout ? 0 : (inv.Rank == InvaderRank.Guard ? 3 : 6);
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
                    muzzleFlashQuad.PlaceOnDeck(sim.CannonX, sim.CannonZ + 0.45f, 0.03f);
                    muzzleFlashQuad.ShowFrame(10); // muzzle_flash frame in cannon_and_shots_sheet
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
                    capsuleQuad.PlaceOnDeck(sim.ActiveCapsule.Position.x, sim.ActiveCapsule.Position.y, 0.03f);
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
                    quad.PlaceOnDeck(shot.Position.x, shot.Position.y, 0.03f);
                    quad.ShowFrame(shot.TwinBolt ? 9 : 8); // 8 = player bolt, 9 = twin bolt
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
                    quad.PlaceOnDeck(shot.Position.x, shot.Position.y, 0.03f);
                    quad.ShowFrame(11 + alienAnim); // 11 & 12 = alien zig-zag A & B
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
                    quad.PlaceOnDeck(fx.Position.x, fx.Position.y, 0.04f);
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

        void SyncHud(PixelInvadersSimulation sim)
        {
            if (scoreText != null) scoreText.text = $"SCORE {sim.Score:D4}";
            if (highScoreText != null) highScoreText.text = $"HI {sim.HighScore:D4}";
            if (waveText != null) waveText.text = $"WAVE {sim.Wave}";
            if (livesText != null) livesText.text = $"LIVES {sim.Lives}";

            if (lifeIcons != null)
            {
                for (int i = 0; i < lifeIcons.Length; i++)
                {
                    if (lifeIcons[i] != null) lifeIcons[i].enabled = i < sim.Lives;
                }
            }

            if (powerupText != null)
            {
                if (sim.ActivePowerup != PowerupKind.None && sim.PowerupTimeLeft > 0f)
                {
                    string label = sim.ActivePowerup == PowerupKind.RapidFire ? "RAPID FIRE" : "TWIN SHOT";
                    powerupText.text = $"{label} {sim.PowerupTimeLeft:0.0}s";
                }
                else
                {
                    powerupText.text = "";
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
    }
}
