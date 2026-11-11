namespace Prototypes.DioramaTurn
{
    public enum JoinKind
    {
        None,
        /// <summary>A normal neighbour on the same level, in every view.</summary>
        Adjacent,
        /// <summary>Joined by the view: exactly one cell step apart on screen, at another depth.</summary>
        Screen,
    }

    /// <summary>
    /// The rule of the game, in one place. In view k, A and B are joined if B - A = n + t * r_k, where n
    /// is one of the four horizontal steps, t is any whole number and r_k is the view's depth step.
    /// t = 0 is a normal neighbour. Joins are two-way.
    /// </summary>
    public static class JoinRule
    {
        public static JoinKind Classify(Cell a, Cell b, int view)
        {
            if (a == b) return JoinKind.None;
            var d = b - a;
            var r = ViewRule.DepthStep(view);
            foreach (var n in ViewRule.HorizontalSteps)
            {
                var rem = d - n;
                int t = rem.Y;
                if (rem.X == t * r.X && rem.Z == t * r.Z)
                {
                    return t == 0 ? JoinKind.Adjacent : JoinKind.Screen;
                }
            }
            return JoinKind.None;
        }

        public static bool AreJoined(Cell a, Cell b, int view) => Classify(a, b, view) != JoinKind.None;
    }
}
