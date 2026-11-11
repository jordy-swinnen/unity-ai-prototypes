using UnityEngine;

namespace Prototypes.DioramaTurn
{
    /// <summary>Puts Pip's sprite on the simulation's cell, walks and hops with it, and picks the animation frame.</summary>
    public sealed class PipView : MonoBehaviour
    {
        public DioramaTurnSettings settings;
        public SpriteQuad quad;
        public CameraRig rig;
        public RoomView room;

        float walkClock;

        public void Show(DioramaSimulation sim, float dt)
        {
            if (quad == null || room == null) return;
            Vector3 feet = RoomView.TopFace(sim.PipCell);
            int frame = IdleFrame();
            if (sim.Phase == Phase.Walking)
            {
                float p = sim.StepProgress;
                feet = Vector3.Lerp(RoomView.TopFace(sim.WalkFrom), RoomView.TopFace(sim.WalkTo), p);
                if (sim.StepIsHop) feet.y += Mathf.Sin(p * Mathf.PI) * settings.walk.hopHeight;
                walkClock += dt;
                frame = WalkFrame();
            }
            else
            {
                walkClock = 0f;
            }

            var push = rig != null ? rig.PushDirection : Vector3.forward;
            quad.Place(feet, push, settings.camera.spritePushToCamera);
            quad.Face(rig != null ? rig.Yaw : 45f);
            quad.ShowFrame(frame);
        }

        public void SnapTo(DioramaSimulation sim) => Show(sim, 0f);

        int IdleFrame()
        {
            if (quad == null || quad.Frames == null) return 0;
            return quad.Frames.TryGetTag("idle", out int from, out _) ? from : 0;
        }

        int WalkFrame()
        {
            if (quad == null || quad.Frames == null) return 0;
            if (!quad.Frames.TryGetTag("walk", out int from, out int to)) return Mathf.Clamp(1, 0, Mathf.Max(0, quad.Frames.FrameCount - 1));
            int count = Mathf.Max(1, to - from + 1);
            int step = Mathf.FloorToInt(walkClock / Mathf.Max(0.06f, settings.walk.stepTime * 0.5f));
            return from + step % count;
        }
    }
}
