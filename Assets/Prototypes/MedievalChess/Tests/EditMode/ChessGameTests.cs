using System.Linq;
using NUnit.Framework;

namespace Prototypes.MedievalChess.Tests
{
    /// <summary>The position's bookkeeping and the end of the game: mate, stalemate and the draws.</summary>
    public class ChessGameTests
    {
        static ChessGame PlayUci(string uciList, string fen = null)
        {
            var game = fen == null ? new ChessGame() : new ChessGame(Fen.Parse(fen));
            foreach (var uci in uciList.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries))
                Assert.IsTrue(game.TryMakeUci(uci), $"{uci} should be legal here: {Fen.Write(game.Position)}");
            return game;
        }

        // --- make and unmake ---

        [Test]
        public void Unmaking_every_move_of_a_long_random_game_restores_the_position_exactly()
        {
            foreach (var fen in new[]
            {
                "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
                "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1",
                "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1",
            })
            {
                var p = Fen.Parse(fen);
                ulong startHash = p.Hash;
                var rng = new System.Random(12345);
                int played = 0;
                for (int i = 0; i < 300; i++)
                {
                    var moves = MoveGenerator.Legal(p);
                    if (moves.Count == 0) break;
                    p.MakeMove(moves[rng.Next(moves.Count)]);
                    played++;
                    Assert.AreEqual(p.ComputeHash(), p.Hash, "the hash must stay in step with the position");
                }
                Assert.Greater(played, 30, "the random game ended too soon to prove anything");
                while (played-- > 0) p.UnmakeMove();
                Assert.AreEqual(fen, Fen.Write(p));
                Assert.AreEqual(startHash, p.Hash);
            }
        }

        [Test]
        public void Two_routes_to_the_same_position_give_the_same_hash()
        {
            var a = PlayUci("g1f3 g8f6 b1c3 b8c6").Position;
            var b = PlayUci("b1c3 b8c6 g1f3 g8f6").Position;
            Assert.AreEqual(a.Hash, b.Hash);
            Assert.AreEqual(Fen.Write(a), Fen.Write(b));
            var c = PlayUci("g1f3 g8f6 b1c3 b8a6").Position;
            Assert.AreNotEqual(a.Hash, c.Hash);
        }

        [Test]
        public void The_side_to_move_is_part_of_the_hash()
        {
            var white = Fen.Parse("4k3/8/8/8/8/8/8/4K3 w - - 0 1");
            var black = Fen.Parse("4k3/8/8/8/8/8/8/4K3 b - - 0 1");
            Assert.AreNotEqual(white.Hash, black.Hash);
        }

        [Test]
        public void The_clocks_count_the_way_chess_does()
        {
            var game = PlayUci("g1f3 g8f6 e2e4");
            Assert.AreEqual(0, game.Position.HalfmoveClock, "a pawn move resets it");
            Assert.AreEqual(2, game.Position.FullmoveNumber);
            game.TryMakeUci("f6e4");
            Assert.AreEqual(0, game.Position.HalfmoveClock, "a capture resets it");
            Assert.AreEqual(3, game.Position.FullmoveNumber);
            game.TryMakeUci("f1e2");
            Assert.AreEqual(1, game.Position.HalfmoveClock);
        }

        // --- the end of the game ---

        [Test]
        public void Fools_mate_is_checkmate_and_black_wins()
        {
            var game = PlayUci("f2f3 e7e5 g2g4 d8h4");
            Assert.AreEqual(GameStatus.Checkmate, game.Status);
            Assert.AreEqual(ChessSide.Black, game.Winner);
            Assert.IsEmpty(game.LegalMoves);
            Assert.IsTrue(game.Position.InCheck());
            Assert.IsTrue(game.IsOver);
        }

        [Test]
        public void The_scholars_mate_is_checkmate_and_white_wins()
        {
            var game = PlayUci("e2e4 e7e5 d1h5 b8c6 f1c4 g8f6 h5f7");
            Assert.AreEqual(GameStatus.Checkmate, game.Status);
            Assert.AreEqual(ChessSide.White, game.Winner);
        }

        [Test]
        public void Sam_Loyds_ten_move_game_ends_in_stalemate()
        {
            var game = PlayUci("e2e3 a7a5 d1h5 a8a6 h5a5 h7h5 h2h4 a6h6 a5c7 f7f6 c7d7 e8f7 d7b7 d8d3 b7b8 d3h7 b8c8 f7g6 c8e6");
            Assert.AreEqual(GameStatus.Stalemate, game.Status);
            Assert.IsNull(game.Winner);
            Assert.IsFalse(game.Position.InCheck());
            Assert.IsEmpty(game.LegalMoves);
        }

