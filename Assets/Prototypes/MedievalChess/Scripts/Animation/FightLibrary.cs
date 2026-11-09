using System;
using UnityEngine;

namespace Prototypes.MedievalChess
{
    /// <summary>
    /// Makes fight plans. One attack routine per attacker type, and one collapse for every victim.
    /// Arms rest hanging or holding their weapon up. A positive rotation about x swings a part from
    /// pointing up toward pointing forward, and a negative one swings a hanging arm up and forward.
    /// A rotation about y turns a part sideways.
    /// </summary>
    public static class FightLibrary
    {
        public static bool HasRoutine(PieceType type) => Enum.IsDefined(typeof(PieceType), type);

        public static FightPlan Create(PieceType attacker, PieceType victim, Vector3 attackerStart, Vector3 victimPosition,
            float attackerYaw, float victimYaw, FightSettings s,
            string attackerId = "attacker", string victimId = "victim", int attackerSide = 0, int victimSide = 1)
        {
            if (!HasRoutine(attacker))
                throw new ArgumentException($"There is no fight routine for piece type {(int)attacker}.", nameof(attacker));

            var flat = victimPosition - attackerStart;
            flat.y = 0f;
            float distance = flat.magnitude;
            Vector3 dir = distance > 1e-4f ? flat / distance : Vector3.forward;

            // The attacker walks up to its stand-off point, strikes, then steps onto the victim's square.
            bool knight = attacker == PieceType.Knight;
            float standOff = knight ? s.standOff + s.knightExtraStandOff : s.standOff;
            float lunge = knight ? s.knightLunge : s.lungeDistance;
            float pull = knight ? 0.25f : 0f; // a knight draws back before it charges
            Vector3 standOffset = dir * Mathf.Max(0f, distance - standOff);
            Vector3 fullOffset = flat;

            float yawToVictim = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float attackerFacing = attackerYaw + Mathf.DeltaAngle(attackerYaw, yawToVictim);
            float victimFacing = victimYaw + Mathf.DeltaAngle(victimYaw, yawToVictim + 180f);

            float approachEnd = s.approachTime;
            float blow = approachEnd + WindUp(attacker, s);
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
                AttackerSpec = new ActorSpec { Id = attackerId, Type = attacker, Side = attackerSide, Start = attackerStart, StartYaw = attackerYaw },
                VictimSpec = new ActorSpec { Id = victimId, Type = victim, Side = victimSide, Start = victimPosition, StartYaw = victimYaw },
                ApproachEnd = approachEnd,
                BlowTime = blow,
                CollapseEnd = collapseEnd,
                Duration = duration,
                Attacker = new ActorPlan { StartYaw = attackerYaw },
                Victim = new ActorPlan { StartYaw = victimYaw },
            };

            // --- the attacker's body: walk up, lunge on the blow, recover, step onto the square ---
            var a = plan.Attacker;
            a.Root.Add(0f, Vector3.zero).Add(approachEnd, standOffset);
            if (pull > 0f) a.Root.Add(blow - 0.2f, standOffset - dir * pull);
            a.Root.Add(blow, standOffset + dir * lunge, Ease.In)
                  .Add(recover, standOffset, Ease.Out)
                  .Add(stepStart, standOffset)
                  .Add(stepEnd, fullOffset)
                  .Add(duration, fullOffset);
            // It faces the victim for the fight, then turns back to face as it did before.
            a.Yaw.Add(0f, attackerYaw).Add(approachEnd, attackerFacing).Add(stepEnd, attackerFacing, Ease.Linear).Add(duration, attackerYaw);

            switch (attacker)
            {
                case PieceType.Pawn: PawnThrust(a, approachEnd, blow, recover, stepStart, stepEnd, duration); break;
                case PieceType.Knight: KnightCharge(a, approachEnd, blow, recover, stepStart, stepEnd, duration); break;
                case PieceType.Bishop: BishopSwing(a, approachEnd, blow, recover, stepStart, stepEnd, duration); break;
                case PieceType.Rook: RookSlam(a, approachEnd, blow, recover, stepStart, stepEnd, duration); break;
                case PieceType.Queen: QueenSweep(a, approachEnd, blow, recover, stepStart, stepEnd, duration); break;
                case PieceType.King: KingBlow(a, approachEnd, blow, recover, stepStart, stepEnd, duration); break;
            }

            // --- the victim: turns to face the attacker, is knocked back, falls, and is gone ---
            var v = plan.Victim;
            v.Yaw.Add(0f, victimYaw).Add(approachEnd, victimFacing).Add(duration, victimFacing, Ease.Linear);
            v.Root.Add(0f, Vector3.zero).Add(blow, Vector3.zero).Add(blow + fall, dir * s.pushBack, Ease.Out);
            v.Pitch.Add(0f, 0f).Add(blow, 0f).Add(blow + fall, -85f, Ease.In);
            // It burns away along a noise edge as it falls (the shader reads this) and is removed when fully burnt.
            float burn = Mathf.Clamp(s.dissolveTime, 0.01f, s.collapseTime);
            v.Dissolve.Add(0f, 0f).Add(collapseEnd - burn, 0f).Add(collapseEnd, 1f, Ease.Linear);
            v.HiddenFrom = collapseEnd;
            foreach (var arm in new[] { "ArmR", "ArmL" })
                v.Part(arm).Add(0f, 0f).Add(blow, 0f).Add(blow + 0.15f, 50f, Ease.Out).Add(blow + fall, 20f);

