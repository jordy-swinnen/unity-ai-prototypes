using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Prototypes.MedievalChess.Tests
{
    /// <summary>
    /// The director turns a described move (a walk, a capture, castling, promotion, check ...) into steps.
    /// These tests check the end state of each, and that every step starts where the last one ended.
    /// </summary>
    public class MoveDirectorTests
    {
        const float Tol = 0.01f;

        MoveSettings moves;
        FightSettings fights;

        [SetUp]
        public void SetUp()
        {
            moves = new MoveSettings();
            fights = new FightSettings();
        }

        // Square (file a = 0, rank 1 = 0) to world position, as in the plan's layout.
        static Vector3 Sq(int file, int rank) => new Vector3(file - 3.5f, 0f, rank - 3.5f);

        static MoveDescription Move(PieceType piece, Vector3 from, Vector3 to, int side = 0) => new MoveDescription
        {
            PieceId = "mover", Piece = piece, Side = side, From = from, To = to, Yaw = side == 0 ? 0f : 180f,
        };

        ActionSequence Plan(MoveDescription d) => MoveDirector.Plan(d, moves, fights);

        static void Final(IActionPlan plan, ActorFrame[] frames) => plan.Evaluate(plan.Duration, frames);

        static ActorFrame[] Frames(int n) => Enumerable.Range(0, n).Select(_ => new ActorFrame()).ToArray();

        // --- quiet moves ---

        [Test]
        public void A_bishop_walks_one_step_per_square_and_ends_on_its_square_facing_as_before()
        {
            var d = Move(PieceType.Bishop, Sq(2, 0), Sq(5, 3));
            var seq = Plan(d);
            Assert.AreEqual(1, seq.Steps.Count);
            var plan = seq.Steps[0];
            Assert.IsInstanceOf<MovePlan>(plan);
            Assert.AreEqual(3, plan.Footfalls.Count, "one thud per square");
            Assert.GreaterOrEqual(plan.Duration, 3 * moves.stepTime);
            Assert.LessOrEqual(plan.Duration, 3 * moves.stepTime + 2 * moves.turnTime + Tol);
            for (int i = 1; i < plan.Footfalls.Count; i++) Assert.Greater(plan.Footfalls[i], plan.Footfalls[i - 1]);
            Assert.LessOrEqual(plan.Footfalls.Last(), plan.Duration);

            var end = seq.End["mover"];
            Assert.AreEqual(Sq(5, 3).x, end.Position.x, Tol);
            Assert.AreEqual(Sq(5, 3).z, end.Position.z, Tol);
            Assert.AreEqual(0f, Mathf.DeltaAngle(end.Yaw, 0f), 0.5f);
            Assert.IsTrue(end.Visible);
        }

        [Test]
        public void A_piece_turns_toward_where_it_is_going_while_it_walks()
        {
            var d = Move(PieceType.Bishop, Sq(2, 0), Sq(5, 3));
            var plan = Plan(d).Steps[0];
            var frames = Frames(1);
            plan.Evaluate(plan.Duration * 0.5f, frames);
            // moving along +x and +z: heading 45 degrees
            Assert.AreEqual(45f, Mathf.DeltaAngle(0f, frames[0].Yaw), 1f);
        }

        [Test]
        public void A_knight_leaps_in_an_arc_and_lands_with_one_thud()
        {
            var d = Move(PieceType.Knight, Sq(1, 0), Sq(2, 2));
            var seq = Plan(d);
            var plan = seq.Steps[0];
            var frames = Frames(1);
            float peak = 0f;
            for (int i = 0; i <= 100; i++)
            {
                plan.Evaluate(plan.Duration * i / 100f, frames);
                peak = Mathf.Max(peak, frames[0].RootOffset.y);
            }
            Assert.AreEqual(moves.leapHeight, peak, moves.leapHeight * 0.15f);
            Final(plan, frames);
            Assert.AreEqual(0f, frames[0].RootOffset.y, Tol);
            Assert.AreEqual(1, plan.Footfalls.Count);
            Assert.GreaterOrEqual(plan.Duration, moves.leapTime);
        }

        [Test]
        public void A_rook_slides_straight_and_never_goes_backwards()
        {
            var d = Move(PieceType.Rook, Sq(0, 0), Sq(0, 4));
            var plan = Plan(d).Steps[0];
            var frames = Frames(1);
            float last = -1f;
            for (int i = 0; i <= 60; i++)
            {
                plan.Evaluate(plan.Duration * i / 60f, frames);
                float along = frames[0].RootOffset.z;
                Assert.GreaterOrEqual(along, last - 1e-4f);
                Assert.AreEqual(0f, frames[0].RootOffset.x, 1e-4f);
                last = along;
            }
            Assert.AreEqual(4f, last, Tol);
        }

        [Test]
        public void A_longer_move_takes_longer_but_a_one_square_move_is_quick()
        {
            var one = Plan(Move(PieceType.Rook, Sq(0, 0), Sq(0, 1))).Steps[0].Duration;
            var seven = Plan(Move(PieceType.Rook, Sq(0, 0), Sq(0, 7))).Steps[0].Duration;
            Assert.Greater(seven, one);
            Assert.Less(one, 0.7f);
        }

        // --- captures ---

        [Test]
        public void A_rook_takes_a_pawn_it_slides_up_to_it_then_fights_and_ends_on_the_square()
        {
            var d = Move(PieceType.Rook, Sq(0, 0), Sq(0, 4));
            d.Capture = true;
            d.VictimId = "victim";
            d.Victim = PieceType.Pawn;
            d.VictimAt = Sq(0, 4);
            d.VictimYaw = 180f;
            var seq = Plan(d);

            Assert.AreEqual(2, seq.Steps.Count);
            Assert.IsInstanceOf<MovePlan>(seq.Steps[0]);
            var fight = seq.Steps[1] as FightPlan;
            Assert.IsNotNull(fight);
            Assert.AreEqual(1, fight.BlowTime > 0 ? 1 : 0);
            Assert.AreEqual(d.VictimAt, fight.VictimPosition);

            // the slide stops a fighting distance short of the victim
            var frames = Frames(1);
            seq.Steps[0].Evaluate(seq.Steps[0].Duration, frames);
            float gap = Vector3.Distance(Sq(0, 0) + frames[0].RootOffset, d.VictimAt);
            Assert.AreEqual(fights.standOff, gap, 0.05f);

            Assert.AreEqual(Sq(0, 4).x, seq.End["mover"].Position.x, Tol);
            Assert.AreEqual(Sq(0, 4).z, seq.End["mover"].Position.z, Tol);
            Assert.IsFalse(seq.End["victim"].Visible);
            Assert.IsTrue(seq.End["mover"].Visible);
            Assert.AreEqual(0f, Mathf.DeltaAngle(seq.End["mover"].Yaw, 0f), 0.5f, "the rook turns back to face forward");
        }

        [Test]
        public void A_pawn_takes_diagonally_and_a_knight_leaps_in_first()
        {
            foreach (var piece in new[] { PieceType.Pawn, PieceType.Knight })
            {
                var from = Sq(3, 3);
                var to = piece == PieceType.Pawn ? Sq(4, 4) : Sq(5, 4);
                var d = Move(piece, from, to);
                d.Capture = true;
                d.VictimId = "victim";
                d.Victim = PieceType.Bishop;
                d.VictimAt = to;
                d.VictimYaw = 180f;
                var seq = Plan(d);
                Assert.IsInstanceOf<FightPlan>(seq.Steps.Last(), piece.ToString());
                Assert.AreEqual(to.x, seq.End["mover"].Position.x, Tol, piece.ToString());
                Assert.AreEqual(to.z, seq.End["mover"].Position.z, Tol, piece.ToString());
                Assert.IsFalse(seq.End["victim"].Visible, piece.ToString());
            }
        }

        [Test]
        public void En_passant_fights_the_pawn_beside_it_and_then_steps_to_the_empty_square()
        {
            var from = Sq(4, 4);   // e5
            var to = Sq(3, 5);     // d6
            var victimAt = Sq(3, 4); // d5
            var d = Move(PieceType.Pawn, from, to);
            d.Capture = true;
            d.EnPassant = true;
            d.VictimId = "victim";
            d.Victim = PieceType.Pawn;
            d.VictimAt = victimAt;
            d.VictimYaw = 180f;
            var seq = Plan(d);

            var fight = seq.Steps.OfType<FightPlan>().Single();
            Assert.AreEqual(victimAt, fight.VictimPosition);
            Assert.AreNotEqual(to, fight.VictimPosition, "the victim is not on the square the pawn lands on");
            Assert.IsInstanceOf<MovePlan>(seq.Steps.Last(), "after the fight the pawn steps on to the empty square");
            Assert.AreEqual(to.x, seq.End["mover"].Position.x, Tol);
            Assert.AreEqual(to.z, seq.End["mover"].Position.z, Tol);
            Assert.IsFalse(seq.End["victim"].Visible);
        }

        // --- castling ---

        [Test]
        public void Castling_moves_the_king_two_squares_and_the_rook_hops_over_it()
        {
            var d = Move(PieceType.King, Sq(4, 0), Sq(6, 0));
            d.Castle = true;
            d.RookId = "rook";
            d.RookFrom = Sq(7, 0);
            d.RookTo = Sq(5, 0);
            var seq = Plan(d);

            Assert.AreEqual(1, seq.Steps.Count);
            var plan = seq.Steps[0];
            Assert.AreEqual(2, plan.Actors.Count);
            Assert.AreEqual("mover", plan.Actors[0].Id);
            Assert.AreEqual("rook", plan.Actors[1].Id);

            var frames = Frames(2);
            float peak = 0f;
            for (int i = 0; i <= 100; i++)
            {
                plan.Evaluate(plan.Duration * i / 100f, frames);
                peak = Mathf.Max(peak, frames[1].RootOffset.y);
            }
            Assert.Greater(peak, 0.2f, "the rook hops");

            Assert.AreEqual(Sq(6, 0).x, seq.End["mover"].Position.x, Tol);
            Assert.AreEqual(Sq(5, 0).x, seq.End["rook"].Position.x, Tol);
            Assert.AreEqual(0f, seq.End["rook"].Position.y, Tol);
            Assert.IsTrue(seq.End["mover"].Visible && seq.End["rook"].Visible);
        }

        [Test]
        public void Queenside_castling_works_in_the_other_direction()
        {
            var d = Move(PieceType.King, Sq(4, 7), Sq(2, 7), 1);
            d.Castle = true;
            d.RookId = "rook";
            d.RookFrom = Sq(0, 7);
            d.RookTo = Sq(3, 7);
            var seq = Plan(d);
            Assert.AreEqual(Sq(2, 7).x, seq.End["mover"].Position.x, Tol);
            Assert.AreEqual(Sq(3, 7).x, seq.End["rook"].Position.x, Tol);
            Assert.AreEqual(0f, Mathf.DeltaAngle(seq.End["mover"].Yaw, 180f), 0.5f);
        }

        // --- promotion ---

        [TestCase(PieceType.Queen)]
        [TestCase(PieceType.Rook)]
        [TestCase(PieceType.Bishop)]
        [TestCase(PieceType.Knight)]
        public void A_pawn_that_promotes_kneels_and_rises_as_the_chosen_piece(PieceType choice)
        {
            var d = Move(PieceType.Pawn, Sq(4, 6), Sq(4, 7));
            d.Promotes = true;
            d.PromoteTo = choice;
            d.PromotedId = "promoted";
            var seq = Plan(d);

            Assert.IsFalse(seq.End["mover"].Visible, "the pawn is gone");
            Assert.IsTrue(seq.End["promoted"].Visible);
            Assert.AreEqual(choice, seq.End["promoted"].Type);
            Assert.AreEqual(Sq(4, 7).x, seq.End["promoted"].Position.x, Tol);
            Assert.AreEqual(Sq(4, 7).z, seq.End["promoted"].Position.z, Tol);

            var promotion = seq.Steps.Last();
            Assert.AreEqual(2, promotion.Actors.Count);
            Assert.AreEqual(PieceType.Pawn, promotion.Actors[0].Type);
            Assert.AreEqual(choice, promotion.Actors[1].Type);

            var frames = Frames(2);
            promotion.Evaluate(promotion.Duration * 0.3f, frames);
            Assert.Greater(frames[0].PartEuler["Body"].x, 20f, "the pawn kneels");
            Assert.IsTrue(frames[0].Visible);
            Assert.IsFalse(frames[1].Visible, "the new piece is not there yet");
            promotion.Evaluate(promotion.Duration, frames);
            Assert.IsFalse(frames[0].Visible);
            Assert.IsTrue(frames[1].Visible);
            Assert.AreEqual(1f, frames[1].Scale, Tol);
        }

        [Test]
        public void A_pawn_can_capture_and_promote_in_one_move()
        {
            var d = Move(PieceType.Pawn, Sq(4, 6), Sq(5, 7));
            d.Capture = true;
            d.VictimId = "victim";
            d.Victim = PieceType.Rook;
            d.VictimAt = Sq(5, 7);
            d.VictimYaw = 180f;
            d.Promotes = true;
            d.PromoteTo = PieceType.Queen;
            d.PromotedId = "promoted";
            var seq = Plan(d);
            Assert.AreEqual(1, seq.Steps.OfType<FightPlan>().Count());
            Assert.IsInstanceOf<MovePlan>(seq.Steps.Last());
            Assert.IsFalse(seq.End["victim"].Visible);
            Assert.IsFalse(seq.End["mover"].Visible);
            Assert.AreEqual(PieceType.Queen, seq.End["promoted"].Type);
            Assert.AreEqual(Sq(5, 7).x, seq.End["promoted"].Position.x, Tol);
        }

        // --- check, mate and resigning ---

        [Test]
        public void A_check_makes_the_enemy_king_brace_and_then_stand_again()
        {
            var d = Move(PieceType.Rook, Sq(0, 0), Sq(0, 7));
            d.Check = true;
            d.KingId = "king";
            d.KingType = PieceType.King;
            d.KingAt = Sq(4, 7);
            d.KingYaw = 180f;
            d.KingSide = 1;
            var seq = Plan(d);

            var brace = seq.Steps.Last();
            Assert.AreEqual("king", brace.Actors[0].Id);
            var frames = Frames(1);
            brace.Evaluate(brace.Duration * 0.5f, frames);
            Assert.Less(frames[0].PartEuler["ArmL"].x, -30f, "the king raises his shield");
            brace.Evaluate(brace.Duration, frames);
            foreach (var e in frames[0].PartEuler.Values) Assert.AreEqual(0f, e.magnitude, Tol);
            Assert.IsTrue(seq.End["king"].Visible);
            Assert.AreEqual(0f, seq.End["king"].Pitch, Tol);
        }

        [Test]
        public void A_checkmate_makes_the_enemy_king_fall_and_stay_down()
        {
            var d = Move(PieceType.Queen, Sq(3, 0), Sq(3, 6));
            d.Check = true;
            d.Checkmate = true;
            d.KingId = "king";
            d.KingType = PieceType.King;
            d.KingAt = Sq(3, 7);
            d.KingYaw = 180f;
            d.KingSide = 1;
            var seq = Plan(d);

            var fall = seq.Steps.Last();
            Assert.AreEqual("king", fall.Actors[0].Id);
            Assert.AreEqual(1, seq.Steps.Count(s => s.Actors.Any(a => a.Id == "king")), "mate is a fall, not a brace and a fall");
            Assert.IsTrue(seq.End["king"].Visible, "the fallen king stays on the board");
            Assert.LessOrEqual(seq.End["king"].Pitch, -80f);
        }

        [Test]
        public void Resigning_makes_the_king_kneel_and_drop_his_sword()
        {
            var king = new ActorSpec { Id = "king", Type = PieceType.King, Side = 0, Start = Sq(4, 0), StartYaw = 0f };
            var seq = MoveDirector.Resign(king, moves);
            var plan = seq.Steps[0];
            var frames = Frames(1);
            plan.Evaluate(plan.Duration, frames);
            Assert.Greater(frames[0].PartEuler["Body"].x, 20f, "kneeling");
            Assert.Greater(Mathf.Abs(frames[0].PartEuler["Weapon"].x), 45f, "the sword is dropped");
            Assert.IsTrue(seq.End["king"].Visible);
        }

        // --- the whole chain ---

        [Test]
        public void Every_step_starts_where_the_last_one_ended()
        {
            var d = Move(PieceType.Pawn, Sq(4, 6), Sq(5, 7));
            d.Capture = true;
            d.VictimId = "victim";
            d.Victim = PieceType.Rook;
            d.VictimAt = Sq(5, 7);
            d.VictimYaw = 180f;
            d.Promotes = true;
            d.PromoteTo = PieceType.Queen;
            d.PromotedId = "promoted";
            d.Check = true;
            d.Checkmate = true;
            d.KingId = "king";
            d.KingType = PieceType.King;
            d.KingAt = Sq(7, 7);
            d.KingYaw = 180f;
            d.KingSide = 1;
            var seq = Plan(d);

            var where = new System.Collections.Generic.Dictionary<string, Vector3>();
            var frames = Frames(3);
            foreach (var step in seq.Steps)
            {
                foreach (var spec in step.Actors)
                {
                    if (where.TryGetValue(spec.Id, out var previous))
                        Assert.AreEqual(0f, Vector3.Distance(previous, spec.Start), Tol, $"{spec.Id} jumps between steps");
                }
                step.Evaluate(step.Duration, frames);
                for (int i = 0; i < step.Actors.Count; i++)
                    where[step.Actors[i].Id] = step.Actors[i].Start + frames[i].RootOffset;
            }
        }

        [Test]
        public void Every_step_is_finite_and_repeatable_at_every_moment()
        {
            var d = Move(PieceType.King, Sq(4, 0), Sq(6, 0));
            d.Castle = true;
            d.RookId = "rook";
            d.RookFrom = Sq(7, 0);
            d.RookTo = Sq(5, 0);
            d.Check = true;
            d.KingId = "king2";
            d.KingType = PieceType.King;
            d.KingAt = Sq(4, 7);
            d.KingYaw = 180f;
            d.KingSide = 1;
            var seq = Plan(d);
            foreach (var step in seq.Steps)
            {
                var a = Frames(step.Actors.Count);
                var b = Frames(step.Actors.Count);
                for (int i = 0; i <= 80; i++)
                {
                    float t = step.Duration * i / 80f;
                    step.Evaluate(t, a);
                    step.Evaluate(t, b);
                    for (int k = 0; k < a.Length; k++)
                    {
                        Assert.AreEqual(a[k].RootOffset, b[k].RootOffset);
                        Assert.IsFalse(float.IsNaN(a[k].RootOffset.x + a[k].RootOffset.y + a[k].RootOffset.z + a[k].Yaw + a[k].Pitch + a[k].Scale));
                    }
                    Assert.IsFalse(float.IsNaN(step.CameraPush(t)));
                }
            }
        }

        [Test]
        public void The_total_duration_is_the_sum_of_the_steps()
        {
            var d = Move(PieceType.Rook, Sq(0, 0), Sq(0, 4));
            d.Capture = true;
            d.VictimId = "victim";
            d.Victim = PieceType.Pawn;
            d.VictimAt = Sq(0, 4);
            var seq = Plan(d);
            Assert.AreEqual(seq.Steps.Sum(s => s.Duration), seq.Duration, 1e-4f);
        }
    }
}
