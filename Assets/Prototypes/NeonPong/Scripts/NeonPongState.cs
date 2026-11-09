using UnityEngine;

namespace Prototypes.NeonPong
{
    /// <summary>
    /// Everything that changes during a match. Plain data: the rules write it, the view reads it.
    /// Vector2 x is the table length, y is the table width (world z).
    /// </summary>
    public class NeonPongState
    {
        public Phase Phase = Phase.Serving;
        public Vector2 BallPos;
        public Vector2 BallVel;
        /// <summary>The current rally speed. It rises on paddle hits and resets after a point.</summary>
        public float BallSpeed;
        public int RallyHits;
        public float ServeTimer;
        /// <summary>The side the next serve goes toward.</summary>
        public Side ServeToward;
        public Side Winner;

        public readonly int[] Score = new int[2];
        public readonly float[] PaddleZ = new float[2];
        public readonly float[] PaddleVelocity = new float[2];

        public int GetScore(Side side) => Score[(int)side];
        public float GetPaddleZ(Side side) => PaddleZ[(int)side];
        public void SetPaddleZ(Side side, float z) => PaddleZ[(int)side] = z;
        public float GetPaddleVelocity(Side side) => PaddleVelocity[(int)side];
    }
}
