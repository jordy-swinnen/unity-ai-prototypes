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
            game.autoStep = false; // the tests step the rules by hand
            game.InputSource = () => default;
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
    }
}
