using System;

namespace Prototypes.DioramaTurn
{
    /// <summary>A point on the screen lattice of one view (see <see cref="ViewRule.Screen"/>).</summary>
    public readonly struct ScreenPoint : IEquatable<ScreenPoint>
    {
        public readonly int U;
        public readonly int V;

        public ScreenPoint(int u, int v)
        {
            U = u;
            V = v;
        }

        public bool Equals(ScreenPoint o) => U == o.U && V == o.V;
        public override bool Equals(object obj) => obj is ScreenPoint p && Equals(p);
        public override int GetHashCode() => U * 397 ^ V;
        public override string ToString() => $"[{U},{V}]";
    }

    /// <summary>
    /// The four views of the room. The camera is orthographic and true isometric, so a step of
    /// (rx, 1, rz) toward the camera does not change where something appears on the screen. That
    /// step is the view's depth step. View 0 sits on the +x, +z side, and each next view is a quarter turn.
    /// </summary>
    public static class ViewRule
    {
        public const int ViewCount = 4;

        static readonly Cell[] Depth =
        {
            new Cell(1, 1, 1),
            new Cell(-1, 1, 1),
            new Cell(-1, 1, -1),
            new Cell(1, 1, -1),
        };

        public static readonly Cell[] HorizontalSteps =
        {
            new Cell(1, 0, 0),
            new Cell(-1, 0, 0),
            new Cell(0, 0, 1),
            new Cell(0, 0, -1),
        };

        public static Cell DepthStep(int view) => Depth[Wrap(view)];

        public static int Wrap(int view) => ((view % ViewCount) + ViewCount) % ViewCount;

        /// <summary>The view after a quarter turn: +1 is one way, -1 the other.</summary>
        public static int Rotate(int view, int direction) => Wrap(view + (direction >= 0 ? 1 : -1));

        /// <summary>
        /// Integer screen lattice of a cell in a view: U to the right, V up. A horizontal step moves
        /// one lattice step in both U and V (a diagonal on screen), and the depth step moves nothing.
        /// Real units: u = U / sqrt 2, v = V / sqrt 6.
        /// </summary>
        public static ScreenPoint Screen(Cell c, int view)
        {
            var r = DepthStep(view);
            int u = r.X * c.Z - r.Z * c.X;
            int v = 2 * c.Y - r.X * c.X - r.Z * c.Z;
            return new ScreenPoint(u, v);
        }

        /// <summary>The lattice in world-like units, for geometry on screen (cursor movement).</summary>
        public static (float X, float Y) ScreenUnits(Cell c, int view)
        {
            var p = Screen(c, view);
            return (p.U * 0.70710678f, p.V * 0.40824829f);
        }
    }
}
