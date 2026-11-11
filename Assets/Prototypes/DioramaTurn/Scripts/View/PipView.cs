using UnityEngine;

namespace Prototypes.DioramaTurn
{
    /// <summary>Puts Pip's sprite on the simulation's cell, walks and hops with it, and picks the animation.</summary>
    public sealed class PipView : MonoBehaviour
    {
        public DioramaTurnSettings settings;
        public SpriteQuad quad;
        public CameraRig rig;
        public RoomView room;

        enum Pose { None, Cheer, Shake }

        float animClock;
        float poseTime;
        Pose pose = Pose.None;

        /// <summary>Play the cheer animation for a moment (the goal was reached).</summary>
        public void Cheer()
        {
            pose = Pose.Cheer;
            poseTime = 1.2f;
        }

        /// <summary>Play the head-shake for a moment (an unreachable pick).</summary>
        public void Shake()
        {
            pose = Pose.Shake;
            poseTime = 0.7f;
        }

        public void Show(DioramaSimulation sim, float dt)
        {
            if (quad == null || room == null) return;

            animClock += dt;
            if (poseTime > 0f)
            {
                poseTime -= dt;
                if (poseTime <= 0f) pose = Pose.None;
            }

            Vector3 feet = RoomView.TopFace(sim.PipCell);
            int frame;
            if (sim.Phase == Phase.Walking)
            {
                float p = sim.StepProgress;
                feet = Vector3.Lerp(RoomView.TopFace(sim.WalkFrom), RoomView.TopFace(sim.WalkTo), p);
                if (sim.StepIsHop) feet.y += Mathf.Sin(p * Mathf.PI) * settings.walk.hopHeight;
                frame = sim.StepIsHop ? TagFrame("hop", p) : TagFrame("walk", animClock / Mathf.Max(0.06f, settings.walk.stepTime * 0.5f));
            }
            else if (pose == Pose.Cheer)
            {
                frame = TagFrame("cheer", animClock / 0.14f);
            }
            else if (pose == Pose.Shake)
            {
                frame = TagFrame("shake", animClock / 0.12f);
            }
            else
            {
                frame = TagFrame("idle", animClock / 0.45f);
            }

            var push = rig != null ? rig.PushDirection : Vector3.forward;
            quad.Place(feet, push, settings.camera.spritePushToCamera);
            quad.Face(rig != null ? rig.Yaw : 45f);
            quad.ShowFrame(frame);

            if (sim.Phase == Phase.Walking && !sim.StepIsHop)
            {
                float squash = Feel.Squash(settings.walk.squash, settings.feel.effectStrength);
                float pinched = 1f - squash * Mathf.Sin(sim.StepProgress * Mathf.PI);
                quad.transform.localScale = new Vector3(quad.size.x, quad.size.y * pinched, 1f);
            }
            else
            {
                quad.transform.localScale = new Vector3(quad.size.x, quad.size.y, 1f);
            }
        }

        public void SnapTo(DioramaSimulation sim) => Show(sim, 0f);

        int TagFrame(string tag, float timeInFrames)
        {
            if (quad == null || quad.Frames == null) return 0;
            if (!quad.Frames.TryGetTag(tag, out int from, out int to))
                return Mathf.Clamp(0, 0, Mathf.Max(0, quad.Frames.FrameCount - 1));
            int count = Mathf.Max(1, to - from + 1);
            int step = Mathf.FloorToInt(Mathf.Max(0f, timeInFrames)) % count;
            return from + step;
        }
    }
}
