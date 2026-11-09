using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;

namespace Prototypes.MedievalChess.Tests
{
    public class ComputerPlayerTests
    {
        const string Kiwipete = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";

        MedievalChessSettings settings;
        ComputerPlayer player;

        [SetUp]
        public void SetUp()
        {
            settings = UnityEngine.ScriptableObject.CreateInstance<MedievalChessSettings>();
            player = new ComputerPlayer(settings.evaluation);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(settings);
        }

        DifficultyProfile Squire => settings.difficulties[0];
        DifficultyProfile Knight => settings.difficulties[1];
        DifficultyProfile Marshal => settings.difficulties[2];

        // A copy of a profile that is quick enough for a test, and has no pause for show.
        static DifficultyProfile Quick(DifficultyProfile p, int maxDepth, double cap)
        {
            var q = p.Clone();
            q.depth = System.Math.Min(p.depth, maxDepth);
            q.timeCapSeconds = (float)System.Math.Min(p.timeCapSeconds, cap);
            q.minThinkSeconds = 0f;
            return q;
        }

        // --- the three levels ---

        [Test]
        public void There_are_three_levels_each_deeper_and_more_careful_than_the_last()
        {
            Assert.AreEqual(3, settings.difficulties.Length);
            CollectionAssert.AreEqual(new[] { "Squire", "Knight", "Marshal" }, settings.difficulties.Select(d => d.name).ToArray());
            Assert.Less(Squire.depth, Knight.depth);
            Assert.Less(Knight.depth, Marshal.depth);
            Assert.Greater(Squire.slackCentipawns, Knight.slackCentipawns);
            Assert.Greater(Knight.slackCentipawns, Marshal.slackCentipawns);
            Assert.AreEqual(0, Marshal.slackCentipawns);
            Assert.Greater(Squire.blunderOneIn, 0);
            Assert.AreEqual(0, Knight.blunderOneIn);
            Assert.AreEqual(0, Marshal.blunderOneIn);
            Assert.Less(Knight.timeCapSeconds, Marshal.timeCapSeconds);
            Assert.Greater(Squire.minThinkSeconds, 0f, "the easy level pauses so its move is easy to follow");
        }

        [Test]
        public void The_spec_numbers_are_the_starting_values()
        {
            Assert.AreEqual(1, Squire.depth);
            Assert.AreEqual(120, Squire.slackCentipawns);
            Assert.AreEqual(7, Squire.blunderOneIn);
            Assert.AreEqual(3, Knight.depth);
            Assert.IsTrue(Knight.quiescence);
            Assert.AreEqual(10, Knight.slackCentipawns);
            Assert.AreEqual(1.5f, Knight.timeCapSeconds, 0.001f);
            Assert.AreEqual(8, Marshal.depth, "measured: five plies take about a second, so the 3 s cap allows six to eight");
            Assert.AreEqual(3f, Marshal.timeCapSeconds, 0.001f);
            Assert.AreEqual(18, Marshal.tableLog2);
        }

        // --- every move is legal ---

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void Every_level_plays_only_legal_moves_through_a_whole_game_against_itself(int level)
        {
            var profile = Quick(settings.difficulties[level], 2, 0.1);
            var game = new ChessGame();
            var rng = new System.Random(level + 10);
            int plies = 0;
            while (!game.IsOver && plies < 70)
            {
                var choice = player.Choose(game, profile, rng, default);
                Assert.IsTrue(choice.Move.IsValid, $"no move in {Fen.Write(game.Position)}");
                Assert.IsTrue(game.TryMake(choice.Move), $"{choice.Move} is not legal in {Fen.Write(game.Position)}");
                plies++;
            }
            Assert.Greater(plies, 4, "a game of random-ish play can end quickly, but not at once");
        }

        [Test]
        public void With_no_legal_moves_the_player_returns_no_move()
        {
            var game = new ChessGame(Fen.Parse("rnb1kbnr/pppp1ppp/8/4p3/6Pq/5P2/PPPPP2P/RNBQKBNR w KQkq - 1 3"));
            Assert.IsTrue(game.IsOver);
            Assert.IsFalse(player.Choose(game, Knight, new System.Random(1), default).Move.IsValid);
        }

        // --- what each level sees ---

        [Test]
        public void Knight_and_Marshal_find_a_mate_in_one_and_so_does_Squire()
        {
            var game = new ChessGame(Fen.Parse("6k1/5ppp/8/8/8/8/8/R3K3 w - - 0 1"));
            foreach (int level in new[] { 0, 1, 2 })
                Assert.AreEqual("a1a8", player.Choose(game, Quick(settings.difficulties[level], 3, 0.3), new System.Random(5), default).Move.ToUci(),
                    settings.difficulties[level].name);
        }

