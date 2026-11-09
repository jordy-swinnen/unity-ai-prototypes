using UnityEngine;

namespace Prototypes.MedievalChess
{
    /// <summary>
    /// Makes the plans for everything that is not a fight. A piece turns toward where it is going,
    /// moves, and turns back, so it always ends facing the way it started.
    /// </summary>
    public static class MoveLibrary
    {
        /// <summary>The right way to travel for a piece: knights leap, rooks slide, the rest walk.</summary>
        public static MovePlan Travel(ActorSpec actor, Vector3 to, MoveSettings m)
        {
            switch (actor.Type)
            {
                case PieceType.Knight: return Leap(actor, to, m);
                case PieceType.Rook: return Slide(actor, to, m);
                default: return Walk(actor, to, m);
            }
        }

        // --- walking: one thud per square ---

        public static MovePlan Walk(ActorSpec actor, Vector3 to, MoveSettings m)
        {
            var plan = new MovePlan { Name = $"{actor.Type} walks" };
            var actorPlan = new ActorPlan { StartYaw = actor.StartYaw };
            plan.Duration = AddWalk(plan, actor, actorPlan, to, m, delay: 0f);
            plan.Add(actor, actorPlan);
            return plan;
        }

        // Builds the walk for one actor starting `delay` seconds in. Returns the time it ends.
        static float AddWalk(MovePlan plan, ActorSpec actor, ActorPlan a, Vector3 to, MoveSettings m, float delay)
        {
            var flat = to - actor.Start;
            flat.y = 0f;
            float chebyshev = Mathf.Max(Mathf.Abs(flat.x), Mathf.Abs(flat.z));
            int steps = Mathf.Max(1, Mathf.RoundToInt(chebyshev));
            float total = Mathf.Min(Mathf.Max(0.15f, chebyshev * m.stepTime), m.maxWalkTime);
            float stepDur = total / steps;

            float heading = actor.StartYaw + Mathf.DeltaAngle(actor.StartYaw, Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg);
            float walkStart = delay + m.turnTime;
            float walkEnd = walkStart + total;
            float end = walkEnd + m.turnTime;

            a.Yaw.Add(0f, actor.StartYaw);
            if (delay > 0f) a.Yaw.Add(delay, actor.StartYaw, Ease.Linear);
            a.Yaw.Add(walkStart, heading).Add(walkEnd, heading, Ease.Linear).Add(end, actor.StartYaw);

            a.Root.Add(0f, Vector3.zero);
            if (delay > 0f) a.Root.Add(delay, Vector3.zero, Ease.Linear);
            a.Root.Add(walkStart, Vector3.zero, Ease.Linear);
            for (int i = 0; i < steps; i++)
            {
                float t0 = walkStart + i * stepDur;
                var p0 = flat * (i / (float)steps);
                var p1 = flat * ((i + 1) / (float)steps);
                a.Root.Add(t0 + stepDur * 0.5f, (p0 + p1) * 0.5f + Vector3.up * m.bobHeight, Ease.Out);
                a.Root.Add(t0 + stepDur, p1, Ease.In);
                plan.FootfallList.Add(t0 + stepDur);
            }
            return end;
        }

        // --- the knight leaps in an arc ---

        public static MovePlan Leap(ActorSpec actor, Vector3 to, MoveSettings m)
        {
            var plan = new MovePlan { Name = "Knight leaps" };
            var a = new ActorPlan { StartYaw = actor.StartYaw };
            var flat = to - actor.Start;
            flat.y = 0f;
            float heading = actor.StartYaw + Mathf.DeltaAngle(actor.StartYaw, Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg);
            float start = m.turnTime, land = start + m.leapTime, end = land + m.turnTime;

            a.Yaw.Add(0f, actor.StartYaw).Add(start, heading).Add(land, heading, Ease.Linear).Add(end, actor.StartYaw);
            a.Root.Add(0f, Vector3.zero).Add(start, Vector3.zero, Ease.Linear);
            const int segments = 8;
            for (int i = 1; i <= segments; i++)
            {
                float u = i / (float)segments;
                a.Root.Add(start + m.leapTime * u, flat * u + Vector3.up * (4f * m.leapHeight * u * (1f - u)), Ease.Linear);
            }
            // the horse's head tosses on the leap
            a.Part("HorseHead").Add(0f, 0f).Add(start, 0f).Add(start + m.leapTime * 0.5f, -18f).Add(land, 8f, Ease.In).Add(end, 0f);
            plan.FootfallList.Add(land);
            plan.Duration = end;
            return plan.Add(actor, a);
        }

        // --- the rook slides ---

