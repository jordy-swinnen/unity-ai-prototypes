using UnityEngine;
using UnityEngine.UI;

namespace Prototypes.Pong
{
    /// <summary>
    /// Copies the game state onto the scene: the ball, both paddles and the score text.
    /// It changes only x and z, so the heights set when the scene was built stay as they are.
    /// </summary>
    public sealed class PongView : MonoBehaviour
    {
        [SerializeField] PongGame game;
        [SerializeField] Transform ball;
        [SerializeField] Transform paddle1;
        [SerializeField] Transform paddle2;
        [SerializeField] Text score1Text;
        [SerializeField] Text score2Text;
        [SerializeField] Text messageText;

        void LateUpdate()
        {
            if (game == null || game.Simulation == null)
            {
                return;
            }

            PongState state = game.Simulation.State;
            float paddleX = game.Settings.paddleX;

            Place(ball, state.BallPosition.x, state.BallPosition.y);
            Place(paddle1, -paddleX, state.Paddle1Z);
            Place(paddle2, paddleX, state.Paddle2Z);

            score1Text.text = state.Score1.ToString();
            score2Text.text = state.Score2.ToString();
            messageText.text = state.Phase == PongPhase.GameOver
                ? $"Player {state.Winner} wins!\nPress Space for a new game"
                : string.Empty;
        }

        static void Place(Transform target, float x, float z)
        {
            Vector3 position = target.position;
            target.position = new Vector3(x, position.y, z);
        }
    }
}
