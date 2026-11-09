using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;

namespace Prototypes.MedievalChess.Tests
{
    public class SearchTests
    {
        const string Kiwipete = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";

        EvaluationSettings eval;

        [SetUp]
        public void SetUp()
        {
            eval = new EvaluationSettings();
        }

        SearchResult Run(string fen, int depth, bool quiescence = true, double cap = 30, int slack = 0, int seed = 1,
            int tableLog2 = 16, CancellationToken token = default)
        {
            var searcher = new Searcher(new Evaluator(eval), eval, tableLog2);
            var options = new SearchOptions { MaxDepth = depth, Quiescence = quiescence, TimeCapSeconds = cap, SlackCentipawns = slack };
            return searcher.Search(Fen.Parse(fen), new List<ulong>(), options, new System.Random(seed), token);
        }

        static bool HasMateInOne(Position p)
        {
            foreach (var m in MoveGenerator.Legal(p))
            {
                p.MakeMove(m);
                bool mate = p.InCheck() && MoveGenerator.Legal(p).Count == 0;
                p.UnmakeMove();
                if (mate) return true;
            }
            return false;
        }

        // --- the basics ---

        [Test]
        public void The_search_returns_a_legal_move_and_leaves_the_position_alone()
        {
            var p = Fen.Parse(Kiwipete);
            string before = Fen.Write(p);
            var result = new Searcher(new Evaluator(eval), eval, 16).Search(p, new List<ulong>(),
                new SearchOptions { MaxDepth = 3, Quiescence = true, TimeCapSeconds = 30 }, new System.Random(1), default);
            Assert.AreEqual(before, Fen.Write(p));
            Assert.IsTrue(MoveGenerator.Legal(p).Any(m => m.Equals(result.Best)), $"{result.Best} is not legal");
            Assert.AreEqual(3, result.Depth);
            Assert.Greater(result.Nodes, 100);
        }

        [Test]
        public void The_same_search_gives_the_same_answer_every_time()
        {
            var a = Run(Kiwipete, 3);
            var b = Run(Kiwipete, 3);
            Assert.AreEqual(a.Best, b.Best);
            Assert.AreEqual(a.Score, b.Score);
            Assert.AreEqual(a.Nodes, b.Nodes);
        }

        [Test]
        public void With_no_legal_moves_there_is_nothing_to_return_and_the_score_says_why()
        {
            var mated = Run("rnb1kbnr/pppp1ppp/8/4p3/6Pq/5P2/PPPPP2P/RNBQKBNR w KQkq - 1 3", 3);
            Assert.IsFalse(mated.Best.IsValid);
            Assert.AreEqual(-eval.mateScore, mated.Score);
            var stalemate = Run("7k/5Q2/6K1/8/8/8/8/8 b - - 0 1", 3);
            Assert.IsFalse(stalemate.Best.IsValid);
            Assert.AreEqual(0, stalemate.Score);
        }

        // --- tactics: the three levels must see what they are meant to ---

        [TestCase("6k1/5ppp/8/8/8/8/8/R3K3 w - - 0 1", "a1a8")]
        [TestCase("r1bqkb1r/pppp1ppp/2n2n2/4p2Q/2B1P3/8/PPPP1PPP/RNB1K1NR w KQkq - 4 4", "h5f7")]
        [TestCase("rnbqkbnr/pppp1ppp/8/4p3/6P1/5P2/PPPPP2P/RNBQKBNR b KQkq - 0 2", "d8h4")]
        public void A_mate_in_one_is_found_at_every_depth_from_one(string fen, string mate)
        {
            foreach (int depth in new[] { 1, 2, 3, 4 })
            {
                var result = Run(fen, depth);
                Assert.AreEqual(mate, result.Best.ToUci(), $"depth {depth}");
                Assert.AreEqual(eval.mateScore - 1, result.Score, $"depth {depth}: mate in one is a mate on the next ply");
            }
        }

        [Test]
        public void A_mate_in_one_is_chosen_over_a_slower_mate()
        {
            // two rooks: Ra8 is mate at once, other rook moves also lead to mate sooner or later
            var result = Run("7k/R7/8/8/8/8/1R6/4K3 w - - 0 1", 4);
            var p = Fen.Parse("7k/R7/8/8/8/8/1R6/4K3 w - - 0 1");
            p.MakeMove(MoveGenerator.Legal(p).First(m => m.Equals(result.Best)));
            Assert.IsTrue(p.InCheck() && MoveGenerator.Legal(p).Count == 0, $"{result.Best} should be checkmate");
        }

        [Test]
        public void A_forced_mate_in_two_is_found_with_a_three_ply_search()
        {
            const string fen = "7k/8/8/8/8/8/R7/1R2K3 w - - 0 1";
            var result = Run(fen, 3);
            var p = Fen.Parse(fen);
            p.MakeMove(MoveGenerator.Legal(p).First(m => m.Equals(result.Best)));
            foreach (var reply in MoveGenerator.Legal(p))
            {
                p.MakeMove(reply);
                Assert.IsTrue(HasMateInOne(p), $"after {result.Best} and {reply} white should have a mate in one");
                p.UnmakeMove();
            }
            Assert.AreEqual(eval.mateScore - 3, result.Score);
        }

