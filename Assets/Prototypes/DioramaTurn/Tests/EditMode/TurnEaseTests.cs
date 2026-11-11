using NUnit.Framework;
using UnityEngine;

namespace Prototypes.DioramaTurn.Tests
{
    public class TurnEaseTests
    {
        const float Tol = 0.0005f;

        [Test]
        public void Starts_at_zero_and_ends_exactly_at_one_for_any_overshoot()
        {
            foreach (float s in new[] { 0f, 0.02f, 4f / 90f, 0.2f })
            {
                Assert.AreEqual(0f, TurnEase.BackOut(0f, s), Tol, $"overshoot {s}");
                Assert.AreEqual(1f, TurnEase.BackOut(1f, s), Tol, $"overshoot {s}");
            }
        }

        [Test]
        public void The_peak_is_the_overshoot_fraction_above_one()
        {
            foreach (float s in new[] { 0.02f, 4f / 90f, 0.1f })
            {
                float peak = 0f;
                for (int i = 0; i <= 200; i++) peak = Mathf.Max(peak, TurnEase.BackOut(i / 200f, s));
                Assert.AreEqual(1f + s, peak, 0.01f * s + 0.001f, $"overshoot {s}");
            }
        }

        [Test]
        public void Zero_overshoot_never_goes_past_one()
        {
            for (int i = 0; i <= 200; i++) Assert.LessOrEqual(TurnEase.BackOut(i / 200f, 0f), 1f + Tol);
        }

        [Test]
        public void Progress_outside_zero_to_one_is_clamped()
        {
            Assert.AreEqual(0f, TurnEase.BackOut(-0.5f, 0.05f), Tol);
            Assert.AreEqual(1f, TurnEase.BackOut(1.5f, 0.05f), Tol);
        }

        [Test]
        public void The_curve_rises_to_its_peak_without_falling_back_before_it()
        {
            float s = 4f / 90f;
            float peak = 0f;
            int peakAt = 0;
            for (int i = 0; i <= 200; i++)
            {
                float v = TurnEase.BackOut(i / 200f, s);
                if (v > peak) { peak = v; peakAt = i; }
            }
            Assert.Greater(peakAt, 200 * 0.3f, "the peak sits late in the turn, not at the start");
        }
    }
}
