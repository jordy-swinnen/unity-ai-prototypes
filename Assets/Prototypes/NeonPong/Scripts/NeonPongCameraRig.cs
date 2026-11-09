using UnityEngine;

namespace Prototypes.NeonPong
{
    /// <summary>
    /// Moves the camera for the shake and for the slow push-in after a win. Both come from the
    /// feel state, so the global effect strength already scales them (zero means no movement).
    /// </summary>
    public class NeonPongCameraRig : MonoBehaviour
    {
        public NeonPongGame game;

        Vector3 basePosition;

        void Awake()
        {
            basePosition = transform.position;
        }

        void LateUpdate()
        {
            if (game == null || game.Feel == null) return;
            var feel = game.Feel;
            var shake = new Vector3(feel.Shake.Offset.x, 0f, feel.Shake.Offset.y);
            // Toward the table centre: the camera looks at the origin.
            var push = -basePosition.normalized * (basePosition.magnitude * feel.PushIn);
            transform.position = basePosition + shake + push;
        }
    }
}
