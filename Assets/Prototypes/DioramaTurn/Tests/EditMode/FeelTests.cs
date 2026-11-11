using NUnit.Framework;

namespace Prototypes.DioramaTurn.Tests
{
    public class FeelTests
    {
        [Test]
        public void Strength_zero_turns_off_every_feel_amount()
        {
            Assert.AreEqual(0f, Feel.OvershootDeg(4f, 0f));
            Assert.AreEqual(0f, Feel.Squash(0.18f, 0f));
            Assert.AreEqual(1f, Feel.SlowMoScale(0.4f, 0f));
            Assert.AreEqual(0, Feel.Particles(12, 0f));
        }

        [Test]
        public void Strength_one_keeps_the_tuned_values()
        {
            Assert.AreEqual(4f, Feel.OvershootDeg(4f, 1f), 0.0001f);
            Assert.AreEqual(0.18f, Feel.Squash(0.18f, 1f), 0.0001f);
            Assert.AreEqual(0.4f, Feel.SlowMoScale(0.4f, 1f), 0.0001f);
            Assert.AreEqual(12, Feel.Particles(12, 1f));
        }

        [Test]
        public void Strength_in_between_scales_linearly_and_rounds_particles()
        {
            Assert.AreEqual(2f, Feel.OvershootDeg(4f, 0.5f), 0.0001f);
            Assert.AreEqual(0.09f, Feel.Squash(0.18f, 0.5f), 0.0001f);
            Assert.AreEqual(6, Feel.Particles(12, 0.5f));
        }
    }
}
