using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace Prototypes.MedievalChess.Tests
{
    public class GamePlayTests
    {
        const string ScenePath = "Assets/Prototypes/MedievalChess/Scenes/MedievalChess.unity";
        const float Step = 1f / 60f;

        GameController game;
        InputController input;
        float savedStrength;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return null;
#endif
            game = UnityEngine.Object.FindAnyObjectByType<GameController>();
            input = UnityEngine.Object.FindAnyObjectByType<InputController>();
            Assert.IsNotNull(game, "the scene has no game controller");
            game.autoStep = false; // the tests step the game by hand
            savedStrength = game.settings.effectStrength;
            // a quick computer with no pause, so the tests do not wait on it
            var quick = game.settings.difficulties[1].Clone();
            quick.depth = 2;
            quick.minThinkSeconds = 0f;
            quick.timeCapSeconds = 0.3f;
            quick.slackCentipawns = 0;
            game.profileOverride = quick;
            game.seed = 7;
            yield return null; // Start has run
            game.speed = AnimationSpeed.Off;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (game != null) game.settings.effectStrength = savedStrength;
            yield return null;
        }

        // Steps the game by hand, giving the computer's thread real time, until the condition holds.
        IEnumerator StepUntil(Func<bool> done, string what, int maxFrames = 1500)
        {
            for (int i = 0; i < maxFrames && !done(); i++)
            {
                game.Step(Step);
                yield return null;
            }
            Assert.IsTrue(done(), $"timed out waiting for {what} (phase {game.Phase})");
        }

        IEnumerator Settled() => StepUntil(() => game.Phase == GamePhase.PlayerTurn || game.Phase == GamePhase.GameOver || game.Phase == GamePhase.ChoosingPromotion, "the game to settle");

        void Click(string square) => game.Click(Squares.Parse(square));

        IEnumerator MovePlayer(string from, string to)
        {
            Click(from);
            Click(to);
            yield return null;
        }

        // --- the menu ---

        [UnityTest]
        public IEnumerator The_game_opens_on_the_menu_with_no_pieces()
        {
            Assert.AreEqual(GamePhase.Menu, game.Phase);
            Assert.IsTrue(game.ui.MenuVisible);
            Assert.IsFalse(game.ui.HudVisible);
            Assert.AreEqual(0, game.Actors.Count);
            Assert.IsNull(game.Game);
            yield return null;
        }

        [UnityTest]
        public IEnumerator The_menu_buttons_choose_the_difficulty_the_side_and_the_speed_and_begin_starts_the_game()
        {
            game.ui.difficultyButtons[2].onClick.Invoke();   // Marshal
            game.ui.sideButtons[1].onClick.Invoke();         // cinnabar (black)
            game.ui.speedButtons[1].onClick.Invoke();        // Quick
            Assert.AreEqual(2, game.MenuDifficulty);
            Assert.AreEqual(SideChoice.Black, game.MenuSide);
            Assert.AreEqual(AnimationSpeed.Quick, game.speed);
            StringAssert.Contains("Hard", game.ui.difficultyDescription.text);

            game.ui.startButton.onClick.Invoke();
            Assert.AreEqual(2, game.DifficultyIndex);
            Assert.AreEqual(ChessSide.Black, game.PlayerSide);
            Assert.IsTrue(game.ui.HudVisible);
            Assert.IsFalse(game.ui.MenuVisible);
            Assert.AreEqual(32, game.Actors.Count);
            Assert.AreEqual(GamePhase.ComputerThinking, game.Phase, "white is the computer, so it moves first");
            yield return Settled();
            Assert.AreEqual(1, game.Game.History.Count);
            Assert.AreEqual(GamePhase.PlayerTurn, game.Phase);
        }

        [UnityTest]
        public IEnumerator Playing_as_black_puts_the_camera_behind_the_black_side()
        {
            game.StartGame(0, SideChoice.Black);
            Assert.Greater(Camera.main.transform.position.z, 0f);
            game.StartGame(0, SideChoice.White);
            Assert.Less(Camera.main.transform.position.z, 0f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Random_picks_one_of_the_two_sides()
        {
            var seen = new System.Collections.Generic.HashSet<ChessSide>();
            for (int i = 0; i < 12; i++)
            {
                game.StartGame(0, SideChoice.Random);
                seen.Add(game.PlayerSide);
            }
            Assert.AreEqual(2, seen.Count);
            yield return null;
        }

        // --- the player's turn ---

        [UnityTest]
        public IEnumerator Picking_a_piece_shows_the_selection_and_where_it_can_go()
        {
            game.StartGame(1, SideChoice.White);
            Assert.AreEqual(GamePhase.PlayerTurn, game.Phase);
            Click("e2");
            Assert.AreEqual(Squares.Parse("e2"), game.SelectedSquare);
            CollectionAssert.AreEquivalent(new[] { Squares.Parse("e2") }, game.highlights.Squares(RingKind.Selection));
            CollectionAssert.AreEquivalent(new[] { Squares.Parse("e3"), Squares.Parse("e4") }, game.highlights.Squares(RingKind.Move));
            Assert.AreEqual(0, game.highlights.Count(RingKind.Capture));

            Click("g1");
            CollectionAssert.AreEquivalent(new[] { Squares.Parse("f3"), Squares.Parse("h3") }, game.highlights.Squares(RingKind.Move));
            Assert.AreEqual(1, game.highlights.Count(RingKind.Selection), "only one piece is selected at a time");
            yield return null;
        }

        [UnityTest]
        public IEnumerator A_capture_square_gets_a_red_ring_and_a_quiet_square_a_gold_one()
        {
            game.StartGame(0, SideChoice.White, Fen.Parse("4k3/8/8/3p4/4P3/8/8/4K3 w - - 0 1"));
            Click("e4");
            CollectionAssert.AreEquivalent(new[] { Squares.Parse("d5") }, game.highlights.Squares(RingKind.Capture));
            CollectionAssert.AreEquivalent(new[] { Squares.Parse("e5") }, game.highlights.Squares(RingKind.Move));
            yield return null;
        }

        [UnityTest]
        public IEnumerator An_illegal_click_changes_nothing_and_clicking_elsewhere_cancels()
        {
            game.StartGame(1, SideChoice.White);
            string before = Fen.Write(game.Game.Position);
            Click("e7");                     // the opponent's pawn, with nothing selected
            Assert.AreEqual(-1, game.SelectedSquare);
            Click("e4");                     // an empty square, with nothing selected
            Assert.AreEqual(-1, game.SelectedSquare);
            Click("e2");
            Click("e5");                     // too far for a pawn
            Assert.AreEqual(-1, game.SelectedSquare, "the selection is cancelled");
            Assert.AreEqual(0, game.highlights.Count(RingKind.Move));
            Click("a1");                     // a rook with no moves
            Assert.AreEqual(-1, game.SelectedSquare);
            game.Click(-1);                  // off the board
            Assert.AreEqual(before, Fen.Write(game.Game.Position));
            Assert.AreEqual(0, game.Game.History.Count);
            Assert.AreEqual(GamePhase.PlayerTurn, game.Phase);
            yield return null;
        }

        [UnityTest]
        public IEnumerator A_move_is_played_the_computer_replies_once_and_the_board_matches_the_rules()
        {
            game.StartGame(1, SideChoice.White);
            yield return MovePlayer("e2", "e4");
            Assert.AreEqual(GamePhase.Animating, game.Phase);
            Assert.AreEqual(1, game.Game.History.Count, "the rules move at once");
            yield return Settled();
            Assert.AreEqual(2, game.Game.History.Count, "the computer replied once");
            Assert.AreEqual(GamePhase.PlayerTurn, game.Phase);
            Assert.IsTrue(game.Tracker.Matches(game.Game.Position, out string problem), problem);

            for (int i = 0; i < 30; i++) { game.Step(Step); yield return null; }
            Assert.AreEqual(2, game.Game.History.Count, "and waited for the player after that");

            var pawn = game.ActorAt(Squares.Parse("e4"));
            Assert.IsNotNull(pawn);
            Assert.AreEqual(PieceType.Pawn, pawn.Type);
            Assert.AreEqual(BoardGeometry.Centre(Squares.Parse("e4")).z, pawn.transform.position.z, 0.01f);
        }

        [UnityTest]
        public IEnumerator The_last_move_is_ringed()
        {
            game.StartGame(1, SideChoice.White);
            yield return MovePlayer("e2", "e4");
            CollectionAssert.AreEquivalent(new[] { Squares.Parse("e2"), Squares.Parse("e4") }, game.highlights.Squares(RingKind.LastMove));
            yield return Settled();
            var reply = game.Game.History.Last();
            CollectionAssert.AreEquivalent(new[] { (int)reply.From, (int)reply.To }, game.highlights.Squares(RingKind.LastMove));
        }

        [UnityTest]
        public IEnumerator Off_speed_finishes_a_move_in_one_step_and_Quick_and_Full_take_longer()
        {
            game.StartGame(1, SideChoice.White);
            game.speed = AnimationSpeed.Off;
            yield return MovePlayer("e2", "e4");
            game.Step(Step);
            Assert.AreNotEqual(GamePhase.Animating, game.Phase, "Off shows the move at once");

            yield return Settled();
            game.speed = AnimationSpeed.Quick;
            Click("g1");
            Click("f3");
            int quick = 0;
            while (game.Phase == GamePhase.Animating && quick < 2000) { game.Step(Step); quick++; }
            yield return Settled();

            game.speed = AnimationSpeed.Full;
            Click("b1");
            Click("c3");
            int full = 0;
            while (game.Phase == GamePhase.Animating && full < 2000) { game.Step(Step); full++; }
            Assert.Greater(quick, 3);
            Assert.Greater(full, quick, "a full move takes longer than a quick one");
        }

        // --- captures and the trays ---

        [UnityTest]
        public IEnumerator A_capture_plays_a_fight_and_the_victim_stands_on_its_tray()
        {
            game.speed = AnimationSpeed.Quick;
            game.StartGame(0, SideChoice.White, Fen.Parse("4k3/8/8/3p4/4P3/8/8/4K3 w - - 0 1"));
            yield return MovePlayer("e4", "d5");
            Assert.AreEqual(GamePhase.Animating, game.Phase);
            // the fight is under way: wait until the capture has finished
            yield return StepUntil(() => game.Phase != GamePhase.Animating, "the fight to finish");

            Assert.AreEqual(1, game.Tracker.Captured.Count);
            Assert.AreEqual(1, game.Tray.Count);
            var captured = game.Tray[0];
            Assert.AreEqual(PieceType.Pawn, captured.Type);
            Assert.Greater(captured.transform.position.x, 4.5f, "the cinnabar side's captured pieces are on the right");
            Assert.AreEqual(game.settings.game.trayScale, captured.transform.localScale.x, 0.001f);
            Assert.IsFalse(game.Actors["d5"].gameObject.activeSelf, "the victim is gone from the board");
            yield return Settled();
            Assert.IsTrue(game.Tracker.Matches(game.Game.Position, out string problem), problem);
        }

        // --- check, promotion, the end ---

        [UnityTest]
        public IEnumerator A_king_in_check_is_ringed_in_red()
        {
            game.StartGame(0, SideChoice.White, Fen.Parse("4k3/8/8/8/8/8/8/R3K3 w - - 0 1"));
            yield return MovePlayer("a1", "a8");
            yield return StepUntil(() => game.Phase != GamePhase.Animating, "the move to finish");
            CollectionAssert.AreEquivalent(new[] { Squares.Parse("e8") }, game.highlights.Squares(RingKind.Check));
            yield return Settled();
            Assert.AreEqual(0, game.highlights.Count(RingKind.Check), "the ring goes when the check is answered");
        }

        [UnityTest]
        public IEnumerator A_promotion_asks_which_piece_and_only_then_moves()
        {
            game.StartGame(0, SideChoice.White, Fen.Parse("8/P6k/8/8/8/8/8/K7 w - - 0 1"));
            Click("a7");
            Click("a8");
            Assert.AreEqual(GamePhase.ChoosingPromotion, game.Phase);
            Assert.IsTrue(game.ui.PromotionVisible);
            Assert.AreEqual(0, game.Game.History.Count, "nothing moves until a piece is chosen");

            game.ui.promotionButtons[3].onClick.Invoke(); // knight
            Assert.IsFalse(game.ui.PromotionVisible);
            Assert.AreEqual(1, game.Game.History.Count);
            Assert.AreEqual(PieceType.Knight, game.Game.History[0].Promotion);
            yield return Settled();
            var piece = game.ActorAt(Squares.Parse("a8"));
            Assert.AreEqual(PieceType.Knight, piece.Type);
            Assert.IsTrue(piece.gameObject.activeSelf);
            Assert.IsTrue(game.Tracker.Matches(game.Game.Position, out string problem), problem);
        }

        [UnityTest]
        public IEnumerator Checkmate_shows_the_result_and_Play_Again_starts_a_new_game()
        {
            game.StartGame(2, SideChoice.White, Fen.Parse("6k1/5ppp/8/8/8/8/8/R3K3 w - - 0 1"));
            yield return MovePlayer("a1", "a8");
            yield return StepUntil(() => game.Phase == GamePhase.GameOver, "the game to end");
            Assert.IsTrue(game.ui.ResultVisible);
            StringAssert.Contains("CHECKMATE", game.ui.Result);
            StringAssert.Contains("You win", game.ui.Result);
            Assert.AreEqual(GameStatus.Checkmate, game.Game.Status);

            game.Click(Squares.Parse("e1")); // no moves once it is over
            Assert.AreEqual(GamePhase.GameOver, game.Phase);

            game.ui.playAgainButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(game.ui.ResultVisible);
            Assert.AreEqual(0, game.Game.History.Count);
            Assert.AreEqual(32, game.Actors.Count);
            Assert.AreEqual(2, game.DifficultyIndex, "the same opponent");
            Assert.AreEqual(GamePhase.PlayerTurn, game.Phase);
            Assert.AreEqual(0, game.Tray.Count);
        }

        [UnityTest]
        public IEnumerator Resigning_ends_the_game()
        {
            game.StartGame(1, SideChoice.White);
            game.ui.resignButton.onClick.Invoke();
            yield return StepUntil(() => game.Phase == GamePhase.GameOver, "the game to end");
            StringAssert.Contains("You resigned", game.ui.Result);
            Assert.AreEqual(GameStatus.Resignation, game.Game.Status);
            Assert.AreEqual(ChessSide.Black, game.Game.Winner);
        }

        [UnityTest]
        public IEnumerator A_stalemate_is_a_draw()
        {
            // White to move, Qg6 gives stalemate
            game.StartGame(0, SideChoice.White, Fen.Parse("7k/5Q2/8/6K1/8/8/8/8 w - - 0 1"));
            yield return MovePlayer("g5", "g6");
            yield return StepUntil(() => game.Phase == GamePhase.GameOver, "the game to end");
            StringAssert.Contains("STALEMATE", game.ui.Result);
            Assert.AreEqual(GameStatus.Stalemate, game.Game.Status);
        }

        [UnityTest]
        public IEnumerator The_menu_button_leaves_the_game_and_clears_the_board()
        {
            game.StartGame(1, SideChoice.White);
            game.ui.menuButton.onClick.Invoke();
            yield return null;
            Assert.AreEqual(GamePhase.Menu, game.Phase);
            Assert.IsTrue(game.ui.MenuVisible);
            Assert.AreEqual(0, game.Actors.Count);
        }

        [UnityTest]
        public IEnumerator Clicks_are_ignored_while_the_computer_is_thinking_or_a_move_is_playing()
        {
            game.speed = AnimationSpeed.Quick;
            game.StartGame(1, SideChoice.White);
            yield return MovePlayer("e2", "e4");
            Assert.AreEqual(GamePhase.Animating, game.Phase);
            Click("g1");
            Assert.AreEqual(-1, game.SelectedSquare, "no picking while a move plays");
            yield return Settled();
        }

        // --- the mouse ---

        [UnityTest]
        public IEnumerator Every_square_is_found_under_the_mouse_from_either_side_of_the_board()
        {
            foreach (var side in new[] { SideChoice.White, SideChoice.Black })
            {
                game.StartGame(0, side);
                yield return null;
                var cam = Camera.main;
                for (int sq = 0; sq < 64; sq++)
                {
                    var screen = cam.WorldToScreenPoint(BoardGeometry.Centre(sq));
                    Assert.AreEqual(sq, input.PickSquare(new Vector2(screen.x, screen.y)), $"{Squares.Name(sq)} seen from {side}");
                }
                // a point in the sky finds no square
                Assert.AreEqual(-1, input.PickSquare(new Vector2(cam.pixelWidth * 0.5f, cam.pixelHeight - 1f)));
            }
        }

        // --- whole games ---

        [UnityTest]
        public IEnumerator Whole_games_against_every_difficulty_and_both_sides_never_get_stuck_and_stay_in_step()
        {
            foreach (int level in new[] { 0, 1, 2 })
            {
                foreach (var side in new[] { SideChoice.White, SideChoice.Black })
                {
                    game.seed = 11 + level;
                    game.StartGame(level, side);
                    // the "player" thinks a little, so the games last (random moves lose to a mate in a few turns)
                    var thinker = new ComputerPlayer(game.settings.evaluation);
                    var human = game.settings.difficulties[1].Clone();
                    human.depth = 2;
                    human.timeCapSeconds = 0.2f;
                    human.slackCentipawns = 0;
                    var rng = new System.Random(level * 10 + (int)side);
                    int turns = 0;
                    while (game.Phase != GamePhase.GameOver && turns < 24)
                    {
                        yield return Settled();
                        if (game.Phase == GamePhase.GameOver) break;
                        Assert.IsTrue(game.Tracker.Matches(game.Game.Position, out string problem), $"level {level} {side} turn {turns}: {problem}");

                        var move = thinker.Choose(game.Game, human, rng, default).Move;
                        Click(Squares.Name(move.From));
                        Click(Squares.Name(move.To));
                        if (game.Phase == GamePhase.ChoosingPromotion) game.ChoosePromotion(PieceType.Queen);
                        Assert.AreEqual(GamePhase.Animating, game.Phase, $"level {level} {side} turn {turns}: the move {move} was not played");
                        turns++;
                    }
                    Assert.Greater(turns, 5);
                }
            }
        }
    }
}
