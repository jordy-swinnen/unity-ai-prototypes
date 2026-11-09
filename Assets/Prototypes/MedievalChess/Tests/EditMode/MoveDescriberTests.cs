using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Prototypes.MedievalChess.Tests
{
    public class MoveDescriberTests
    {
        static MoveDescription Describe(string fen, string uci)
        {
            var position = Fen.Parse(fen);
            var tracker = new BoardTracker(position);
            var move = MoveGenerator.Legal(position).First(m => m.ToUci() == uci);
            return MoveDescriber.Describe(position, move, tracker);
        }

        [Test]
        public void A_quiet_move_has_positions_ids_yaws_and_no_flags()
        {
            var d = Describe(Fen.StartFen, "g1f3");
            Assert.AreEqual("g1", d.PieceId);
            Assert.AreEqual(PieceType.Knight, d.Piece);
            Assert.AreEqual(0, d.Side);
            Assert.AreEqual(BoardGeometry.Centre(Squares.Parse("g1")), d.From);
            Assert.AreEqual(BoardGeometry.Centre(Squares.Parse("f3")), d.To);
            Assert.AreEqual(0f, d.Yaw);
            Assert.IsFalse(d.Capture || d.Castle || d.Promotes || d.Check || d.Checkmate || d.EnPassant);
        }

        [Test]
        public void Black_pieces_face_the_other_way()
        {
            var d = Describe("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1", "g8f6");
            Assert.AreEqual(1, d.Side);
            Assert.AreEqual(180f, d.Yaw);
        }

        [Test]
        public void A_capture_names_the_victim_and_where_it_stands()
        {
            var d = Describe("4k3/8/8/3p4/4P3/8/8/4K3 w - - 0 1", "e4d5");
            Assert.IsTrue(d.Capture);
            Assert.IsFalse(d.EnPassant);
            Assert.AreEqual("d5", d.VictimId);
            Assert.AreEqual(PieceType.Pawn, d.Victim);
            Assert.AreEqual(BoardGeometry.Centre(Squares.Parse("d5")), d.VictimAt);
            Assert.AreEqual(180f, d.VictimYaw);
        }

        [Test]
        public void En_passant_puts_the_victim_beside_not_on_the_landing_square()
        {
            var d = Describe("4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 1", "e5d6");
            Assert.IsTrue(d.Capture && d.EnPassant);
            Assert.AreEqual("d5", d.VictimId);
            Assert.AreEqual(BoardGeometry.Centre(Squares.Parse("d5")), d.VictimAt);
            Assert.AreEqual(BoardGeometry.Centre(Squares.Parse("d6")), d.To);

            var black = Describe("4k3/8/8/8/3pP3/8/8/4K3 b - e3 0 1", "d4e3");
            Assert.AreEqual("e4", black.VictimId);
            Assert.AreEqual(BoardGeometry.Centre(Squares.Parse("e4")), black.VictimAt);
        }

        [Test]
        public void Castling_names_the_rook_and_where_it_goes()
        {
            var king = Describe("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1", "e1g1");
            Assert.IsTrue(king.Castle);
            Assert.AreEqual("h1", king.RookId);
            Assert.AreEqual(BoardGeometry.Centre(Squares.Parse("h1")), king.RookFrom);
            Assert.AreEqual(BoardGeometry.Centre(Squares.Parse("f1")), king.RookTo);

            var queen = Describe("r3k2r/8/8/8/8/8/8/R3K2R b KQkq - 0 1", "e8c8");
            Assert.AreEqual("a8", queen.RookId);
            Assert.AreEqual(BoardGeometry.Centre(Squares.Parse("d8")), queen.RookTo);
        }

        [Test]
        public void Promotion_says_which_piece_and_gives_the_new_piece_its_own_id()
        {
            var d = Describe("8/P6k/8/8/8/8/8/K7 w - - 0 1", "a7a8n");
            Assert.IsTrue(d.Promotes);
            Assert.AreEqual(PieceType.Knight, d.PromoteTo);
            Assert.AreNotEqual(d.PieceId, d.PromotedId);
            Assert.IsNotEmpty(d.PromotedId);
        }

        [Test]
        public void Check_and_mate_name_the_enemy_king()
        {
            var check = Describe("4k3/8/8/8/8/8/8/R3K3 w - - 0 1", "a1a8");
            Assert.IsTrue(check.Check);
            Assert.IsFalse(check.Checkmate);
            Assert.AreEqual("e8", check.KingId);
            Assert.AreEqual(1, check.KingSide);
            Assert.AreEqual(180f, check.KingYaw);
            Assert.AreEqual(BoardGeometry.Centre(Squares.Parse("e8")), check.KingAt);

            var mate = Describe("6k1/5ppp/8/8/8/8/8/R3K3 w - - 0 1", "a1a8");
            Assert.IsTrue(mate.Check && mate.Checkmate);
            Assert.AreEqual("g8", mate.KingId);

            var quiet = Describe("4k3/8/8/8/8/8/8/R3K3 w - - 0 1", "a1a2");
            Assert.IsFalse(quiet.Check);
        }

        [Test]
        public void Describing_does_not_change_the_position()
        {
            var position = Fen.Parse("6k1/5ppp/8/8/8/8/8/R3K3 w - - 0 1");
            string before = Fen.Write(position);
            var tracker = new BoardTracker(position);
            MoveDescriber.Describe(position, MoveGenerator.Legal(position).First(m => m.ToUci() == "a1a8"), tracker);
            Assert.AreEqual(before, Fen.Write(position));
        }
    }
}
