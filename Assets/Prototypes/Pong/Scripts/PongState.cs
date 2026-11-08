using UnityEngine;

namespace Prototypes.Pong
{
    public enum PongPhase
    {
        Serving,
        Playing,
        GameOver,
    }

    /// <summary>
    /// The whole state of a Pong game. Positions are 2D: x along the table, y across it
    /// (the world z axis). Player 1 is on the left (x &lt; 0) and player 2 on the right.
    /// </summary>
    public sealed class PongState
    {
        public Vector2 BallPosition;
        public Vector2 BallVelocity;
        public float BallSpeed;
        public float Paddle1Z;
        public float Paddle2Z;
        public int Score1;
        public int Score2;
        public PongPhase Phase;
        public float ServeTimer;
        /// <summary>+1 serves toward player 2 (right), -1 toward player 1 (left).</summary>
        public float ServeSign;
        public float ServeAngleDegrees;
        /// <summary>0 while the game is on, otherwise 1 or 2.</summary>
        public int Winner;
    }
}
