using System.Collections.Generic;
using UnityEngine;

namespace Prototypes.MedievalChess
{
    /// <summary>
    /// A step that is not a fight: a walk, a leap, a slide, castling, a promotion, a brace, a fall.
    /// It has one or more actors, each with its own plan, and it never touches the rules.
    /// </summary>
    public sealed class MovePlan : IActionPlan
    {
        public readonly List<ActorSpec> ActorList = new List<ActorSpec>();
        public readonly List<ActorPlan> Plans = new List<ActorPlan>();
        public readonly List<float> FootfallList = new List<float>();
        public readonly Channel Camera = new Channel();

        public string Name { get; set; }
        public float Duration { get; set; }
        /// <summary>A move has no blow, so no hit-stop and no shake.</summary>
        public float BlowTime => float.NaN;
        /// <summary>For a promotion: when the pawn becomes the new piece. NaN for other steps.</summary>
        public float SwapTime = float.NaN;

        public IReadOnlyList<ActorSpec> Actors => ActorList;
        public IReadOnlyList<float> Footfalls => FootfallList;

        public MovePlan Add(ActorSpec spec, ActorPlan plan)
        {
            ActorList.Add(spec);
            Plans.Add(plan);
            return this;
        }

        public void Evaluate(float t, ActorFrame[] frames)
        {
            t = Mathf.Clamp(t, 0f, Duration);
            for (int i = 0; i < Plans.Count; i++) Plans[i].Evaluate(t, frames[i]);
        }

        public float CameraPush(float t) => Camera.HasKeys ? Camera.EvaluateScalar(Mathf.Clamp(t, 0f, Duration)) : 0f;
    }
}
