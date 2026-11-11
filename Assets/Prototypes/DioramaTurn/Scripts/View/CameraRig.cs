using UnityEngine;

namespace Prototypes.DioramaTurn
{
    /// <summary>Places the camera for a view and eases a quarter turn with a small overshoot.</summary>
    public sealed class CameraRig : MonoBehaviour
    {
        public DioramaTurnSettings settings;
        public Camera camera;
        [Tooltip("Rotates with the camera, so every view is lit the same way.")] public Light keyLight;

        /// <summary>The point the camera looks at: the middle of the room box.</summary>
        public static readonly Vector3 Focus = new Vector3(0f, 2f, 0f);

        int lastFromView = -1;
        int lastToView = -1;
        int lastHeight = -1;
        float yaw;
        float startYaw;
        float targetYaw;

        /// <summary>The camera's yaw this frame; sprites face it.</summary>
        public float Yaw => yaw;

        /// <summary>Pointing from the room centre toward the camera, flat. Sprites are pushed this way.</summary>
        public Vector3 PushDirection
        {
            get
            {
                var toCamera = transform.position - Focus;
                toCamera.y = 0f;
                return toCamera.sqrMagnitude > 0.0001f ? toCamera.normalized : Vector3.forward;
            }
        }

        public static int TurnDirection(int fromView, int toView) => CameraMath.TurnDirection(fromView, toView);

        void OnPreCull()
        {
            // Runs just before the camera renders, when its pixel rect is final (a capture sets its own size).
            ApplySize();
        }

        /// <summary>Puts the camera exactly on a view's angle (the builder and tests use this).</summary>
        public void SnapToView(int view)
        {
            lastFromView = -1;
            lastToView = -1;
            yaw = CameraMath.YawForView(view, settings.camera.baseYawDeg);
            Apply();
        }

        /// <summary>Shows a turn in progress: progress 0 to 1 from one view to the next.</summary>
        public void ShowTurn(int fromView, int toView, float progress)
        {
            if (fromView != lastFromView || toView != lastToView)
            {
                lastFromView = fromView;
                lastToView = toView;
                startYaw = CameraMath.YawForView(fromView, settings.camera.baseYawDeg);
                targetYaw = startYaw + 90f * CameraMath.TurnDirection(fromView, toView);
            }
            float overshoot = Feel.OvershootDeg(settings.turn.overshootDeg, settings.feel.effectStrength) / 90f;
            yaw = Mathf.LerpUnclamped(startYaw, targetYaw, TurnEase.BackOut(progress, overshoot));
            Apply();
        }

        void Apply()
        {
            var c = settings.camera;
            var rotation = CameraMath.Rotation(c.pitchDeg, yaw);
            transform.SetPositionAndRotation(CameraMath.Position(Focus, rotation, c.distance), rotation);
            ApplySize();
            if (keyLight != null)
            {
                keyLight.transform.rotation = Quaternion.Euler(
                    settings.light.keyPitchDeg,
                    yaw + settings.light.keyYawOffsetDeg,
                    0f);
            }
        }

        void ApplySize()
        {
            if (camera == null) return;
            // The camera's own pixel height is what a capture renders at; the game window is the fallback.
            int height = Application.isPlaying
                ? (camera.pixelHeight > 0 ? camera.pixelHeight : Screen.height)
                : settings.camera.referenceHeight;
            // A resizing game view can report a tiny height for one frame; do not bake that in.
            if (height < 240) return;
            if (height == lastHeight) return;
            lastHeight = height;
            int scale = PixelScale.Scale(height, settings.camera.pixelsPerUnit, settings.camera.roomExtentUnits, settings.camera.maxScale);
            camera.orthographicSize = PixelScale.OrthoSize(height, settings.camera.pixelsPerUnit, scale);
        }
    }
}
