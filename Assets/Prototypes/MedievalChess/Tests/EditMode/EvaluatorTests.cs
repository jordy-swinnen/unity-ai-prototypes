using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Prototypes.MedievalChess.Tests
{
    public class EvaluatorTests
    {
        EvaluationSettings settings;
        Evaluator evaluator;

        [SetUp]
        public void SetUp()
        {
            settings = new EvaluationSettings();
            evaluator = new Evaluator(settings);
        }

        int Eval(string fen) => evaluator.Evaluate(Fen.Parse(fen));

        // The same position with the colours swapped and the board turned upside down.
        static string Mirror(string fen)
        {
            var f = fen.Split(' ');
            var ranks = f[0].Split('/').Reverse().Select(r => new string(r.Select(c => char.IsUpper(c) ? char.ToLower(c) : char.ToUpper(c)).ToArray()));
            string side = f[1] == "w" ? "b" : "w";
            string castling = f[2] == "-" ? "-" : new string(f[2].Select(c => char.IsUpper(c) ? char.ToLower(c) : char.ToUpper(c)).ToArray());
            string ep = f[3] == "-" ? "-" : $"{f[3][0]}{(char)('1' + 7 - (f[3][1] - '1'))}";
            return $"{string.Join("/", ranks)} {side} {castling} {ep} {f[4]} {f[5]}";
        }

        [Test]
        public void The_starting_position_is_level_for_the_side_to_move_except_for_the_tempo()
        {
            Assert.AreEqual(settings.tempo, Eval("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"));
            Assert.AreEqual(settings.tempo, Eval("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR b KQkq - 0 1"));
        }

        [TestCase("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1")]
        [TestCase("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1")]
        [TestCase("r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10")]
        [TestCase("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8")]
        public void A_position_and_its_mirror_image_score_the_same_for_the_side_to_move(string fen)
        {
            Assert.AreEqual(Eval(fen), Eval(Mirror(fen)));
        }

        [Test]
        public void An_extra_queen_is_worth_about_a_queen_and_an_extra_pawn_about_a_pawn()
        {
            int level = Eval("4k3/pppppppp/8/8/8/8/PPPPPPPP/4K3 w - - 0 1");
            int queenUp = Eval("3qk3/pppppppp/8/8/8/8/PPPPPPPP/3QK3 w - - 0 1");
            int queenForNothing = Eval("4k3/pppppppp/8/8/8/8/PPPPPPPP/3QK3 w - - 0 1");
            int pawnUp = Eval("4k3/1ppppppp/8/8/8/8/PPPPPPPP/4K3 w - - 0 1");
            Assert.AreEqual(level, queenUp, 40);
            Assert.AreEqual(settings.queen, queenForNothing - level, 60);
            Assert.AreEqual(settings.pawn, pawnUp - level, 60);
        }

        [Test]
        public void The_pieces_are_worth_what_the_settings_say_in_order()
        {
            Assert.Less(settings.pawn, settings.knight);
            Assert.LessOrEqual(settings.knight, settings.bishop);
            Assert.Less(settings.bishop, settings.rook);
            Assert.Less(settings.rook, settings.queen);
            Assert.Greater(settings.knight, 3 * settings.pawn);
        }

        [Test]
        public void A_knight_is_better_in_the_centre_than_on_the_rim()
        {
            // a pawn each, so this is not a dead draw (which scores zero whatever the knight does)
            int centre = Eval("4k3/7p/8/8/3N4/8/7P/4K3 w - - 0 1");
            int rim = Eval("4k3/7p/8/8/N7/8/7P/4K3 w - - 0 1");
            int corner = Eval("4k3/7p/8/8/8/8/7P/N3K3 w - - 0 1");
            Assert.Greater(centre, rim);
            Assert.Greater(rim, corner);
        }

        [Test]
        public void In_the_middlegame_a_castled_king_is_safer_than_a_king_in_the_open()
        {
            int castled = Eval("r2q1rk1/ppp2ppp/2n2n2/3pp3/3PP3/2N2N2/PPP2PPP/R2Q1RK1 w - - 0 1");
            int open = Eval("r2q1rk1/ppp2ppp/2n2n2/3pp3/3PP3/2N2N2/PPP2PPP/R2QK2R w - - 0 1");
            Assert.Greater(castled, open);
        }

        [Test]
        public void In_an_endgame_the_king_belongs_in_the_middle()
        {
            int centre = Eval("8/8/8/4k3/8/3K4/4P3/8 w - - 0 1");
            int corner = Eval("8/8/8/4k3/8/8/4P3/K7 w - - 0 1");
            Assert.Greater(centre, corner);
        }

        [Test]
        public void The_bishop_pair_is_worth_extra()
        {
            int pair = Eval("4k3/8/8/8/8/8/8/2B1KB2 w - - 0 1");
            int bishopAndKnight = Eval("4k3/8/8/8/8/8/8/2B1KN2 w - - 0 1");
            Assert.Greater(pair - bishopAndKnight, 0);
        }

        [Test]
        public void An_advanced_passed_pawn_is_worth_more_than_one_at_home_and_more_than_a_blocked_one()
        {
            int advanced = Eval("4k3/8/4P3/8/8/8/8/4K3 w - - 0 1");
            int home = Eval("4k3/8/8/8/8/8/4P3/4K3 w - - 0 1");
            Assert.Greater(advanced, home);
            // the e4 pawn is passed when the only black pawn is on the a file, and held up when it is on the f file
            int passed = Eval("4k3/p7/8/8/4P3/8/8/4K3 w - - 0 1");
            int blockedByFileNeighbour = Eval("4k3/5p2/8/8/4P3/8/8/4K3 w - - 0 1");
            Assert.Greater(passed, blockedByFileNeighbour);
        }

        [Test]
        public void Doubled_pawns_score_worse_than_the_same_pawns_side_by_side()
        {
            int doubled = Eval("4k3/8/8/8/4P3/4P3/8/4K3 w - - 0 1");
            int sideBySide = Eval("4k3/8/8/8/4P3/3P4/8/4K3 w - - 0 1");
            Assert.Less(doubled, sideBySide);
        }

        [Test]
        public void The_score_is_the_same_every_time()
        {
            const string fen = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";
            Assert.AreEqual(Eval(fen), Eval(fen));
        }

        [Test]
        public void Evaluating_does_not_change_the_position()
        {
            var p = Fen.Parse("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1");
            string before = Fen.Write(p);
            evaluator.Evaluate(p);
            Assert.AreEqual(before, Fen.Write(p));
        }

        [TestCase("4k3/8/8/8/8/8/8/4K3 w - - 0 1")]
        [TestCase("4k3/8/8/8/8/8/8/3NK3 w - - 0 1")]
        [TestCase("2b1k3/8/8/8/8/8/8/3BK3 w - - 0 1")]
        public void A_position_nobody_can_win_scores_exactly_zero(string fen)
        {
            Assert.AreEqual(0, Eval(fen));
        }

        [Test]
        public void Positions_where_a_win_is_possible_do_not_score_zero_just_because_material_is_low()
        {
            Assert.AreNotEqual(0, Eval("4k3/8/8/8/8/8/8/3RK3 w - - 0 1"));
            Assert.AreNotEqual(0, Eval("4kb2/8/8/8/8/8/8/3BK3 w - - 0 1"), "opposite-coloured bishops can still mate");
            Assert.AreNotEqual(0, Eval("4k3/8/8/8/8/8/4P3/4K3 w - - 0 1"));
        }
    }
}
