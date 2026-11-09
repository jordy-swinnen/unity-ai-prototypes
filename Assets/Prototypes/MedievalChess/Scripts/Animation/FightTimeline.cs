using Shared.Feel;
using UnityEngine;

namespace Prototypes.MedievalChess
{
    /// <summary>
    /// Plays a fight plan in real time. Full speed plays the whole fight with a hit-stop on the blow,
    /// a camera push and a shake. Quick plays it faster with none of those. Off jumps to the end.
    /// The effect strength (0 to 1) scales the hit-stop, the shake and the camera push, and 0 removes them.
    /// </summary>
    public sealed class FightTimeline
    {
        readonly IActionPlan plan;
        readonly AnimationSpeed speed;
        readonly FightSettings settings;
        readonly float strength;
        readonly TimeFeel timeFeel = new TimeFeel();

        public float PlanTime { get; private set; }
        public bool Finished { get; private set; }
        /// <summary>True for the one frame in which the blow lands.</summary>
        public bool BlowThisFrame { get; private set; }
        /// <summary>The camera shake to start on the blow, and 0 on every other frame.</summary>
        public float BlowShake { get; private set; }
        public float CameraPush { get; private set; }
        public bool HitStopActive => timeFeel.HitStopActive;

        public FightTimeline(IActionPlan plan, AnimationSpeed speed, FightSettings settings, float effectStrength)
        {
            this.plan = plan;
            this.speed = speed;
            this.settings = settings;
            strength = Mathf.Clamp01(effectStrength);
            if (speed == AnimationSpeed.Off) Finish();
        }

        public void Advance(float realDt)
        {
            BlowThisFrame = false;
            BlowShake = 0f;
            if (Finished) return;

            if (speed == AnimationSpeed.Off)
            {
                Finish();
                return;
            }

            float dt = timeFeel.Advance(realDt);
            float planDt = speed == AnimationSpeed.Quick ? dt * settings.quickFactor : dt;
            float next = Mathf.Min(PlanTime + planDt, plan.Duration);

            // Stop exactly on the blow, so a long frame can't skip over it.
            bool hasBlow = !float.IsNaN(plan.BlowTime);
            if (hasBlow && PlanTime < plan.BlowTime && next >= plan.BlowTime)
            {
                next = plan.BlowTime;
                BlowThisFrame = true;
                if (speed == AnimationSpeed.Full && strength > 0f)
                {
                    BlowShake = settings.shakeStrength * strength;
                    timeFeel.HitStop(settings.hitStop * strength);
                }
            }

            PlanTime = next;
            CameraPush = speed == AnimationSpeed.Full ? plan.CameraPush(PlanTime) * strength : 0f;
            if (PlanTime >= plan.Duration) Finished = true;
        }

        void Finish()
        {
            PlanTime = plan.Duration;
            Finished = true;
            CameraPush = 0f;
        }
    }
}
