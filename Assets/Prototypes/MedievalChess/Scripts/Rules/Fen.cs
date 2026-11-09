using System;
using System.Text;

namespace Prototypes.MedievalChess
{
    /// <summary>Reads and writes positions in FEN, the standard text form.</summary>
    public static class Fen
    {
        public const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        public static Position Parse(string fen)
        {
            if (string.IsNullOrWhiteSpace(fen)) throw new FormatException("The FEN is empty.");
            var fields = fen.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length < 4 || fields.Length > 6) throw new FormatException($"A FEN needs 4 to 6 fields, this has {fields.Length}.");

            var p = new Position();
            ParsePlacement(p, fields[0]);

            switch (fields[1])
            {
                case "w": p.SideToMove = ChessSide.White; break;
                case "b": p.SideToMove = ChessSide.Black; break;
                default: throw new FormatException($"The side to move must be w or b, not '{fields[1]}'.");
            }

            p.Castling = ParseCastling(p, fields[2]);

            if (fields[3] == "-") p.EnPassant = -1;
            else
            {
                if (!Squares.TryParse(fields[3], out int ep)) throw new FormatException($"'{fields[3]}' is not a square for en passant.");
                int expectedRank = p.SideToMove == ChessSide.White ? 5 : 2;
                if (Squares.RankOf(ep) != expectedRank)
                    throw new FormatException($"The en passant square {fields[3]} is on the wrong rank for {p.SideToMove} to move.");
                p.EnPassant = ep;
            }

            p.HalfmoveClock = fields.Length > 4 ? ParseCounter(fields[4], "halfmove clock") : 0;
            p.FullmoveNumber = fields.Length > 5 ? ParseCounter(fields[5], "move number") : 1;
            if (p.FullmoveNumber < 1) throw new FormatException("The move number must be at least 1.");

            p.Finish();

            // The side that has just moved cannot have left its own king in check.
            if (p.InCheck(Pieces.Opposite(p.SideToMove)))
                throw new FormatException("The side that is not to move is in check, which cannot happen.");
            return p;
        }

        static void ParsePlacement(Position p, string placement)
        {
            var ranks = placement.Split('/');
            if (ranks.Length != 8) throw new FormatException($"The board needs 8 ranks separated by '/', this has {ranks.Length}.");

            int[] kings = new int[2];
            for (int i = 0; i < 8; i++)
            {
                int rank = 7 - i;
                int file = 0;
                foreach (char c in ranks[i])
                {
                    if (c >= '1' && c <= '8') { file += c - '0'; continue; }
                    if (file > 7) throw new FormatException($"Rank {rank + 1} has more than 8 squares.");
                    int piece = Pieces.FromChar(c);
                    var type = Pieces.TypeOf(piece);
                    if (type == PieceType.Pawn && (rank == 0 || rank == 7)) throw new FormatException("A pawn cannot stand on the first or the last rank.");
                    if (type == PieceType.King) kings[(int)Pieces.SideOf(piece)]++;
                    p.Set(Squares.Make(file, rank), piece);
                    file++;
                }
                if (file != 8) throw new FormatException($"Rank {rank + 1} has {file} squares, not 8.");
            }
            if (kings[0] != 1 || kings[1] != 1) throw new FormatException("Each side needs exactly one king.");
        }

        static int ParseCastling(Position p, string field)
        {
            if (field == "-") return 0;
            int rights = 0;
            foreach (char c in field)
            {
                switch (c)
                {
                    case 'K': rights |= Position.WhiteKing; Require(p, 4, PieceType.King, ChessSide.White, 7, c); break;
                    case 'Q': rights |= Position.WhiteQueen; Require(p, 4, PieceType.King, ChessSide.White, 0, c); break;
                    case 'k': rights |= Position.BlackKing; Require(p, 60, PieceType.King, ChessSide.Black, 63, c); break;
                    case 'q': rights |= Position.BlackQueen; Require(p, 60, PieceType.King, ChessSide.Black, 56, c); break;
                    default: throw new FormatException($"'{c}' is not a castling right (use K, Q, k, q or -).");
                }
            }
            return rights;
        }

        // A castling right needs the king and the rook on their home squares.
        static void Require(Position p, int kingSquare, PieceType king, ChessSide side, int rookSquare, char right)
        {
            if (p[kingSquare] != Pieces.Make(king, side) || p[rookSquare] != Pieces.Make(PieceType.Rook, side))
                throw new FormatException($"The castling right '{right}' needs the king and the rook on their home squares.");
        }

        static int ParseCounter(string text, string what)
        {
            if (!int.TryParse(text, out int value) || value < 0) throw new FormatException($"The {what} '{text}' is not a number.");
            return value;
        }

        public static string Write(Position p)
        {
            var sb = new StringBuilder();
            for (int rank = 7; rank >= 0; rank--)
            {
                int empty = 0;
                for (int file = 0; file < 8; file++)
                {
                    int piece = p[Squares.Make(file, rank)];
                    if (piece == Pieces.Empty) { empty++; continue; }
                    if (empty > 0) { sb.Append(empty); empty = 0; }
                    sb.Append(Pieces.ToChar(piece));
                }
                if (empty > 0) sb.Append(empty);
                if (rank > 0) sb.Append('/');
            }

            sb.Append(p.SideToMove == ChessSide.White ? " w " : " b ");
            if (p.Castling == 0) sb.Append('-');
            else
            {
                if ((p.Castling & Position.WhiteKing) != 0) sb.Append('K');
                if ((p.Castling & Position.WhiteQueen) != 0) sb.Append('Q');
                if ((p.Castling & Position.BlackKing) != 0) sb.Append('k');
                if ((p.Castling & Position.BlackQueen) != 0) sb.Append('q');
            }
            sb.Append(' ').Append(p.EnPassant < 0 ? "-" : Squares.Name(p.EnPassant));
            sb.Append(' ').Append(p.HalfmoveClock).Append(' ').Append(p.FullmoveNumber);
            return sb.ToString();
        }
    }
}
