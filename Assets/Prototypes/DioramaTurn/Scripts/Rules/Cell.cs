using System;

namespace Prototypes.DioramaTurn
{
    /// <summary>A whole-number cell of the room: x and z run along the floor, y is up.</summary>
    public readonly struct Cell : IEquatable<Cell>
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public Cell(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Cell operator +(Cell a, Cell b) => new Cell(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Cell operator -(Cell a, Cell b) => new Cell(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Cell operator *(Cell a, int k) => new Cell(a.X * k, a.Y * k, a.Z * k);
        public static bool operator ==(Cell a, Cell b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        public static bool operator !=(Cell a, Cell b) => !(a == b);

        public bool Equals(Cell other) => this == other;
        public override bool Equals(object obj) => obj is Cell c && this == c;
        public override int GetHashCode() => (X * 73856093) ^ (Y * 19349663) ^ (Z * 83492791);
        public override string ToString() => $"({X},{Y},{Z})";

        /// <summary>A fixed order for ties, so the same room always gives the same route.</summary>
        public int CompareTo(Cell o)
        {
            int c = X.CompareTo(o.X);
            if (c != 0) return c;
            c = Y.CompareTo(o.Y);
            return c != 0 ? c : Z.CompareTo(o.Z);
        }
    }
}
