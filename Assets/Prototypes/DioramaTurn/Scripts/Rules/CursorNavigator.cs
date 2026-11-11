using System.Collections.Generic;

namespace Prototypes.DioramaTurn
{
    public enum ScreenDirection { Left, Right, Up, Down }

    /// <summary>Moves a keyboard or gamepad cursor to the nearest node in a direction on the screen.</summary>
    public static class CursorNavigator
    {
        // A node counts for a direction when it lies within 60 degrees of it.
        const float MinCosine = 0.5f;

        public static Cell? Move(int view, Cell from, ScreenDirection direction, IEnumerable<Cell> candidates)
        {
            float dx = direction == ScreenDirection.Right ? 1f : direction == ScreenDirection.Left ? -1f : 0f;
            float dy = direction == ScreenDirection.Up ? 1f : direction == ScreenDirection.Down ? -1f : 0f;
            var origin = ViewRule.ScreenUnits(from, view);

            Cell? best = null;
            float bestScore = float.MaxValue;
            foreach (var c in candidates)
            {
                if (c == from) continue;
                var p = ViewRule.ScreenUnits(c, view);
                float ox = p.X - origin.X, oy = p.Y - origin.Y;
                float dist = (float)System.Math.Sqrt(ox * ox + oy * oy);
                if (dist < 1e-4f) continue;
                float along = ox * dx + oy * dy;
                if (along / dist < MinCosine) continue;
                float perp = System.Math.Abs(ox * dy - oy * dx);
                float score = dist + 1.5f * perp;
                if (score < bestScore - 1e-5f || (System.Math.Abs(score - bestScore) <= 1e-5f && best.HasValue && c.CompareTo(best.Value) < 0))
                {
                    bestScore = score;
                    best = c;
                }
            }
            return best;
        }
    }
}
