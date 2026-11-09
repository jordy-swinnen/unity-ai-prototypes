using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prototypes.MedievalChess
{
    /// <summary>One piece that takes part in a step: who it is, which side, and where it stands at the start.</summary>
    public struct ActorSpec
    {
        public string Id;
        public PieceType Type;
        /// <summary>0 for the bone side, 1 for the cinnabar side.</summary>
        public int Side;
        public Vector3 Start;
        public float StartYaw;
    }

    /// <summary>Where a piece is after a step: position, heading, tilt, and whether it is on the board.</summary>
    public sealed class ActorState
    {
        public string Id;
        public PieceType Type;
        public int Side;
        public Vector3 Position;
        public float Yaw;
        public float Pitch;
        public bool Visible = true;

        public ActorState Clone() => (ActorState)MemberwiseClone();
    }

    /// <summary>
    /// One step of what the player sees: a walk, a leap, a fight, castling, a promotion, a brace, a fall.
    /// A step is a pure function of time. It never touches the game rules: the move has already happened,
    /// and a step only shows it.
    /// </summary>
    public interface IActionPlan
    {
        string Name { get; }
        float Duration { get; }
        /// <summary>The moment a blow lands, or NaN for a step without one (it then has no hit-stop or shake).</summary>
        float BlowTime { get; }
        IReadOnlyList<ActorSpec> Actors { get; }
        /// <summary>Times at which a foot (or a landing) strikes the board, for the thud.</summary>
        IReadOnlyList<float> Footfalls { get; }
        /// <summary>Fills one frame per actor, in the order of <see cref="Actors"/>.</summary>
        void Evaluate(float t, ActorFrame[] frames);
        /// <summary>0 to 1: how far the camera pushes in toward this step.</summary>
        float CameraPush(float t);
    }

    /// <summary>The steps of one move, in order, and where every piece ends up.</summary>
    public sealed class ActionSequence
    {
        readonly List<IActionPlan> steps = new List<IActionPlan>();
        readonly Dictionary<string, ActorState> states = new Dictionary<string, ActorState>();

        public IReadOnlyList<IActionPlan> Steps => steps;
        /// <summary>Where each piece is when the whole sequence has played.</summary>
        public IReadOnlyDictionary<string, ActorState> End => states;
        public float Duration { get; private set; }

        /// <summary>Wraps one plan whose actors start where the plan says.</summary>
        public static ActionSequence Single(IActionPlan plan)
        {
            var seq = new ActionSequence();
            seq.Add(plan);
            return seq;
        }

        /// <summary>Adds a step and updates where its pieces end up by evaluating it at its end.</summary>
        public void Add(IActionPlan plan)
        {
            steps.Add(plan);
            Duration += plan.Duration;
            var frames = new ActorFrame[plan.Actors.Count];
            for (int i = 0; i < frames.Length; i++) frames[i] = new ActorFrame();
            plan.Evaluate(plan.Duration, frames);
            for (int i = 0; i < frames.Length; i++)
            {
                var spec = plan.Actors[i];
                states[spec.Id] = new ActorState
                {
                    Id = spec.Id,
                    Type = spec.Type,
                    Side = spec.Side,
                    Position = spec.Start + frames[i].RootOffset,
                    Yaw = frames[i].Yaw,
                    Pitch = frames[i].Pitch,
                    Visible = frames[i].Visible,
                };
            }
        }

        /// <summary>The state of a piece before the step is played, or null if it is new.</summary>
        public ActorState Current(string id) => states.TryGetValue(id, out var s) ? s : null;
    }
}
