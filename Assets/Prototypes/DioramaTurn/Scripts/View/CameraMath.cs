using UnityEngine;

namespace Prototypes.DioramaTurn
{
    /// <summary>The camera's placement for a view, as plain maths so tests need no scene.</summary>
    public static class CameraMath
    {
        /// <summary>
        /// View 0 looks from the (+1, +1, +1) corner toward the origin (yaw = baseYaw + 180 = 225 deg),
        /// and each next view steps -90 deg around Y so the camera's screen axes match ViewRule.ScreenUnits.
        /// </summary>
        public static float YawForView(int view, float baseYawDeg) =>
            Mathf.Repeat(baseYawDeg + 180f - 90f * ViewRule.Wrap(view), 360f);

        public static Quaternion Rotation(float pitchDeg, float yawDeg) => Quaternion.Euler(pitchDeg, yawDeg, 0f);

        /// <summary>The camera sits this far behind the focus, looking straight at it.</summary>
        public static Vector3 Position(Vector3 focus, Quaternion rotation, float distance) =>
            focus - rotation * Vector3.forward * distance;

        public static float OrthoSize(float screenHeight, float pixelsPerUnit, int scale) =>
            PixelScale.OrthoSize(screenHeight, pixelsPerUnit, scale);

        /// <summary>+1 or -1: which way round the room turns to get from one view to the next.</summary>
        public static int TurnDirection(int fromView, int toView) => ((toView - fromView + 5) % 4) - 1;
    }
}
