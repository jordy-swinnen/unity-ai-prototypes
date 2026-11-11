using UnityEngine;

namespace Prototypes.DioramaTurn
{
    /// <summary>The shape of a quarter turn: fast out, a small overshoot, then settle. Pure maths, so tests need no scene.</summary>
    public static class TurnEase
    {
        /// <summary>
        /// 0 at t = 0, exactly 1 at t = 1, and 1 + overshootFraction at the peak in between.
        /// f(t) = 1 + A*u^3 + B*u^2 with u = t - 1 and A = 1 + B. f(0) = 0 and f(1) = 1 hold for any B,
        /// and the peak sits 4*B^3 / (27*(1+B)^2) above 1, so B is solved for the wanted overshoot.
        /// </summary>
        public static float BackOut(float t, float overshootFraction)
        {
            t = Mathf.Clamp01(t);
            float b = SolveB(Mathf.Clamp(overshootFraction, 0f, 0.5f));
            float a = 1f + b;
            float u = t - 1f;
            return 1f + a * u * u * u + b * u * u;
        }

        static float SolveB(float overshoot)
        {
            if (overshoot <= 1e-5f) return 0f;
            float target = 27f * overshoot / 4f;
            float lo = 0f, hi = 1f;
            while (Bump(hi) < target && hi < 64f) hi *= 2f;
            for (int i = 0; i < 40; i++)
            {
                float mid = (lo + hi) * 0.5f;
                if (Bump(mid) < target) lo = mid;
                else hi = mid;
            }
            return (lo + hi) * 0.5f;
        }

        static float Bump(float b) => b * b * b / ((1f + b) * (1f + b));
    }
}
