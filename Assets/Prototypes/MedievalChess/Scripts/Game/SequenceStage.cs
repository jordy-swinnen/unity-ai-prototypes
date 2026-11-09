using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prototypes.MedievalChess
{
    /// <summary>
    /// Plays an action sequence on the pieces in the scene: it advances a timeline, evaluates each step and
    /// applies the poses to the actors. It also reports the camera push, the shake of a blow, and where a fight is.
    /// </summary>
    public sealed class SequenceStage
    {
        readonly Func<string, PieceActor> findActor;
        ActorFrame[] frames = new ActorFrame[0];
        ActionSequence sequence;
        int shownStep = -1;

        public SequenceTimeline Timeline { get; private set; }
        public bool Playing => Timeline != null && !Timeline.Finished;
        public bool Finished => Timeline == null || Timeline.Finished;
        public float CameraPush { get; private set; }
        public float BlowShake { get; private set; }
        public bool BlowThisFrame { get; private set; }
        /// <summary>The middle of the fight being played, or null when the step is not a fight.</summary>
        public Vector3? FightFocus { get; private set; }
        public ActionSequence Sequence => sequence;

        public SequenceStage(Func<string, PieceActor> findActor)
        {
            this.findActor = findActor;
        }

        /// <summary>Starts a sequence. With Off speed every step is shown at its end, in order, at once.</summary>
        public void Begin(ActionSequence seq, AnimationSpeed speed, FightSettings fight, float effectStrength)
        {
            sequence = seq;
            shownStep = -1;
            CameraPush = 0f;
            BlowShake = 0f;
            BlowThisFrame = false;
            FightFocus = null;
            Timeline = new SequenceTimeline(seq, speed, fight, effectStrength);
            if (speed == AnimationSpeed.Off)
            {
                for (int i = 0; i < seq.Steps.Count; i++) Show(i, seq.Steps[i].Duration);
            }
            else if (seq.Steps.Count > 0)
            {
                Show(0, 0f);
            }
        }

        /// <summary>Advances by a real frame time and applies the poses.</summary>
        public void Step(float dt)
        {
            BlowThisFrame = false;
            BlowShake = 0f;
            if (Timeline == null || Timeline.Finished) return;

            int before = Timeline.StepIndex;
            Timeline.Advance(dt);
            if (Timeline.StepIndex != before) Show(before, sequence.Steps[before].Duration);
            Show(Timeline.StepIndex, Timeline.PlanTime);
            CameraPush = Timeline.CameraPush;
            BlowThisFrame = Timeline.BlowThisFrame;
            BlowShake = Timeline.BlowShake;
            if (Timeline.Finished) CameraPush = 0f;
        }

        void Show(int index, float t)
        {
            var plan = sequence.Steps[index];
            if (frames.Length < plan.Actors.Count)
            {
                frames = new ActorFrame[plan.Actors.Count];
                for (int i = 0; i < frames.Length; i++) frames[i] = new ActorFrame();
            }
            if (shownStep != index)
            {
                shownStep = index;
                foreach (var spec in plan.Actors) findActor(spec.Id).Place(spec.Start, spec.StartYaw);
                FightFocus = plan is FightPlan fight ? (fight.AttackerStart + fight.VictimPosition) * 0.5f : (Vector3?)null;
            }
            plan.Evaluate(t, frames);
            for (int i = 0; i < plan.Actors.Count; i++) findActor(plan.Actors[i].Id).Apply(frames[i]);
        }
    }
}
