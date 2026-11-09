using System.Diagnostics;
using NUnit.Framework;

namespace Prototypes.MedievalChess.Tests
{
    /// <summary>
    /// Perft counts every legal move sequence of a given length. The numbers are published
    /// (Chess Programming Wiki, "Perft Results", see docs/research/medieval-chess.md), and one wrong
    /// rule anywhere changes them, so matching them is a strong check of the move generator.
    /// </summary>
    public class PerftTests
    {
        const string Start = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        const string Kiwipete = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";
        const string Position3 = "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1";
        const string Position4 = "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1";
        const string Position4Mirrored = "r2q1rk1/pP1p2pp/Q4n2/bbp1p3/Np6/1B3NBn/pPPP1PPP/R3K2R b KQ - 0 1";
        const string Position5 = "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8";
        const string Position6 = "r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10";

        static void Check(string fen, int depth, long expected)
        {
            var position = Fen.Parse(fen);
            string before = Fen.Write(position);
            long nodes = Perft.Count(position, depth);
            Assert.AreEqual(expected, nodes, $"perft({depth}) of {fen}");
            Assert.AreEqual(before, Fen.Write(position), "perft must leave the position as it found it");
        }

        [TestCase(1, 20L)]
        [TestCase(2, 400L)]
        [TestCase(3, 8902L)]
        [TestCase(4, 197281L)]
        [TestCase(5, 4865609L)]
        public void The_starting_position(int depth, long expected) => Check(Start, depth, expected);

        [TestCase(1, 48L)]
        [TestCase(2, 2039L)]
        [TestCase(3, 97862L)]
        public void Kiwipete_which_has_castling_pins_and_en_passant_everywhere(int depth, long expected) =>
            Check(Kiwipete, depth, expected);

        [TestCase(1, 14L)]
        [TestCase(2, 191L)]
        [TestCase(3, 2812L)]
        [TestCase(4, 43238L)]
        public void Position_3_an_endgame_with_en_passant_and_pins(int depth, long expected) =>
            Check(Position3, depth, expected);

        [TestCase(1, 6L)]
        [TestCase(2, 264L)]
        [TestCase(3, 9467L)]
        public void Position_4_with_promotions_and_checks(int depth, long expected) => Check(Position4, depth, expected);

        [TestCase(1, 6L)]
        [TestCase(2, 264L)]
        [TestCase(3, 9467L)]
        public void Position_4_mirrored_gives_the_same_counts_for_black(int depth, long expected) =>
            Check(Position4Mirrored, depth, expected);

        [TestCase(1, 44L)]
        [TestCase(2, 1486L)]
        [TestCase(3, 62379L)]
        public void Position_5_with_a_promotion_that_gives_check(int depth, long expected) => Check(Position5, depth, expected);

        [TestCase(1, 46L)]
        [TestCase(2, 2079L)]
        [TestCase(3, 89890L)]
        public void Position_6_a_quiet_middlegame(int depth, long expected) => Check(Position6, depth, expected);

        [Test]
        public void All_of_it_runs_in_a_reasonable_time()
        {
            // The AI search needs a fast generator, so keep an eye on it. This is a loose limit.
            var watch = Stopwatch.StartNew();
            Perft.Count(Fen.Parse(Start), 4);
            Perft.Count(Fen.Parse(Kiwipete), 3);
            watch.Stop();
            Assert.Less(watch.ElapsedMilliseconds, 10000, $"took {watch.ElapsedMilliseconds} ms");
            TestContext.WriteLine($"perft(4) of the start plus perft(3) of Kiwipete: {watch.ElapsedMilliseconds} ms");
        }
    }
}
