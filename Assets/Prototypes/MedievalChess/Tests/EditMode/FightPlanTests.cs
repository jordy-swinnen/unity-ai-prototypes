using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Prototypes.MedievalChess.Tests
{
    public class FightPlanTests
    {
        const float Tol = 0.001f;

        static readonly PieceType[] Attackers =
            { PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen, PieceType.King };

        FightSettings settings;
        FightFrame frame;

        [SetUp]
        public void SetUp()
        {
            settings = new FightSettings();
            frame = new FightFrame();
        }

        FightPlan Plan(PieceType attacker, PieceType victim, Vector3 from, Vector3 to)
        {
            return FightLibrary.Create(attacker, victim, from, to, 0f, 180f, settings);
        }

        static IEnumerable<TestCaseData> Cases()
        {
            // attacker, start, victim: side by side, from behind, diagonal, and next door
            foreach (var attacker in Attackers)
            {
                yield return new TestCaseData(attacker, new Vector3(-1.5f, 0f, 0f), new Vector3(1f, 0f, 0f)).SetName($"{attacker}_from_the_left");
                yield return new TestCaseData(attacker, new Vector3(0f, 0f, -2f), new Vector3(0f, 0f, 1f)).SetName($"{attacker}_from_below");
                yield return new TestCaseData(attacker, new Vector3(2f, 0f, 2f), new Vector3(-1f, 0f, -1f)).SetName($"{attacker}_diagonal");
                yield return new TestCaseData(attacker, new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f)).SetName($"{attacker}_next_door");
            }
        }

        // --- the start and the end ---

        [TestCaseSource(nameof(Cases))]
        public void At_time_zero_everything_is_at_rest(PieceType attacker, Vector3 from, Vector3 to)
        {
            var plan = Plan(attacker, PieceType.Pawn, from, to);
            plan.Evaluate(0f, frame);
            Assert.AreEqual(Vector3.zero, frame.Attacker.RootOffset);
            Assert.AreEqual(Vector3.zero, frame.Victim.RootOffset);
            Assert.AreEqual(0f, frame.Attacker.Yaw, Tol);
            Assert.AreEqual(180f, frame.Victim.Yaw, Tol);
            Assert.AreEqual(1f, frame.Attacker.Scale, Tol);
            Assert.AreEqual(1f, frame.Victim.Scale, Tol);
            Assert.IsTrue(frame.Victim.Visible);
            Assert.AreEqual(0f, frame.CameraPush, Tol);
            foreach (var e in frame.Attacker.PartEuler.Values) Assert.AreEqual(Vector3.zero, e);
            foreach (var e in frame.Victim.PartEuler.Values) Assert.AreEqual(Vector3.zero, e);
        }

        [TestCaseSource(nameof(Cases))]
        public void At_the_end_the_attacker_stands_on_the_victims_square_and_the_victim_is_gone(PieceType attacker, Vector3 from, Vector3 to)
        {
            var plan = Plan(attacker, PieceType.Pawn, from, to);
            plan.Evaluate(plan.Duration, frame);
            Assert.AreEqual((to - from).x, frame.Attacker.RootOffset.x, Tol);
            Assert.AreEqual((to - from).z, frame.Attacker.RootOffset.z, Tol);
            Assert.AreEqual(0f, frame.Attacker.RootOffset.y, Tol);
            Assert.IsFalse(frame.Victim.Visible);
            Assert.IsTrue(frame.Attacker.Visible);
            Assert.AreEqual(0f, frame.CameraPush, Tol);
            Assert.AreEqual(1f, frame.Attacker.Scale, Tol);
            foreach (var e in frame.Attacker.PartEuler.Values)
                Assert.AreEqual(0f, e.magnitude, Tol, "the attacker's parts must return to rest");
        }

        [TestCaseSource(nameof(Cases))]
        public void The_marks_are_in_order_and_inside_the_plan(PieceType attacker, Vector3 from, Vector3 to)
        {
            var plan = Plan(attacker, PieceType.Pawn, from, to);
            Assert.Greater(plan.ApproachEnd, 0f);
            Assert.Greater(plan.BlowTime, plan.ApproachEnd);
            Assert.Greater(plan.CollapseEnd, plan.BlowTime);
            Assert.Greater(plan.Duration, plan.CollapseEnd);
            Assert.AreEqual(settings.collapseTime, plan.CollapseEnd - plan.BlowTime, Tol);
        }

        // --- the fight itself ---

        [TestCaseSource(nameof(Cases))]
        public void The_attacker_faces_the_victim_before_the_blow_and_the_victim_faces_back(PieceType attacker, Vector3 from, Vector3 to)
        {
            var plan = Plan(attacker, PieceType.Pawn, from, to);
            plan.Evaluate(plan.BlowTime, frame);
            var dir = to - from;
            float wanted = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            Assert.AreEqual(0f, Mathf.DeltaAngle(frame.Attacker.Yaw, wanted), 0.5f);
            Assert.AreEqual(0f, Mathf.DeltaAngle(frame.Victim.Yaw, wanted + 180f), 0.5f);
        }

        [TestCaseSource(nameof(Cases))]
        public void The_attacker_is_close_but_not_inside_the_victim_at_the_blow(PieceType attacker, Vector3 from, Vector3 to)
        {
            var plan = Plan(attacker, PieceType.Pawn, from, to);
            plan.Evaluate(plan.BlowTime, frame);
            var attackerPos = from + frame.Attacker.RootOffset;
            float distance = Vector3.Distance(attackerPos, to);
            Assert.GreaterOrEqual(distance, 0.45f, "too close: the pieces overlap");
            Assert.LessOrEqual(distance, 1.2f, "too far: the blow would miss");
        }

        [TestCaseSource(nameof(Cases))]
        public void The_victim_stays_put_until_the_blow_then_falls_and_vanishes(PieceType attacker, Vector3 from, Vector3 to)
        {
            var plan = Plan(attacker, PieceType.Pawn, from, to);

            plan.Evaluate(plan.BlowTime - 0.01f, frame);
            Assert.AreEqual(0f, frame.Victim.RootOffset.magnitude, Tol, "no push before the blow");
            Assert.AreEqual(0f, frame.Victim.Pitch, Tol, "no fall before the blow");
            Assert.IsTrue(frame.Victim.Visible);

            plan.Evaluate((plan.BlowTime + plan.CollapseEnd) * 0.5f, frame);
            Assert.Less(frame.Victim.Pitch, -20f, "the victim is going over");
            Assert.IsTrue(frame.Victim.Visible);

            plan.Evaluate(plan.CollapseEnd - 0.001f, frame);
            Assert.Less(frame.Victim.Pitch, -60f, "the victim is down");
            Assert.IsTrue(frame.Victim.Visible);

            plan.Evaluate(plan.CollapseEnd + 0.001f, frame);
            Assert.IsFalse(frame.Victim.Visible);
        }

        [TestCaseSource(nameof(Cases))]
        public void The_victim_burns_away_from_the_fall_to_the_end_and_keeps_its_size(PieceType attacker, Vector3 from, Vector3 to)
        {
            var plan = Plan(attacker, PieceType.Pawn, from, to);

            plan.Evaluate(0f, frame);
            Assert.AreEqual(0f, frame.Victim.Dissolve, Tol);
            plan.Evaluate(plan.BlowTime, frame);
            Assert.AreEqual(0f, frame.Victim.Dissolve, Tol, "nothing burns before the blow");

            float previous = -1f;
            for (float t = plan.BlowTime; t <= plan.CollapseEnd; t += 0.02f)
            {
                plan.Evaluate(t, frame);
                Assert.GreaterOrEqual(frame.Victim.Dissolve, previous - Tol, "the burn never runs backward");
                Assert.AreEqual(1f, frame.Victim.Scale, Tol, "the victim burns instead of shrinking");
                previous = frame.Victim.Dissolve;
            }

            plan.Evaluate(plan.CollapseEnd - settings.dissolveTime * 0.5f, frame);
            Assert.That(frame.Victim.Dissolve, Is.InRange(0.3f, 0.7f), "half way through the burn");
            plan.Evaluate(plan.CollapseEnd, frame);
            Assert.AreEqual(1f, frame.Victim.Dissolve, Tol, "fully burnt when it is removed");

            plan.Evaluate(0f, frame);
            Assert.AreEqual(0f, frame.Attacker.Dissolve, Tol);
            plan.Evaluate(plan.Duration, frame);
            Assert.AreEqual(0f, frame.Attacker.Dissolve, Tol, "the attacker never burns");
        }

        [TestCaseSource(nameof(Cases))]
        public void The_victim_is_pushed_away_from_the_attacker(PieceType attacker, Vector3 from, Vector3 to)
        {
            var plan = Plan(attacker, PieceType.Pawn, from, to);
            plan.Evaluate(plan.CollapseEnd - 0.001f, frame);
            var away = (to - from).normalized;
            Assert.Greater(Vector3.Dot(frame.Victim.RootOffset, away), 0.1f);
        }

        [TestCaseSource(nameof(Cases))]
        public void The_camera_pushes_in_during_the_fight_and_is_back_at_the_end(PieceType attacker, Vector3 from, Vector3 to)
        {
            var plan = Plan(attacker, PieceType.Pawn, from, to);
            plan.Evaluate(plan.BlowTime, frame);
            Assert.AreEqual(1f, frame.CameraPush, 0.01f);
            plan.Evaluate(plan.Duration, frame);
            Assert.AreEqual(0f, frame.CameraPush, Tol);
        }

        // --- the two routines differ, and the blow shows ---

        [Test]
        public void The_pawn_thrusts_with_one_arm_and_the_rook_slams_with_two()
        {
            var from = new Vector3(-1.5f, 0f, 0f);
            var to = new Vector3(1f, 0f, 0f);
            var pawn = Plan(PieceType.Pawn, PieceType.Pawn, from, to);
            var rook = Plan(PieceType.Rook, PieceType.Pawn, from, to);

            pawn.Evaluate(pawn.BlowTime, frame);
            Assert.Greater(frame.Attacker.PartEuler["ArmR"].x, 60f, "the spear points forward");
            Assert.AreEqual(0f, frame.Attacker.PartEuler["ArmL"].x, 15f, "the shield arm stays about still");

            rook.Evaluate(rook.BlowTime, frame);
            Assert.AreNotEqual(0f, frame.Attacker.PartEuler["ArmR"].x);
            Assert.AreNotEqual(0f, frame.Attacker.PartEuler["ArmL"].x);
            Assert.AreEqual(frame.Attacker.PartEuler["ArmR"].x, frame.Attacker.PartEuler["ArmL"].x, 5f, "both fists come down together");
        }

        [Test]
        public void The_rook_raises_its_fists_over_its_head_before_the_slam()
        {
            var rook = Plan(PieceType.Rook, PieceType.Pawn, new Vector3(-1.5f, 0f, 0f), new Vector3(1f, 0f, 0f));
            rook.Evaluate(rook.BlowTime - 0.02f, frame);
            // a hanging arm is at 0 degrees, and a negative angle swings it up and forward
            Assert.Less(frame.Attacker.PartEuler["ArmR"].x, -100f);
        }

        [Test]
        public void Each_attacker_type_has_its_own_wind_up_and_so_its_own_blow_time()
        {
            var from = new Vector3(-1.5f, 0f, 0f);
            var to = new Vector3(1f, 0f, 0f);
            var expected = new System.Collections.Generic.Dictionary<PieceType, float>
            {
                { PieceType.Pawn, settings.windUpPawn }, { PieceType.Knight, settings.windUpKnight },
                { PieceType.Bishop, settings.windUpBishop }, { PieceType.Rook, settings.windUpRook },
                { PieceType.Queen, settings.windUpQueen }, { PieceType.King, settings.windUpKing },
            };
            var blows = new System.Collections.Generic.HashSet<float>();
            foreach (var kv in expected)
            {
                var plan = Plan(kv.Key, PieceType.Pawn, from, to);
                Assert.AreEqual(settings.approachTime + kv.Value, plan.BlowTime, Tol, kv.Key.ToString());
                blows.Add(plan.BlowTime);
            }
            Assert.AreEqual(6, blows.Count, "every attacker type should land its blow at a different moment");
        }

        // --- the plan is a function of time ---

        [TestCaseSource(nameof(Cases))]
        public void Evaluating_the_same_time_twice_gives_the_same_pose_and_never_nan(PieceType attacker, Vector3 from, Vector3 to)
        {
            var plan = Plan(attacker, PieceType.Pawn, from, to);
            var other = new FightFrame();
            for (int i = 0; i <= 200; i++)
            {
                float t = plan.Duration * i / 200f;
                plan.Evaluate(t, frame);
                plan.Evaluate(t, other);
                AssertSame(frame.Attacker, other.Attacker);
                AssertSame(frame.Victim, other.Victim);
                AssertFinite(frame.Attacker);
                AssertFinite(frame.Victim);
                Assert.IsFalse(float.IsNaN(frame.CameraPush));
            }
        }

        [Test]
        public void Times_before_zero_and_after_the_end_are_clamped()
        {
            var plan = Plan(PieceType.Rook, PieceType.Pawn, new Vector3(-1.5f, 0f, 0f), new Vector3(1f, 0f, 0f));
            var before = new FightFrame();
            plan.Evaluate(-5f, before);
            plan.Evaluate(0f, frame);
            AssertSame(before.Attacker, frame.Attacker);
            plan.Evaluate(plan.Duration + 5f, before);
            plan.Evaluate(plan.Duration, frame);
            AssertSame(before.Attacker, frame.Attacker);
        }

        [Test]
        public void A_longer_collapse_setting_moves_the_end_of_the_fight()
        {
            var from = new Vector3(-1.5f, 0f, 0f);
            var to = new Vector3(1f, 0f, 0f);
            var normal = Plan(PieceType.Pawn, PieceType.Pawn, from, to);
            settings.collapseTime += 0.5f;
            var longer = Plan(PieceType.Pawn, PieceType.Pawn, from, to);
            Assert.AreEqual(normal.Duration + 0.5f, longer.Duration, Tol);
        }

        [Test]
        public void Every_piece_type_has_a_routine_and_an_undefined_one_is_refused_clearly()
        {
            foreach (PieceType type in System.Enum.GetValues(typeof(PieceType)))
                Assert.IsTrue(FightLibrary.HasRoutine(type), type.ToString());
            Assert.IsFalse(FightLibrary.HasRoutine((PieceType)99));
            var ex = Assert.Throws<System.ArgumentException>(() =>
                Plan((PieceType)99, PieceType.Pawn, new Vector3(-1.5f, 0f, 0f), new Vector3(1f, 0f, 0f)));
            StringAssert.Contains("99", ex.Message);
        }

        static void AssertSame(ActorFrame a, ActorFrame b)
        {
            Assert.AreEqual(a.RootOffset, b.RootOffset);
            Assert.AreEqual(a.Yaw, b.Yaw);
            Assert.AreEqual(a.Pitch, b.Pitch);
            Assert.AreEqual(a.Scale, b.Scale);
            Assert.AreEqual(a.Visible, b.Visible);
            Assert.AreEqual(a.PartEuler.Count, b.PartEuler.Count);
            foreach (var kv in a.PartEuler) Assert.AreEqual(kv.Value, b.PartEuler[kv.Key]);
        }

        static void AssertFinite(ActorFrame f)
        {
            Assert.IsFalse(float.IsNaN(f.RootOffset.x) || float.IsNaN(f.RootOffset.y) || float.IsNaN(f.RootOffset.z));
            Assert.IsFalse(float.IsNaN(f.Yaw) || float.IsNaN(f.Pitch) || float.IsNaN(f.Scale));
            foreach (var e in f.PartEuler.Values)
                Assert.IsFalse(float.IsNaN(e.x) || float.IsNaN(e.y) || float.IsNaN(e.z));
        }
    }
}
