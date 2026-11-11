using UnityEngine;

namespace Prototypes.DioramaTurn
{
    /// <summary>The goal star: a sprite that twinkles, placed above the Goal cell.</summary>
    public sealed class StarView : MonoBehaviour
    {
        public SpriteQuad quad;
        public CameraRig rig;
        public float frameSeconds = 0.18f;

        float clock;
        Cell goal;

        public void Place(Cell cell)
        {
            goal = cell;
            if (quad != null) quad.transform.position = RoomView.TopFace(cell) + Vector3.up * 0.4f;
        }

        public void Show(float dt)
        {
            if (quad == null) return;
            clock += dt;
            int count = quad.Frames != null ? Mathf.Max(1, quad.Frames.FrameCount) : 1;
            int frame = Mathf.FloorToInt(clock / Mathf.Max(0.05f, frameSeconds)) % count;
            quad.Place(RoomView.TopFace(goal) + Vector3.up * 0.4f,
                rig != null ? rig.PushDirection : Vector3.forward, 0f);
            quad.Face(rig != null ? rig.Yaw : 45f);
            quad.ShowFrame(frame);
        }
    }
}
