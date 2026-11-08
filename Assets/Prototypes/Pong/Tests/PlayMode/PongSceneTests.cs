using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Prototypes.Pong.Tests
{
    // These tests play the real Pong scene, which the "Prototypes/Pong/Build Scene" command wrote.
    // The assembly must cover all platforms: Unity runs an Editor-only test assembly as EditMode.
    // The scene is loaded by its asset path, which only works inside the Editor.
    public class PongSceneTests
    {
        const string ScenePath = "Assets/Prototypes/Pong/Scenes/Pong.unity";
        const float Tolerance = 0.05f;

        PongGame game;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("These tests load the scene by asset path and only run in the Editor.");
            yield break;
#endif
            yield return null;
            game = Object.FindFirstObjectByType<PongGame>();
            Assert.IsNotNull(game, "the scene has no PongGame");
        }

        static Transform Find(string objectName)
        {
            GameObject found = GameObject.Find(objectName);
            Assert.IsNotNull(found, $"the scene has no object called {objectName}");
            return found.transform;
        }

        static string TextOf(string objectName) => Find(objectName).GetComponent<Text>().text;

        [Test]
        public void Scene_HasEveryPartOfTheGame()
        {
            foreach (string name in new[] { "Table", "WallTop", "WallBottom", "Paddle1", "Paddle2", "Ball" })
            {
                Assert.IsNotNull(Find(name).GetComponentInChildren<MeshRenderer>(), $"{name} has nothing to draw");
            }

            Assert.IsNotNull(Camera.main, "no main camera");
            Assert.IsNotNull(game.GetComponent<PongView>());
        }

        [UnityTest]
        public IEnumerator Ball_FollowsTheGameState()
        {
            var state = game.Simulation.State;
            state.Phase = PongPhase.Playing;
            state.BallPosition = new Vector2(2f, -1.5f);
            state.BallVelocity = Vector2.zero;

            yield return null;
            yield return null;

            Vector3 ball = Find("Ball").position;
            Assert.AreEqual(2f, ball.x, Tolerance);
            Assert.AreEqual(-1.5f, ball.z, Tolerance);
        }

        [UnityTest]
        public IEnumerator Player1Input_MovesTheLeftPaddleUp()
        {
            game.InputSource = () => new PongInput(1f, 0f);

            yield return new WaitForSeconds(0.3f);

            Assert.Greater(Find("Paddle1").position.z, 1.5f);
            Assert.AreEqual(0f, Find("Paddle2").position.z, Tolerance);
            Assert.AreEqual(-game.Settings.paddleX, Find("Paddle1").position.x, Tolerance);
        }

        [UnityTest]
        public IEnumerator Player2Input_MovesTheRightPaddleDown()
        {
            game.InputSource = () => new PongInput(0f, -1f);

            yield return new WaitForSeconds(0.3f);

            Assert.Less(Find("Paddle2").position.z, -1.5f);
            Assert.AreEqual(0f, Find("Paddle1").position.z, Tolerance);
        }

        [UnityTest]
        public IEnumerator PointScored_UpdatesTheScoreOnScreen()
        {
            var state = game.Simulation.State;
            state.Phase = PongPhase.Playing;
            state.BallPosition = new Vector2(7.9f, 3f);
            state.BallVelocity = new Vector2(8f, 0f);
            state.BallSpeed = 8f;

            yield return new WaitForSeconds(0.2f);

            Assert.AreEqual(1, state.Score1);
            Assert.AreEqual("1", TextOf("Score1"));
            Assert.AreEqual("0", TextOf("Score2"));
        }

        [UnityTest]
        public IEnumerator Winning_ShowsTheWinnerOnScreen()
        {
            var state = game.Simulation.State;
            state.Score2 = game.Settings.winScore - 1;
            state.Phase = PongPhase.Playing;
            state.BallPosition = new Vector2(-7.9f, 3f);
            state.BallVelocity = new Vector2(-8f, 0f);
            state.BallSpeed = 8f;

            yield return new WaitForSeconds(0.2f);

            Assert.AreEqual(PongPhase.GameOver, state.Phase);
            StringAssert.Contains("Player 2 wins", TextOf("Message"));
        }

        [UnityTest]
        public IEnumerator NewGame_ClearsTheWinnerMessage()
        {
            var state = game.Simulation.State;
            state.Score1 = game.Settings.winScore - 1;
            state.Phase = PongPhase.Playing;
            state.BallPosition = new Vector2(7.9f, 3f);
            state.BallVelocity = new Vector2(8f, 0f);
            state.BallSpeed = 8f;
            yield return new WaitForSeconds(0.2f);
            StringAssert.Contains("wins", TextOf("Message"));

            game.Simulation.Restart();
            yield return null;
            yield return null;

            Assert.AreEqual(string.Empty, TextOf("Message"));
            Assert.AreEqual("0", TextOf("Score1"));
        }
    }
}
