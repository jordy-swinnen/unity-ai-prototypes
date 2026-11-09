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
    /// <summary>The sounds, particles, camera moods, slow motion, wash and torch dimming, in the real scene.</summary>
    public class FeelPlayTests
    {
        const string ScenePath = "Assets/Prototypes/MedievalChess/Scenes/MedievalChess.unity";
        const float Step = 1f / 60f;

        GameController game;
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
            game.autoStep = false;
            savedStrength = game.settings.effectStrength;
            var quick = game.settings.difficulties[1].Clone();
            quick.depth = 2;
            quick.minThinkSeconds = 0f;
            quick.timeCapSeconds = 0.3f;
            quick.slackCentipawns = 0;
            game.profileOverride = quick;
            game.seed = 7;
            yield return null;
            game.speed = AnimationSpeed.Full;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (game != null)
            {
                game.settings.effectStrength = savedStrength;
                game.speed = AnimationSpeed.Off;
            }
            yield return null;
        }

        IEnumerator StepUntil(Func<bool> done, string what, int maxFrames = 1500)
        {
            for (int i = 0; i < maxFrames && !done(); i++)
            {
                game.Step(Step);
                yield return null;
            }
            Assert.IsTrue(done(), $"timed out waiting for {what} (phase {game.Phase})");
        }

        void Click(string square) => game.Click(Squares.Parse(square));

        static readonly string KnightTakesPawn = "4k3/8/8/3p4/8/2N5/8/4K3 w - - 0 1";

        // --- starting ---

        [UnityTest]
        public IEnumerator Starting_a_game_beats_the_drum_and_the_camera_walks_in_from_the_hall()
        {
            game.sound.ClearHistory();
            game.StartGame(0, SideChoice.White);
            CollectionAssert.Contains(game.sound.History, SoundId.GameStart);
            var rig = game.cameraRig;
            Assert.AreEqual(game.settings.feel.startMood, rig.Mood, 1e-4f, "it starts pulled back");
            Assert.AreEqual(0f, rig.MoodTarget);
            float startDistance = (rig.transform.position - rig.Centre).magnitude;
            yield return StepUntil(() => rig.Mood > -0.001f, "the camera to arrive", 400);
            float endDistance = (rig.transform.position - rig.Centre).magnitude;
            Assert.Less(endDistance, startDistance - 0.5f, "it moved in toward the board");
        }

        [UnityTest]
        public IEnumerator At_effect_strength_zero_the_camera_does_not_move_from_the_start_view()
        {
            game.settings.effectStrength = 0f;
            game.StartGame(0, SideChoice.White);
            game.Step(0f);
            var settled = game.cameraRig.BasePosition;
            Assert.AreEqual(settled.x, game.cameraRig.transform.position.x, 1e-4f);
            Assert.AreEqual(settled.y, game.cameraRig.transform.position.y, 1e-4f);
            Assert.AreEqual(settled.z, game.cameraRig.transform.position.z, 1e-4f);
            yield return null;
        }

        // --- the player's clicks ---

        [UnityTest]
        public IEnumerator Picking_a_piece_clinks_hovering_ticks_and_a_wrong_pick_knocks()
        {
            game.StartGame(0, SideChoice.White);
            game.profileOverride = game.profileOverride; // unchanged
            game.sound.ClearHistory();

            game.Hover(Squares.Parse("e2"));
            CollectionAssert.Contains(game.sound.History, SoundId.HoverTick);
            game.sound.ClearHistory();
            game.Hover(Squares.Parse("e2"));
            Assert.AreEqual(0, game.sound.PlayCount, "the same square does not tick again");

            Click("e2");
            CollectionAssert.Contains(game.sound.History, SoundId.SelectClink);

            game.sound.ClearHistory();
            Click("e7"); // an enemy piece that cannot be taken
            CollectionAssert.Contains(game.sound.History, SoundId.IllegalKnock);

            game.sound.ClearHistory();
            Click("e4"); // an empty square only cancels
            Assert.IsFalse(game.sound.History.Contains(SoundId.IllegalKnock));
            yield return null;
        }

        // --- a capture ---

        [UnityTest]
        public IEnumerator A_capture_swings_strikes_and_collapses_in_that_order_with_sparks_and_embers()
        {
            game.StartGame(0, SideChoice.White, Fen.Parse(KnightTakesPawn));
            game.sound.ClearHistory();
            game.effects.ResetCount();
            Click("c3");
            Click("d5");
            yield return StepUntil(() => game.Phase != GamePhase.Animating, "the fight to end");

            var heard = game.sound.History.ToList();
            int swing = heard.IndexOf(SoundId.SwingKnight);
            int impact = heard.IndexOf(SoundId.Impact);
            int collapse = heard.IndexOf(SoundId.Collapse);
            Assert.GreaterOrEqual(swing, 0, string.Join(",", heard));
            Assert.Greater(impact, swing);
            Assert.Greater(collapse, impact);
            CollectionAssert.Contains(heard, SoundId.Drum);
            Assert.Greater(game.effects.EmittedCount, 20, "sparks on the blow and embers from the collapse");
        }

        [UnityTest]
        public IEnumerator At_effect_strength_zero_a_capture_makes_the_same_sounds_but_no_particles()
        {
            game.settings.effectStrength = 0f;
            game.StartGame(0, SideChoice.White, Fen.Parse(KnightTakesPawn));
            game.sound.ClearHistory();
            game.effects.ResetCount();
            Click("c3");
            Click("d5");
            yield return StepUntil(() => game.Phase != GamePhase.Animating, "the fight to end");
            CollectionAssert.Contains(game.sound.History, SoundId.Impact);
            Assert.AreEqual(0, game.effects.EmittedCount);
            Assert.IsTrue(game.Tracker.Matches(game.Game.Position, out string problem), problem);
        }

        [UnityTest]
        public IEnumerator With_fights_off_a_capture_is_one_clash_and_no_particles()
        {
            game.speed = AnimationSpeed.Off;
            game.StartGame(0, SideChoice.White, Fen.Parse(KnightTakesPawn));
            game.sound.ClearHistory();
            game.effects.ResetCount();
            Click("c3");
            Click("d5");
            yield return StepUntil(() => game.Phase != GamePhase.Animating, "the move to show");
            Assert.AreEqual(1, game.sound.History.Count(s => s == SoundId.Impact));
            Assert.IsFalse(game.sound.History.Contains(SoundId.SwingKnight));
            Assert.AreEqual(0, game.effects.EmittedCount);
        }

        [UnityTest]
        public IEnumerator A_walk_thuds_and_raises_dust_at_each_step()
        {
            game.StartGame(0, SideChoice.White, Fen.Parse(KnightTakesPawn));
            game.sound.ClearHistory();
            game.effects.ResetCount();
            Click("e1");
            Click("e2");
            yield return StepUntil(() => game.Phase != GamePhase.Animating, "the walk to end");
            Assert.GreaterOrEqual(game.sound.History.Count(s => s == SoundId.Footstep), 1);
            Assert.Greater(game.effects.EmittedCount, 0);
        }

        // --- check ---

        [UnityTest]
        public IEnumerator A_check_sounds_the_horn()
        {
            game.speed = AnimationSpeed.Off;
            game.profileOverride = null;
            game.StartGame(0, SideChoice.White, Fen.Parse("4k3/8/8/8/8/8/8/R3K3 w - - 0 1"));
            game.sound.ClearHistory();
            Click("a1");
            Click("a7"); // not check
            yield return null;
            game.StartGame(0, SideChoice.White, Fen.Parse("4k3/8/8/8/8/8/8/R3K3 w - - 0 1"));
            game.sound.ClearHistory();
            Click("a1");
            Click("e1".Replace("e1", "a8")); // Ra8+ is check along the back rank
            yield return StepUntil(() => game.Phase != GamePhase.Animating, "the move to show");
            CollectionAssert.Contains(game.sound.History, SoundId.CheckHorn);
        }

        // --- checkmate ---

        [UnityTest]
        public IEnumerator Checkmate_falls_in_slow_motion_with_a_fanfare_and_a_wash_that_fades_in()
        {
            game.StartGame(2, SideChoice.White, Fen.Parse("6k1/5ppp/8/8/8/8/8/R3K3 w - - 0 1"));
            game.sound.ClearHistory();
            Click("a1");
            Click("a8");
            bool sawSlowMotion = false;
            float washBefore = 0f;
            yield return StepUntil(() =>
            {
                sawSlowMotion |= game.SlowMotionActive;
                if (game.ui.WashAlpha > 0f && washBefore == 0f) washBefore = game.ui.WashAlpha;
                return game.Phase == GamePhase.GameOver;
            }, "the game to end");
            Assert.IsTrue(sawSlowMotion, "the fall played in slow motion");
            CollectionAssert.Contains(game.sound.History, SoundId.MateFanfare);
            Assert.IsTrue(game.ui.ResultVisible);

            // the wash is still fading in or is done: give it its time, and it ends at the set alpha
            for (int i = 0; i < 120; i++) { game.Step(Step); yield return null; }
            Assert.AreEqual(game.settings.feel.washAlpha, game.ui.WashAlpha, 0.01f);
            Assert.Greater(game.ui.wash.color.r, game.ui.wash.color.b, "a warm wash for a win");
            Assert.IsTrue(game.Tracker.Matches(game.Game.Position, out string problem), problem);
        }

        [UnityTest]
        public IEnumerator At_effect_strength_zero_a_checkmate_has_no_slow_motion_and_no_wash()
        {
            game.settings.effectStrength = 0f;
            game.StartGame(2, SideChoice.White, Fen.Parse("6k1/5ppp/8/8/8/8/8/R3K3 w - - 0 1"));
            Click("a1");
            Click("a8");
            bool sawSlowMotion = false;
            yield return StepUntil(() => { sawSlowMotion |= game.SlowMotionActive; return game.Phase == GamePhase.GameOver; }, "the game to end");
            Assert.IsFalse(sawSlowMotion);
            for (int i = 0; i < 90; i++) { game.Step(Step); yield return null; }
            Assert.AreEqual(0f, game.ui.WashAlpha, 1e-5f);
        }

        [UnityTest]
        public IEnumerator Playing_again_clears_the_wash_the_slow_motion_and_the_torch_level()
        {
            game.StartGame(2, SideChoice.White, Fen.Parse("6k1/5ppp/8/8/8/8/8/R3K3 w - - 0 1"));
            Click("a1");
            Click("a8");
            yield return StepUntil(() => game.Phase == GamePhase.GameOver, "the game to end");
            for (int i = 0; i < 90; i++) { game.Step(Step); yield return null; }
            Assert.Greater(game.ui.WashAlpha, game.settings.feel.washAlpha * 0.9f);
            game.PlayAgain();
            Assert.AreEqual(0f, game.ui.WashAlpha, 1e-5f);
            Assert.IsFalse(game.SlowMotionActive);
            Assert.AreEqual(1f, game.TorchLevel);
        }

        // --- a draw and resigning ---

        [UnityTest]
        public IEnumerator A_draw_tolls_the_bell_and_the_torches_burn_down_a_little()
        {
            game.StartGame(0, SideChoice.White, Fen.Parse("7k/5Q2/8/6K1/8/8/8/8 w - - 0 1"));
            game.sound.ClearHistory();
            Click("g5");
            Click("g6");
            yield return StepUntil(() => game.Phase == GamePhase.GameOver, "the game to end");
            CollectionAssert.Contains(game.sound.History, SoundId.DrawBell);
            for (int i = 0; i < 150; i++) { game.Step(Step); yield return null; }
            Assert.AreEqual(game.settings.feel.drawTorchLevel, game.TorchLevel, 0.01f);
            Assert.Less(game.torches[0].Dim, 0.6f);
            Assert.IsTrue(game.cameraRig.MoodTarget < 0f, "the camera draws back");
        }

        [UnityTest]
        public IEnumerator At_effect_strength_zero_a_draw_leaves_the_torches_alone()
        {
            game.settings.effectStrength = 0f;
            game.StartGame(0, SideChoice.White, Fen.Parse("7k/5Q2/8/6K1/8/8/8/8 w - - 0 1"));
            Click("g5");
            Click("g6");
            yield return StepUntil(() => game.Phase == GamePhase.GameOver, "the game to end");
            for (int i = 0; i < 150; i++) { game.Step(Step); yield return null; }
            Assert.AreEqual(1f, game.TorchLevel, 1e-4f);
        }

        [UnityTest]
        public IEnumerator Resigning_sounds_the_low_horn_and_the_camera_walks_toward_the_king()
        {
            game.StartGame(1, SideChoice.White);
            game.sound.ClearHistory();
            game.Resign();
            CollectionAssert.Contains(game.sound.History, SoundId.ResignHorn);
            Assert.AreEqual(game.settings.feel.resignMood, game.cameraRig.MoodTarget, 1e-4f);
            yield return StepUntil(() => game.Phase == GamePhase.GameOver, "the game to end");
        }

        [UnityTest]
        public IEnumerator Every_button_creaks_when_pressed()
        {
            game.ShowMenu();
            game.sound.ClearHistory();
            game.ui.difficultyButtons[0].onClick.Invoke();
            CollectionAssert.Contains(game.sound.History, SoundId.UiCreak);
            yield return null;
        }
    }
}
