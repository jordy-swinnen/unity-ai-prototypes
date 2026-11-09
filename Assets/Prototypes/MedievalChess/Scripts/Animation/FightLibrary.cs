using System;
using UnityEngine;

namespace Prototypes.MedievalChess
{
    /// <summary>
    /// Makes fight plans. One attack routine per attacker type, and one collapse for every victim.
    /// Arms rest hanging or holding their weapon up. A positive rotation about x swings a part from
    /// pointing up toward pointing forward, and a negative one swings a hanging arm up and forward.
    /// </summary>
    public static class FightLibrary
    {
        public static bool HasRoutine(PieceType type) => type == PieceType.Pawn || type == PieceType.Rook;

        public static FightPlan Create(PieceType attacker, PieceType victim, Vector3 attackerStart, Vector3 victimPosition,
            float attackerYaw, float victimYaw, FightSettings s)
        {
            if (!HasRoutine(attacker))
                throw new ArgumentException($"There is no fight routine for {attacker} yet.", nameof(attacker));

            var flat = victimPosition - attackerStart;
            flat.y = 0f;
            float distance = flat.magnitude;
            Vector3 dir = distance > 1e-4f ? flat / distance : Vector3.forward;

            // The attacker walks up to the stand-off point, strikes, then steps onto the victim's square.
            Vector3 standOffset = dir * Mathf.Max(0f, distance - s.standOff);
            Vector3 fullOffset = flat;

            float yawToVictim = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float attackerFacing = attackerYaw + Mathf.DeltaAngle(attackerYaw, yawToVictim);
            float victimFacing = victimYaw + Mathf.DeltaAngle(victimYaw, yawToVictim + 180f);

            float windUp = attacker == PieceType.Rook ? s.windUpRook : s.windUpPawn;
            float approachEnd = s.approachTime;
            float blow = approachEnd + windUp;
            float collapseEnd = blow + s.collapseTime;
            float stepStart = collapseEnd;
            float stepEnd = stepStart + s.stepInTime;
            float duration = stepEnd + s.settleTime;
            float recover = Mathf.Min(blow + 0.25f, collapseEnd);
            float fall = s.collapseTime * 0.75f;

            var plan = new FightPlan
            {
                AttackerType = attacker,
                VictimType = victim,
                AttackerStart = attackerStart,
                VictimPosition = victimPosition,
                ApproachEnd = approachEnd,
                BlowTime = blow,
                CollapseEnd = collapseEnd,
                Duration = duration,
                Attacker = new ActorPlan { StartYaw = attackerYaw },
                Victim = new ActorPlan { StartYaw = victimYaw },
            };

            // --- the attacker's body: walk up, lunge on the blow, recover, step onto the square ---
            var a = plan.Attacker;
            a.Root.Add(0f, Vector3.zero)
                  .Add(approachEnd, standOffset)
                  .Add(blow, standOffset + dir * s.lungeDistance, Ease.In)
                  .Add(recover, standOffset, Ease.Out)
                  .Add(stepStart, standOffset)
                  .Add(stepEnd, fullOffset)
                  .Add(duration, fullOffset);
            a.Yaw.Add(0f, attackerYaw).Add(approachEnd, attackerFacing).Add(duration, attackerFacing, Ease.Linear);

            if (attacker == PieceType.Pawn) PawnThrust(a, approachEnd, blow, recover, stepStart, stepEnd, duration);
            else RookSlam(a, approachEnd, blow, recover, stepStart, stepEnd, duration);

            // --- the victim: turns to face the attacker, is knocked back, falls, and is gone ---
            var v = plan.Victim;
            v.Yaw.Add(0f, victimYaw).Add(approachEnd, victimFacing).Add(duration, victimFacing, Ease.Linear);
            v.Root.Add(0f, Vector3.zero).Add(blow, Vector3.zero).Add(blow + fall, dir * s.pushBack, Ease.Out);
            v.Pitch.Add(0f, 0f).Add(blow, 0f).Add(blow + fall, -85f, Ease.In);
            v.Scale.Add(0f, 1f).Add(Mathf.Max(0f, collapseEnd - 0.2f), 1f).Add(collapseEnd, 0f, Ease.In);
            v.HiddenFrom = collapseEnd;
            foreach (var arm in new[] { "ArmR", "ArmL" })
                v.Part(arm).Add(0f, 0f).Add(blow, 0f).Add(blow + 0.15f, 50f, Ease.Out).Add(blow + fall, 20f);

            // --- the camera pushes in for the fight and goes back ---
            plan.Camera.Add(0f, 0f).Add(approachEnd, 1f).Add(collapseEnd, 1f).Add(duration, 0f);
            return plan;
        }

        // The pawn pulls its spear back, then thrusts it forward. The shield arm braces.
        static void PawnThrust(ActorPlan a, float approachEnd, float blow, float recover, float stepStart, float stepEnd, float duration)
        {
            float wind = blow - 0.05f;
            a.Part("ArmR").Add(0f, 0f).Add(approachEnd, 0f)
                .Add(wind, -45f).Add(blow, 100f, Ease.In).Add(recover, 85f, Ease.Out)
                .Add(stepStart, 85f).Add(stepEnd, 0f).Add(duration, 0f);
            a.Part("ArmL").Add(0f, 0f).Add(approachEnd, 0f)
                .Add(wind, 10f).Add(recover, 10f).Add(stepEnd, 0f).Add(duration, 0f);
            Lean(a, approachEnd, wind, blow, recover, stepStart, stepEnd, duration, back: -8f, forward: 12f, nod: 10f);
        }

        // The rook raises both fists over its head and slams them down and forward onto the victim, together.
        static void RookSlam(ActorPlan a, float approachEnd, float blow, float recover, float stepStart, float stepEnd, float duration)
        {
            float wind = blow - 0.05f;
            foreach (var arm in new[] { "ArmR", "ArmL" })
            {
                a.Part(arm).Add(0f, 0f).Add(approachEnd, 0f)
                    .Add(wind, -165f).Add(blow, -80f, Ease.In).Add(recover, -80f, Ease.Out)
                    .Add(stepStart, -80f).Add(stepEnd, 0f).Add(duration, 0f);
            }
            Lean(a, approachEnd, wind, blow, recover, stepStart, stepEnd, duration, back: -6f, forward: 10f, nod: 20f);
        }

        // The body leans back in the wind-up and forward on the blow. The head nods.
        static void Lean(ActorPlan a, float approachEnd, float wind, float blow, float recover, float stepStart,
            float stepEnd, float duration, float back, float forward, float nod)
        {
            a.Part("Body").Add(0f, 0f).Add(approachEnd, 0f)
                .Add(wind, back).Add(blow, forward, Ease.In).Add(recover, forward * 0.6f, Ease.Out)
                .Add(stepStart, forward * 0.6f).Add(stepEnd, 0f).Add(duration, 0f);
            a.Part("Head").Add(0f, 0f).Add(approachEnd, 0f)
                .Add(wind, back * 0.6f).Add(blow, nod, Ease.In).Add(recover, nod * 0.5f, Ease.Out)
                .Add(stepStart, nod * 0.5f).Add(stepEnd, 0f).Add(duration, 0f);
        }
    }
}
