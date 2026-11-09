using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Prototypes.MedievalChess.Tests
{
    public class SequenceTimelineTests
    {
        const float Step = 1f / 120f;

        MoveSettings moves;
        FightSettings fights;
        ActionSequence sequence;

        static Vector3 Sq(int file, int rank) => new Vector3(file - 3.5f, 0f, rank - 3.5f);

        [SetUp]
        public void SetUp()
        {
            moves = new MoveSettings();
            fights = new FightSettings();
            var d = new MoveDescription
            {
                PieceId = "mover", Piece = PieceType.Rook, From = Sq(0, 0), To = Sq(0, 4), Yaw = 0f,
                Capture = true, VictimId = "victim", Victim = PieceType.Pawn, VictimAt = Sq(0, 4), VictimYaw = 180f,
            };
            sequence = MoveDirector.Plan(d, moves, fights);
        }

        (float real, int stepsSeen, int blows) Run(SequenceTimeline timeline)
        {
            float real = 0f;
            int steps = 1, blows = 0, last = timeline.StepIndex;
            while (!timeline.Finished && real < 30f)
            {
                timeline.Advance(Step);
                real += Step;
                if (timeline.StepIndex != last) { steps++; last = timeline.StepIndex; }
                if (timeline.BlowThisFrame) blows++;
            }
            return (real, steps, blows);
        }

        [Test]
        public void It_plays_every_step_in_order_and_reports_one_blow()
        {
            var timeline = new SequenceTimeline(sequence, AnimationSpeed.Full, fights, 1f);
            var r = Run(timeline);
            Assert.IsTrue(timeline.Finished);
            Assert.AreEqual(sequence.Steps.Count, r.stepsSeen);
            Assert.AreEqual(1, r.blows);
            Assert.AreEqual(sequence.Duration + fights.hitStop, r.real, 0.1f);
        }

        [Test]
        public void A_step_that_has_no_blow_has_no_hit_stop()
        {
            var timeline = new SequenceTimeline(sequence, AnimationSpeed.Full, fights, 1f);
            // the slide comes first
            Assert.IsInstanceOf<MovePlan>(timeline.CurrentPlan);
            for (int i = 0; i < 20; i++) timeline.Advance(Step);
            Assert.IsFalse(timeline.HitStopActive);
            Assert.IsFalse(timeline.BlowThisFrame);
        }

        [Test]
        public void Quick_is_about_a_third_of_the_time_with_no_hit_stop_and_no_camera_cut()
        {
            var timeline = new SequenceTimeline(sequence, AnimationSpeed.Quick, fights, 1f);
            float maxPush = 0f, real = 0f;
            while (!timeline.Finished && real < 30f)
            {
                timeline.Advance(Step);
                real += Step;
                maxPush = Mathf.Max(maxPush, timeline.CameraPush);
                Assert.IsFalse(timeline.HitStopActive);
            }
            Assert.AreEqual(sequence.Duration / fights.quickFactor, real, 0.15f);
            Assert.AreEqual(0f, maxPush);
        }

        [Test]
        public void Off_finishes_every_step_at_once()
        {
            var timeline = new SequenceTimeline(sequence, AnimationSpeed.Off, fights, 1f);
            timeline.Advance(Step);
            Assert.IsTrue(timeline.Finished);
            Assert.AreEqual(sequence.Steps.Count - 1, timeline.StepIndex);
        }

        [Test]
        public void Effect_strength_zero_takes_away_the_shake_and_the_hit_stop()
        {
            var timeline = new SequenceTimeline(sequence, AnimationSpeed.Full, fights, 0f);
            float real = 0f, shake = 0f;
            while (!timeline.Finished && real < 30f)
            {
                timeline.Advance(Step);
                real += Step;
                shake = Mathf.Max(shake, timeline.BlowShake);
                Assert.IsFalse(timeline.HitStopActive);
            }
            Assert.AreEqual(sequence.Duration, real, 0.1f);
            Assert.AreEqual(0f, shake);
        }

        [Test]
        public void A_single_plan_can_be_wrapped_as_a_sequence()
        {
            var fight = FightLibrary.Create(PieceType.King, PieceType.Queen, new Vector3(-1.5f, 0f, 0f), new Vector3(1f, 0f, 0f), 90f, -90f, fights);
            var seq = ActionSequence.Single(fight);
            Assert.AreEqual(1, seq.Steps.Count);
            Assert.AreEqual(fight.Duration, seq.Duration, 1e-4f);
            Assert.IsFalse(seq.End["victim"].Visible);
            Assert.AreEqual(1f, seq.End["attacker"].Position.x, 0.01f);
        }

        [Test]
        public void The_camera_push_comes_from_the_step_being_played()
        {
            var timeline = new SequenceTimeline(sequence, AnimationSpeed.Full, fights, 1f);
            float pushDuringSlide = 0f, pushDuringFight = 0f;
            float real = 0f;
            while (!timeline.Finished && real < 30f)
            {
                timeline.Advance(Step);
                real += Step;
                if (timeline.CurrentPlan is MovePlan) pushDuringSlide = Mathf.Max(pushDuringSlide, timeline.CameraPush);
                else pushDuringFight = Mathf.Max(pushDuringFight, timeline.CameraPush);
            }
            Assert.AreEqual(0f, pushDuringSlide, 0.01f);
            Assert.Greater(pushDuringFight, 0.9f);
        }
    }
}