        [Test]
        public void A_game_in_progress_is_ongoing_with_moves_to_play()
        {
            var game = PlayUci("e2e4 e7e5");
            Assert.AreEqual(GameStatus.Ongoing, game.Status);
            Assert.IsNull(game.Winner);
            Assert.IsFalse(game.IsOver);
            Assert.AreEqual(ChessSide.White, game.Position.SideToMove);
            Assert.Greater(game.LegalMoves.Count, 0);
        }

        [Test]
        public void Check_is_not_mate_when_there_is_a_reply()
        {
            var game = PlayUci("e2e4 f7f5 d1h5");
            Assert.IsTrue(game.Position.InCheck());
            Assert.AreEqual(GameStatus.Ongoing, game.Status);
            Assert.AreEqual(1, game.LegalMoves.Count(m => m.ToUci() == "g7g6"));
        }

        [Test]
        public void The_same_position_three_times_is_a_draw()
        {
            var game = PlayUci("g1f3 g8f6 f3g1 f6g8");
            Assert.AreEqual(GameStatus.Ongoing, game.Status, "the start position has now been seen twice");
            game.TryMakeUci("g1f3");
            game.TryMakeUci("g8f6");
            game.TryMakeUci("f3g1");
            Assert.AreEqual(GameStatus.Ongoing, game.Status);
            game.TryMakeUci("f6g8");
            Assert.AreEqual(GameStatus.ThreefoldRepetition, game.Status);
            Assert.IsNull(game.Winner);
        }

        [Test]
        public void The_same_position_with_different_castling_rights_is_not_a_repetition()
        {
            // the rooks leave and come back, so the pieces repeat but the right to castle is gone
            var game = PlayUci("h1h2 h8h7 h2h1 h7h8 h1h2 h8h7 h2h1 h7h8", "r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1");
            Assert.AreEqual(GameStatus.Ongoing, game.Status);
        }

        [Test]
        public void A_forty_nine_move_pawnless_stretch_is_fine_and_the_fiftieth_move_is_a_draw()
        {
            var one = new ChessGame(Fen.Parse("4k3/8/8/8/8/8/8/R3K3 w - - 98 100"));
            Assert.AreEqual(GameStatus.Ongoing, one.Status);
            one.TryMakeUci("a1a2");
            Assert.AreEqual(99, one.Position.HalfmoveClock);
            Assert.AreEqual(GameStatus.Ongoing, one.Status);
            one.TryMakeUci("e8d8");
            Assert.AreEqual(GameStatus.FiftyMoveRule, one.Status);
            Assert.IsNull(one.Winner);
        }

        [Test]
        public void A_pawn_move_or_a_capture_resets_the_fifty_move_count()
        {
            var game = new ChessGame(Fen.Parse("4k3/8/8/8/8/8/4P3/R3K3 w - - 99 100"));
            game.TryMakeUci("e2e3");
            Assert.AreEqual(0, game.Position.HalfmoveClock);
            Assert.AreEqual(GameStatus.Ongoing, game.Status);
        }

        [Test]
        public void Checkmate_on_the_hundredth_half_move_is_still_checkmate()
        {
            var game = new ChessGame(Fen.Parse("7k/8/6K1/8/8/8/8/R7 w - - 99 100"));
            game.TryMakeUci("a1a8");
            Assert.AreEqual(GameStatus.Checkmate, game.Status);
        }

        [TestCase("4k3/8/8/8/8/8/8/4K3 w - - 0 1", true, "king against king")]
        [TestCase("4k3/8/8/8/8/8/8/3NK3 w - - 0 1", true, "king and knight against king")]
        [TestCase("4k3/8/8/8/8/8/8/3BK3 w - - 0 1", true, "king and bishop against king")]
        [TestCase("4kb2/8/8/8/8/8/8/3BK3 w - - 0 1", false, "bishops on opposite colours can still mate")]
        [TestCase("2b1k3/8/8/8/8/8/8/3BK3 w - - 0 1", true, "bishops on the same colour cannot")]
        [TestCase("4k3/8/8/8/8/8/8/2NNK3 w - - 0 1", false, "two knights can be forced to mate with help")]
        [TestCase("4k3/8/8/8/8/8/4P3/4K3 w - - 0 1", false, "a pawn can promote")]
        [TestCase("4k3/8/8/8/8/8/8/3RK3 w - - 0 1", false, "a rook can mate")]
        [TestCase("4k3/8/8/8/8/8/8/3QK3 w - - 0 1", false, "a queen can mate")]
        [TestCase("4kn2/8/8/8/8/8/8/3NK3 w - - 0 1", false, "two knights, one each, is not an automatic draw")]
        public void Insufficient_material_is_a_draw_only_when_nobody_can_mate(string fen, bool draw, string why)
        {
            var game = new ChessGame(Fen.Parse(fen));
            Assert.AreEqual(draw, game.Status == GameStatus.InsufficientMaterial, why);
        }

