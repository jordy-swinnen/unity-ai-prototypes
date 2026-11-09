using NUnit.Framework;

namespace Prototypes.MedievalChess.Tests
{
    public class FenTests
    {
        const string Start = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        [TestCase(Start)]
        [TestCase("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1")]
        [TestCase("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1")]
        [TestCase("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8")]
        [TestCase("rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR w KQkq e6 0 2")]
        [TestCase("4k3/8/8/8/8/8/8/R3K2R b K - 37 80")]
        public void A_fen_survives_a_round_trip(string fen)
        {
            Assert.AreEqual(fen, Fen.Write(Fen.Parse(fen)));
        }

        [Test]
        public void The_starting_position_is_the_standard_one()
        {
            var p = Position.Start();
            Assert.AreEqual(Start, Fen.Write(p));
            Assert.AreEqual(ChessSide.White, p.SideToMove);
            Assert.AreEqual(Pieces.Make(PieceType.King, ChessSide.White), p[Squares.Parse("e1")]);
            Assert.AreEqual(Pieces.Make(PieceType.Queen, ChessSide.Black), p[Squares.Parse("d8")]);
            Assert.AreEqual(Pieces.Empty, p[Squares.Parse("e4")]);
        }

        [Test]
        public void Fields_are_read_correctly()
        {
            var p = Fen.Parse("r3k2r/8/8/8/8/8/8/R3K2R b Kq e3 12 34");
            Assert.AreEqual(ChessSide.Black, p.SideToMove);
            Assert.AreEqual(Squares.Parse("e3"), p.EnPassant);
            Assert.AreEqual(12, p.HalfmoveClock);
            Assert.AreEqual(34, p.FullmoveNumber);
            Assert.IsTrue(p.CanCastle(ChessSide.White, kingSide: true));
            Assert.IsFalse(p.CanCastle(ChessSide.White, kingSide: false));
            Assert.IsFalse(p.CanCastle(ChessSide.Black, kingSide: true));
            Assert.IsTrue(p.CanCastle(ChessSide.Black, kingSide: false));
        }

        [Test]
        public void The_move_counters_may_be_left_out()
        {
            var p = Fen.Parse("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - -");
            Assert.AreEqual(0, p.HalfmoveClock);
            Assert.AreEqual(1, p.FullmoveNumber);
        }

        [TestCase("")]
        [TestCase("not a fen")]
        [TestCase("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP w KQkq - 0 1")]
        [TestCase("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR x KQkq - 0 1")]
        [TestCase("rnbqkbnr/pppppppp/9/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")]
        [TestCase("rnbqkbnr/pppppppp/7/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")]
        [TestCase("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNZ w KQkq - 0 1")]
        [TestCase("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq z9 0 1")]
        [TestCase("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - x 1")]
        [TestCase("rnbq1bnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")]
        [TestCase("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKKNR w KQkq - 0 1")]
        [TestCase("Pnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")]
        [TestCase("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNp w KQkq - 0 1")]
        public void A_bad_fen_is_refused_with_a_clear_message(string fen)
        {
            var ex = Assert.Throws<System.FormatException>(() => Fen.Parse(fen));
            Assert.IsNotEmpty(ex.Message);
        }

        [Test]
        public void Squares_have_names_and_names_have_squares()
        {
            Assert.AreEqual(0, Squares.Parse("a1"));
            Assert.AreEqual(7, Squares.Parse("h1"));
            Assert.AreEqual(8, Squares.Parse("a2"));
            Assert.AreEqual(63, Squares.Parse("h8"));
            for (int sq = 0; sq < 64; sq++) Assert.AreEqual(sq, Squares.Parse(Squares.Name(sq)));
            Assert.Throws<System.FormatException>(() => Squares.Parse("i1"));
            Assert.Throws<System.FormatException>(() => Squares.Parse("a9"));
        }
    }
}