        public static MovePlan Slide(ActorSpec actor, Vector3 to, MoveSettings m)
        {
            var plan = new MovePlan { Name = "Rook slides" };
            var a = new ActorPlan { StartYaw = actor.StartYaw };
            var flat = to - actor.Start;
            flat.y = 0f;
            int squares = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(Mathf.Abs(flat.x), Mathf.Abs(flat.z))));
            float duration = m.slideBase + m.slidePerSquare * (squares - 1);
            a.Root.Add(0f, Vector3.zero).Add(duration, flat);
            plan.FootfallList.Add(duration);
            plan.Duration = duration;
            return plan.Add(actor, a);
        }

        // --- castling: the king strides, the rook hops over ---

        public static MovePlan Castle(ActorSpec king, Vector3 kingTo, ActorSpec rook, Vector3 rookTo, MoveSettings m)
        {
            var plan = new MovePlan { Name = "Castling" };
            var kingPlan = new ActorPlan { StartYaw = king.StartYaw };
            float kingEnd = AddWalk(plan, king, kingPlan, kingTo, m, delay: 0f);
            plan.Add(king, kingPlan);

            var rookPlan = new ActorPlan { StartYaw = rook.StartYaw };
            var flat = rookTo - rook.Start;
            flat.y = 0f;
            float start = m.castleRookDelay + m.turnTime;
            float land = start + m.leapTime;
            rookPlan.Root.Add(0f, Vector3.zero).Add(start, Vector3.zero, Ease.Linear);
            const int segments = 8;
            for (int i = 1; i <= segments; i++)
            {
                float u = i / (float)segments;
                rookPlan.Root.Add(start + m.leapTime * u, flat * u + Vector3.up * (4f * m.castleHopHeight * u * (1f - u)), Ease.Linear);
            }
            plan.Add(rook, rookPlan);
            plan.FootfallList.Add(land);
            plan.FootfallList.Sort();
            plan.Duration = Mathf.Max(kingEnd, land);
            return plan;
        }

        // --- promotion: the pawn kneels, a flash, the new piece rises ---

        public static MovePlan Promote(ActorSpec pawn, PieceType to, string newId, MoveSettings m)
        {
            var plan = new MovePlan { Name = $"Pawn promotes to {to}" };
            float kneel = m.kneelTime;
            float duration = kneel + m.riseTime;

            var pawnPlan = new ActorPlan { StartYaw = pawn.StartYaw, HiddenFrom = kneel };
            pawnPlan.Part("Body").Add(0f, 0f).Add(kneel, 35f, Ease.Out);
            pawnPlan.Part("Head").Add(0f, 0f).Add(kneel, 25f, Ease.Out);
            pawnPlan.Root.Add(0f, Vector3.zero).Add(kneel, Vector3.down * 0.08f, Ease.Out);
            plan.Add(pawn, pawnPlan);

            var risen = new ActorSpec { Id = newId, Type = to, Side = pawn.Side, Start = pawn.Start, StartYaw = pawn.StartYaw };
            var risePlan = new ActorPlan { StartYaw = pawn.StartYaw, HiddenUntil = kneel };
            risePlan.Root.Add(0f, Vector3.down * 0.2f).Add(kneel, Vector3.down * 0.2f).Add(duration, Vector3.zero, Ease.Out);
            risePlan.Scale.Add(0f, 0.85f).Add(kneel, 0.85f).Add(duration, 1f, Ease.Out);
            plan.Add(risen, risePlan);

            plan.SwapTime = kneel;
            plan.Camera.Add(0f, 0f).Add(kneel, 0.4f).Add(duration, 0f);
            plan.FootfallList.Add(duration);
            plan.Duration = duration;
            return plan;
        }

        // --- the king in check braces behind his shield ---

        public static MovePlan Brace(ActorSpec king, MoveSettings m)
        {
            var plan = new MovePlan { Name = "King braces" };
            var a = new ActorPlan { StartYaw = king.StartYaw };
            float b = m.braceTime;
            a.Part("ArmL").Add(0f, 0f).Add(b * 0.3f, -60f, Ease.Out).Add(b * 0.7f, -60f).Add(b, 0f);
            a.Part("Body").Add(0f, 0f).Add(b * 0.3f, -10f, Ease.Out).Add(b * 0.7f, -10f).Add(b, 0f);
            a.Part("Head").Add(0f, 0f).Add(b * 0.3f, -8f, Ease.Out).Add(b * 0.7f, -8f).Add(b, 0f);
            plan.Duration = b;
            return plan.Add(king, a);
        }

        // --- the mated king falls and stays down ---

        public static MovePlan Fall(ActorSpec king, MoveSettings m)
        {
            var plan = new MovePlan { Name = "King falls" };
            var a = new ActorPlan { StartYaw = king.StartYaw };
            float f = m.fallTime;
            a.Pitch.Add(0f, 0f).Add(f * 0.2f, 0f).Add(f, -88f, Ease.In);
            foreach (var arm in new[] { "ArmR", "ArmL" })
                a.Part(arm).Add(0f, 0f).Add(f * 0.2f, 0f).Add(f * 0.6f, 55f, Ease.Out).Add(f, 25f);
            plan.Camera.Add(0f, 0f).Add(f * 0.8f, 1f);
            plan.FootfallList.Add(f);
            plan.Duration = f;
            return plan.Add(king, a);
        }

        // --- resigning: the king kneels and drops his sword ---

        public static MovePlan Resign(ActorSpec king, MoveSettings m)
        {
            var plan = new MovePlan { Name = "King resigns" };
            var a = new ActorPlan { StartYaw = king.StartYaw };
            float r = m.resignTime;
            a.Part("Body").Add(0f, 0f).Add(r * 0.5f, 40f, Ease.Out).Add(r, 40f);
            a.Part("Head").Add(0f, 0f).Add(r * 0.5f, 35f, Ease.Out).Add(r, 35f);
            a.Part("Weapon").Add(0f, 0f).Add(r * 0.4f, 0f).Add(r * 0.75f, 70f, Ease.In).Add(r, 70f);
            a.Root.Add(0f, Vector3.zero).Add(r * 0.5f, Vector3.down * 0.1f, Ease.Out).Add(r, Vector3.down * 0.1f);
            plan.Camera.Add(0f, 0f).Add(r, 0.6f);
            plan.Duration = r;
            return plan.Add(king, a);
        }
    }
}
