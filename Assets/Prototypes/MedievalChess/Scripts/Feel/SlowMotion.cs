using UnityEngine;

namespace Prototypes.MedievalChess
{
    /// <summary>
    /// A stretch of slow motion for the checkmate. It scales the time the animation gets for a while of real time, then
    /// gives it back. At effect strength 0 it does nothing.
    /// </summary>
    public sealed class SlowMotion
    {
        float scale = 1f;
        float remaining;

        public bool Active => remaining > 1e-4f;

        public void Begin(float timeScale, float realSeconds, float effectStrength)
        {
            if (effectStrength <= 0f || realSeconds <= 0f)
            {
                remaining = 0f;
                return;
            }
            // a lower strength slows less: strength 0.5 turns a 0.3 scale into 0.65
            scale = Mathf.Lerp(1f, Mathf.Clamp(timeScale, 0.01f, 1f), Mathf.Clamp01(effectStrength));
            remaining = realSeconds;
        }

        public void Cancel() => remaining = 0f;

        /// <summary>The time the animation gets for a real frame time.</summary>
        public float Apply(float realDt)
        {
            if (remaining <= 1e-4f) return realDt;
            float slowPart = Mathf.Min(realDt, remaining);
            remaining -= slowPart;
            return slowPart * scale + (realDt - slowPart);
        }
    }

    /// <summary>A value that eases toward a target over a time: the camera's mood, the torches' level.</summary>
    public sealed class Ease1D
    {
        float start, target, duration = 1f, elapsed = 1f;

        public float Value { get; private set; }
        public float Target => target;

        public Ease1D(float value = 0f)
        {
            Value = start = target = value;
        }

        public void Snap(float value)
        {
            Value = start = target = value;
            elapsed = duration;
        }

        public void MoveTo(float newTarget, float seconds)
        {
            start = Value;
            target = newTarget;
            duration = Mathf.Max(0.0001f, seconds);
            elapsed = 0f;
        }

        public void Update(float dt)
        {
            if (elapsed >= duration) { Value = target; return; }
            elapsed = Mathf.Min(duration, elapsed + dt);
            float t = elapsed / duration;
            Value = Mathf.Lerp(start, target, t * t * (3f - 2f * t));
        }
    }
}
