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

        [Header("Palette (from the spec)")]
        public Color baseColor = new Color32(0x05, 0x06, 0x0D, 0xFF);
        public Color gridColor = new Color32(0x12, 0x30, 0x5A, 0xFF);
        public Color player1Color = new Color32(0x22, 0xD3, 0xEE, 0xFF);
        public Color player2Color = new Color32(0xFF, 0x2E, 0x93, 0xFF);
        public Color ballColor = new Color32(0xFF, 0xE2, 0x7A, 0xFF);
        public Color textColor = new Color32(0xE6, 0xF0, 0xFF, 0xFF);

        [Header("Look: starting values, tuned on renders (see the plan)")]
        [Tooltip("Emission is the colour times this number. Brightest first: ball, paddles, rails, grid.")]
        public float ballEmission = 2f;
        public float paddleEmission = 1.6f;
        public float railEmission = 0.15f;
        public float gridEmission = 0.4f;
        public float bloomThreshold = 0.6f;
        public float bloomIntensity = 2.4f;
        [Range(0f, 1f)] public float bloomScatter = 0.7f;
        [Range(0f, 1f)] public float vignetteIntensity = 0.25f;
        [Tooltip("Degrees the camera is pitched down. 90 looks straight down.")]
        public float cameraPitch = 62f;
        public float cameraFieldOfView = 35f;
        [Tooltip("Metres from the table centre to the camera.")]
        public float cameraDistance = 18.3f;
        public int scoreFontSize = 120;
        [Tooltip("Pixels below the top of the screen (at 1920 x 1080), negative is down.")]
        public float scoreOffsetY = -80f;
        public float scoreOffsetX = 260f;
    }
}
