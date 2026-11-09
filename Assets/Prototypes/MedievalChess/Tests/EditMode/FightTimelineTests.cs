using NUnit.Framework;
using UnityEngine;

namespace Prototypes.MedievalChess.Tests
{
    public class FightTimelineTests
    {
        const float Step = 1f / 120f;
        const float Tol = 0.001f;

        FightSettings settings;
        FightPlan plan;

        [SetUp]
        public void SetUp()
        {
            settings = new FightSettings();
            plan = FightLibrary.Create(PieceType.Rook, PieceType.Pawn, new Vector3(-1.5f, 0f, 0f), new Vector3(1f, 0f, 0f), 0f, 180f, settings);
        }

        // Runs the timeline in real-time steps and returns the real time it took, the time spent
        // frozen on the blow, and how many frames reported the blow.
        (float real, float frozen, int blows, float shake) Run(FightTimeline timeline, float limit = 20f)
        {
            float real = 0f, frozen = 0f, shake = 0f;
            int blows = 0;
            while (!timeline.Finished && real < limit)
            {
                float before = timeline.PlanTime;
                timeline.Advance(Step);
                real += Step;
                if (timeline.BlowThisFrame) { blows++; shake = Mathf.Max(shake, timeline.BlowShake); }
                if (Mathf.Approximately(timeline.PlanTime, plan.BlowTime) && Mathf.Approximately(before, plan.BlowTime)) frozen += Step;
            }
            return (real, frozen, blows, shake);
        }

        [Test]
        public void At_full_speed_the_fight_takes_its_duration_plus_the_hit_stop()
        {
            var timeline = new FightTimeline(plan, AnimationSpeed.Full, settings, 1f);
            var r = Run(timeline);
            Assert.IsTrue(timeline.Finished);
            Assert.AreEqual(plan.Duration + settings.hitStop, r.real, 0.05f);
            Assert.AreEqual(plan.Duration, timeline.PlanTime, Tol);
        }

        [Test]
        public void The_blow_is_reported_exactly_once_and_time_freezes_on_it()
        {
            var timeline = new FightTimeline(plan, AnimationSpeed.Full, settings, 1f);
            var r = Run(timeline);
            Assert.AreEqual(1, r.blows);
            Assert.AreEqual(settings.hitStop, r.frozen, 0.03f);
            Assert.AreEqual(settings.shakeStrength, r.shake, Tol);
        }

        [Test]
        public void The_hit_stop_ends()
        {
            var timeline = new FightTimeline(plan, AnimationSpeed.Full, settings, 1f);
            Run(timeline);
            Assert.IsFalse(timeline.HitStopActive);
        }

        [Test]
        public void Quick_is_faster_by_the_quick_factor_with_no_camera_cut_and_no_shake()
        {
            var timeline = new FightTimeline(plan, AnimationSpeed.Quick, settings, 1f);
            float maxPush = 0f;
            float real = 0f, shake = 0f;
            while (!timeline.Finished && real < 20f)
            {
                timeline.Advance(Step);
                real += Step;
                maxPush = Mathf.Max(maxPush, timeline.CameraPush);
                shake = Mathf.Max(shake, timeline.BlowShake);
            }
            Assert.AreEqual(plan.Duration / settings.quickFactor, real, settings.hitStop + 0.05f);
            Assert.AreEqual(0f, maxPush, Tol);
            Assert.AreEqual(0f, shake, Tol);
        }

        [Test]
        public void Off_finishes_at_once_with_no_effects()
        {
            var timeline = new FightTimeline(plan, AnimationSpeed.Off, settings, 1f);
            timeline.Advance(Step);
            Assert.IsTrue(timeline.Finished);
            Assert.AreEqual(plan.Duration, timeline.PlanTime, Tol);
            Assert.IsFalse(timeline.BlowThisFrame);
            Assert.IsFalse(timeline.HitStopActive);
            Assert.AreEqual(0f, timeline.CameraPush);
        }

        [Test]
        public void Effect_strength_zero_removes_the_hit_stop_the_shake_and_the_camera_cut()
        {
            var timeline = new FightTimeline(plan, AnimationSpeed.Full, settings, 0f);
            float real = 0f, shake = 0f, maxPush = 0f;
            while (!timeline.Finished && real < 20f)
            {
                timeline.Advance(Step);
                real += Step;
                shake = Mathf.Max(shake, timeline.BlowShake);
                maxPush = Mathf.Max(maxPush, timeline.CameraPush);
                Assert.IsFalse(timeline.HitStopActive);
            }
            Assert.AreEqual(plan.Duration, real, 0.05f);
            Assert.AreEqual(0f, shake);
            Assert.AreEqual(0f, maxPush);
        }

        [Test]
        public void Half_strength_halves_the_shake()
        {
            var timeline = new FightTimeline(plan, AnimationSpeed.Full, settings, 0.5f);
            var r = Run(timeline);
            Assert.AreEqual(settings.shakeStrength * 0.5f, r.shake, Tol);
        }

        [Test]
        public void The_plan_time_never_goes_backwards_or_past_the_end()
        {
            var timeline = new FightTimeline(plan, AnimationSpeed.Full, settings, 1f);
            float last = 0f;
            for (int i = 0; i < 2000; i++)
            {
                timeline.Advance(Step);
                Assert.GreaterOrEqual(timeline.PlanTime, last);
                Assert.LessOrEqual(timeline.PlanTime, plan.Duration + 1e-4f);
                last = timeline.PlanTime;
            }
        }

        [Test]
        public void A_big_frame_still_stops_on_the_blow()
        {
            var timeline = new FightTimeline(plan, AnimationSpeed.Full, settings, 1f);
            timeline.Advance(plan.BlowTime + 0.2f); // one very long frame
            Assert.AreEqual(plan.BlowTime, timeline.PlanTime, Tol);
            Assert.IsTrue(timeline.BlowThisFrame);
            Assert.IsTrue(timeline.HitStopActive);
        }
    }
}
