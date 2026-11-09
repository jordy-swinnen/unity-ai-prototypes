using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Prototypes.MedievalChess.Tests
{
    /// <summary>
    /// The tracker follows the pieces on the board through every animated step. After each move the pieces it
    /// shows must be exactly the pieces the rules say are there, on the same squares.
    /// </summary>
    public class BoardTrackerTests
    {
        MoveSettings moves;
        FightSettings fights;

        [SetUp]
        public void SetUp()
        {
            moves = new MoveSettings();
            fights = new FightSettings();
        }

        // Plays moves on both the rules and the tracker, and checks they agree after every move.
        void Play(string fen, params string[] ucis)
        {
            var game = new ChessGame(Fen.Parse(fen));
            var tracker = new BoardTracker(game.Position);
            AssertMatches(tracker, game.Position, "at the start");
            foreach (var uci in ucis)
            {
                var before = game.Position.Clone();
                var move = game.LegalMoves.FirstOrDefault(m => m.ToUci() == uci);
                Assert.IsTrue(move.IsValid, $"{uci} is not legal in {Fen.Write(game.Position)}");
                Assert.IsTrue(game.TryMake(move));
                var sequence = tracker.Apply(before, move, moves, fights);
                Assert.Greater(sequence.Steps.Count, 0);
                AssertMatches(tracker, game.Position, $"after {uci}");
            }
        }

        static void AssertMatches(BoardTracker tracker, Position position, string when)
        {
            Assert.IsTrue(tracker.Matches(position, out string problem), $"{when}: {problem}");
        }

        // --- the start ---

        [Test]
        public void At_the_start_there_are_thirty_two_pieces_on_their_squares()
        {
            var tracker = new BoardTracker(Position.Start());
            Assert.AreEqual(32, tracker.VisibleCount);
            Assert.AreEqual("e2", tracker.IdAt[Squares.Parse("e2")]);
            Assert.IsNull(tracker.IdAt[Squares.Parse("e4")]);
            var knight = tracker.Actors["g1"];
            Assert.AreEqual(PieceType.Knight, knight.Type);
            Assert.AreEqual(0, knight.Side);
            Assert.AreEqual(0f, knight.Yaw);
            var queen = tracker.Actors["d8"];
            Assert.AreEqual(1, queen.Side);
            Assert.AreEqual(180f, queen.Yaw);
            Assert.AreEqual(BoardGeometry.Centre(Squares.Parse("g1")), knight.Position);
        }

        // --- quiet moves ---

        [Test]
        public void Ordinary_moves_keep_the_two_in_step()
        {
            Play(Fen.StartFen, "e2e4", "e7e5", "g1f3", "b8c6", "f1c4", "g8f6", "d2d3", "f8c5");
        }

        [Test]
        public void The_piece_keeps_its_id_as_it_moves()
        {
            var game = new ChessGame();
            var tracker = new BoardTracker(game.Position);
            var before = game.Position.Clone();
            var move = game.LegalMoves.First(m => m.ToUci() == "g1f3");
            game.TryMake(move);
            tracker.Apply(before, move, moves, fights);
            Assert.AreEqual("g1", tracker.IdAt[Squares.Parse("f3")]);
            Assert.IsNull(tracker.IdAt[Squares.Parse("g1")]);
        }

        // --- captures ---

        [Test]
        public void A_capture_removes_the_victim_and_lists_it_as_captured()
        {
            var game = new ChessGame(Fen.Parse("4k3/8/8/3p4/4P3/8/8/4K3 w - - 0 1"));
            var tracker = new BoardTracker(game.Position);
            var before = game.Position.Clone();
            var move = game.LegalMoves.First(m => m.ToUci() == "e4d5");
            game.TryMake(move);
            var seq = tracker.Apply(before, move, moves, fights);
            Assert.IsTrue(seq.Steps.OfType<FightPlan>().Any(), "a capture is a fight");
            Assert.AreEqual(1, tracker.Captured.Count);
            Assert.AreEqual(PieceType.Pawn, tracker.Captured[0].Type);
            Assert.AreEqual(1, tracker.Captured[0].Side);
            Assert.IsFalse(tracker.Captured[0].Visible);
            AssertMatches(tracker, game.Position, "after the capture");
        }

        [Test]
        public void En_passant_removes_the_pawn_beside_not_on_the_landing_square()
        {
            Play("4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 1", "e5d6");
            Play("4k3/8/8/8/3pP3/8/8/4K3 b - e3 0 1", "d4e3");
        }

        // --- castling ---

        [Test]
        public void Castling_moves_both_pieces_for_each_side_and_each_way()
        {
            Play("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1", "e1g1", "e8c8");
            Play("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1", "e1c1", "e8g8");
        }

        // --- promotion ---

        [Test]
        public void Promotion_makes_a_new_piece_of_the_chosen_kind()
        {
            foreach (var choice in new[] { "q", "r", "b", "n" })
            {
                var game = new ChessGame(Fen.Parse("8/P6k/8/8/8/8/8/K7 w - - 0 1"));
                var tracker = new BoardTracker(game.Position);
                var before = game.Position.Clone();
                var move = game.LegalMoves.First(m => m.ToUci() == "a7a8" + choice);
                game.TryMake(move);
                tracker.Apply(before, move, moves, fights);
                AssertMatches(tracker, game.Position, "after a7a8" + choice);
                var id = tracker.IdAt[Squares.Parse("a8")];
                Assert.AreEqual(move.Promotion, tracker.Actors[id].Type);
                Assert.IsFalse(tracker.Actors["a7"].Visible, "the pawn is gone");
            }
        }

        [Test]
        public void A_capture_that_promotes_does_both()
        {
            Play("1n5k/P7/8/8/8/8/8/K7 w - - 0 1", "a7b8q");
        }

        // --- check and mate ---

        [Test]
        public void A_check_is_followed_by_a_brace_and_a_mate_by_a_fall()
        {
            var game = new ChessGame(Fen.Parse("6k1/5ppp/8/8/8/8/8/R3K3 w - - 0 1"));
            var tracker = new BoardTracker(game.Position);
            var before = game.Position.Clone();
            var move = game.LegalMoves.First(m => m.ToUci() == "a1a8");
            game.TryMake(move);
            var seq = tracker.Apply(before, move, moves, fights);
            Assert.AreEqual("King falls", seq.Steps.Last().Name);
            AssertMatches(tracker, game.Position, "after the mate");
            Assert.LessOrEqual(tracker.Actors["g8"].Pitch, -80f, "the mated king is lying down");

            var game2 = new ChessGame(Fen.Parse("4k3/8/8/8/8/8/8/R3K3 w - - 0 1"));
            var tracker2 = new BoardTracker(game2.Position);
            var before2 = game2.Position.Clone();
            var check = game2.LegalMoves.First(m => m.ToUci() == "a1a8");
            game2.TryMake(check);
            var seq2 = tracker2.Apply(before2, check, moves, fights);
            Assert.AreEqual("King braces", seq2.Steps.Last().Name);
        }

        // --- whole games ---

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void A_whole_game_played_by_the_computer_stays_in_step_with_the_rules(int level)
        {
            var settings = ScriptableObject.CreateInstance<MedievalChessSettings>();
            try
            {
                var player = new ComputerPlayer(settings.evaluation);
                var profile = settings.difficulties[level].Clone();
                profile.depth = System.Math.Min(profile.depth, 2);
                profile.timeCapSeconds = 0.1f;
                profile.minThinkSeconds = 0f;
                var game = new ChessGame();
                var tracker = new BoardTracker(game.Position);
                var rng = new System.Random(level + 3);
                int plies = 0;
                var kinds = new HashSet<string>();
                while (!game.IsOver && plies < 160)
                {
                    var before = game.Position.Clone();
                    var move = player.Choose(game, profile, rng, default).Move;
                    Assert.IsTrue(game.TryMake(move), $"{move} is not legal");
                    var seq = tracker.Apply(before, move, moves, fights);
                    foreach (var step in seq.Steps) kinds.Add(step.Name.Split(' ')[0]);
                    AssertMatches(tracker, game.Position, $"after ply {plies + 1} ({move})");
                    plies++;
                }
                Assert.Greater(plies, 30);
                TestContext.WriteLine($"level {level}: {plies} plies, step kinds seen: {string.Join(", ", kinds)}");
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [Test]
        public void The_tracker_can_say_when_it_has_gone_wrong()
        {
            var game = new ChessGame();
            var tracker = new BoardTracker(game.Position);
            var other = Fen.Parse("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1");
            Assert.IsFalse(tracker.Matches(other, out string problem));
            StringAssert.Contains("e", problem);
        }
    }
}
