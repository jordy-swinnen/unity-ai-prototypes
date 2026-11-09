using System;

namespace Prototypes.MedievalChess
{
    [Flags]
    public enum MoveFlags : byte
    {
        None = 0,
        Capture = 1,
        EnPassant = 2,
        DoublePush = 4,
        CastleKing = 8,
        CastleQueen = 16,
    }

    /// <summary>One move: from and to squares, the piece a pawn promotes to (if any), and what kind of move it is.</summary>
    public readonly struct Move : IEquatable<Move>
    {
        public readonly byte From;
        public readonly byte To;
        readonly sbyte promotion;
        public readonly MoveFlags Flags;

        public Move(int from, int to, MoveFlags flags = MoveFlags.None, PieceType? promotion = null)
        {
            From = (byte)from;
            To = (byte)to;
            this.promotion = promotion.HasValue ? (sbyte)promotion.Value : (sbyte)-1;
            Flags = flags;
        }

        public PieceType? Promotion => promotion < 0 ? (PieceType?)null : (PieceType)promotion;
        public bool IsValid => From != To;
        public bool IsCapture => (Flags & MoveFlags.Capture) != 0;
        public bool IsCastle => (Flags & (MoveFlags.CastleKing | MoveFlags.CastleQueen)) != 0;

        /// <summary>The move as four letters and digits (e2e4), with a fifth for a promotion (e7e8q).</summary>
        public string ToUci()
        {
            string s = Squares.Name(From) + Squares.Name(To);
            if (Promotion.HasValue) s += "nbrq"[Math.Max(0, (int)Promotion.Value - 1)];
            return s;
        }

        /// <summary>The same move for the purpose of matching: same squares and same promotion.</summary>
        public bool Matches(int from, int to, PieceType? promotionPiece) =>
            From == from && To == to && promotion == (promotionPiece.HasValue ? (sbyte)promotionPiece.Value : (sbyte)-1);

        public bool Equals(Move other) => From == other.From && To == other.To && promotion == other.promotion && Flags == other.Flags;
        public override bool Equals(object obj) => obj is Move m && Equals(m);
        public override int GetHashCode() => From | (To << 6) | ((promotion + 1) << 12) | ((int)Flags << 16);
        public override string ToString() => ToUci();
    }
}