        [Test]
        public void Knight_and_Marshal_do_not_throw_away_a_queen_for_a_defended_pawn()
        {
            var game = new ChessGame(Fen.Parse("4k3/8/2p5/3p4/8/8/3Q4/4K3 w - - 0 1"));
            foreach (int seed in Enumerable.Range(1, 12))
            {
                Assert.AreNotEqual("d2d5", player.Choose(game, Quick(Knight, 3, 0.3), new System.Random(seed), default).Move.ToUci());
                Assert.AreNotEqual("d2d5", player.Choose(game, Quick(Marshal, 4, 0.3), new System.Random(seed), default).Move.ToUci());
            }
        }

        [Test]
        public void Squire_sometimes_plays_worse_than_the_best_move_and_Marshal_never_does()
        {
            var game = new ChessGame(Fen.Parse(Kiwipete));
            var best = player.Choose(game, Quick(Marshal, 3, 1), new System.Random(1), default).Search.BestByScore;
            int squireOffBest = 0, marshalOffBest = 0;
            var squireMoves = new HashSet<string>();
            for (int seed = 0; seed < 60; seed++)
            {
                var squire = player.Choose(game, Quick(Squire, 1, 1), new System.Random(seed), default);
                squireMoves.Add(squire.Move.ToUci());
                if (!squire.Move.Equals(squire.Search.BestByScore)) squireOffBest++;
                var marshal = player.Choose(game, Quick(Marshal, 3, 1), new System.Random(seed), default);
                if (!marshal.Move.Equals(best)) marshalOffBest++;
            }
            Assert.Greater(squireOffBest, 3, "Squire should make slips");
            Assert.Less(squireOffBest, 58, "but it should not be random");
            Assert.Greater(squireMoves.Count, 3, "and it should vary its moves");
            Assert.AreEqual(0, marshalOffBest, "Marshal always plays the best move it finds");
        }

        [Test]
        public void A_blunder_chance_of_one_makes_every_move_random_and_zero_makes_none()
        {
            var game = new ChessGame(Fen.Parse(Kiwipete));
            var always = Squire.Clone();
            always.blunderOneIn = 1;
            always.minThinkSeconds = 0f;
            var moves = new HashSet<string>();
            for (int seed = 0; seed < 100; seed++)
            {
                var choice = player.Choose(game, always, new System.Random(seed), default);
                Assert.IsTrue(choice.Blundered);
                Assert.IsTrue(game.LegalMoves.Any(m => m.Equals(choice.Move)));
                moves.Add(choice.Move.ToUci());
            }
            Assert.Greater(moves.Count, 12, "random moves should vary widely");

            var never = Squire.Clone();
            never.blunderOneIn = 0;
            never.minThinkSeconds = 0f;
            for (int seed = 0; seed < 30; seed++) Assert.IsFalse(player.Choose(game, never, new System.Random(seed), default).Blundered);
        }

        [Test]
        public void The_same_seed_gives_the_same_move_and_a_different_seed_may_not()
        {
            var game = new ChessGame(Fen.Parse(Kiwipete));
            var a = player.Choose(game, Quick(Squire, 1, 1), new System.Random(7), default).Move;
            var b = player.Choose(game, Quick(Squire, 1, 1), new System.Random(7), default).Move;
            Assert.AreEqual(a, b);
            var k1 = player.Choose(game, Quick(Knight, 2, 1), new System.Random(7), default).Move;
            var k2 = player.Choose(game, Quick(Knight, 2, 1), new System.Random(7), default).Move;
            Assert.AreEqual(k1, k2);
        }

        [Test]
        public void The_player_does_not_change_the_game_it_looks_at()
        {
            var game = new ChessGame(Fen.Parse(Kiwipete));
            string before = Fen.Write(game.Position);
            player.Choose(game, Quick(Knight, 3, 0.5), new System.Random(1), default);
            Assert.AreEqual(before, Fen.Write(game.Position));
            Assert.AreEqual(0, game.History.Count);
        }

        // --- time, waiting and cancelling ---

        [Test]
        public void Marshal_stays_inside_its_time_cap_even_when_asked_for_a_deep_search()
        {
            var deep = Marshal.Clone();
            deep.depth = 30;
            deep.timeCapSeconds = 0.4f;
            deep.minThinkSeconds = 0f;
            var game = new ChessGame(Fen.Parse(Kiwipete));
            var watch = Stopwatch.StartNew();
            var choice = player.Choose(game, deep, new System.Random(1), default);
            watch.Stop();
            Assert.Less(watch.Elapsed.TotalSeconds, 1.0, $"took {watch.Elapsed.TotalSeconds:F2} s");
            Assert.IsTrue(choice.Move.IsValid);
            TestContext.WriteLine($"Marshal with a 0.4 s cap reached depth {choice.Search.Depth} ({choice.Search.Nodes} nodes)");
        }

        [Test]
        public void The_async_player_returns_the_same_kind_of_answer_without_blocking()
        {
            var game = new ChessGame(Fen.Parse(Kiwipete));
            var profile = Quick(Knight, 3, 0.5);
            var task = player.ChooseAsync(game, profile, 3, default);
            Assert.IsTrue(task.Wait(5000));
            Assert.IsTrue(game.LegalMoves.Any(m => m.Equals(task.Result.Move)));
        }

