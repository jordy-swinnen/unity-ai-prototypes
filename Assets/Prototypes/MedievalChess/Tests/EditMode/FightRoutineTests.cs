using NUnit.Framework;
using UnityEngine;

namespace Prototypes.MedievalChess.Tests
{
    /// <summary>What makes each attacker's fight its own, and that every victim falls the same way.</summary>
    public class FightRoutineTests
    {
        const float Tol = 0.001f;
        static readonly Vector3 From = new Vector3(-1.5f, 0f, 0f);
        static readonly Vector3 To = new Vector3(1f, 0f, 0f);

        FightSettings settings;
        FightFrame frame;

        [SetUp]
        public void SetUp()
        {
            settings = new FightSettings();
            frame = new FightFrame();
        }

        FightPlan Plan(PieceType attacker, PieceType victim = PieceType.Pawn) =>
            FightLibrary.Create(attacker, victim, From, To, 90f, -90f, settings);

        Vector3 Arm(FightPlan plan, float t, string part = "ArmR")
        {
            plan.Evaluate(t, frame);
            return frame.Attacker.PartEuler[part];
        }

        [Test]
        public void The_knight_pulls_back_then_charges_with_a_levelled_lance()
        {
            var plan = Plan(PieceType.Knight);
            var dir = (To - From).normalized;
            var standOffset = dir * Mathf.Max(0f, (To - From).magnitude - (settings.standOff + settings.knightExtraStandOff));

            plan.Evaluate(plan.BlowTime - 0.2f, frame);
            float back = Vector3.Dot(frame.Attacker.RootOffset - standOffset, dir);
            Assert.Less(back, -0.15f, "the horse draws back before it charges");

            plan.Evaluate(plan.BlowTime, frame);
            float forward = Vector3.Dot(frame.Attacker.RootOffset - standOffset, dir);
            Assert.GreaterOrEqual(forward, settings.knightLunge - Tol, "the horse has charged");
            Assert.Greater(frame.Attacker.PartEuler["ArmR"].x, 80f, "the lance is levelled at the victim");
            Assert.IsTrue(frame.Attacker.PartEuler.ContainsKey("HorseHead"));
        }

        [Test]
        public void The_knight_lowers_its_horses_head_on_the_charge()
        {
            var plan = Plan(PieceType.Knight);
            plan.Evaluate(plan.BlowTime - 0.2f, frame);
            float rearing = frame.Attacker.PartEuler["HorseHead"].x;
            plan.Evaluate(plan.BlowTime, frame);
            Assert.Less(rearing, -10f);
            Assert.Greater(frame.Attacker.PartEuler["HorseHead"].x, 5f);
        }

        [Test]
        public void The_bishop_swings_its_mace_from_behind_its_head_to_the_front()
        {
            var plan = Plan(PieceType.Bishop);
            Assert.Less(Arm(plan, plan.BlowTime - 0.1f).x, -50f);
            Assert.Greater(Arm(plan, plan.BlowTime).x, 80f);
            Assert.Less(Arm(plan, plan.BlowTime, "ArmL").x, -20f, "the other hand holds up the book");
        }

        [Test]
        public void The_queen_sweeps_her_sword_across_in_a_level_arc()
        {
            var plan = Plan(PieceType.Queen);
            var windUp = Arm(plan, plan.BlowTime - 0.1f);
            var blow = Arm(plan, plan.BlowTime);
            Assert.Less(windUp.y, -60f, "the sword starts across her body");
            Assert.AreEqual(0f, blow.y, 5f, "the blow lands as the sword passes the victim");
            Assert.Greater(blow.x, 60f, "the sword is level");
            plan.Evaluate(plan.BlowTime + 0.25f, frame);
            Assert.Greater(frame.Attacker.PartEuler["ArmR"].y, 40f, "and follows through to the other side");
        }

        [Test]
        public void The_king_raises_his_sword_slowly_and_brings_it_down_hardest()
        {
            var plan = Plan(PieceType.King);
            Assert.Less(Arm(plan, plan.BlowTime - 0.1f).x, -30f);
            Assert.Greater(Arm(plan, plan.BlowTime).x, 90f);
            Assert.Greater(plan.BlowTime, Plan(PieceType.Queen).BlowTime, "the king is the slowest to strike");
        }

        [Test]
        public void No_two_attackers_have_the_same_pose_at_the_blow()
        {
            var seen = new System.Collections.Generic.List<Vector3>();
            foreach (PieceType type in System.Enum.GetValues(typeof(PieceType)))
            {
                var plan = Plan(type);
                plan.Evaluate(plan.BlowTime, frame);
                var signature = frame.Attacker.PartEuler["ArmR"] + frame.Attacker.RootOffset * 100f;
                foreach (var other in seen) Assert.Greater((signature - other).magnitude, 1f, $"{type} looks like another attacker at the blow");
                seen.Add(signature);
            }
        }

        [Test]
        public void Every_attacker_has_weapon_arm_channels_that_exist_on_its_model()
        {
            string[] allowed = { "ArmR", "ArmL", "Body", "Head", "Weapon", "Shield", "HorseHead", "Horse" };
            foreach (PieceType type in System.Enum.GetValues(typeof(PieceType)))
            {
                var plan = Plan(type);
                foreach (var name in plan.Attacker.PartEuler.Keys)
                    CollectionAssert.Contains(allowed, name, $"{type} animates a part called {name}");
            }
        }

        [Test]
        public void Every_victim_type_is_knocked_down_and_vanishes_in_the_same_way()
        {
            foreach (PieceType victim in System.Enum.GetValues(typeof(PieceType)))
            {
                var plan = Plan(PieceType.Pawn, victim);
                Assert.AreEqual(victim, plan.VictimType);
                plan.Evaluate(plan.CollapseEnd - 0.001f, frame);
                Assert.Less(frame.Victim.Pitch, -60f, victim.ToString());
                Assert.IsTrue(frame.Victim.Visible);
                plan.Evaluate(plan.CollapseEnd + 0.001f, frame);
                Assert.IsFalse(frame.Victim.Visible, victim.ToString());
            }
        }

        [Test]
        public void The_knight_stands_further_back_than_the_others_because_its_lance_is_long()
        {
            var knight = Plan(PieceType.Knight);
            var pawn = Plan(PieceType.Pawn);
            knight.Evaluate(knight.ApproachEnd, frame);
            var knightStop = frame.Attacker.RootOffset;
            pawn.Evaluate(pawn.ApproachEnd, frame);
            Assert.Less(knightStop.magnitude, frame.Attacker.RootOffset.magnitude);
        }
    }
}
