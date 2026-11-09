using Shared.Feel;
using UnityEngine;

namespace Prototypes.NeonPong
{

    /// <summary>
    /// Turns game events into feedback values: shake, hit-stop, flashes, pops and the win's
    /// wash and push-in. Plain C#, so tests can drive it without a scene. The views read it.
    /// The global effect strength scales or switches off everything here.
    /// </summary>
    public sealed class FeelState
    {
        readonly NeonPongSettings s;

        public ShakeState Shake { get; } = new ShakeState();
        public TimeFeel Time { get; } = new TimeFeel();
        public readonly float[] PaddleFlash = new float[2];
        public readonly float[] ScorePop = new float[2];
        public float RailFlash { get; private set; }
        public float BallPop { get; private set; }
        public bool WinActive { get; private set; }
        public Side WinSide { get; private set; }
        /// <summary>0 to 1 over the win time, in slow motion time.</summary>
        public float WinProgress { get; private set; }

        public float Strength => Mathf.Clamp01(s.effectStrength);
        public float WashAlpha => Strength * s.winWashAlpha * Smooth(WinProgress * 2f);
        public float PushIn => Strength * s.winPushIn * Smooth(WinProgress);

        public FeelState(NeonPongSettings settings)
        {
            s = settings;
        }

        static float Smooth(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }

        public void OnEvent(GameEvent e)
        {
            if (e.Kind == EventKind.Restart)
            {
                WinActive = false;
                WinProgress = 0f;
                Time.Clear();
                return;
            }
            if (Strength <= 0f) return;

            float k = Strength;
            switch (e.Kind)
            {
                case EventKind.WallHit:
                    RailFlash = 1f;
                    Shake.Add(s.wallShake * k, new Vector2(0f, Mathf.Sign(e.Position.y)), s.wallShakeDuration, s.shakeFrequency);
                    break;

                case EventKind.PaddleHit:
                {
                    float fast = Mathf.InverseLerp(s.baseSpeed, s.maxSpeed, e.Speed);
                    float away = e.Side == Side.Right ? -1f : 1f; // the ball leaves toward this side of x
                    PaddleFlash[(int)e.Side] = 1f;
                    BallPop = 1f;
                    Shake.Add(s.paddleShake * Mathf.Lerp(s.shakeAtBaseSpeed, s.shakeAtMaxSpeed, fast) * k, new Vector2(away, 0f), s.shakeDuration, s.shakeFrequency);
                    Time.HitStop(s.hitStopPaddle * k);
                    break;
                }

                case EventKind.Point:
                    RailFlash = 1f;
                    ScorePop[(int)e.Side] = 1f;
                    Shake.Add(s.scoreShake * k, new Vector2(e.Side == Side.Left ? -1f : 1f, 0f), s.scoreShakeDuration, s.shakeFrequency);
                    Time.HitStop(s.hitStopScore * k);
                    break;

                case EventKind.Win:
                    WinActive = true;
                    WinSide = e.Side;
                    WinProgress = 0f;
                    Time.SlowMotion(s.winSlowScale, s.winSlowDuration);
                    break;
            }
        }

        public void Update(float dt)
        {
            Time.UpdateSlow(dt);
            Shake.Update(dt);
            float flashStep = dt / Mathf.Max(s.flashDecay, 1e-3f);
            PaddleFlash[0] = Mathf.MoveTowards(PaddleFlash[0], 0f, flashStep);
            PaddleFlash[1] = Mathf.MoveTowards(PaddleFlash[1], 0f, flashStep);
            RailFlash = Mathf.MoveTowards(RailFlash, 0f, flashStep);

            BallPop = Mathf.MoveTowards(BallPop, 0f, dt / Mathf.Max(s.popDecay, 1e-3f));
            float scoreStep = dt / Mathf.Max(s.scorePopDecay, 1e-3f);
            ScorePop[0] = Mathf.MoveTowards(ScorePop[0], 0f, scoreStep);
            ScorePop[1] = Mathf.MoveTowards(ScorePop[1], 0f, scoreStep);

            if (WinActive && Strength > 0f)
                WinProgress = Mathf.MoveTowards(WinProgress, 1f, dt * Time.VisualScale / Mathf.Max(s.winTime, 1e-3f));
        }
    }
}
