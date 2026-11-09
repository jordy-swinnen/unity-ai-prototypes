using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Prototypes.MedievalChess.Tests
{
    /// <summary>The special rules of chess, one case at a time.</summary>
    public class ChessRulesTests
    {
        static List<Move> Legal(string fen) => MoveGenerator.Legal(Fen.Parse(fen));

        static IEnumerable<string> Ucis(string fen) => Legal(fen).Select(m => m.ToUci());

        static Position Play(string fen, params string[] ucis)
        {
            var p = Fen.Parse(fen);
            foreach (var uci in ucis)
            {
                var move = MoveGenerator.Legal(p).FirstOrDefault(m => m.ToUci() == uci);
                Assert.IsTrue(move.IsValid, $"{uci} is not legal in {Fen.Write(p)}");
                p.MakeMove(move);
            }
            return p;
        }

        // --- castling ---

        [Test]
        public void Both_castles_are_legal_when_the_way_is_clear_and_safe()
        {
            var moves = Ucis("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1").ToList();
            CollectionAssert.Contains(moves, "e1g1");
            CollectionAssert.Contains(moves, "e1c1");
        }

        [Test]
        public void Castling_moves_the_king_two_squares_and_the_rook_beside_it()
        {
            var king = Play("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1", "e1g1");
            Assert.AreEqual("r3k2r/8/8/8/8/8/8/R4RK1 b kq - 1 1", Fen.Write(king));
            var queen = Play("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1", "e1c1");
            Assert.AreEqual("r3k2r/8/8/8/8/8/8/2KR3R b kq - 1 1", Fen.Write(queen));
            var black = Play("r3k2r/8/8/8/8/8/8/R3K2R b KQkq - 0 1", "e8c8");
            Assert.AreEqual("2kr3r/8/8/8/8/8/8/R3K2R w KQ - 1 2", Fen.Write(black));
        }

        [Test]
        public void Castling_is_not_allowed_through_an_attacked_square()
        {
            // a black rook on f8 covers f1, so the king cannot pass it
            var moves = Ucis("r3kr2/8/8/8/8/8/8/R3K2R w KQq - 0 1").ToList();
            CollectionAssert.DoesNotContain(moves, "e1g1");
            CollectionAssert.Contains(moves, "e1c1");
        }

        [Test]
        public void Castling_is_not_allowed_out_of_check()
        {
            var moves = Ucis("4r1k1/8/8/8/8/8/8/R3K2R w KQ - 0 1").ToList();
            CollectionAssert.DoesNotContain(moves, "e1g1");
            CollectionAssert.DoesNotContain(moves, "e1c1");
        }

        [Test]
        public void Castling_is_not_allowed_into_check()
        {
            var moves = Ucis("2k3r1/8/8/8/8/8/8/R3K2R w KQ - 0 1").ToList();
            CollectionAssert.DoesNotContain(moves, "e1g1");
            CollectionAssert.Contains(moves, "e1c1");
        }

        [Test]
        public void The_rook_may_pass_through_an_attacked_square_when_castling_queenside()
        {
            // b1 is attacked, but only the rook crosses it
            var moves = Ucis("1r4k1/8/8/8/8/8/8/R3K2R w KQ - 0 1").ToList();
            CollectionAssert.Contains(moves, "e1c1");
        }

        [Test]
        public void Castling_needs_clear_squares_between_the_king_and_the_rook()
        {
            var moves = Ucis("r3k2r/8/8/8/8/8/8/RN2K1NR w KQkq - 0 1").ToList();
            CollectionAssert.DoesNotContain(moves, "e1g1");
            CollectionAssert.DoesNotContain(moves, "e1c1");
        }

        [Test]
        public void Castling_rights_go_when_the_king_or_a_rook_moves_or_a_rook_is_taken()
        {
            Assert.AreEqual("r3k2r/8/8/8/8/8/8/R4K1R b kq - 1 1", Fen.Write(Play("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1", "e1f1")));
            Assert.AreEqual("r3k2r/8/8/8/8/8/8/R3K1R1 b Qkq - 1 1", Fen.Write(Play("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1", "h1g1")));
            Assert.AreEqual("R3k2r/8/8/8/8/8/8/4K2R b Kk - 0 1", Fen.Write(Play("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1", "a1a8")),
                "white loses the queenside right (the rook moved) and black loses its queenside right (the rook was taken)");
        }

        [Test]
        public void Castling_rights_stay_gone_after_the_rook_returns()
        {
            var p = Play("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1", "h1h2", "a8a7", "h2h1", "a7a8");
            Assert.AreEqual("r3k2r/8/8/8/8/8/8/R3K2R w Qk - 4 3", Fen.Write(p));
        }

        // --- en passant ---

        [Test]
        public void A_double_push_opens_an_en_passant_capture_for_one_move_only()
        {
            var p = Play("4k3/3p4/8/4P3/8/8/8/4K3 b - - 0 1", "d7d5");
            Assert.AreEqual(Squares.Parse("d6"), p.EnPassant);
            CollectionAssert.Contains(MoveGenerator.Legal(p).Select(m => m.ToUci()).ToList(), "e5d6");

            var later = Play(Fen.Write(p), "e1e2", "e8e7");
            Assert.AreEqual(-1, later.EnPassant);
            CollectionAssert.DoesNotContain(MoveGenerator.Legal(later).Select(m => m.ToUci()).ToList(), "e5d6");
        }

        [Test]
        public void En_passant_removes_the_pawn_beside_not_the_one_on_the_landing_square()
        {
            var p = Play("4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 1", "e5d6");
            Assert.AreEqual("4k3/8/3P4/8/8/8/8/4K3 b - - 0 1", Fen.Write(p));
        }

        [Test]
        public void En_passant_works_for_black_too()
        {
            var p = Play("4k3/8/8/8/3pP3/8/8/4K3 b - e3 0 1", "d4e3");
            Assert.AreEqual("4k3/8/8/8/8/4p3/8/4K3 w - - 0 2", Fen.Write(p));
        }

        [Test]
        public void En_passant_is_illegal_when_it_would_expose_the_king_along_the_rank()
        {
            // both pawns leave the fifth rank, and the rook on h5 would then attack the king on a5
            var moves = Ucis("8/8/8/K2pP2r/8/8/8/4k3 w - d6 0 1").ToList();
            CollectionAssert.DoesNotContain(moves, "e5d6");
            CollectionAssert.Contains(moves, "e5e6");
        }

        [Test]
        public void En_passant_may_be_the_move_that_answers_a_check()
        {
            // the pawn that has just pushed two squares gives check to the king on e5, and can be taken en passant
            var p = Fen.Parse("8/8/8/4k3/2pP4/8/8/4K3 b - d3 0 1");
            Assert.IsTrue(p.InCheck());
            CollectionAssert.Contains(MoveGenerator.Legal(p).Select(m => m.ToUci()).ToList(), "c4d3");
        }

        // --- promotion ---

        [Test]
        public void A_pawn_on_the_seventh_rank_promotes_to_each_of_four_pieces()
        {
            var moves = Legal("8/P6k/8/8/8/8/8/K7 w - - 0 1").Where(m => m.From == Squares.Parse("a7")).ToList();
            Assert.AreEqual(4, moves.Count);
            CollectionAssert.AreEquivalent(new[] { "a7a8q", "a7a8r", "a7a8b", "a7a8n" }, moves.Select(m => m.ToUci()).ToArray());
        }

        [Test]
        public void A_pawn_may_capture_and_promote_in_one_move()
        {
            var moves = Legal("1n5k/P7/8/8/8/8/8/K7 w - - 0 1").Where(m => m.From == Squares.Parse("a7")).ToList();
            Assert.AreEqual(8, moves.Count, "four promotions straight ahead and four capturing the knight");
            Assert.AreEqual(4, moves.Count(m => m.Flags.HasFlag(MoveFlags.Capture)));
        }

        [TestCase("a7a8q", PieceType.Queen)]
        [TestCase("a7a8r", PieceType.Rook)]
        [TestCase("a7a8b", PieceType.Bishop)]
        [TestCase("a7a8n", PieceType.Knight)]
        public void The_promoted_piece_replaces_the_pawn(string uci, PieceType type)
        {
            var p = Play("8/P6k/8/8/8/8/8/K7 w - - 0 1", uci);
            Assert.AreEqual(Pieces.Make(type, ChessSide.White), p[Squares.Parse("a8")]);
            Assert.AreEqual(Pieces.Empty, p[Squares.Parse("a7")]);
        }

        [Test]
        public void Black_promotes_on_the_first_rank()
        {
            var p = Play("4k3/8/8/8/8/8/p7/4K3 b - - 0 1", "a2a1q");
            Assert.AreEqual(Pieces.Make(PieceType.Queen, ChessSide.Black), p[Squares.Parse("a1")]);
        }

        // --- pins, checks and legality ---

        [Test]
        public void A_pinned_piece_cannot_leave_the_line()
        {
            var moves = Ucis("4r1k1/8/8/8/8/8/4N3/4K3 w - - 0 1").Where(u => u.StartsWith("e2")).ToList();
            Assert.IsEmpty(moves, "the knight is pinned to the king");
        }

        [Test]
        public void A_pinned_rook_can_still_move_along_the_pin()
        {
            var moves = Ucis("4r1k1/8/8/8/8/8/4R3/4K3 w - - 0 1").Where(u => u.StartsWith("e2")).ToList();
            CollectionAssert.AreEquivalent(new[] { "e2e3", "e2e4", "e2e5", "e2e6", "e2e7", "e2e8" }, moves);
        }

        [Test]
        public void In_check_only_moves_that_end_the_check_are_legal()
        {
            // a rook gives check along the e file: the king steps aside, or the bishop blocks on e3
            var moves = Ucis("4r1k1/8/8/8/8/8/3B4/R3K3 w - - 0 1").ToList();
            CollectionAssert.AreEquivalent(new[] { "e1d1", "e1f1", "e1f2", "d2e3" }, moves);
        }

        [Test]
        public void A_king_cannot_step_next_to_the_other_king()
        {
            var moves = Ucis("8/8/8/8/8/3k4/8/3K4 w - - 0 1").ToList();
            CollectionAssert.DoesNotContain(moves, "d1d2");
            CollectionAssert.DoesNotContain(moves, "d1c2");
            CollectionAssert.DoesNotContain(moves, "d1e2");
            CollectionAssert.Contains(moves, "d1c1");
            CollectionAssert.Contains(moves, "d1e1");
        }

        [Test]
        public void A_king_in_check_from_an_undefended_rook_can_take_it_or_step_off_its_lines()
        {
            // the rook on d2 gives check; c2 and e2 are on its rank, but c1 and e1 are safe and d2 can be taken
            var moves = Ucis("8/8/8/8/8/8/k2r4/3K4 w - - 0 1").ToList();
            CollectionAssert.AreEquivalent(new[] { "d1d2", "d1c1", "d1e1" }, moves);
        }

        [Test]
        public void Pawns_push_one_or_two_squares_from_the_start_but_not_through_a_piece()
        {
            var moves = Ucis("4k3/8/8/8/8/4n3/4P3/4K3 w - - 0 1").Where(u => u.StartsWith("e2")).ToList();
            Assert.IsEmpty(moves, "the knight on e3 blocks both pushes");
            var open = Ucis("4k3/8/8/8/8/8/4P3/4K3 w - - 0 1").Where(u => u.StartsWith("e2")).ToList();
            CollectionAssert.AreEquivalent(new[] { "e2e3", "e2e4" }, open);
        }

        [Test]
        public void Pawns_capture_diagonally_only_and_only_enemies()
        {
            var moves = Ucis("4k3/8/8/8/3pP3/3NP3/8/4K3 w - - 0 1").Where(u => u.StartsWith("e3") || u.StartsWith("e4")).ToList();
            CollectionAssert.Contains(moves, "e4e5");
            CollectionAssert.DoesNotContain(moves, "e4d4", "pawns do not capture sideways");
            CollectionAssert.Contains(moves, "e3d4", "the pawn on e3 takes the black pawn on d4");
            CollectionAssert.DoesNotContain(moves, "e3e4", "a pawn cannot move onto its own piece");
        }

        [Test]
        public void Moves_carry_the_right_flags()
        {
            var p = Fen.Parse("r3k2r/8/8/3pP3/8/8/8/R3K2R w KQkq d6 0 1");
            var moves = MoveGenerator.Legal(p);
            Assert.IsTrue(moves.First(m => m.ToUci() == "e5d6").Flags.HasFlag(MoveFlags.EnPassant | MoveFlags.Capture));
            Assert.IsTrue(moves.First(m => m.ToUci() == "e1g1").Flags.HasFlag(MoveFlags.CastleKing));
            Assert.IsTrue(moves.First(m => m.ToUci() == "e1c1").Flags.HasFlag(MoveFlags.CastleQueen));
            Assert.IsTrue(moves.First(m => m.ToUci() == "a1a8").Flags.HasFlag(MoveFlags.Capture));
            var start = MoveGenerator.Legal(Position.Start());
            Assert.IsTrue(start.First(m => m.ToUci() == "e2e4").Flags.HasFlag(MoveFlags.DoublePush));
            Assert.IsFalse(start.First(m => m.ToUci() == "e2e3").Flags.HasFlag(MoveFlags.DoublePush));
        }
    }
}
