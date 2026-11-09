using Shared.Feel;
using UnityEngine;

namespace Prototypes.MedievalChess
{
    /// <summary>
    /// The game camera. It sits behind the player's side, looking down at the board. In a fight it pushes in
    /// toward the fight and lowers, keeps looking at it, and shakes on the blow.
    /// </summary>
    public sealed class GameCameraRig : MonoBehaviour
    {
        public MedievalChessSettings settings;
        public Camera cam;

        readonly ShakeState shake = new ShakeState();
        readonly Ease1D mood = new Ease1D();
        Vector3? moodFocus;
        Vector3 centre;
        Vector3 basePosition;

        public Vector3 BasePosition => basePosition;
        /// <summary>How far the camera is moved toward what matters at this moment (negative pulls back), before the effect strength.</summary>
        public float Mood => mood.Value;
        public float MoodTarget => mood.Target;

        /// <summary>Eases the camera's mood to a value over a time. The focus is where it looks (the board's middle if none).</summary>
        public void SetMood(float target, float seconds, Vector3? focus = null)
        {
            moodFocus = focus;
            mood.MoveTo(target, seconds);
        }

        public void SnapMood(float value)
        {
            moodFocus = null;
            mood.Snap(value);
        }
        public Vector3 Centre => centre;

        /// <summary>Puts the camera behind the given side, looking at the middle of the board.</summary>
        public void SetPlayerSide(ChessSide side)
        {
            var g = settings.game;
            centre = new Vector3(0f, g.cameraFocusHeight, 0f);
            var rotation = Quaternion.Euler(g.cameraPitch, 0f, 0f);
            basePosition = centre - (rotation * Vector3.forward) * g.cameraDistance;
            if (side == ChessSide.Black) basePosition = new Vector3(-basePosition.x, basePosition.y, -basePosition.z);
            cam.fieldOfView = g.cameraFieldOfView;
            Tick(0f, 0f, null);
        }

        public void AddShake(float strength) => shake.Add(strength, Vector2.up, settings.fight.shakeDuration);

        /// <summary>Moves the camera for this frame. Push is 0 to 1, and the focus is where the fight is.</summary>
        public void Tick(float dt, float push, Vector3? focus)
        {
            shake.Update(dt);
            mood.Update(dt);
            var f = settings.fight;
            var target = focus ?? centre;
            var position = basePosition + (target - basePosition) * (f.cameraPush * push);
            position.y -= basePosition.y * f.cameraLower * push;
            var look = Vector3.Lerp(centre, target, 0.8f * push);

            // a slow mood move (the start, checkmate, resigning, a draw), scaled by the effect strength
            float m = mood.Value * settings.effectStrength;
            if (Mathf.Abs(m) > 0.0001f)
            {
                var moodTarget = moodFocus ?? centre;
                position += (moodTarget - basePosition) * (m * 0.5f);
                if (m > 0f) position.y -= basePosition.y * f.cameraLower * m * 0.5f;
                look = Vector3.Lerp(look, moodTarget, 0.8f * Mathf.Max(0f, m));
            }
            cam.transform.position = position;
            cam.transform.rotation = Quaternion.LookRotation(look - position);
            cam.transform.position = position + cam.transform.right * shake.Offset.x + cam.transform.up * shake.Offset.y;
        }
    }
}