        [Test]
        public void A_free_piece_is_taken()
        {
            foreach (int depth in new[] { 1, 2, 3 })
                Assert.AreEqual("d1d5", Run("4k3/8/8/3n4/8/8/8/3QK3 w - - 0 1", depth).Best.ToUci(), $"depth {depth}");
        }

        [Test]
        public void A_defended_pawn_is_not_worth_the_queen()
        {
            foreach (int depth in new[] { 2, 3, 4 })
                Assert.AreNotEqual("d2d5", Run("4k3/8/2p5/3p4/8/8/3Q4/4K3 w - - 0 1", depth).Best.ToUci(), $"depth {depth}");
        }

        [Test]
        public void An_attacked_queen_is_moved_or_used_not_left_to_be_taken()
        {
            const string fen = "4k3/8/8/8/3q4/2P5/8/4K3 b - - 0 1";
            foreach (int depth in new[] { 2, 3, 4 })
            {
                var p = Fen.Parse(fen);
                var best = Run(fen, depth).Best;
                p.MakeMove(MoveGenerator.Legal(p).First(m => m.Equals(best)));
                bool queenCanBeTaken = MoveGenerator.Legal(p).Any(m => m.IsCapture && Pieces.TypeOf(p[m.To]) == PieceType.Queen);
                Assert.IsFalse(queenCanBeTaken, $"depth {depth}: {best} leaves the queen to be taken");
            }
        }

        [Test]
        public void A_shallow_search_misses_a_trap_that_a_deeper_one_sees()
        {
            // Qxb7 wins a pawn, but the pawn is defended by the rook on b8, so the queen is lost. A one-move search grabs it.
            const string fen = "1r2k3/1p6/8/8/8/8/1Q6/4K3 w - - 0 1";
            var shallow = Run(fen, 1, quiescence: false);
            Assert.AreEqual("b2b7", shallow.Best.ToUci());
            var deep = Run(fen, 4);
            Assert.AreNotEqual("b2b7", deep.Best.ToUci());
        }

        [Test]
        public void A_dead_drawn_position_scores_zero()
        {
            Assert.AreEqual(0, Run("4k3/8/8/8/8/8/8/4K3 w - - 0 1", 3).Score);
        }

        // --- the clock and cancelling ---

        [Test]
        public void The_time_cap_is_respected()
        {
            var watch = Stopwatch.StartNew();
            var result = Run(Kiwipete, 20, cap: 0.3);
            watch.Stop();
            Assert.Less(watch.Elapsed.TotalSeconds, 0.8, $"took {watch.Elapsed.TotalSeconds:F2} s for a 0.3 s cap");
            Assert.GreaterOrEqual(result.Depth, 1);
            Assert.IsTrue(result.Best.IsValid);
            TestContext.WriteLine($"cap 0.3 s: depth {result.Depth}, {result.Nodes} nodes, {watch.ElapsedMilliseconds} ms");
        }

        [Test]
        public void A_cancelled_search_stops_at_once_and_still_returns_a_legal_move()
        {
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();
                var watch = Stopwatch.StartNew();
                var result = Run(Kiwipete, 20, token: cts.Token);
                watch.Stop();
                Assert.Less(watch.ElapsedMilliseconds, 200);
                Assert.IsTrue(result.Best.IsValid);
                Assert.IsTrue(result.Stopped);
            }
        }

        [Test]
        public void A_search_cancelled_part_way_stops_soon_after()
        {
            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(150);
                var watch = Stopwatch.StartNew();
                var result = Run(Kiwipete, 30, token: cts.Token);
                watch.Stop();
                Assert.Less(watch.ElapsedMilliseconds, 700);
                Assert.IsTrue(result.Best.IsValid);
                Assert.IsTrue(result.Stopped);
            }
        }

        [Test]
        public void A_deeper_search_visits_more_nodes_and_the_depth_is_reported()
        {
            var shallow = Run(Kiwipete, 2);
            var deeper = Run(Kiwipete, 4);
            Assert.Greater(deeper.Nodes, shallow.Nodes);
            Assert.AreEqual(2, shallow.Depth);
            Assert.AreEqual(4, deeper.Depth);
        }

        [Test]
        public void Slack_gives_a_choice_of_near_best_moves_and_zero_slack_gives_the_best_only()
        {
            var none = Run(Kiwipete, 2, slack: 0);
            Assert.AreEqual(1, none.Candidates.Count);
            var loose = Run(Kiwipete, 2, slack: 300);
            Assert.Greater(loose.Candidates.Count, 1);
            Assert.IsTrue(loose.Candidates.Any(m => m.Equals(loose.BestByScore)));
        }

        [Test]
        public void Slack_never_trades_away_a_mate()
        {
            var result = Run("6k1/5ppp/8/8/8/8/8/R3K3 w - - 0 1", 1, slack: 5000);
            Assert.AreEqual(1, result.Candidates.Count);
            Assert.AreEqual("a1a8", result.Best.ToUci());
        }
    }
}
