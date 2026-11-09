using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace Prototypes.MedievalChess.Tests
{
    public class DuelPlayTests
    {
        const string ScenePath = "Assets/Prototypes/MedievalChess/Scenes/Duel.unity";
        const float Step = 1f / 60f;
        const float Tol = 0.01f;

        DuelController duel;
        float savedStrength;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return null;
#endif
            duel = Object.FindAnyObjectByType<DuelController>();
            Assert.IsNotNull(duel, "the Duel scene has no controller");
            savedStrength = duel.settings.effectStrength;
            duel.autoStep = false; // the tests step the fight by hand
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (duel != null) duel.settings.effectStrength = savedStrength; // never leave the asset changed
            yield return null;
        }

        int RunToEnd(float step = Step)
        {
            int frames = 0;
            while (!duel.Timeline.Finished && frames < 2000)
            {
                duel.Step(step);
                frames++;
            }
            return frames;
        }

        [UnityTest]
        public IEnumerator The_two_pieces_stand_ready_on_the_board()
        {
            duel.SetUp();
            yield return null;
            Assert.IsNotNull(duel.Attacker);
            Assert.IsNotNull(duel.Victim);
            Assert.AreEqual(PieceType.Rook, duel.Attacker.Type);
            Assert.AreEqual(PieceType.Pawn, duel.Victim.Type);
            Assert.IsTrue(duel.Attacker.gameObject.activeSelf);
            Assert.IsTrue(duel.Victim.gameObject.activeSelf);
            Assert.AreEqual(duel.attackerStart, duel.Attacker.transform.position);
            Assert.IsTrue(duel.Attacker.HasPart("ArmR") && duel.Victim.HasPart("Head"));
        }

        [UnityTest]
        public IEnumerator After_the_fight_the_victim_is_gone_and_the_attacker_stands_on_its_square()
        {
            duel.Play();
            RunToEnd();
            yield return null;
            Assert.IsFalse(duel.Victim.gameObject.activeSelf);
            Assert.IsTrue(duel.Attacker.gameObject.activeSelf);
            Assert.AreEqual(duel.victimPosition.x, duel.Attacker.transform.position.x, Tol);
            Assert.AreEqual(duel.victimPosition.z, duel.Attacker.transform.position.z, Tol);
        }

        [UnityTest]
        public IEnumerator After_the_fight_the_attackers_arms_are_back_at_rest()
        {
            duel.Play();
            var arm = duel.Attacker.Part("ArmR");
            var rest = arm.localRotation;
            RunToEnd();
            yield return null;
            Assert.AreEqual(0f, Quaternion.Angle(rest, arm.localRotation), 0.5f);
        }

        [UnityTest]
        public IEnumerator The_rook_raises_its_fists_before_the_slam()
        {
            duel.Play();
            var arm = duel.Attacker.Part("ArmR");
            var rest = arm.localRotation;
            duel.ShowAt(duel.Plan.BlowTime - 0.02f);
            yield return null;
            Assert.Greater(Quaternion.Angle(rest, arm.localRotation), 90f);
        }

        [UnityTest]
        public IEnumerator The_pawn_thrusts_its_spear_forward_on_the_blow()
        {
            duel.attackerType = PieceType.Pawn;
            duel.victimType = PieceType.Rook;
            duel.Play();
            var arm = duel.Attacker.Part("ArmR");
            var spear = duel.Attacker.Part("Weapon");
            var rest = arm.localRotation;
            duel.ShowAt(duel.Plan.BlowTime);
            yield return null;
            Assert.Greater(Quaternion.Angle(rest, arm.localRotation), 60f);
            // the spear tip points along the attacker's facing, toward the victim (+x here)
            Assert.Greater(spear.up.x, 0.8f);
        }

        [UnityTest]
        public IEnumerator The_victim_topples_away_from_the_attacker()
        {
            duel.Play();
            duel.ShowAt((duel.Plan.BlowTime + duel.Plan.CollapseEnd) * 0.5f + 0.2f);
            yield return null;
            // the victim's up axis has tipped toward +x, away from the attacker at -x
            Assert.Greater(duel.Victim.transform.up.x, 0.5f);
        }

        [UnityTest]
        public IEnumerator Quick_takes_about_a_third_of_the_frames_and_Off_takes_none()
        {
            duel.speed = AnimationSpeed.Full;
            duel.Play();
            int full = RunToEnd();

            duel.speed = AnimationSpeed.Quick;
            duel.Play();
            int quick = RunToEnd();

            duel.speed = AnimationSpeed.Off;
            duel.Play();
            yield return null;
            Assert.IsTrue(duel.Timeline.Finished, "Off is over at once");
            Assert.IsFalse(duel.Victim.gameObject.activeSelf);

            Assert.Less(quick, full * 0.5f);
            Assert.Greater(quick, full * 0.2f);
        }

        [UnityTest]
        public IEnumerator Every_speed_ends_in_the_same_place()
        {
            var ends = new Vector3[3];
            var speeds = new[] { AnimationSpeed.Full, AnimationSpeed.Quick, AnimationSpeed.Off };
            for (int i = 0; i < 3; i++)
            {
                duel.speed = speeds[i];
                duel.Play();
                RunToEnd();
                ends[i] = duel.Attacker.transform.position;
                Assert.IsFalse(duel.Victim.gameObject.activeSelf, speeds[i].ToString());
            }
            yield return null;
            Assert.AreEqual(ends[0].x, ends[1].x, Tol);
            Assert.AreEqual(ends[0].x, ends[2].x, Tol);
            Assert.AreEqual(ends[0].z, ends[2].z, Tol);
        }

        [UnityTest]
        public IEnumerator The_camera_pushes_in_and_shakes_on_the_blow_then_settles_back_exactly()
        {
            duel.speed = AnimationSpeed.Full;
            duel.Play();
            float farthest = 0f;
            while (!duel.Timeline.Finished)
            {
                duel.Step(Step);
                farthest = Mathf.Max(farthest, Vector3.Distance(duel.CameraPosition, duel.CameraBasePosition));
            }
            for (int i = 0; i < 90; i++) duel.Step(Step); // let the shake die out
            yield return null;
            Assert.Greater(farthest, 0.5f, "the camera should push in during the fight");
            Assert.AreEqual(0f, Vector3.Distance(duel.CameraPosition, duel.CameraBasePosition), 0.0001f);
        }

        [UnityTest]
        public IEnumerator With_effect_strength_zero_the_camera_never_moves_and_there_is_no_hit_stop()
        {
            duel.settings.effectStrength = 0f;
            duel.speed = AnimationSpeed.Full;
            duel.Play();
            while (!duel.Timeline.Finished)
            {
                duel.Step(Step);
                Assert.IsFalse(duel.Timeline.HitStopActive);
                Assert.AreEqual(0f, Vector3.Distance(duel.CameraPosition, duel.CameraBasePosition), 0.0001f);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Playing_again_gives_the_same_ending()
        {
            duel.Play();
            RunToEnd();
            var first = duel.Attacker.transform.position;
            duel.Play();
            Assert.IsTrue(duel.Victim.gameObject.activeSelf, "the victim is back for the replay");
            RunToEnd();
            yield return null;
            Assert.AreEqual(first, duel.Attacker.transform.position);
        }

        // --- all six attackers, and the moves ---

        [UnityTest]
        public IEnumerator Every_attacker_type_kills_a_pawn_and_ends_on_its_square()
        {
            foreach (PieceType type in System.Enum.GetValues(typeof(PieceType)))
            {
                duel.attackerType = type;
                duel.victimType = PieceType.Pawn;
                duel.speed = AnimationSpeed.Full;
                duel.Play();
                Assert.AreEqual(type, duel.Attacker.Type);
                RunToEnd();
                yield return null;
                Assert.IsFalse(duel.Victim.gameObject.activeSelf, type.ToString());
                Assert.AreEqual(duel.victimPosition.x, duel.Attacker.transform.position.x, Tol, type.ToString());
                Assert.AreEqual(0f, Quaternion.Angle(Quaternion.Euler(0f, 90f, 0f), duel.Attacker.transform.rotation), 1f,
                    $"{type} turns back to face as it did before");
            }
        }

        [UnityTest]
        public IEnumerator Every_victim_type_is_killed_by_the_queen()
        {
            foreach (PieceType type in System.Enum.GetValues(typeof(PieceType)))
            {
                duel.attackerType = PieceType.Queen;
                duel.victimType = type;
                duel.speed = AnimationSpeed.Full;
                duel.Play();
                Assert.AreEqual(type, duel.Victim.Type);
                RunToEnd();
                yield return null;
                Assert.IsFalse(duel.Victim.gameObject.activeSelf, type.ToString());
            }
        }

        static Vector3 At(PieceActor a) => a.transform.position;

        void AssertAt(PieceActor a, float x, float z, string what)
        {
            Assert.IsTrue(a.gameObject.activeSelf, $"{what} should be on the board");
            Assert.AreEqual(x, At(a).x, Tol, what);
            Assert.AreEqual(z, At(a).z, Tol, what);
            Assert.AreEqual(0f, At(a).y, Tol, what);
        }

        void PlayDemoToEnd(int index, AnimationSpeed speed = AnimationSpeed.Full)
        {
            duel.speed = speed;
            duel.PlayDemo(index);
            RunToEnd();
        }

        [UnityTest]
        public IEnumerator The_quiet_moves_end_on_their_squares()
        {
            PlayDemoToEnd(0);
            AssertAt(duel.Actor("mover"), -1f, 1f, "bishop");
            PlayDemoToEnd(1);
            AssertAt(duel.Actor("mover"), 0f, 0f, "knight");
            PlayDemoToEnd(2);
            AssertAt(duel.Actor("mover"), 2f, 0f, "rook");
            yield return null;
        }

        [UnityTest]
        public IEnumerator A_capture_slides_up_fights_and_the_victim_is_gone()
        {
            PlayDemoToEnd(3);
            AssertAt(duel.Actor("mover"), 1f, 0f, "rook");
            Assert.IsFalse(duel.Actor("victim").gameObject.activeSelf);
            yield return null;
        }

        [UnityTest]
        public IEnumerator En_passant_removes_the_pawn_beside_it_and_lands_on_the_empty_square()
        {
            duel.SetUpDemo(4);
            Assert.AreEqual(1f, At(duel.Actor("victim")).x, Tol);
            Assert.AreEqual(0f, At(duel.Actor("victim")).z, Tol);
            PlayDemoToEnd(4);
            AssertAt(duel.Actor("mover"), 1f, 1f, "the capturing pawn");
            Assert.IsFalse(duel.Actor("victim").gameObject.activeSelf);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Castling_moves_both_the_king_and_the_rook()
        {
            PlayDemoToEnd(5);
            AssertAt(duel.Actor("mover"), 2f, 0f, "king");
            AssertAt(duel.Actor("rook"), 1f, 0f, "rook");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Promotion_replaces_the_pawn_with_the_chosen_piece()
        {
            PlayDemoToEnd(6);
            Assert.IsFalse(duel.Actor("mover").gameObject.activeSelf, "the pawn is gone");
            var queen = duel.Actor("promoted");
            AssertAt(queen, 0f, 1f, "the new queen");
            Assert.AreEqual(PieceType.Queen, queen.Type);
            Assert.AreEqual(Vector3.one, queen.transform.localScale);

            PlayDemoToEnd(7);
            Assert.IsFalse(duel.Actor("victim").gameObject.activeSelf);
            Assert.AreEqual(PieceType.Knight, duel.Actor("promoted").Type);
            AssertAt(duel.Actor("promoted"), 1f, 1f, "the new knight");
            yield return null;
        }

        [UnityTest]
        public IEnumerator A_check_makes_the_king_brace_and_stand_up_again()
        {
            PlayDemoToEnd(8);
            var king = duel.Actor("king");
            Assert.IsTrue(king.gameObject.activeSelf);
            Assert.Greater(king.transform.up.y, 0.99f);
            var endPose = king.Part("ArmL").localRotation;

            duel.SetUpDemo(8); // a fresh king shows the resting pose
            var rest = duel.Actor("king").Part("ArmL").localRotation;
            Assert.AreEqual(0f, Quaternion.Angle(rest, endPose), 0.5f, "the shield arm is back down");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Checkmate_leaves_the_king_lying_on_the_board()
        {
            PlayDemoToEnd(9);
            var king = duel.Actor("king");
            Assert.IsTrue(king.gameObject.activeSelf);
            Assert.Less(king.transform.up.y, 0.2f, "the king is lying down");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Resigning_leaves_the_king_kneeling()
        {
            PlayDemoToEnd(10);
            var king = duel.Actor("king");
            Assert.IsTrue(king.gameObject.activeSelf);
            Assert.Less(Vector3.Dot(king.Part("Body").up, Vector3.up), 0.9f, "the body is bowed");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Every_demo_ends_in_the_same_place_at_every_speed()
        {
            for (int demo = 0; demo < DuelDemos.Count; demo++)
            {
                var ends = new System.Collections.Generic.List<string>();
                foreach (var speed in new[] { AnimationSpeed.Full, AnimationSpeed.Quick, AnimationSpeed.Off })
                {
                    PlayDemoToEnd(demo, speed);
                    var sb = new System.Text.StringBuilder();
                    foreach (var id in new[] { "mover", "victim", "rook", "promoted", "king" })
                    {
                        var a = duel.Actor(id);
                        if (a == null) continue;
                        var p = a.transform.position;
                        sb.Append($"{id}:{a.gameObject.activeSelf}:{p.x:F1},{p.z:F1}:{a.transform.up.y:F1};");
                    }
                    ends.Add(sb.ToString());
                }
                Assert.AreEqual(ends[0], ends[1], $"demo {demo} ({DuelDemos.Names[demo]}): Quick differs from Full");
                Assert.AreEqual(ends[0], ends[2], $"demo {demo} ({DuelDemos.Names[demo]}): Off differs from Full");
            }
            yield return null;
        }
    }
}
