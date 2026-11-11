using NUnit.Framework;
using UnityEngine;

namespace Prototypes.DioramaTurn.Tests
{
    public class CameraMathTests
    {
        const float Tol = 0.001f;

        [Test]
        public void Each_view_is_a_quarter_turn_from_the_base_yaw()
        {
            float[] expected = { 225f, 135f, 45f, 315f };
            for (int v = 0; v < 4; v++)
                Assert.AreEqual(expected[v], CameraMath.YawForView(v, 45f), Tol, $"view {v}");
            Assert.AreEqual(expected[0], CameraMath.YawForView(4, 45f), Tol, "four turns come back to view 0");
            Assert.AreEqual(expected[3], CameraMath.YawForView(-1, 45f), Tol);
        }

        [Test]
        public void The_pitch_of_a_true_isometric_camera_is_35_264_degrees()
        {
            var r = CameraMath.Rotation(35.264f, 45f);
            // In true isometric, the camera forward has equal absolute x, y and z components.
            var f = r * Vector3.forward;
            Assert.AreEqual(Mathf.Abs(f.x), Mathf.Abs(f.y), 0.001f);
            Assert.AreEqual(Mathf.Abs(f.y), Mathf.Abs(f.z), 0.001f);
        }

        [Test]
        public void The_camera_sits_the_configured_distance_from_the_focus_looking_at_it()
        {
            var focus = new Vector3(0f, 2f, 0f);
            var rot = CameraMath.Rotation(35.264f, 45f);
            var pos = CameraMath.Position(focus, rot, 30f);
            Assert.AreEqual(30f, Vector3.Distance(pos, focus), Tol);
            Assert.Greater(pos.y, focus.y, "the camera sits above the room, looking down at it");
            var toFocus = (focus - pos).normalized;
            Assert.AreEqual(0f, Vector3.Dot(rot * Vector3.forward - toFocus, rot * Vector3.up), Tol, "the focus is centred");
        }

        [Test]
        public void A_turn_direction_is_read_from_the_two_views()
        {
            Assert.AreEqual(1, CameraRig.TurnDirection(0, 1));
            Assert.AreEqual(-1, CameraRig.TurnDirection(1, 0));
            Assert.AreEqual(1, CameraRig.TurnDirection(3, 0), "turning past view 3 forward reaches view 0");
            Assert.AreEqual(-1, CameraRig.TurnDirection(0, 3));
        }

        [Test]
        public void The_screen_lattice_agrees_with_the_camera_yaw_of_each_view()
        {
            // View k looks from the r_k side: projecting a cell onto the camera's right and up axes
            // must equal ViewRule.ScreenUnits(cell, k), and a depth step must not move it on screen.
            for (int v = 0; v < 4; v++)
            {
                var rot = CameraMath.Rotation(35.2643897f, CameraMath.YawForView(v, 45f));
                var right = rot * Vector3.right;
                var up = rot * Vector3.up;

                var cell = new Cell(1, 2, 3);
                var world = new Vector3(cell.X, cell.Y, cell.Z);
                var screen = ViewRule.ScreenUnits(cell, v);
                Assert.AreEqual(screen.X, Vector3.Dot(world, right), 0.001f, $"view {v} right");
                Assert.AreEqual(screen.Y, Vector3.Dot(world, up), 0.001f, $"view {v} up");

                var shifted = cell + ViewRule.DepthStep(v) * 2;
                var shiftedWorld = new Vector3(shifted.X, shifted.Y, shifted.Z);
                Assert.AreEqual(Vector3.Dot(world, right), Vector3.Dot(shiftedWorld, right), 0.001f, $"view {v} depth right");
                Assert.AreEqual(Vector3.Dot(world, up), Vector3.Dot(shiftedWorld, up), 0.001f, $"view {v} depth up");
            }
        }
    }
}
