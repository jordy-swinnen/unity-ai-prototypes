using System;

namespace Prototypes.MedievalChess
{
    public enum ChessSide
    {
        White = 0,
        Black = 1,
    }

    /// <summary>
    /// Piece codes on the board: 0 is empty, 1 to 6 are the white pieces (pawn, knight, bishop, rook, queen,
    /// king, in the order of <see cref="PieceType"/>), and 7 to 12 are the black ones.
    /// </summary>
    public static class Pieces
    {
        public const int Empty = 0;

        public static int Make(PieceType type, ChessSide side) => 1 + (int)type + 6 * (int)side;
        public static PieceType TypeOf(int piece) => (PieceType)((piece - 1) % 6);
        public static ChessSide SideOf(int piece) => (ChessSide)((piece - 1) / 6);

        const string Letters = "PNBRQKpnbrqk";

        public static char ToChar(int piece) => Letters[piece - 1];

        public static int FromChar(char c)
        {
            int i = Letters.IndexOf(c);
            if (i < 0) throw new FormatException($"'{c}' is not a piece letter.");
            return i + 1;
        }

        public static ChessSide Opposite(ChessSide side) => side == ChessSide.White ? ChessSide.Black : ChessSide.White;
    }

    /// <summary>Squares are numbered 0 to 63 from a1 (0) along the first rank to h1 (7), then a2 (8), up to h8 (63).</summary>
    public static class Squares
    {
        public static int Make(int file, int rank) => rank * 8 + file;
        public static int FileOf(int square) => square & 7;
        public static int RankOf(int square) => square >> 3;

        public static string Name(int square) => $"{(char)('a' + FileOf(square))}{(char)('1' + RankOf(square))}";

        public static int Parse(string name)
        {
            if (name == null || name.Length != 2 || name[0] < 'a' || name[0] > 'h' || name[1] < '1' || name[1] > '8')
                throw new FormatException($"'{name}' is not a square name like e4.");
            return Make(name[0] - 'a', name[1] - '1');
        }

        public static bool TryParse(string name, out int square)
        {
            square = -1;
            if (name == null || name.Length != 2 || name[0] < 'a' || name[0] > 'h' || name[1] < '1' || name[1] > '8') return false;
            square = Make(name[0] - 'a', name[1] - '1');
            return true;
        }

        /// <summary>True for a dark square (a1 is dark).</summary>
        public static bool IsDark(int square) => ((FileOf(square) + RankOf(square)) & 1) == 0;
    }
}