            // --- the camera pushes in for the fight and goes back ---
            plan.Camera.Add(0f, 0f).Add(approachEnd, 1f).Add(collapseEnd, 1f).Add(duration, 0f);
            return plan;
        }

        static float WindUp(PieceType type, FightSettings s)
        {
            switch (type)
            {
                case PieceType.Knight: return s.windUpKnight;
                case PieceType.Bishop: return s.windUpBishop;
                case PieceType.Rook: return s.windUpRook;
                case PieceType.Queen: return s.windUpQueen;
                case PieceType.King: return s.windUpKing;
                default: return s.windUpPawn;
            }
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

        // The knight draws back, rears its horse's head, levels the lance and charges.
        static void KnightCharge(ActorPlan a, float approachEnd, float blow, float recover, float stepStart, float stepEnd, float duration)
        {
            float wind = blow - 0.2f;
            a.Part("ArmR").Add(0f, 0f).Add(approachEnd, 0f)
                .Add(wind, 70f).Add(blow, 95f, Ease.In).Add(recover, 90f, Ease.Out)
                .Add(stepStart, 90f).Add(stepEnd, 0f).Add(duration, 0f);
            a.Part("ArmL").Add(0f, 0f).Add(approachEnd, 0f)
                .Add(wind, -30f).Add(recover, -30f).Add(stepEnd, 0f).Add(duration, 0f);
            a.Part("HorseHead").Add(0f, 0f).Add(approachEnd, 0f)
                .Add(wind, -25f).Add(blow, 12f, Ease.In).Add(recover, 6f, Ease.Out)
                .Add(stepStart, 6f).Add(stepEnd, 0f).Add(duration, 0f);
            Lean(a, approachEnd, wind, blow, recover, stepStart, stepEnd, duration, back: -10f, forward: 15f, nod: 8f);
        }

        // The bishop lifts his book with one hand and swings his mace from behind his head.
        static void BishopSwing(ActorPlan a, float approachEnd, float blow, float recover, float stepStart, float stepEnd, float duration)
        {
            float wind = blow - 0.1f;
            a.Part("ArmR").Add(0f, 0f).Add(approachEnd, 0f)
                .Add(wind, -70f).Add(blow, 95f, Ease.In).Add(recover, 85f, Ease.Out)
                .Add(stepStart, 85f).Add(stepEnd, 0f).Add(duration, 0f);
            a.Part("ArmL").Add(0f, 0f).Add(approachEnd, 0f)
                .Add(wind, -45f).Add(recover, -45f).Add(stepEnd, 0f).Add(duration, 0f);
            Lean(a, approachEnd, wind, blow, recover, stepStart, stepEnd, duration, back: -8f, forward: 14f, nod: 10f);
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

        // The queen holds her sword level and sweeps it across the victim from one side to the other.
        static void QueenSweep(ActorPlan a, float approachEnd, float blow, float recover, float stepStart, float stepEnd, float duration)
        {
            float wind = blow - 0.1f;
            a.Part("ArmR").Add(0f, Vector3.zero).Add(approachEnd, Vector3.zero)
                .Add(wind, new Vector3(80f, -85f, 0f)).Add(blow, new Vector3(80f, 0f, 0f), Ease.In)
                .Add(recover, new Vector3(80f, 70f, 0f), Ease.Out).Add(stepStart, new Vector3(80f, 70f, 0f))
                .Add(stepEnd, Vector3.zero).Add(duration, Vector3.zero);
            a.Part("ArmL").Add(0f, 0f).Add(approachEnd, 0f)
                .Add(wind, -20f).Add(recover, -20f).Add(stepEnd, 0f).Add(duration, 0f);
            // her body turns with the sweep
            a.Part("Body").Add(0f, Vector3.zero).Add(approachEnd, Vector3.zero)
                .Add(wind, new Vector3(0f, -25f, 0f)).Add(blow, Vector3.zero, Ease.In)
                .Add(recover, new Vector3(0f, 25f, 0f), Ease.Out).Add(stepStart, new Vector3(0f, 25f, 0f))
                .Add(stepEnd, Vector3.zero).Add(duration, Vector3.zero);
            a.Part("Head").Add(0f, Vector3.zero).Add(approachEnd, Vector3.zero)
                .Add(wind, new Vector3(0f, -15f, 0f)).Add(blow, Vector3.zero, Ease.In)
                .Add(recover, new Vector3(0f, 15f, 0f), Ease.Out).Add(stepStart, new Vector3(0f, 15f, 0f))
                .Add(stepEnd, Vector3.zero).Add(duration, Vector3.zero);
        }

        // The king lifts his great sword high and slowly, and brings it down the hardest.
        static void KingBlow(ActorPlan a, float approachEnd, float blow, float recover, float stepStart, float stepEnd, float duration)
        {
            float wind = blow - 0.1f;
            a.Part("ArmR").Add(0f, 0f).Add(approachEnd, 0f)
                .Add(wind, -60f).Add(blow, 110f, Ease.In).Add(recover, 100f, Ease.Out)
                .Add(stepStart, 100f).Add(stepEnd, 0f).Add(duration, 0f);
            a.Part("ArmL").Add(0f, 0f).Add(approachEnd, 0f)
                .Add(wind, -35f).Add(recover, -35f).Add(stepEnd, 0f).Add(duration, 0f);
            Lean(a, approachEnd, wind, blow, recover, stepStart, stepEnd, duration, back: -14f, forward: 20f, nod: 14f);
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
