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
        /// <summary>Sounds and effects whose moment passed in the last frame, with where they happen.</summary>
        public IReadOnlyList<PositionedCue> CuesThisFrame => cuesThisFrame;
        /// <summary>The name of the step being played, or an empty string.</summary>
        public string CurrentStepName => Timeline != null && sequence != null && sequence.Steps.Count > 0 ? sequence.Steps[Timeline.StepIndex].Name : string.Empty;

        public struct PositionedCue
        {
            public FeelCue Cue;
            public Vector3 Position;
        }

        readonly List<PositionedCue> cuesThisFrame = new List<PositionedCue>();
        readonly List<FeelCue> scratch = new List<FeelCue>();
        readonly Dictionary<int, List<FeelCue>> cueCache = new Dictionary<int, List<FeelCue>>();
        FeelSettings feel = new FeelSettings();

        public SequenceStage(Func<string, PieceActor> findActor)
        {
            this.findActor = findActor;
        }

        /// <summary>Starts a sequence. With Off speed every step is shown at its end, in order, at once.</summary>
        public void Begin(ActionSequence seq, AnimationSpeed speed, FightSettings fight, float effectStrength, FeelSettings feelSettings = null)
        {
            sequence = seq;
            if (feelSettings != null) feel = feelSettings;
            cueCache.Clear();
            cuesThisFrame.Clear();
            shownStep = -1;
            CameraPush = 0f;
            BlowShake = 0f;
            BlowThisFrame = false;
            FightFocus = null;
            Timeline = new SequenceTimeline(seq, speed, fight, effectStrength);
            if (speed == AnimationSpeed.Off)
            {
                for (int i = 0; i < seq.Steps.Count; i++) Show(i, seq.Steps[i].Duration);
                OffCue();
            }
            else if (seq.Steps.Count > 0)
            {
                Show(0, 0f);
                Collect(0, -1f, 0f);
            }
        }

        // With Off the move is only shown, so it gets one sound: the clash of a capture, or a footfall.
        void OffCue()
        {
            if (sequence.Steps.Count == 0) return;
            bool capture = false;
            foreach (var step in sequence.Steps) if (step is FightPlan) capture = true;
            var plan = sequence.Steps[sequence.Steps.Count - 1];
            var cue = new FeelCue { Sound = capture ? SoundId.Impact : SoundId.Footstep, HasSound = true, Effect = EffectKind.None };
            cuesThisFrame.Add(new PositionedCue { Cue = cue, Position = findActor(plan.Actors[0].Id).transform.position });
        }

        void Collect(int step, float from, float to)
        {
            if (!cueCache.TryGetValue(step, out var cues))
                cueCache[step] = cues = SoundCues.For(sequence.Steps[step], feel);
            scratch.Clear();
            SoundCues.Between(cues, from, to, scratch);
            var plan = sequence.Steps[step];
            foreach (var cue in scratch)
            {
                int actor = Mathf.Clamp(cue.Actor, 0, plan.Actors.Count - 1);
                cuesThisFrame.Add(new PositionedCue { Cue = cue, Position = findActor(plan.Actors[actor].Id).transform.position });
            }
        }

        /// <summary>Advances by a real frame time and applies the poses.</summary>
        public void Step(float dt)
        {
            BlowThisFrame = false;
            BlowShake = 0f;
            cuesThisFrame.Clear();
            if (Timeline == null || Timeline.Finished) return;

            int before = Timeline.StepIndex;
            float beforeTime = Timeline.PlanTime;
            Timeline.Advance(dt);
            if (Timeline.StepIndex != before) Show(before, sequence.Steps[before].Duration);
            Show(Timeline.StepIndex, Timeline.PlanTime);
            if (Timeline.StepIndex != before)
            {
                Collect(before, beforeTime, sequence.Steps[before].Duration);
                Collect(Timeline.StepIndex, -1f, Timeline.PlanTime);
            }
            else Collect(before, beforeTime, Timeline.PlanTime);
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
