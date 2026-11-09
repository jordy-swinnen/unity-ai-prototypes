using NUnit.Framework;
using UnityEngine;

namespace Prototypes.NeonPong.Tests
{
    public class NeonPongFeelTests
    {
        const float Tol = 0.0001f;

        NeonPongSettings settings;
        FeelState feel;

        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<NeonPongSettings>();
            feel = new FeelState(settings);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(settings);
        }

        GameEvent PaddleHit(float speed, Side side = Side.Right) =>
            new GameEvent(EventKind.PaddleHit, side, new Vector2(7.3f, 0f), speed);

        GameEvent Of(EventKind kind, Side side = Side.Left, float speed = 9f) =>
            new GameEvent(kind, side, Vector2.zero, speed);

        // --- shake ---

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

        // --- hit-stop and slow motion ---

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

        // --- events turn into feedback ---

        [Test]
        public void A_paddle_hit_shakes_stops_time_flashes_the_paddle_and_pops_the_ball()
        {
            feel.OnEvent(PaddleHit(15f));
            Assert.Greater(feel.Shake.Amplitude, 0f);
            Assert.IsTrue(feel.Time.HitStopActive);
            Assert.AreEqual(1f, feel.PaddleFlash[(int)Side.Right], Tol);
            Assert.AreEqual(0f, feel.PaddleFlash[(int)Side.Left], Tol);
            Assert.AreEqual(1f, feel.BallPop, Tol);
        }

        [Test]
        public void A_paddle_hit_shakes_away_from_the_paddle()
        {
            // The right paddle sends the ball left, so the shake runs along x.
            feel.OnEvent(PaddleHit(15f, Side.Right));
            feel.Update(0.001f);
            Assert.AreEqual(0f, feel.Shake.Offset.y, Tol);
            Assert.AreNotEqual(0f, feel.Shake.Offset.x);
        }

        [Test]
        public void A_faster_hit_shakes_harder()
        {
            var slow = new FeelState(settings);
            var fast = new FeelState(settings);
            slow.OnEvent(PaddleHit(settings.baseSpeed));
            fast.OnEvent(PaddleHit(settings.maxSpeed));
            Assert.Greater(fast.Shake.Amplitude, slow.Shake.Amplitude);
        }

        [Test]
        public void A_wall_hit_flashes_the_rails_and_shakes_a_little_without_hit_stop()
        {
            feel.OnEvent(Of(EventKind.WallHit));
            Assert.AreEqual(1f, feel.RailFlash, Tol);
            Assert.Greater(feel.Shake.Amplitude, 0f);
            Assert.Less(feel.Shake.Amplitude, settings.paddleShake);
            Assert.IsFalse(feel.Time.HitStopActive);
        }

        [Test]
        public void A_point_shakes_harder_than_a_hit_stops_longer_and_pops_the_scorers_number()
        {
            feel.OnEvent(Of(EventKind.Point, Side.Left));
            Assert.AreEqual(settings.scoreShake, feel.Shake.Amplitude, Tol);
            Assert.Greater(settings.scoreShake, settings.paddleShake);
            Assert.IsTrue(feel.Time.HitStopActive);
            Assert.AreEqual(1f, feel.ScorePop[(int)Side.Left], Tol);
            Assert.AreEqual(0f, feel.ScorePop[(int)Side.Right], Tol);
            Assert.AreEqual(1f, feel.RailFlash, Tol);
        }

        [Test]
        public void A_win_starts_the_wash_the_push_in_and_slow_motion_and_a_restart_clears_them()
        {
            feel.OnEvent(Of(EventKind.Win, Side.Right));
            Assert.IsTrue(feel.WinActive);
            Assert.AreEqual(Side.Right, feel.WinSide);
            Assert.Less(feel.Time.VisualScale, 1f);

            for (int i = 0; i < 400; i++) feel.Update(0.01f);
            Assert.AreEqual(1f, feel.WinProgress, Tol);

            feel.OnEvent(Of(EventKind.Restart));
            Assert.IsFalse(feel.WinActive);
            Assert.AreEqual(0f, feel.WinProgress, Tol);
            Assert.AreEqual(1f, feel.Time.VisualScale, Tol);
        }

        [Test]
        public void Flashes_pops_and_shake_all_die_out()
        {
            feel.OnEvent(PaddleHit(15f));
            feel.OnEvent(Of(EventKind.Point, Side.Left));
            feel.OnEvent(Of(EventKind.WallHit));
            for (int i = 0; i < 200; i++) feel.Update(0.01f);
            Assert.AreEqual(0f, feel.PaddleFlash[0]);
            Assert.AreEqual(0f, feel.PaddleFlash[1]);
            Assert.AreEqual(0f, feel.RailFlash);
            Assert.AreEqual(0f, feel.BallPop);
            Assert.AreEqual(0f, feel.ScorePop[0]);
            Assert.AreEqual(0f, feel.Shake.Amplitude);
            Assert.AreEqual(Vector2.zero, feel.Shake.Offset);
        }

        // --- the global strength ---

        [Test]
        public void Effect_strength_scales_the_shake()
        {
            var full = new FeelState(settings);
            full.OnEvent(Of(EventKind.Point, Side.Left));
            settings.effectStrength = 0.5f;
            var half = new FeelState(settings);
            half.OnEvent(Of(EventKind.Point, Side.Left));
            Assert.AreEqual(full.Shake.Amplitude * 0.5f, half.Shake.Amplitude, Tol);
        }

        [Test]
        public void Effect_strength_zero_turns_off_every_visual_effect()
        {
            settings.effectStrength = 0f;
            var off = new FeelState(settings);
            off.OnEvent(PaddleHit(20f));
            off.OnEvent(Of(EventKind.WallHit));
            off.OnEvent(Of(EventKind.Point, Side.Left));
            off.OnEvent(Of(EventKind.Win, Side.Left));
            off.Update(0.1f);

            Assert.AreEqual(0f, off.Shake.Amplitude);
            Assert.AreEqual(Vector2.zero, off.Shake.Offset);
            Assert.IsFalse(off.Time.HitStopActive);
            Assert.AreEqual(1f, off.Time.VisualScale);
            Assert.AreEqual(0f, off.PaddleFlash[0]);
            Assert.AreEqual(0f, off.PaddleFlash[1]);
            Assert.AreEqual(0f, off.RailFlash);
            Assert.AreEqual(0f, off.BallPop);
            Assert.AreEqual(0f, off.ScorePop[0]);
            Assert.AreEqual(0f, off.WinProgress);
            Assert.AreEqual(0f, off.WashAlpha);
            Assert.AreEqual(0f, off.PushIn);
        }

        [Test]
        public void The_wash_and_push_in_follow_the_win_progress()
        {
            feel.OnEvent(Of(EventKind.Win, Side.Left));
            Assert.AreEqual(0f, feel.WashAlpha, Tol);
            for (int i = 0; i < 400; i++) feel.Update(0.01f);
            Assert.AreEqual(settings.winWashAlpha, feel.WashAlpha, Tol);
            Assert.AreEqual(settings.winPushIn, feel.PushIn, Tol);
        }
    }
}
