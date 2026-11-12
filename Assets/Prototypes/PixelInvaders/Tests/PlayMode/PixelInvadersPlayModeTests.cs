using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Prototypes.PixelInvaders.Tests
{
    public class PixelInvadersPlayModeTests
    {
        const string ScenePath = "Assets/Prototypes/PixelInvaders/Scenes/PixelInvaders.unity";

        static AsyncOperation LoadPrototypeScene()
        {
#if UNITY_EDITOR
            return EditorSceneManager.LoadSceneAsyncInPlayMode(
                ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
#else
            return SceneManager.LoadSceneAsync("PixelInvaders", LoadSceneMode.Single);
#endif
        }

        [UnityTest]
        public IEnumerator Scene_loads_and_input_moves_cannon_destroys_invader_and_updates_hud()
        {
            yield return LoadPrototypeScene();

            var game = Object.FindAnyObjectByType<PixelInvadersGame>();
            Assert.IsNotNull(game, "PixelInvadersGame must exist in loaded scene");
            game.autoStep = false;

            var view = game.view;
            Assert.IsNotNull(view);
            Assert.IsNotNull(Object.FindAnyObjectByType<AudioListener>(), "Scene must have an active AudioListener");
            Assert.IsNotNull(game.audioDirector.musicSource.clip, "MusicSource must have music.wav assigned");
            Assert.IsTrue(game.audioDirector.musicSource.isPlaying, "MusicSource must be playing in Play Mode");
            Assert.AreEqual(55, view.ActiveInvaderQuadCount);
            Assert.AreEqual(4, view.bunkerViews.Length);
            Assert.IsNotNull(view.bunkerViews[0].RuntimeTexture);

            // 1. Move cannon right and verify view transform follows
            game.StepFrame(0.10f, new PixelInvadersInput { moveAxis = 1f });
            Assert.Greater(game.Simulation.CannonX, 0.5f);
            Assert.AreEqual(game.Simulation.CannonX, view.cannonQuad.transform.localPosition.x, 1e-3f);

            // 2. Align under column 5 (x = 0), clear bunkers so shot hits bottom Heavy invader directly
            foreach (var b in game.Simulation.Bunkers) b.ClearAll();
            game.Simulation.SetCannonXForTest(0f);
            game.StepFrame(0.016f, new PixelInvadersInput { firePressed = true });

            for (int i = 0; i < 100 && game.Simulation.AliveInvaderCount == 55; i++)
            {
                game.StepFrame(0.02f, default);
            }

            Assert.AreEqual(54, game.Simulation.AliveInvaderCount);
            Assert.AreEqual(55, view.ActiveInvaderQuadCount, "Killed invader quad stays active briefly for its white hit-flash frame");

            // Step past the 0.055s white hit-flash timer so the killed quad deactivates
            for (int i = 0; i < 4; i++)
            {
                game.StepFrame(0.02f, default);
            }
            Assert.AreEqual(54, view.ActiveInvaderQuadCount);
            Assert.AreEqual(10, game.Simulation.Score);
            StringAssert.Contains("0010", view.scoreText.text);
        }

        [UnityTest]
        public IEnumerator Powerup_capsule_and_game_over_restart_work_end_to_end()
        {
            yield return LoadPrototypeScene();

            var game = Object.FindAnyObjectByType<PixelInvadersGame>();
            Assert.IsNotNull(game);
            game.autoStep = false;

            // Spawn UFO at x = 0, destroy it, catch capsule
            game.Simulation.SetCannonXForTest(0f);
            game.Simulation.SpawnUfoForTest(0f, 1, PowerupKind.RapidFire);
            game.Simulation.KillUfoWithShotForTest();
            game.StepFrame(0.02f, default);
            Assert.IsTrue(game.view.capsuleQuad.gameObject.activeSelf);

            for (int i = 0; i < 200 && game.Simulation.ActiveCapsule.Active; i++)
            {
                game.StepFrame(0.02f, default);
            }
            Assert.AreEqual(PowerupKind.RapidFire, game.Simulation.ActivePowerup);
            StringAssert.Contains("RAPID FIRE", game.view.powerupText.text);

            // Force invasion Game Over and verify overlay + restart
            int highScoreBefore = game.Simulation.HighScore;
            game.Simulation.SetInvaderPositionForTest(0, new Vector2(0f, game.settings.arena.defenseLineZ));
            for (int i = 0; i < 60 && game.Simulation.Phase != GamePhase.GameOver; i++)
            {
                game.StepFrame(0.02f, default);
            }
            Assert.AreEqual(GamePhase.GameOver, game.Simulation.Phase);
            StringAssert.Contains("GAME OVER", game.view.statusOverlayText.text);

            game.StepFrame(0.02f, new PixelInvadersInput { restartPressed = true });
            Assert.AreEqual(GamePhase.Playing, game.Simulation.Phase);
            Assert.AreEqual(55, game.Simulation.AliveInvaderCount);
            Assert.AreEqual(55, game.view.ActiveInvaderQuadCount);
            Assert.AreEqual(highScoreBefore, game.Simulation.HighScore);
        }

        [UnityTest]
        public IEnumerator EffectStrength_zero_disables_runtime_shake_hitstop_and_transient_fx()
        {
            yield return LoadPrototypeScene();

            var game = Object.FindAnyObjectByType<PixelInvadersGame>();
            Assert.IsNotNull(game);
            game.autoStep = false;

            float savedStrength = game.settings.feel.effectStrength;
            try
            {
                game.settings.feel.effectStrength = 0f;
                game.Simulation.KillInvaderForTest(0);
                game.StepFrame(0.02f, new PixelInvadersInput { firePressed = true });

                Assert.IsFalse(game.TimeFeel.HitStopActive, "Hit-stop must be off when effectStrength is 0");
                Assert.AreEqual(0f, game.Shake.Amplitude, 1e-5f, "Shake amplitude must be 0 when effectStrength is 0");
                Assert.AreEqual(0, game.view.ActiveExplosionCount, "Transient explosions must not spawn when effectStrength is 0");
                Assert.AreEqual(0, game.view.ActivePopupCount, "Score popups must not spawn when effectStrength is 0");
            }
            finally
            {
                game.settings.feel.effectStrength = savedStrength;
            }
        }
    }
}
