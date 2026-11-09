using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Prototypes.MedievalChess.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Prototypes.MedievalChess.Tests
{
    public class FeelCueTests
    {
        readonly FeelSettings feel = new FeelSettings();
        readonly FightSettings fight = new FightSettings();
        readonly MoveSettings moves = new MoveSettings();

        static ActorSpec Spec(string id, PieceType type, int side, Vector3 at, float yaw = 0f)
            => new ActorSpec { Id = id, Type = type, Side = side, Start = at, StartYaw = yaw };

        FightPlan Fight(PieceType attacker) => (FightPlan)FightLibrary.Create(attacker, PieceType.Pawn, Vector3.zero, new Vector3(0f, 0f, 1.5f), 0f, 180f, fight, "a", "v", 0, 1);

        [TestCase(PieceType.Pawn, SoundId.SwingPawn)]
        [TestCase(PieceType.Knight, SoundId.SwingKnight)]
        [TestCase(PieceType.Bishop, SoundId.SwingBishop)]
        [TestCase(PieceType.Rook, SoundId.SwingRook)]
        [TestCase(PieceType.Queen, SoundId.SwingQueen)]
        [TestCase(PieceType.King, SoundId.SwingKing)]
        public void A_fight_swings_then_lands_the_blow_then_the_victim_collapses(PieceType attacker, SoundId swing)
        {
            var plan = Fight(attacker);
            var cues = SoundCues.For(plan, feel);
            var swingCue = cues.Single(c => c.Sound == swing);
            var impact = cues.Single(c => c.Sound == SoundId.Impact);
            var collapse = cues.Single(c => c.Sound == SoundId.Collapse);
            Assert.AreEqual(plan.BlowTime, impact.Time, 1e-5f);
            Assert.Less(swingCue.Time, impact.Time, "the swing comes before the blow");
            Assert.Greater(collapse.Time, impact.Time, "the collapse comes after the blow");
            Assert.AreEqual(EffectKind.Sparks, impact.Effect);
            Assert.AreEqual(EffectKind.Embers, collapse.Effect);
            Assert.AreEqual(1, collapse.Actor, "the embers rise from the victim, the second piece of the step");
            Assert.IsTrue(cues.Any(c => c.Sound == SoundId.Drum && Mathf.Approximately(c.Time, plan.BlowTime)), "a drum hit lands with the blow");
            Assert.AreEqual(1, cues.Count(c => c.Sound == swing));
        }

        [Test]
        public void Each_attacker_type_has_its_own_swing_sound()
        {
            var swings = new[] { PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen, PieceType.King }.Select(SoundIds.Swing).ToArray();
            Assert.AreEqual(6, swings.Distinct().Count());
        }

        [Test]
        public void Every_footfall_of_a_walk_is_a_thud_with_dust()
        {
            var plan = MoveLibrary.Travel(Spec("p", PieceType.Pawn, 0, Vector3.zero), new Vector3(0f, 0f, 2f), moves);
            var cues = SoundCues.For(plan, feel);
            Assert.GreaterOrEqual(plan.Footfalls.Count, 2);
            foreach (float f in plan.Footfalls)
                Assert.IsTrue(cues.Any(c => c.Sound == SoundId.Footstep && Mathf.Approximately(c.Time, f) && c.Effect == EffectKind.Dust), $"no thud at {f}");
        }

        [Test]
        public void A_knights_leap_and_a_rooks_slide_have_their_own_sound_and_land_with_a_thud()
        {
            var leap = SoundCues.For(MoveLibrary.Travel(Spec("n", PieceType.Knight, 0, Vector3.zero), new Vector3(1f, 0f, 2f), moves), feel);
            Assert.IsTrue(leap.Any(c => c.Sound == SoundId.KnightLeap && c.Time == 0f));
            Assert.IsTrue(leap.Any(c => c.Sound == SoundId.Footstep), "a landing");
            var slide = SoundCues.For(MoveLibrary.Travel(Spec("r", PieceType.Rook, 0, Vector3.zero), new Vector3(0f, 0f, 4f), moves), feel);
            Assert.IsTrue(slide.Any(c => c.Sound == SoundId.RookSlide && c.Time == 0f));
            Assert.IsTrue(slide.Any(c => c.Sound == SoundId.Footstep), "the rook stops with a thud");
        }

        [Test]
        public void Castling_gives_a_thud_for_the_king_and_the_rook()
        {
            var plan = MoveLibrary.Castle(Spec("k", PieceType.King, 0, Vector3.zero), new Vector3(2f, 0f, 0f), Spec("r", PieceType.Rook, 0, new Vector3(3f, 0f, 0f)), new Vector3(1f, 0f, 0f), moves);
            Assert.GreaterOrEqual(SoundCues.For(plan, feel).Count(c => c.Sound == SoundId.Footstep), 2);
        }

        [Test]
        public void A_promotion_rings_the_anvil_when_the_pawn_becomes_the_new_piece()
        {
            var plan = MoveLibrary.Promote(Spec("p", PieceType.Pawn, 0, Vector3.zero), PieceType.Queen, "p->Q", moves);
            var cue = SoundCues.For(plan, feel).Single(c => c.Sound == SoundId.PromotionAnvil);
            Assert.AreEqual(plan.SwapTime, cue.Time, 1e-5f);
            Assert.AreEqual(EffectKind.Sparks, cue.Effect, "the forge flash");
        }

        [Test]
        public void Resigning_sounds_a_low_horn_and_the_mated_king_falls_with_a_collapse()
        {
            var king = Spec("k", PieceType.King, 1, Vector3.zero, 180f);
            Assert.IsTrue(SoundCues.For(MoveLibrary.Resign(king, moves), feel).Any(c => c.Sound == SoundId.ResignHorn && c.Time == 0f));
            var fall = SoundCues.For(MoveLibrary.Fall(king, moves), feel);
            Assert.IsTrue(fall.Any(c => c.Sound == SoundId.Collapse && c.Effect == EffectKind.Embers));
        }

        [Test]
        public void Cutting_time_into_any_frames_plays_every_cue_exactly_once()
        {
            var plan = Fight(PieceType.Knight);
            var cues = SoundCues.For(plan, feel);
            var rng = new System.Random(5);
            for (int round = 0; round < 20; round++)
            {
                var heard = new List<FeelCue>();
                float t = 0f;
                SoundCues.Between(cues, -1f, 0f, heard); // the frame in which the step starts
                while (t < plan.Duration)
                {
                    float next = Mathf.Min(plan.Duration, t + (float)rng.NextDouble() * 0.2f + 0.001f);
                    SoundCues.Between(cues, t, next, heard);
                    t = next;
                }
                Assert.AreEqual(cues.Count, heard.Count, $"round {round}");
            }
        }

        [Test]
        public void Every_sound_has_a_snake_case_file_name_and_they_are_all_different()
        {
            var ids = (SoundId[])System.Enum.GetValues(typeof(SoundId));
            Assert.AreEqual("swing_pawn", SoundIds.FileName(SoundId.SwingPawn));
            Assert.AreEqual("check_horn", SoundIds.FileName(SoundId.CheckHorn));
            Assert.AreEqual("drum", SoundIds.FileName(SoundId.Drum));
            Assert.AreEqual(ids.Length, ids.Select(SoundIds.FileName).Distinct().Count());
        }
    }

    public class FeelStateTests
    {
        [Test]
        public void Slow_motion_runs_slowly_for_a_stretch_of_real_time_and_then_stops()
        {
            var slow = new SlowMotion();
            slow.Begin(0.3f, 1.2f, 1f);
            float game = 0f;
            for (int i = 0; i < 60; i++) game += slow.Apply(0.02f); // 1.2 s of real time
            Assert.AreEqual(1.2f * 0.3f, game, 1e-3f);
            Assert.IsFalse(slow.Active);
            Assert.AreEqual(0.02f, slow.Apply(0.02f), 1e-6f, "back to normal speed afterwards");
        }

        [Test]
        public void A_frame_that_crosses_the_end_of_slow_motion_is_split_correctly()
        {
            var slow = new SlowMotion();
            slow.Begin(0.5f, 0.1f, 1f);
            Assert.AreEqual(0.1f * 0.5f + 0.4f, slow.Apply(0.5f), 1e-5f);
        }

        [Test]
        public void At_effect_strength_zero_there_is_no_slow_motion_and_at_half_it_is_gentler()
        {
            var off = new SlowMotion();
            off.Begin(0.3f, 1.2f, 0f);
            Assert.IsFalse(off.Active);
            Assert.AreEqual(0.1f, off.Apply(0.1f), 1e-6f);

            var half = new SlowMotion();
            half.Begin(0.3f, 1.2f, 0.5f);
            Assert.AreEqual(0.1f * 0.65f, half.Apply(0.1f), 1e-5f);
        }

        [Test]
        public void An_eased_value_reaches_its_target_in_the_given_time_and_never_overshoots()
        {
            var e = new Ease1D(-0.35f);
            e.MoveTo(0f, 1.4f);
            float last = e.Value;
            for (int i = 0; i < 70; i++)
            {
                e.Update(0.02f);
                Assert.GreaterOrEqual(e.Value, last - 1e-6f, "it only rises toward the target");
                Assert.LessOrEqual(e.Value, 0f + 1e-6f);
                last = e.Value;
            }
            Assert.AreEqual(0f, e.Value, 1e-5f);
            e.Snap(0.5f);
            Assert.AreEqual(0.5f, e.Value);
        }

        [Test]
        public void The_check_ring_pulses_within_its_amount_and_an_amount_of_zero_is_steady()
        {
            for (int i = 0; i < 200; i++)
            {
                float f = HighlightView.PulseFactor(i * 0.037f, 1.6f, 0.12f);
                Assert.That(f, Is.InRange(0.88f - 1e-4f, 1.12f + 1e-4f));
                Assert.AreEqual(1f, HighlightView.PulseFactor(i * 0.037f, 1.6f, 0f));
            }
            Assert.Greater(HighlightView.PulseFactor(0.15f, 1.6f, 0.12f), HighlightView.PulseFactor(0.45f, 1.6f, 0.12f));
        }
    }

    public class SoundFileTests
    {
        const string Folder = "Assets/Prototypes/MedievalChess/Audio";

        public static IEnumerable<SoundId> AllSounds => (SoundId[])System.Enum.GetValues(typeof(SoundId));

        [TestCaseSource(nameof(AllSounds))]
        public void Every_sound_exists_is_audible_does_not_clip_and_ends_near_zero(SoundId id)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Folder}/{SoundIds.FileName(id)}.wav");
            Assert.IsNotNull(clip, $"{id}: run Tools/audio/MedievalChess/make_sounds.py");
            Assert.Less(clip.length, 2f, "a sound is under two seconds");
            Assert.Greater(clip.length, 0.02f);
            var data = new float[clip.samples * clip.channels];
            Assert.IsTrue(clip.GetData(data, 0), "the samples can be read");
            float peak = data.Max(Mathf.Abs);
            Assert.Greater(peak, 0.1f, "not silent");
            Assert.Less(peak, 0.95f, "does not clip");
            float tail = data.Skip(data.Length - 20).Max(Mathf.Abs);
            Assert.Less(tail, 0.01f, "ends near zero, so it does not click");
        }
    }

    public class FeelBuilderTests
    {
        const string TempScene = "Assets/MedievalChessFeelTest_Temp.unity";
        MedievalChessSettings settings;

        [OneTimeSetUp]
        public void BuildOnce()
        {
            GameSceneBuilder.Build(TempScene);
            settings = AssetDatabase.LoadAssetAtPath<MedievalChessSettings>(DuelSceneBuilder.SettingsPath);
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(TempScene);
        }

        [Test]
        public void The_sound_bank_has_a_clip_for_every_sound_and_several_sources()
        {
            var bank = Object.FindFirstObjectByType<SoundBank>();
            Assert.IsNotNull(bank);
            foreach (SoundId id in System.Enum.GetValues(typeof(SoundId))) Assert.IsNotNull(bank.Clip(id), id.ToString());
            Assert.GreaterOrEqual(bank.sources.Length, 4, "sounds can overlap");
            foreach (var s in bank.sources) Assert.IsFalse(s.playOnAwake);
        }

        [Test]
        public void The_effects_have_dust_embers_and_sparks_that_are_emitted_by_code()
        {
            var view = Object.FindFirstObjectByType<EffectsView>();
            Assert.IsNotNull(view);
            foreach (var ps in new[] { view.dust, view.embers, view.sparks })
            {
                Assert.IsNotNull(ps);
                Assert.IsFalse(ps.emission.enabled);
                Assert.IsFalse(ps.main.playOnAwake);
                Assert.AreEqual(ParticleSystemSimulationSpace.World, ps.main.simulationSpace);
            }
        }

        [Test]
        public void The_wash_is_the_first_thing_in_the_canvas_so_the_menus_draw_over_it_and_clicks_pass_through()
        {
            var ui = Object.FindFirstObjectByType<UiView>(FindObjectsInactive.Include);
            Assert.IsNotNull(ui.wash);
            Assert.IsFalse(ui.wash.raycastTarget);
            Assert.AreEqual(0, ui.wash.transform.GetSiblingIndex());
            Assert.AreEqual(0f, ui.wash.color.a);
        }

        [Test]
        public void The_controller_knows_the_audio_the_effects_and_the_torches()
        {
            var controller = Object.FindFirstObjectByType<GameController>();
            Assert.IsNotNull(controller.sound);
            Assert.IsNotNull(controller.effects);
            Assert.AreEqual(4, controller.torches.Length);
        }

        [Test]
        public void The_feel_settings_have_sane_starting_values()
        {
            var f = settings.feel;
            Assert.Greater(f.masterVolume, 0f);
            Assert.Less(f.slowMotionScale, 1f);
            Assert.AreEqual(1.2f, f.slowMotionSeconds, 1e-4f, "the spec's slow motion length");
            Assert.Less(f.startMood, 0f, "the camera starts back in the hall");
            Assert.Greater(f.mateMood, 0f);
            Assert.Less(f.drawMood, 0f);
        }
    }
}
