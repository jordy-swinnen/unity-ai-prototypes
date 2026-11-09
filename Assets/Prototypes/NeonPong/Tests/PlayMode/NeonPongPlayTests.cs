using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace Prototypes.NeonPong.Tests
{
    public class NeonPongPlayTests
    {
        const string ScenePath = "Assets/Prototypes/NeonPong/Scenes/NeonPong.unity";
        const float Tol = 0.01f;

        NeonPongGame game;
        NeonPongView view;
        NeonPongSettings settings;
        float savedStrength;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return null;
#endif
            game = Object.FindFirstObjectByType<NeonPongGame>();
            view = Object.FindFirstObjectByType<NeonPongView>();
            Assert.IsNotNull(game, "the NeonPong scene has no game");
            Assert.IsNotNull(view, "the NeonPong scene has no view");
            settings = game.settings;
            savedStrength = settings.effectStrength;
            game.autoStep = false; // the tests step the rules by hand
            game.InputSource = () => default;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (settings != null) settings.effectStrength = savedStrength; // never leave the asset changed
            yield return null;
        }

        static Text Text(string name) => GameObject.Find(name).GetComponent<Text>();

        [UnityTest]
        public IEnumerator Paddles_and_ball_follow_the_state()
        {
            var state = game.Simulation.State;
            state.SetPaddleZ(Side.Left, 2f);
            state.SetPaddleZ(Side.Right, -3f);
            state.BallPos = new Vector2(1.5f, -2.5f);
            view.Refresh();
            yield return null;

            Assert.AreEqual(2f, view.leftPaddle.position.z, Tol);
            Assert.AreEqual(-settings.paddleX, view.leftPaddle.position.x, Tol);
            Assert.AreEqual(-3f, view.rightPaddle.position.z, Tol);
            Assert.AreEqual(settings.paddleX, view.rightPaddle.position.x, Tol);
            Assert.AreEqual(1.5f, view.ball.position.x, Tol);
            Assert.AreEqual(-2.5f, view.ball.position.z, Tol);
            Assert.AreEqual(settings.ballRadius, view.ball.position.y, Tol);
        }

        [UnityTest]
        public IEnumerator Input_moves_the_paddles()
        {
            game.InputSource = () => new NeonPongInput { Left = 1f, Right = -1f };
            game.Tick(0.3f);
            view.Refresh();
            yield return null;

            Assert.Greater(view.leftPaddle.position.z, 0.5f);
            Assert.Less(view.rightPaddle.position.z, -0.5f);
        }

        [UnityTest]
        public IEnumerator A_point_updates_the_score_on_screen()
        {
            Assert.AreEqual("0", Text("Score Left").text);
            Assert.AreEqual("0", Text("Score Right").text);

            var state = game.Simulation.State;
            state.Phase = Phase.Playing;
            state.BallPos = new Vector2(-7.9f, 3f); // past the left paddle, heading to the left end line
            state.BallVel = new Vector2(-9f, 0f);
            state.BallSpeed = 9f;
            game.Tick(0.1f);
            view.Refresh();
            yield return null;

            Assert.AreEqual("0", Text("Score Left").text);
            Assert.AreEqual("1", Text("Score Right").text);
        }

        [UnityTest]
        public IEnumerator A_win_shows_the_winner_and_Space_restarts()
        {
            var state = game.Simulation.State;
            state.Score[(int)Side.Left] = settings.winScore - 1;
            state.Phase = Phase.Playing;
            state.BallPos = new Vector2(7.9f, 3f);
            state.BallVel = new Vector2(9f, 0f);
            state.BallSpeed = 9f;
            game.Tick(0.1f);
            view.Refresh();
            yield return null;

            Assert.AreEqual(Phase.GameOver, state.Phase);
            StringAssert.Contains("PLAYER 1", Text("Message").text);
            StringAssert.Contains("WINS", Text("Message").text);
            Assert.AreEqual(settings.winScore.ToString(), Text("Score Left").text);

            game.InputSource = () => new NeonPongInput { Restart = true };
            game.Tick(0.01f);
            view.Refresh();
            yield return null;

            Assert.AreEqual(Phase.Serving, state.Phase);
            Assert.AreEqual("0", Text("Score Left").text);
            Assert.AreEqual("", Text("Message").text);
        }

        [UnityTest]
        public IEnumerator The_game_steps_the_rules_by_itself_when_autoStep_is_on()
        {
            game.autoStep = true;
            game.InputSource = () => new NeonPongInput { Left = 1f };
            for (int i = 0; i < 20; i++) yield return null;
            Assert.Greater(game.Simulation.State.GetPaddleZ(Side.Left), 0f);
        }

        [UnityTest]
        public IEnumerator Space_on_a_keyboard_asks_for_a_restart()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                yield return null; // the event is processed at the start of this frame
                Assert.IsTrue(NeonPongKeyboard.Read(keyboard).Restart);

                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                yield return null;
                Assert.IsFalse(NeonPongKeyboard.Read(keyboard).Restart);
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
            }
        }

        // A right-paddle hit: the ball is a short way from the paddle, heading right.
        void SetUpPaddleHit(float speed)
        {
            var state = game.Simulation.State;
            state.Phase = Phase.Playing;
            state.BallPos = new Vector2(6.8f, 0f);
            state.BallVel = new Vector2(speed, 0f);
            state.BallSpeed = speed;
        }

        [UnityTest]
        public IEnumerator A_paddle_hit_triggers_feedback_sound_and_particles()
        {
            var raised = new System.Collections.Generic.List<EventKind>();
            game.EventRaised += e => raised.Add(e.Kind);
            var audio = Object.FindFirstObjectByType<NeonPongAudio>();
            var bursts = Object.FindFirstObjectByType<NeonPongBursts>();
            int played = audio.PlayCount;
            int emitted = bursts.TotalEmitted;

            SetUpPaddleHit(12f);
            game.Tick(0.1f);
            view.Refresh();
            yield return null;

            CollectionAssert.Contains(raised, EventKind.PaddleHit);
            Assert.AreEqual(1f, game.Feel.PaddleFlash[(int)Side.Right], 0.5f);
            Assert.Greater(game.Feel.Shake.Amplitude, 0f);
            Assert.Greater(audio.PlayCount, played);
            Assert.AreEqual(EventKind.PaddleHit, audio.LastPlayed);
            Assert.Greater(bursts.TotalEmitted, emitted);
        }

        [UnityTest]
        public IEnumerator A_faster_rally_plays_the_paddle_sound_at_a_higher_pitch()
        {
            var audio = Object.FindFirstObjectByType<NeonPongAudio>();
            SetUpPaddleHit(settings.baseSpeed);
            game.Tick(0.1f);
            float slow = audio.LastPitch;

            game.Feel.Time.Clear(); // end the hit-stop so the second hit can happen
            game.Simulation.State.SetPaddleZ(Side.Right, 0f);
            SetUpPaddleHit(settings.maxSpeed);
            game.Tick(0.1f);
            yield return null;
            Assert.Greater(audio.LastPitch, slow);
        }

        [UnityTest]
        public IEnumerator Hit_stop_pauses_the_rules_for_a_moment_and_then_ends()
        {
            SetUpPaddleHit(12f);
            game.Tick(0.1f); // the hit
            var state = game.Simulation.State;
            Assert.IsTrue(game.Feel.Time.HitStopActive);

            var frozen = state.BallPos;
            game.Tick(0.01f);
            Assert.AreEqual(frozen, state.BallPos, "the ball must not move during the hit-stop");

            for (int i = 0; i < 10; i++) game.Tick(0.016f);
            yield return null;
            Assert.IsFalse(game.Feel.Time.HitStopActive);
            Assert.AreNotEqual(frozen, state.BallPos, "the ball moves again after it");
        }

        [UnityTest]
        public IEnumerator The_camera_shakes_on_a_hit_and_settles_back_exactly()
        {
            var cam = Camera.main.transform;
            var rest = cam.position;
            var rig = cam.GetComponent<NeonPongCameraRig>();

            SetUpPaddleHit(12f);
            game.Tick(0.1f);
            game.Tick(0.01f);
            rig.SendMessage("LateUpdate");
            yield return null;
            Assert.AreNotEqual(rest, cam.position, "the camera should move during the shake");

            // Park the rules (a long serve wait) so no new hit restarts the shake.
            game.Simulation.State.Phase = Phase.Serving;
            game.Simulation.State.ServeTimer = 100f;
            for (int i = 0; i < 60; i++) game.Tick(0.02f);
            rig.SendMessage("LateUpdate");
            yield return null;
            Assert.AreEqual(rest.x, cam.position.x, 0.0001f);
            Assert.AreEqual(rest.y, cam.position.y, 0.0001f);
            Assert.AreEqual(rest.z, cam.position.z, 0.0001f);
        }

        [UnityTest]
        public IEnumerator With_effect_strength_zero_nothing_shakes_flashes_or_bursts()
        {
            settings.effectStrength = 0f;
            var cam = Camera.main.transform;
            var rest = cam.position;
            var bursts = Object.FindFirstObjectByType<NeonPongBursts>();
            int emitted = bursts.TotalEmitted;

            SetUpPaddleHit(12f);
            game.Tick(0.1f);
            game.Tick(0.01f);
            Object.FindFirstObjectByType<NeonPongCameraRig>().SendMessage("LateUpdate");
            view.Refresh();
            yield return null;

            Assert.AreEqual(rest, cam.position);
            Assert.AreEqual(0f, game.Feel.PaddleFlash[(int)Side.Right]);
            Assert.IsFalse(game.Feel.Time.HitStopActive);
            Assert.AreEqual(emitted, bursts.TotalEmitted);
        }

        [UnityTest]
        public IEnumerator A_win_washes_the_screen_in_the_winners_colour_and_pushes_the_camera_in()
        {
            var cam = Camera.main.transform;
            var rest = cam.position;
            var wash = GameObject.Find("Wash").GetComponent<Image>();
            Assert.AreEqual(0f, wash.color.a, 0.001f);

            var state = game.Simulation.State;
            state.Score[(int)Side.Left] = settings.winScore - 1;
            state.Phase = Phase.Playing;
            state.BallPos = new Vector2(7.9f, 3f);
            state.BallVel = new Vector2(9f, 0f);
            state.BallSpeed = 9f;
            game.Tick(0.1f);
            for (int i = 0; i < 400; i++) game.Tick(0.02f);
            Object.FindFirstObjectByType<NeonPongCameraRig>().SendMessage("LateUpdate");
            view.Refresh();
            yield return null;

            Assert.AreEqual(settings.winWashAlpha, wash.color.a, 0.01f);
            Assert.AreEqual(settings.player1Color.r, wash.color.r, 0.01f);
            Assert.AreEqual(settings.player1Color.g, wash.color.g, 0.01f);
            Assert.Less(cam.position.magnitude, rest.magnitude * (1f - settings.winPushIn * 0.9f));
        }
    }
}
