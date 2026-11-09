using UnityEngine;

namespace Prototypes.NeonPong
{
    /// <summary>
    /// Every tuning value for Neon Pong. Look and feel values are added in stage 3.
    /// Units are metres and seconds. The table top is y = 0. The simulation plays on x (length)
    /// and z (width).
    /// </summary>
    [CreateAssetMenu(menuName = "Prototypes/Neon Pong/Settings", fileName = "NeonPongSettings")]
    public class NeonPongSettings : ScriptableObject
    {
        [Header("Table")]
        public float halfLength = 8f;
        public float halfWidth = 5f;
        public float ballRadius = 0.25f;
        public float paddleHalfLength = 1f;
        public float paddleHalfThickness = 0.2f;
        public float paddleX = 7.5f;

        [Header("Rules")]
        public float baseSpeed = 9f;
        public float speedStep = 0.8f;
        public float maxSpeed = 22f;
        [Tooltip("Degrees. A hit on the very end of a paddle returns the ball at this angle.")]
        public float maxBounceAngle = 60f;
        [Tooltip("Degrees. The serve leaves the centre within this angle of the x axis.")]
        public float maxServeAngle = 20f;
        public float serveDelay = 1f;
        public float paddleSpeed = 10f;
        [Tooltip("Seconds a paddle takes to reach full speed or stop. 0 means instant.")]
        public float paddleEaseTime = 0.08f;
        public int winScore = 5;
        [Tooltip("The largest time slice the rules take in one go. Longer steps are split.")]
        public float maxSubstep = 1f / 120f;
    }
}
