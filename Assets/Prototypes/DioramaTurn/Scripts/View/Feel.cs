using UnityEngine;

namespace Prototypes.DioramaTurn
{
    /// <summary>The feel amounts as pure functions, so a test can prove effectStrength 0 turns them all off.</summary>
    public static class Feel
    {
        public static float OvershootDeg(float degrees, float strength) => degrees * Mathf.Clamp01(strength);

        public static float Squash(float amount, float strength) => amount * Mathf.Clamp01(strength);

        public static float SlowMoScale(float scale, float strength) => strength <= 0f ? 1f : scale;

        public static int Particles(int count, float strength) => Mathf.RoundToInt(count * Mathf.Clamp01(strength));
    }
}
