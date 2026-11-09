using UnityEngine;
using UnityEngine.UI;

namespace Prototypes.NeonPong
{
    /// <summary>Copies the game state onto the scene objects and the score text. It holds no rules.</summary>
    public class NeonPongView : MonoBehaviour
    {
        public NeonPongGame game;
        public Transform leftPaddle;
        public Transform rightPaddle;
        public Transform ball;
        public Text scoreLeft;
        public Text scoreRight;
        public Text message;

        void LateUpdate()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (game == null || game.Simulation == null) return;
            var s = game.settings;
            var state = game.Simulation.State;

            // The simulation's y is the table width, which is world z.
            leftPaddle.position = new Vector3(-s.paddleX, 0f, state.GetPaddleZ(Side.Left));
            rightPaddle.position = new Vector3(s.paddleX, 0f, state.GetPaddleZ(Side.Right));
            ball.position = new Vector3(state.BallPos.x, s.ballRadius, state.BallPos.y);

            scoreLeft.text = state.GetScore(Side.Left).ToString();
            scoreRight.text = state.GetScore(Side.Right).ToString();
            message.text = state.Phase == Phase.GameOver
                ? $"PLAYER {(state.Winner == Side.Left ? 1 : 2)} WINS\nPRESS SPACE"
                : string.Empty;
        }
    }
}
