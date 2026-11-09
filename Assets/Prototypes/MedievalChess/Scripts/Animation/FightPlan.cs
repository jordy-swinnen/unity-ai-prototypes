using System.Collections.Generic;
using UnityEngine;

namespace Prototypes.MedievalChess
{
    /// <summary>The pose of both pieces and the camera at one moment of a fight.</summary>
    public sealed class FightFrame
    {
        public readonly ActorFrame Attacker = new ActorFrame();
        public readonly ActorFrame Victim = new ActorFrame();
        /// <summary>0 to 1: how far the camera has pushed in toward the fight.</summary>
        public float CameraPush;
    }

    /// <summary>
    /// One piece killing another, as a plan. The poses are a pure function of time (<see cref="Evaluate(float, FightFrame)"/>),
    /// so a test or a capture can look at any moment exactly. The plan never touches the game rules:
    /// the capture has already happened, and this only shows it.
    /// </summary>
    public sealed class FightPlan : IActionPlan
    {
        static readonly float[] NoFootfalls = new float[0];

        public PieceType AttackerType;
        public PieceType VictimType;
        /// <summary>World positions the plan was made for.</summary>
        public Vector3 AttackerStart;
        public Vector3 VictimPosition;

        public ActorSpec AttackerSpec;
        public ActorSpec VictimSpec;
        public ActorPlan Attacker;
        public ActorPlan Victim;
        public Channel Camera = new Channel();

        /// <summary>When the attacker has reached the victim and starts to strike.</summary>
        public float ApproachEnd;
        /// <summary>The moment the blow lands.</summary>
        public float BlowTime { get; set; }
        /// <summary>When the victim has collapsed and disappears.</summary>
        public float CollapseEnd;
        public float Duration { get; set; }

        public string Name => $"{AttackerType} takes {VictimType}";
        public IReadOnlyList<ActorSpec> Actors => new[] { AttackerSpec, VictimSpec };
        public IReadOnlyList<float> Footfalls => NoFootfalls;

        public float CameraPush(float t) => Camera.EvaluateScalar(Clamp(t));

        public void Evaluate(float t, FightFrame frame)
        {
            t = Clamp(t);
            Attacker.Evaluate(t, frame.Attacker);
            Victim.Evaluate(t, frame.Victim);
            frame.CameraPush = Camera.EvaluateScalar(t);
        }

        public void Evaluate(float t, ActorFrame[] frames)
        {
            t = Clamp(t);
            Attacker.Evaluate(t, frames[0]);
            Victim.Evaluate(t, frames[1]);
        }

        float Clamp(float t) => Mathf.Clamp(t, 0f, Duration);
    }
}
