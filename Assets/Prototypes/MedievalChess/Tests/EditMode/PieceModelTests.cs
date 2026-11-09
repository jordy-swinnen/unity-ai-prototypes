using System.Linq;
using Prototypes.MedievalChess.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Prototypes.MedievalChess.Tests
{
    /// <summary>
    /// Checks the models the Blender script wrote, after Unity imported them: the parts are there,
    /// the right arm is on the right, the pivots work, and the sizes and triangle counts are as planned.
    /// </summary>
    public class PieceModelTests
    {
        const string Folder = "Assets/Prototypes/MedievalChess/Art/Exports";

        static readonly string[] Required = { "Base", "Body", "Head", "ArmR", "Weapon", "ArmL", "Shield" };

        public static object[] Pieces =
        {
            new object[] { "pawn", 0.5f },
            new object[] { "knight", 0.86f },
            new object[] { "bishop", 0.82f },
            new object[] { "rook", 0.75f },
            new object[] { "queen", 1.04f },
            new object[] { "king", 1.12f },
        };

        static GameObject Load(string name)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/{name}.glb");
            Assert.IsNotNull(go, $"{name}.glb did not import. Run Tools/blender/MedievalChess/make_pieces.py");
            return go;
        }

        static Transform Part(GameObject go, string name)
        {
            var t = go.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == name);
            Assert.IsNotNull(t, $"{go.name} has no part named {name}");
            return t;
        }

        [TestCaseSource(nameof(Pieces))]
        public void Every_piece_has_the_named_parts(string name, float height)
        {
            var go = Load(name);
            foreach (var part in Required) Part(go, part);
        }

        [TestCaseSource(nameof(Pieces))]
        public void The_weapon_hangs_from_the_right_arm_and_the_shield_from_the_left(string name, float height)
        {
            var go = Load(name);
            Assert.AreEqual(Part(go, "ArmR"), Part(go, "Weapon").parent);
            Assert.AreEqual(Part(go, "ArmL"), Part(go, "Shield").parent);
        }

        [TestCaseSource(nameof(Pieces))]
        public void The_right_arm_is_on_the_pieces_right_which_is_plus_x_when_it_faces_plus_z(string name, float height)
        {
            var go = Load(name);
            var right = go.transform.InverseTransformPoint(Part(go, "ArmR").position);
            var left = go.transform.InverseTransformPoint(Part(go, "ArmL").position);
            Assert.Greater(right.x, 0.1f);
            Assert.Less(left.x, -0.1f);
        }

        [TestCaseSource(nameof(Pieces))]
        public void The_arms_pivot_at_the_shoulder_not_at_the_base(string name, float height)
        {
            var go = Load(name);
            var arm = go.transform.InverseTransformPoint(Part(go, "ArmR").position);
            Assert.Greater(arm.y, 0.25f, "the arm's origin should be at the joint");
            Assert.Less(arm.y, height);
        }

        [TestCaseSource(nameof(Pieces))]
        public void Every_part_has_scale_one_so_rotations_are_clean(string name, float height)
        {
            var go = Load(name);
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                Assert.AreEqual(Vector3.one, t.localScale, $"{name}.{t.name}");
        }

        [TestCaseSource(nameof(Pieces))]
        public void The_height_and_footprint_are_as_planned(string name, float height)
        {
            var go = Load(name);
            // the arms and what they hold are not part of the piece's height
            var body = go.GetComponentsInChildren<Renderer>().Where(r => !IsUnderArm(r.transform)).ToArray();
            float top = body.Max(r => r.bounds.max.y);
            float bottom = body.Min(r => r.bounds.min.y);
            Assert.AreEqual(height, top, 0.06f);
            Assert.AreEqual(0f, bottom, 0.01f);

            var basePart = Part(go, "Base").GetComponent<Renderer>();
            Assert.LessOrEqual(basePart.bounds.size.x, 0.9f);
            Assert.LessOrEqual(basePart.bounds.size.z, 0.9f);
        }

        static bool IsUnderArm(Transform t)
        {
            for (var p = t; p != null; p = p.parent)
                if (p.name == "ArmR" || p.name == "ArmL") return true;
            return false;
        }

        [TestCaseSource(nameof(Pieces))]
        public void The_triangle_count_is_within_the_budget(string name, float height)
        {
            var go = Load(name);
            int tris = go.GetComponentsInChildren<MeshFilter>().Sum(m => m.sharedMesh.triangles.Length / 3);
            Assert.LessOrEqual(tris, 6000);
            Assert.Greater(tris, 100);
        }

        [TestCaseSource(nameof(Pieces))]
        public void Materials_are_named_body_iron_or_trim_so_the_side_colour_can_be_set(string name, float height)
        {
            var go = Load(name);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                foreach (var m in r.sharedMaterials)
                    Assert.IsTrue(m.name == "body" || m.name == "iron" || m.name == "trim", $"{name}.{r.name}: material '{m.name}'");
        }

        [Test]
        public void The_knight_has_a_horse_and_a_horse_head_that_can_move()
        {
            var go = Load("knight");
            Part(go, "Horse");
            var neck = Part(go, "HorseHead");
            Assert.Greater(go.transform.InverseTransformPoint(neck.position).y, 0.3f, "the neck pivot is up on the horse");
        }

        [TestCaseSource(nameof(Pieces))]
        public void Every_piece_has_a_face_and_two_pairs_of_shoulders_one_round_and_one_pointed(string name, float height)
        {
            var go = Load(name);
            Assert.AreEqual(Part(go, "Head"), Part(go, "Face").parent, "the face turns with the head");
            Assert.AreEqual(Part(go, "Body"), Part(go, "ShoulderBone").parent);
            Assert.AreEqual(Part(go, "Body"), Part(go, "ShoulderRed").parent);
            var round = Part(go, "ShoulderBone").GetComponent<MeshFilter>().sharedMesh.bounds.size;
            var pointed = Part(go, "ShoulderRed").GetComponent<MeshFilter>().sharedMesh.bounds.size;
            Assert.Greater(pointed.y, round.y, "the red side's shoulders reach up in a point, the bone side's are round");
        }

        [TestCaseSource(nameof(Pieces))]
        public void Binding_a_piece_shows_the_shoulders_of_its_side_only(string name, float height)
        {
            foreach (int side in new[] { 0, 1 })
            {
                var instance = Object.Instantiate(Load(name));
                try
                {
                    var mat = new Material(Shader.Find(DuelSceneBuilder.SurfaceShaderName));
                    var actor = PieceActor.Bind(instance, System.Enum.Parse<PieceType>(name, true), mat, mat, side);
                    Assert.AreEqual(side == 0, actor.Part("ShoulderBone").gameObject.activeSelf, $"{name} side {side}");
                    Assert.AreEqual(side == 1, actor.Part("ShoulderRed").gameObject.activeSelf, $"{name} side {side}");
                    Object.DestroyImmediate(mat);
                }
                finally { Object.DestroyImmediate(instance); }
            }
        }
    }
}
