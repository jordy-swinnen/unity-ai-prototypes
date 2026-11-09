namespace Prototypes.MedievalChess
{
    /// <summary>
    /// Plays the steps of an action sequence one after another, each with its own timeline.
    /// It reports the current step and the blow, shake and camera push of that step.
    /// </summary>
    public sealed class SequenceTimeline
    {
        readonly ActionSequence sequence;
        readonly AnimationSpeed speed;
        readonly FightSettings settings;
        readonly float strength;
        FightTimeline current;

        public int StepIndex { get; private set; }
        public bool Finished { get; private set; }
        /// <summary>True for the one frame in which a new step starts.</summary>
        public bool StepChangedThisFrame { get; private set; }
        public bool BlowThisFrame { get; private set; }
        public float BlowShake { get; private set; }
        public float CameraPush { get; private set; }
        public bool HitStopActive => current != null && current.HitStopActive;
        public IActionPlan CurrentPlan => sequence.Steps[StepIndex];
        /// <summary>Time into the current step.</summary>
        public float PlanTime => current != null ? current.PlanTime : 0f;

        public SequenceTimeline(ActionSequence sequence, AnimationSpeed speed, FightSettings settings, float effectStrength)
        {
            this.sequence = sequence;
            this.speed = speed;
            this.settings = settings;
            strength = effectStrength;
            if (sequence.Steps.Count == 0)
            {
                Finished = true;
                return;
            }
            // Off skips the whole move: the last step is shown at its end.
            StepIndex = speed == AnimationSpeed.Off ? sequence.Steps.Count - 1 : 0;
            current = new FightTimeline(sequence.Steps[StepIndex], speed, settings, strength);
            if (speed == AnimationSpeed.Off) Finished = true;
        }

        public void Advance(float realDt)
        {
            StepChangedThisFrame = false;
            BlowThisFrame = false;
            BlowShake = 0f;
            if (Finished) return;

            current.Advance(realDt);
            BlowThisFrame = current.BlowThisFrame;
            BlowShake = current.BlowShake;
            CameraPush = current.CameraPush;

            if (!current.Finished) return;
            if (StepIndex + 1 < sequence.Steps.Count)
            {
                StepIndex++;
                current = new FightTimeline(sequence.Steps[StepIndex], speed, settings, strength);
                StepChangedThisFrame = true;
            }
            else
            {
                Finished = true;
            }
        }
    }
}
