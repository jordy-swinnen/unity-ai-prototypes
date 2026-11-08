using UnityEngine;

namespace Prototypes.Pong
{
    /// <summary>
    /// Every tuning value for Pong. The game rules read these and hold no numbers of their own.
    /// Distances are in metres. The play field is seen from above: x runs along the table and
    /// z across it. The rules call the across axis "y" because they use 2D vectors.
    /// </summary>
    [CreateAssetMenu(menuName = "Prototypes/Pong/Settings", fileName = "PongSettings")]
    public sealed class PongSettings : ScriptableObject
    {
        [Header("Table")]
        [Tooltip("Distance from the centre to each end line.")]
        public float fieldHalfLength = 8f;
        [Tooltip("Distance from the centre to the inner face of each side wall.")]
        public float fieldHalfWidth = 5f;

        [Header("Ball")]
        public float ballRadius = 0.25f;
        public float ballBaseSpeed = 8f;
        [Tooltip("Speed added on every paddle hit.")]
        public float ballSpeedStep = 0.75f;
        public float ballMaxSpeed = 20f;
        [Tooltip("Largest angle from straight back, in degrees, when the ball hits a paddle edge.")]
        public float maxBounceAngleDegrees = 60f;
        [Tooltip("Largest random angle of a serve, in degrees.")]
        public float maxServeAngleDegrees = 20f;
        [Tooltip("Pause before each serve, in seconds.")]
        public float serveDelay = 1f;

        [Header("Paddles")]
        [Tooltip("Distance from the centre to each paddle's middle.")]
        public float paddleX = 7.5f;
        public float paddleHalfThickness = 0.2f;
        public float paddleHalfLength = 1f;
        public float paddleSpeed = 10f;

        [Header("Match")]
        public int winScore = 5;

        /// <summary>Largest distance a paddle's middle can be from the table's centre line.</summary>
        public float PaddleLimit => fieldHalfWidth - paddleHalfLength;

        /// <summary>Ball-centre x position when the ball touches a paddle's front face.</summary>
        public float PaddleContactX => paddleX - paddleHalfThickness - ballRadius;
    }
}
