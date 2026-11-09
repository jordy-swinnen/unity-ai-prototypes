using UnityEngine;

namespace Prototypes.NeonPong
{
    /// <summary>
    /// A camera shake that decays to zero. Add it a strength, a direction and a duration. The
    /// offset swings along the direction and shrinks smoothly. A stronger shake replaces a weaker
    /// one, and a weaker one never cuts a stronger one short.
    /// </summary>
    public sealed class ShakeState
    {
        float peak;
        float duration = 0.3f;
        float elapsed;
        Vector2 direction = Vector2.up;

        public Vector2 Offset { get; private set; }
        public float Amplitude { get; private set; }

        public void Add(float strength, Vector2 dir, float seconds)
        {
            if (strength <= 0f || seconds <= 0f) return;
            if (strength < Amplitude) return;
            peak = strength;
            duration = seconds;
            elapsed = 0f;
            direction = dir.sqrMagnitude > 1e-6f ? dir.normalized : Vector2.up;
            Amplitude = strength;
        }

        public void Update(float dt)
        {
            if (Amplitude <= 0f) return;
            elapsed += dt;
            if (elapsed >= duration)
            {
                Amplitude = 0f;
                Offset = Vector2.zero;
                return;
            }
            float k = 1f - elapsed / duration;
            Amplitude = peak * k * k;
            Offset = direction * (Amplitude * Mathf.Cos(elapsed * 90f));
        }
    }

    /// <summary>
    /// Hit-stop (the rules pause, the rest keeps running) and the visual slow motion for the win.
    /// </summary>
    public sealed class TimeFeel
    {
        float hitStopLeft;
        float slowLeft;
        float slowScale = 1f;

        public bool HitStopActive => hitStopLeft > 0f;
        /// <summary>1 normally, below 1 during the win's slow motion. Effects use it, the rules don't.</summary>
        public float VisualScale => slowLeft > 0f ? slowScale : 1f;

        public void HitStop(float seconds)
        {
            if (seconds > 0f) hitStopLeft = Mathf.Max(hitStopLeft, seconds);
        }

        public void SlowMotion(float scale, float seconds)
        {
            if (seconds <= 0f) return;
            slowScale = Mathf.Clamp(scale, 0.05f, 1f);
            slowLeft = seconds;
        }

        public void Clear()
        {
            hitStopLeft = 0f;
            slowLeft = 0f;
        }

        /// <summary>Counts the slow motion down in real time.</summary>
        public void UpdateSlow(float realDt)
        {
            if (slowLeft > 0f) slowLeft -= realDt;
        }

        /// <summary>Returns how much time the rules may run for this frame, and counts the hit-stop down.</summary>
        public float Advance(float realDt)
        {
            if (hitStopLeft > 0f)
            {
                hitStopLeft -= realDt;
                return 0f;
            }
            return realDt;
        }
    }

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
                    Shake.Add(s.wallShake * k, new Vector2(0f, Mathf.Sign(e.Position.y)), s.shakeDuration * 0.6f);
                    break;

                case EventKind.PaddleHit:
                {
                    float fast = Mathf.InverseLerp(s.baseSpeed, s.maxSpeed, e.Speed);
                    float away = e.Side == Side.Right ? -1f : 1f; // the ball leaves toward this side of x
                    PaddleFlash[(int)e.Side] = 1f;
                    BallPop = 1f;
                    Shake.Add(s.paddleShake * Mathf.Lerp(0.6f, 1.4f, fast) * k, new Vector2(away, 0f), s.shakeDuration);
                    Time.HitStop(s.hitStopPaddle * k);
                    break;
                }

                case EventKind.Point:
                    RailFlash = 1f;
                    ScorePop[(int)e.Side] = 1f;
                    Shake.Add(s.scoreShake * k, new Vector2(e.Side == Side.Left ? -1f : 1f, 0f), s.shakeDuration * 1.3f);
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

            float popStep = dt / Mathf.Max(s.popDecay, 1e-3f);
            BallPop = Mathf.MoveTowards(BallPop, 0f, popStep);
            ScorePop[0] = Mathf.MoveTowards(ScorePop[0], 0f, popStep * 0.6f);
            ScorePop[1] = Mathf.MoveTowards(ScorePop[1], 0f, popStep * 0.6f);

            if (WinActive && Strength > 0f)
                WinProgress = Mathf.MoveTowards(WinProgress, 1f, dt * Time.VisualScale / Mathf.Max(s.winTime, 1e-3f));
        }
    }
}
