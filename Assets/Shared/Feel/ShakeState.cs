using UnityEngine;

namespace Shared.Feel
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
        float frequency = 90f;

        public Vector2 Offset { get; private set; }
        public float Amplitude { get; private set; }

        public void Add(float strength, Vector2 dir, float seconds, float frequency = 90f)
        {
            if (strength <= 0f || seconds <= 0f) return;
            if (strength < Amplitude) return;
            peak = strength;
            duration = seconds;
            this.frequency = frequency;
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
            Offset = direction * (Amplitude * Mathf.Cos(elapsed * frequency));
        }
    }
}
