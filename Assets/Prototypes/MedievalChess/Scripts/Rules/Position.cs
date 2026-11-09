using System.Collections.Generic;

namespace Prototypes.MedievalChess
{
    /// <summary>
    /// A chess position: the board, whose move it is, castling rights, the en passant square and the
    /// clocks. Moves are made and unmade in place (the search relies on that), and a hash is kept up to date.
    /// </summary>
    public sealed class Position
    {
        public const int WhiteKing = 1, WhiteQueen = 2, BlackKing = 4, BlackQueen = 8;

        // Which castling rights survive a move from or to each square.
        static readonly int[] CastleMask = BuildCastleMask();

        static int[] BuildCastleMask()
        {
            var mask = new int[64];
            for (int i = 0; i < 64; i++) mask[i] = 15;
            mask[0] = 15 & ~WhiteQueen;
            mask[4] = 15 & ~(WhiteKing | WhiteQueen);
            mask[7] = 15 & ~WhiteKing;
            mask[56] = 15 & ~BlackQueen;
            mask[60] = 15 & ~(BlackKing | BlackQueen);
            mask[63] = 15 & ~BlackKing;
            return mask;
        }

        struct Undo
        {
            public Move Move;
            public int Captured;
            public int Castling;
            public int EnPassant;
            public int HalfmoveClock;
            public ulong Hash;
        }

        readonly int[] board = new int[64];
        readonly int[] kingSquare = { -1, -1 };
        readonly List<Undo> undo = new List<Undo>(256);

        public ChessSide SideToMove { get; internal set; }
        public int Castling { get; internal set; }
        /// <summary>The square a pawn could capture onto en passant, or -1.</summary>
        public int EnPassant { get; internal set; } = -1;
        public int HalfmoveClock { get; internal set; }
        public int FullmoveNumber { get; internal set; } = 1;
        public ulong Hash { get; private set; }
        /// <summary>How many moves have been made on this object (and not unmade).</summary>
        public int Ply => undo.Count;

        public int this[int square] => board[square];

        public static Position Start() => Fen.Parse(Fen.StartFen);

        public int KingSquare(ChessSide side) => kingSquare[(int)side];

        public bool CanCastle(ChessSide side, bool kingSide)
        {
            int bit = side == ChessSide.White ? (kingSide ? WhiteKing : WhiteQueen) : (kingSide ? BlackKing : BlackQueen);
            return (Castling & bit) != 0;
        }

        // --- setting up (used by the FEN reader) ---

        internal void Set(int square, int piece)
        {
            board[square] = piece;
            if (piece != Pieces.Empty && Pieces.TypeOf(piece) == PieceType.King) kingSquare[(int)Pieces.SideOf(piece)] = square;
        }

        internal void Finish() => Hash = ComputeHash();

        public Position Clone()
        {
            var copy = new Position
            {
                SideToMove = SideToMove, Castling = Castling, EnPassant = EnPassant,
                HalfmoveClock = HalfmoveClock, FullmoveNumber = FullmoveNumber, Hash = Hash,
            };
            board.CopyTo(copy.board, 0);
            copy.kingSquare[0] = kingSquare[0];
            copy.kingSquare[1] = kingSquare[1];
            return copy;
        }

        // --- the hash ---

        /// <summary>Works the hash out from scratch. It must always equal <see cref="Hash"/>.</summary>
        public ulong ComputeHash()
        {
            ulong h = 0;
            for (int sq = 0; sq < 64; sq++)
                if (board[sq] != Pieces.Empty) h ^= Zobrist.Piece[board[sq], sq];
            h ^= Zobrist.Castling[Castling];
            if (SideToMove == ChessSide.Black) h ^= Zobrist.SideToMove;
            return h;
        }

        /// <summary>
        /// The key for counting repetitions. It is the hash, plus the en passant file only when an en passant
        /// capture is really possible (two positions with the same pieces are the same only if the same moves exist).
        /// </summary>
        public ulong RepetitionKey()
        {
            if (EnPassant < 0 || !EnPassantCapturePossible()) return Hash;
            return Hash ^ Zobrist.EnPassantFile[Squares.FileOf(EnPassant)];
        }

        bool EnPassantCapturePossible()
        {
            int file = Squares.FileOf(EnPassant);
            int pawn = Pieces.Make(PieceType.Pawn, SideToMove);
            int rank = SideToMove == ChessSide.White ? 4 : 3;
            return (file > 0 && board[Squares.Make(file - 1, rank)] == pawn)
                || (file < 7 && board[Squares.Make(file + 1, rank)] == pawn);
        }

        // --- making and unmaking moves ---

