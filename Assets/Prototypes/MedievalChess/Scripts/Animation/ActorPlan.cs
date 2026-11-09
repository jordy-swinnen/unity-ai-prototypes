using System.Collections.Generic;
using UnityEngine;

namespace Prototypes.MedievalChess
{
    /// <summary>
    /// What one piece does during a plan: how its root moves, turns and tips, how big it is, and how
    /// each named part rotates. Parts are the ones in the piece model (Body, Head, ArmR, ...).
    /// </summary>
    public sealed class ActorPlan
    {
        /// <summary>The root's offset from where it started, in world space.</summary>
        public readonly Channel Root = new Channel();
        /// <summary>Heading in degrees. With no keys the actor keeps <see cref="StartYaw"/>.</summary>
        public readonly Channel Yaw = new Channel();
        /// <summary>Forward tilt in degrees (negative falls backward).</summary>
        public readonly Channel Pitch = new Channel();
        public readonly Channel Scale = new Channel();
        /// <summary>0 whole, 1 burnt away. Drives the dissolve shader. With no keys the piece is whole.</summary>
        public readonly Channel Dissolve = new Channel();
        public readonly Dictionary<string, Channel> PartEuler = new Dictionary<string, Channel>();

        public float StartYaw;
        /// <summary>The actor is not on the board before this time (a piece that appears mid-step).</summary>
        public float HiddenUntil;
        /// <summary>The actor disappears from this time on.</summary>
        public float HiddenFrom = float.PositiveInfinity;

        public Channel Part(string name)
        {
            if (!PartEuler.TryGetValue(name, out var channel))
            {
                channel = new Channel();
                PartEuler[name] = channel;
            }
            return channel;
        }

        public void Evaluate(float t, ActorFrame frame)
        {
            frame.RootOffset = Root.HasKeys ? Root.Evaluate(t) : Vector3.zero;
            frame.Yaw = Yaw.HasKeys ? Yaw.EvaluateScalar(t) : StartYaw;
            frame.Pitch = Pitch.HasKeys ? Pitch.EvaluateScalar(t) : 0f;
            frame.Scale = Scale.HasKeys ? Scale.EvaluateScalar(t) : 1f;
            frame.Dissolve = Dissolve.HasKeys ? Dissolve.EvaluateScalar(t) : 0f;
            frame.Visible = t >= HiddenUntil && t < HiddenFrom;
            frame.PartEuler.Clear();
            foreach (var kv in PartEuler) frame.PartEuler[kv.Key] = kv.Value.Evaluate(t);
        }
    }

    /// <summary>One piece's pose at one moment. Reused between frames so nothing is allocated.</summary>
    public sealed class ActorFrame
    {
        public Vector3 RootOffset;
        public float Yaw;
        public float Pitch;
        public float Scale = 1f;
        public float Dissolve;
        public bool Visible = true;
        public readonly Dictionary<string, Vector3> PartEuler = new Dictionary<string, Vector3>();
    }
}