        [Test]
        public void Insufficient_material_ends_the_game_after_the_last_capture()
        {
            var game = new ChessGame(Fen.Parse("4k3/8/8/8/8/8/2r5/3BK3 w - - 0 1"));
            Assert.AreEqual(GameStatus.Ongoing, game.Status);
            Assert.IsTrue(game.TryMakeUci("d1c2"), "the bishop takes the last rook");
            Assert.AreEqual(GameStatus.InsufficientMaterial, game.Status);
            Assert.IsNull(game.Winner);
        }

        [Test]
        public void Resigning_ends_the_game_and_the_other_side_wins()
        {
            var game = PlayUci("e2e4 e7e5");
            game.Resign(ChessSide.White);
            Assert.AreEqual(GameStatus.Resignation, game.Status);
            Assert.AreEqual(ChessSide.Black, game.Winner);
            Assert.IsFalse(game.TryMakeUci("g1f3"), "no moves after the game is over");
        }

        // --- making moves through the game ---

        [Test]
        public void An_illegal_move_is_refused_and_changes_nothing()
        {
            var game = new ChessGame();
            string before = Fen.Write(game.Position);
            Assert.IsFalse(game.TryMakeUci("e2e5"), "a pawn cannot jump three squares");
            Assert.IsFalse(game.TryMakeUci("e7e5"), "it is white's move");
            Assert.IsFalse(game.TryMakeUci("nonsense"));
            Assert.IsFalse(game.TryMakeUci("a1a1"));
            Assert.AreEqual(before, Fen.Write(game.Position));
            Assert.AreEqual(0, game.History.Count);
        }

        [Test]
        public void A_promotion_must_say_which_piece()
        {
            var game = new ChessGame(Fen.Parse("8/P6k/8/8/8/8/8/K7 w - - 0 1"));
            Assert.IsFalse(game.TryMakeUci("a7a8"), "the piece to promote to is needed");
            Assert.IsTrue(game.TryMakeUci("a7a8q"));
        }

        [Test]
        public void The_game_keeps_a_hash_for_every_position_it_has_been_in()
        {
            var game = PlayUci("e2e4 e7e5 g1f3");
            Assert.AreEqual(4, game.Hashes.Count, "the start and one after each move");
            Assert.AreEqual(game.Position.Hash, game.Hashes[game.Hashes.Count - 1]);
            Assert.AreEqual(new ChessGame().Position.Hash, game.Hashes[0]);
        }

        [Test]
        public void The_history_lists_the_moves_played_in_order()
        {
            var game = PlayUci("e2e4 e7e5 g1f3");
            CollectionAssert.AreEqual(new[] { "e2e4", "e7e5", "g1f3" }, game.History.Select(m => m.ToUci()).ToArray());
        }

        [Test]
        public void A_move_found_by_squares_is_the_legal_move_with_its_flags()
        {
            var game = new ChessGame(Fen.Parse("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1"));
            var move = game.FindLegal(Squares.Parse("e1"), Squares.Parse("g1"), promotion: null);
            Assert.IsTrue(move.IsValid);
            Assert.IsTrue(move.Flags.HasFlag(MoveFlags.CastleKing));
            Assert.IsFalse(game.FindLegal(Squares.Parse("e1"), Squares.Parse("e5"), promotion: null).IsValid);
        }

        [Test]
        public void Legal_moves_of_a_piece_can_be_listed_for_highlighting()
        {
            var game = new ChessGame();
            var moves = game.LegalMovesFrom(Squares.Parse("g1")).Select(m => Squares.Name(m.To)).ToList();
            CollectionAssert.AreEquivalent(new[] { "f3", "h3" }, moves);
            Assert.IsEmpty(game.LegalMovesFrom(Squares.Parse("e4")));
        }
    }
}
