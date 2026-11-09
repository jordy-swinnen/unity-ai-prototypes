using UnityEngine;
using UnityEngine.UI;

namespace Prototypes.NeonPong
{
    /// <summary>
    /// Copies the game state and the feel state onto the scene objects, the score text and the
    /// glow. It holds no rules. All strengths come from the settings asset, and the global effect
    /// strength switches the feedback off.
    /// </summary>
    public class NeonPongView : MonoBehaviour
    {
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        public NeonPongGame game;
        public Transform leftPaddle;
        public Transform rightPaddle;
        public Transform ball;
        public Transform[] rails;
        public Text scoreLeft;
        public Text scoreRight;
        public Text message;
        public Image wash;
        public TrailRenderer trail;
        public Light ballLight;

        sealed class Glow
        {
            public Renderer[] renderers;
            public Color[] baseEmission;
            public float last = -1f;
            public MaterialPropertyBlock block = new MaterialPropertyBlock();

            public Glow(Transform root)
            {
                renderers = root.GetComponentsInChildren<MeshRenderer>();
                baseEmission = new Color[renderers.Length];
                for (int i = 0; i < renderers.Length; i++)
                    baseEmission[i] = renderers[i].sharedMaterial.GetColor(EmissionColor);
            }

            /// <summary>Sets the emission to the base colour times a multiplier, only when it changed.</summary>
            public void Set(float multiplier)
            {
                if (Mathf.Approximately(multiplier, last)) return;
                last = multiplier;
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].GetPropertyBlock(block);
                    // GetColor returns the value the shader sees (linear), and SetColor converts again, so go back with .gamma.
                    block.SetColor(EmissionColor, (baseEmission[i] * multiplier).gamma);
                    renderers[i].SetPropertyBlock(block);
                }
            }
        }

        Glow leftGlow, rightGlow;
        Glow[] railGlow;
        Vector3 ballScale = Vector3.one;
        Vector3 leftPaddleScale = Vector3.one, rightPaddleScale = Vector3.one;
        Vector3 scoreScale = Vector3.one;
        int shownLeft = -1, shownRight = -1;
        bool shownOver;
        Side shownWinner;

        void Awake()
        {
            leftGlow = new Glow(leftPaddle);
            rightGlow = new Glow(rightPaddle);
            railGlow = new Glow[rails.Length];
            for (int i = 0; i < rails.Length; i++) railGlow[i] = new Glow(rails[i]);
            ballScale = ball.localScale;
            leftPaddleScale = leftPaddle.localScale;
            rightPaddleScale = rightPaddle.localScale;
            scoreScale = scoreLeft.transform.localScale;
        }

        void OnEnable()
        {
            if (game != null) game.EventRaised += OnGameEvent;
        }

        void OnDisable()
        {
            if (game != null) game.EventRaised -= OnGameEvent;
        }

        void OnGameEvent(GameEvent e)
        {
            // The ball jumps back to the centre on a serve, a point and a restart.
            if (trail != null && (e.Kind == EventKind.Serve || e.Kind == EventKind.Point || e.Kind == EventKind.Restart))
                trail.Clear();
        }

        void LateUpdate()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (game == null || game.Simulation == null || game.Feel == null) return;
            var s = game.settings;
            var state = game.Simulation.State;
            var feel = game.Feel;
            float k = feel.Strength;

            // The simulation's y is the table width, which is world z.
            leftPaddle.position = new Vector3(-s.paddleX, 0f, state.GetPaddleZ(Side.Left));
            rightPaddle.position = new Vector3(s.paddleX, 0f, state.GetPaddleZ(Side.Right));
            ball.position = new Vector3(state.BallPos.x, s.ballRadius, state.BallPos.y);

            // Paddles stretch and lean a little in the direction they move.
            Move(leftPaddle, leftPaddleScale, state.GetPaddleVelocity(Side.Left) / s.paddleSpeed, s, k);
            Move(rightPaddle, rightPaddleScale, state.GetPaddleVelocity(Side.Right) / s.paddleSpeed, s, k);

            // Flashes: emission goes up for an instant, then settles.
            leftGlow.Set(1f + feel.PaddleFlash[(int)Side.Left] * s.flashBoost);
            rightGlow.Set(1f + feel.PaddleFlash[(int)Side.Right] * s.flashBoost);
            foreach (var rail in railGlow) rail.Set(1f + feel.RailFlash * s.flashBoost * 2f);

            ball.localScale = ballScale * (1f + feel.BallPop * s.ballPop);

            // The trail and the light grow with the rally speed.
            float fast = Mathf.InverseLerp(s.baseSpeed, s.maxSpeed, state.BallSpeed);
            if (trail != null)
            {
                trail.emitting = state.Phase == Phase.Playing;
                trail.time = Mathf.Lerp(s.trailTimeMin, s.trailTimeMax, fast);
                trail.widthMultiplier = s.trailWidth * Mathf.Lerp(s.trailWidthAtBaseSpeed, 1f, fast);
            }
            if (ballLight != null)
            {
                ballLight.intensity = s.lightIntensity * Mathf.Lerp(s.lightAtBaseSpeed, s.lightAtMaxSpeed, fast);
                ballLight.range = s.lightRange;
            }

            // Only build new strings when something changed.
            int left = state.GetScore(Side.Left), right = state.GetScore(Side.Right);
            if (left != shownLeft) { shownLeft = left; scoreLeft.text = left.ToString(); }
            if (right != shownRight) { shownRight = right; scoreRight.text = right.ToString(); }
            scoreLeft.transform.localScale = scoreScale * (1f + feel.ScorePop[(int)Side.Left] * s.scorePop);
            scoreRight.transform.localScale = scoreScale * (1f + feel.ScorePop[(int)Side.Right] * s.scorePop);

            bool over = state.Phase == Phase.GameOver;
            if (over != shownOver || state.Winner != shownWinner)
            {
                shownOver = over;
                shownWinner = state.Winner;
                message.text = over
                    ? $"PLAYER {(state.Winner == Side.Left ? 1 : 2)} WINS\nPRESS SPACE"
                    : string.Empty;
            }
            // The message fades in with the win. With effects off it is simply there.
            var messageColor = s.textColor;
            messageColor.a = k > 0f ? Mathf.Clamp01(feel.WinProgress * 3f) : 1f;
            message.color = messageColor;

            if (wash != null)
            {
                var c = feel.WinSide == Side.Left ? s.player1Color : s.player2Color;
                c.a = feel.WashAlpha;
                wash.color = c;
            }
        }

        static void Move(Transform paddle, Vector3 baseScale, float velocityFraction, NeonPongSettings s, float k)
        {
            var scale = baseScale;
            scale.z *= 1f + Mathf.Abs(velocityFraction) * s.paddleStretch * k;
            paddle.localScale = scale;
            paddle.rotation = Quaternion.Euler(0f, -velocityFraction * s.paddleLean * k, 0f);
        }
    }
}
