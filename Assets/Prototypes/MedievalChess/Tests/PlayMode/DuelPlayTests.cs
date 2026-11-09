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
    }
}
