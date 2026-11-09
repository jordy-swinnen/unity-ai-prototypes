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
        [Tooltip("Emission is the colour (in linear light) times this number. Brightest first: ball, paddles, grid, rails.")]
        public float ballEmission = 1.6f;
        public float paddleEmission = 1.1f;
        public float railEmission = 0.06f;
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

        [Header("Feel: one master strength, then each effect")]
        [Tooltip("0 turns off shake, hit-stop, flashes, pops, the wash and the push-in. 1 is full. Sound has its own volume.")]
        [Range(0f, 1f)] public float effectStrength = 1f;
        [Tooltip("Camera shake, in metres of camera offset.")]
        public float wallShake = 0.04f;
        public float paddleShake = 0.12f;
        public float scoreShake = 0.3f;
        public float shakeDuration = 0.3f;
        [Tooltip("Seconds the rules pause on a hit or a point. The camera and effects keep running.")]
        public float hitStopPaddle = 0.04f;
        public float hitStopScore = 0.08f;
        [Tooltip("Seconds a flash takes to fade. Flash adds this many times the emission.")]
        public float flashDecay = 0.2f;
        public float flashBoost = 3f;
        [Tooltip("The ball grows by this fraction on a paddle hit, and settles back.")]
        public float ballPop = 0.35f;
        public float popDecay = 0.15f;
        public float scorePop = 0.5f;
        [Tooltip("A paddle stretches by this fraction at full speed, and leans by this many degrees.")]
        public float paddleStretch = 0.1f;
        public float paddleLean = 5f;
        [Header("Feel: the win")]
        public float winSlowScale = 0.3f;
        public float winSlowDuration = 1.2f;
        [Tooltip("Small numbers: the project blends in linear light, so 0.04 already shows as about a fifth.")]
        [Range(0f, 1f)] public float winWashAlpha = 0.04f;
        [Tooltip("The camera moves this fraction of its distance toward the table.")]
        public float winPushIn = 0.12f;
        [Tooltip("Seconds of slow motion time the wash and push-in take to finish.")]
        public float winTime = 1.5f;

        [Header("Effects")]
        public int wallBurst = 6;
        public int paddleBurst = 14;
        public int pointBurst = 40;
        public int winBurst = 90;
        public float burstSize = 0.14f;
        public float burstLifetime = 0.5f;
        public float burstSpeed = 4f;
        public float burstEmission = 3f;
        public float trailTimeMin = 0.06f;
        public float trailTimeMax = 0.3f;
        public float trailWidth = 0.4f;
        public float trailEmission = 1.5f;
        public float lightIntensity = 3f;
        public float lightRange = 5f;

        [Header("Audio")]
        [Range(0f, 1f)] public float masterVolume = 0.6f;
        [Tooltip("The paddle sound is played at this pitch at base speed, and the higher one at max speed.")]
        public float pitchMin = 0.85f;
        public float pitchMax = 1.6f;
    }
}