        [Test]
        public void The_async_player_waits_for_the_minimum_think_time()
        {
            var game = new ChessGame();
            var profile = Quick(Squire, 1, 1);
            profile.minThinkSeconds = 0.35f;
            var watch = Stopwatch.StartNew();
            var task = player.ChooseAsync(game, profile, 1, default);
            Assert.IsTrue(task.Wait(5000));
            watch.Stop();
            Assert.GreaterOrEqual(watch.Elapsed.TotalSeconds, 0.33);
            Assert.Less(watch.Elapsed.TotalSeconds, 1.5);
        }

        [Test]
        public void Cancelling_the_async_player_stops_it_quickly_whether_it_is_searching_or_waiting()
        {
            var game = new ChessGame(Fen.Parse(Kiwipete));
            var deep = Marshal.Clone();
            deep.depth = 30;
            deep.timeCapSeconds = 20f;
            deep.minThinkSeconds = 0f;
            using (var cts = new CancellationTokenSource())
            {
                var task = player.ChooseAsync(game, deep, 1, cts.Token);
                Thread.Sleep(100);
                var watch = Stopwatch.StartNew();
                cts.Cancel();
                Assert.IsTrue(task.Wait(2000), "a cancelled search must stop");
                Assert.Less(watch.ElapsedMilliseconds, 800);
            }

            var waiting = Squire.Clone();
            waiting.minThinkSeconds = 5f;
            using (var cts = new CancellationTokenSource())
            {
                var task = player.ChooseAsync(game, waiting, 1, cts.Token);
                Thread.Sleep(50);
                cts.Cancel();
                Assert.IsTrue(task.Wait(1500), "a cancelled wait must stop");
            }
        }

        // --- the order of strength ---

        // Plays a game between two profiles and scores it for White: 1 win, 0.5 draw, 0 loss.
        // A game that is still going after maxPlies is decided by material (3 pawns or more ahead wins).
        double PlayGame(DifficultyProfile white, DifficultyProfile black, int seed, int maxPlies)
        {
            var game = new ChessGame();
            var rng = new System.Random(seed);
            int plies = 0;
            while (!game.IsOver && plies < maxPlies)
            {
                var profile = game.Position.SideToMove == ChessSide.White ? white : black;
                var move = player.Choose(game, profile, rng, default).Move;
                if (!game.TryMake(move)) Assert.Fail($"illegal move {move} in {Fen.Write(game.Position)}");
                plies++;
            }
            if (game.Status == GameStatus.Checkmate) return game.Winner == ChessSide.White ? 1 : 0;
            if (game.IsOver) return 0.5;
            int balance = Material(game.Position, ChessSide.White) - Material(game.Position, ChessSide.Black);
            return balance >= 300 ? 1 : balance <= -300 ? 0 : 0.5;
        }

        int Material(Position p, ChessSide side)
        {
            var e = settings.evaluation;
            int total = 0;
            for (int sq = 0; sq < 64; sq++)
            {
                int piece = p[sq];
                if (piece == Pieces.Empty || Pieces.SideOf(piece) != side) continue;
                switch (Pieces.TypeOf(piece))
                {
                    case PieceType.Pawn: total += e.pawn; break;
                    case PieceType.Knight: total += e.knight; break;
                    case PieceType.Bishop: total += e.bishop; break;
                    case PieceType.Rook: total += e.rook; break;
                    case PieceType.Queen: total += e.queen; break;
                }
            }
            return total;
        }

        // Games with each colour for each seed. Returns the first profile's score out of 2 * seeds.
        // The time caps are long, so the depth limits decide the moves and the result is the same every run.
        double Match(DifficultyProfile a, DifficultyProfile b, int seeds, int maxPlies)
        {
            double score = 0;
            for (int seed = 1; seed <= seeds; seed++)
            {
                score += PlayGame(a, b, seed, maxPlies);
                score += 1 - PlayGame(b, a, seed + 100, maxPlies);
            }
            return score;
        }

        [Test]
        public void Knight_beats_Squire_over_four_seeded_games_two_with_each_colour()
        {
            var watch = Stopwatch.StartNew();
            double score = Match(Quick(Knight, 3, 30), Quick(Squire, 1, 30), 2, 100);
            TestContext.WriteLine($"Knight {score} - {4 - score} Squire in {watch.Elapsed.TotalSeconds:F1} s");
            Assert.GreaterOrEqual(score, 3.0, "Knight should beat Squire clearly (it won 16 of 16 when measured over more games)");
        }

        [Test, Explicit("slow: about six minutes. Plays ten games of Marshal (four plies) against Knight (three plies).")]
        public void Marshal_beats_Knight_over_ten_seeded_games()
        {
            double score = Match(Quick(Marshal, 4, 30), Quick(Knight, 3, 30), 5, 100);
            TestContext.WriteLine($"Marshal {score} - {10 - score} Knight");
            Assert.Greater(score, 5.0);
        }
    }
}
