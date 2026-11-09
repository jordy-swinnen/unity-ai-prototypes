using UnityEngine;

namespace Shared.Feel
{
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
}