        public void MakeMove(Move m)
        {
            int from = m.From, to = m.To;
            int piece = board[from];
            var side = SideToMove;
            var u = new Undo { Move = m, Castling = Castling, EnPassant = EnPassant, HalfmoveClock = HalfmoveClock, Hash = Hash };

            // the captured piece
            if ((m.Flags & MoveFlags.EnPassant) != 0)
            {
                int capturedSquare = side == ChessSide.White ? to - 8 : to + 8;
                u.Captured = board[capturedSquare];
                board[capturedSquare] = Pieces.Empty;
                Hash ^= Zobrist.Piece[u.Captured, capturedSquare];
            }
            else if (board[to] != Pieces.Empty)
            {
                u.Captured = board[to];
                Hash ^= Zobrist.Piece[u.Captured, to];
            }

            // the moving piece (a pawn becomes its promotion piece on arrival)
            int placed = m.Promotion.HasValue ? Pieces.Make(m.Promotion.Value, side) : piece;
            board[from] = Pieces.Empty;
            Hash ^= Zobrist.Piece[piece, from];
            board[to] = placed;
            Hash ^= Zobrist.Piece[placed, to];
            if (Pieces.TypeOf(piece) == PieceType.King) kingSquare[(int)side] = to;

            // the rook jumps in castling
            if ((m.Flags & MoveFlags.CastleKing) != 0) MoveRook(side, from + 3, from + 1);
            else if ((m.Flags & MoveFlags.CastleQueen) != 0) MoveRook(side, from - 4, from - 1);

            // castling rights, the en passant square and the clocks
            int newCastling = Castling & CastleMask[from] & CastleMask[to];
            Hash ^= Zobrist.Castling[Castling] ^ Zobrist.Castling[newCastling];
            Castling = newCastling;
            EnPassant = (m.Flags & MoveFlags.DoublePush) != 0 ? (from + to) / 2 : -1;
            HalfmoveClock = (Pieces.TypeOf(piece) == PieceType.Pawn || u.Captured != Pieces.Empty) ? 0 : HalfmoveClock + 1;
            if (side == ChessSide.Black) FullmoveNumber++;

            SideToMove = Pieces.Opposite(side);
            Hash ^= Zobrist.SideToMove;
            undo.Add(u);
        }

        void MoveRook(ChessSide side, int from, int to)
        {
            int rook = board[from];
            board[from] = Pieces.Empty;
            board[to] = rook;
            Hash ^= Zobrist.Piece[rook, from] ^ Zobrist.Piece[rook, to];
        }

        public void UnmakeMove()
        {
            var u = undo[undo.Count - 1];
            undo.RemoveAt(undo.Count - 1);
            var m = u.Move;
            int from = m.From, to = m.To;

            SideToMove = Pieces.Opposite(SideToMove);
            var side = SideToMove;
            if (side == ChessSide.Black) FullmoveNumber--;

            int placed = board[to];
            int piece = m.Promotion.HasValue ? Pieces.Make(PieceType.Pawn, side) : placed;
            board[from] = piece;
            board[to] = Pieces.Empty;
            if (Pieces.TypeOf(piece) == PieceType.King) kingSquare[(int)side] = from;

            if ((m.Flags & MoveFlags.EnPassant) != 0)
                board[side == ChessSide.White ? to - 8 : to + 8] = u.Captured;
            else if (u.Captured != Pieces.Empty)
                board[to] = u.Captured;

            if ((m.Flags & MoveFlags.CastleKing) != 0) { board[from + 3] = board[from + 1]; board[from + 1] = Pieces.Empty; }
            else if ((m.Flags & MoveFlags.CastleQueen) != 0) { board[from - 4] = board[from - 1]; board[from - 1] = Pieces.Empty; }

            Castling = u.Castling;
            EnPassant = u.EnPassant;
            HalfmoveClock = u.HalfmoveClock;
            Hash = u.Hash;
        }

        // --- attacks ---

        public bool InCheck() => InCheck(SideToMove);

        public bool InCheck(ChessSide side) => IsAttacked(kingSquare[(int)side], Pieces.Opposite(side));

        /// <summary>Is the square attacked by a piece of the given side?</summary>
        public bool IsAttacked(int square, ChessSide by)
        {
            int file = Squares.FileOf(square), rank = Squares.RankOf(square);

            int pawnRank = by == ChessSide.White ? rank - 1 : rank + 1;
            if (pawnRank >= 0 && pawnRank < 8)
            {
                int pawn = Pieces.Make(PieceType.Pawn, by);
                if (file > 0 && board[Squares.Make(file - 1, pawnRank)] == pawn) return true;
                if (file < 7 && board[Squares.Make(file + 1, pawnRank)] == pawn) return true;
            }

            int knight = Pieces.Make(PieceType.Knight, by);
            foreach (int t in Tables.KnightTargets[square]) if (board[t] == knight) return true;

            int king = Pieces.Make(PieceType.King, by);
            foreach (int t in Tables.KingTargets[square]) if (board[t] == king) return true;

            int rook = Pieces.Make(PieceType.Rook, by), queen = Pieces.Make(PieceType.Queen, by), bishop = Pieces.Make(PieceType.Bishop, by);
            foreach (var ray in Tables.Rays[square])
            {
                bool straight = ray.Straight;
                foreach (int t in ray.Squares)
                {
                    int p = board[t];
                    if (p == Pieces.Empty) continue;
                    if (p == queen || (straight ? p == rook : p == bishop)) return true;
                    break;
                }
            }
            return false;
        }

        // --- material ---

        /// <summary>True when neither side can possibly give checkmate, however it is played.</summary>
        public bool IsInsufficientMaterial()
        {
            int minors = 0, lightBishops = 0, darkBishops = 0, knights = 0;
            for (int sq = 0; sq < 64; sq++)
            {
                int p = board[sq];
                if (p == Pieces.Empty) continue;
                switch (Pieces.TypeOf(p))
                {
                    case PieceType.Pawn:
                    case PieceType.Rook:
                    case PieceType.Queen:
                        return false;
                    case PieceType.Knight:
                        knights++;
                        minors++;
                        break;
                    case PieceType.Bishop:
                        if (Squares.IsDark(sq)) darkBishops++; else lightBishops++;
                        minors++;
                        break;
                }
            }
            if (minors <= 1) return true;                         // king against king, or one minor piece
            if (knights == 0 && (lightBishops == 0 || darkBishops == 0)) return true; // bishops that all stand on one colour
            return false;
        }
    }
}
