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
            for (int v = 0; v < 4; v++)
                Assert.AreEqual(45f + 90f * v, CameraMath.YawForView(v, 45f), Tol);
            Assert.AreEqual(45f, CameraMath.YawForView(4, 45f), Tol, "four turns come back to view 0");
            Assert.AreEqual(315f, CameraMath.YawForView(-1, 45f), Tol);
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
            // View k looks from the r_k side, so a depth step must not move a cell on screen.
            for (int v = 0; v < 4; v++)
            {
                var cell = new Cell(1, 2, 3);
                var same = cell + ViewRule.DepthStep(v) * 2;
                var a = ViewRule.ScreenUnits(cell, v);
                var b = ViewRule.ScreenUnits(same, v);
                Assert.AreEqual(a.X, b.X, 0.0001f, $"view {v}");
                Assert.AreEqual(a.Y, b.Y, 0.0001f, $"view {v}");
            }
        }
    }
}
