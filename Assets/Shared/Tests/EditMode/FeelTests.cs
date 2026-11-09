using NUnit.Framework;
using Shared.Feel;
using UnityEngine;

namespace Shared.Tests
{
    public class ShakeStateTests
    {
        const float Tol = 0.0001f;

        [Test]
        public void Shake_decays_to_zero()
        {
            var shake = new ShakeState();
            shake.Add(1f, Vector2.right, 0.3f);
            shake.Update(0.05f);
            Assert.Greater(shake.Amplitude, 0f);
            for (int i = 0; i < 20; i++) shake.Update(0.05f);
            Assert.AreEqual(0f, shake.Amplitude);
            Assert.AreEqual(Vector2.zero, shake.Offset);
        }

        [Test]
        public void Shake_offset_never_exceeds_the_strength_and_follows_its_direction()
        {
            var shake = new ShakeState();
            shake.Add(0.5f, Vector2.right, 0.4f);
            for (int i = 0; i < 40; i++)
            {
                shake.Update(0.01f);
                Assert.LessOrEqual(shake.Offset.magnitude, 0.5f + Tol);
                Assert.AreEqual(0f, shake.Offset.y, Tol);
            }
        }

        [Test]
        public void A_stronger_shake_replaces_a_weaker_one_and_a_weaker_one_does_not_cut_a_stronger()
        {
            var shake = new ShakeState();
            shake.Add(0.2f, Vector2.up, 0.3f);
            shake.Add(0.8f, Vector2.up, 0.3f);
            Assert.AreEqual(0.8f, shake.Amplitude, Tol);
            shake.Add(0.1f, Vector2.up, 0.3f);
            Assert.AreEqual(0.8f, shake.Amplitude, Tol);
        }
    }

    public class TimeFeelTests
    {
        const float Tol = 0.0001f;

        [Test]
        public void Hit_stop_pauses_the_rules_and_then_ends()
        {
            var time = new TimeFeel();
            time.HitStop(0.04f);
            Assert.IsTrue(time.HitStopActive);
            Assert.AreEqual(0f, time.Advance(0.016f));
            Assert.AreEqual(0f, time.Advance(0.016f));
            Assert.AreEqual(0f, time.Advance(0.016f)); // 0.048 s of stop used up
            Assert.IsFalse(time.HitStopActive);
            Assert.AreEqual(0.016f, time.Advance(0.016f), Tol);
        }

        [Test]
        public void Slow_motion_scales_visual_time_and_ends()
        {
            var time = new TimeFeel();
            Assert.AreEqual(1f, time.VisualScale);
            time.SlowMotion(0.3f, 0.5f);
            Assert.AreEqual(0.3f, time.VisualScale, Tol);
            for (int i = 0; i < 40; i++) time.UpdateSlow(0.02f);
            Assert.AreEqual(1f, time.VisualScale, Tol);
        }
    }
}
