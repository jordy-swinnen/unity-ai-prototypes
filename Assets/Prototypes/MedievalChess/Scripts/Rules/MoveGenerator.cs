using System.Collections.Generic;

namespace Prototypes.MedievalChess
{
    /// <summary>Makes the legal moves of a position.</summary>
    public static class MoveGenerator
    {
        static readonly PieceType[] PromotionPieces = { PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight };

        public static List<Move> Legal(Position p)
        {
            var moves = new List<Move>(48);
            GenerateLegal(p, moves);
            return moves;
        }

        /// <summary>Fills the list with the legal moves (the list is cleared first).</summary>
        public static void GenerateLegal(Position p, List<Move> moves)
        {
            moves.Clear();
            GeneratePseudoLegal(p, moves);
            var mover = p.SideToMove;
            int kept = 0;
            for (int i = 0; i < moves.Count; i++)
            {
                var m = moves[i];
                p.MakeMove(m);
                bool legal = !p.InCheck(mover);
                p.UnmakeMove();
                if (legal) moves[kept++] = m;
            }
            moves.RemoveRange(kept, moves.Count - kept);
        }

        /// <summary>The moves of each piece, not yet checking whether they leave the king in check.</summary>
        static void GeneratePseudoLegal(Position p, List<Move> moves)
        {
            var side = p.SideToMove;
            var enemy = Pieces.Opposite(side);
            for (int sq = 0; sq < 64; sq++)
            {
                int piece = p[sq];
                if (piece == Pieces.Empty || Pieces.SideOf(piece) != side) continue;
                switch (Pieces.TypeOf(piece))
                {
                    case PieceType.Pawn: Pawn(p, sq, side, moves); break;
                    case PieceType.Knight: Steps(p, sq, side, Tables.KnightTargets[sq], moves); break;
                    case PieceType.King:
                        Steps(p, sq, side, Tables.KingTargets[sq], moves);
                        Castling(p, sq, side, enemy, moves);
                        break;
                    case PieceType.Bishop: Slide(p, sq, side, moves, straight: false, diagonal: true); break;
                    case PieceType.Rook: Slide(p, sq, side, moves, straight: true, diagonal: false); break;
                    case PieceType.Queen: Slide(p, sq, side, moves, straight: true, diagonal: true); break;
                }
            }
        }

        static void Steps(Position p, int from, ChessSide side, int[] targets, List<Move> moves)
        {
            foreach (int to in targets)
            {
                int target = p[to];
                if (target == Pieces.Empty) moves.Add(new Move(from, to));
                else if (Pieces.SideOf(target) != side) moves.Add(new Move(from, to, MoveFlags.Capture));
            }
        }

        static void Slide(Position p, int from, ChessSide side, List<Move> moves, bool straight, bool diagonal)
        {
            foreach (var ray in Tables.Rays[from])
            {
                if (ray.Straight ? !straight : !diagonal) continue;
                foreach (int to in ray.Squares)
                {
                    int target = p[to];
                    if (target == Pieces.Empty) { moves.Add(new Move(from, to)); continue; }
                    if (Pieces.SideOf(target) != side) moves.Add(new Move(from, to, MoveFlags.Capture));
                    break;
                }
            }
        }

        static void Pawn(Position p, int from, ChessSide side, List<Move> moves)
        {
            int dir = side == ChessSide.White ? 8 : -8;
            int rank = Squares.RankOf(from), file = Squares.FileOf(from);
            int startRank = side == ChessSide.White ? 1 : 6;
            int beforePromotion = side == ChessSide.White ? 6 : 1;

            int one = from + dir;
            if (p[one] == Pieces.Empty)
            {
                if (rank == beforePromotion) AddPromotions(from, one, MoveFlags.None, moves);
                else
                {
                    moves.Add(new Move(from, one));
                    if (rank == startRank && p[one + dir] == Pieces.Empty) moves.Add(new Move(from, one + dir, MoveFlags.DoublePush));
                }
            }

            for (int df = -1; df <= 1; df += 2)
            {
                int f = file + df;
                if (f < 0 || f > 7) continue;
                int to = one + df;
                int target = p[to];
                if (target != Pieces.Empty && Pieces.SideOf(target) != side)
                {
                    if (rank == beforePromotion) AddPromotions(from, to, MoveFlags.Capture, moves);
                    else moves.Add(new Move(from, to, MoveFlags.Capture));
                }
                else if (to == p.EnPassant && target == Pieces.Empty)
                {
                    moves.Add(new Move(from, to, MoveFlags.EnPassant | MoveFlags.Capture));
                }
            }
        }

        static void AddPromotions(int from, int to, MoveFlags flags, List<Move> moves)
        {
            foreach (var piece in PromotionPieces) moves.Add(new Move(from, to, flags, piece));
        }

        static void Castling(Position p, int kingSquare, ChessSide side, ChessSide enemy, List<Move> moves)
        {
            int home = side == ChessSide.White ? 4 : 60;
            if (kingSquare != home) return;
            int rook = Pieces.Make(PieceType.Rook, side);

            bool kingSide = p.CanCastle(side, true) && p[home + 3] == rook && p[home + 1] == Pieces.Empty && p[home + 2] == Pieces.Empty;
            bool queenSide = p.CanCastle(side, false) && p[home - 4] == rook && p[home - 1] == Pieces.Empty
                && p[home - 2] == Pieces.Empty && p[home - 3] == Pieces.Empty;
            if (!kingSide && !queenSide) return;
            if (p.IsAttacked(home, enemy)) return; // not out of check

            // The king may not pass through or land on an attacked square. The rook may.
            if (kingSide && !p.IsAttacked(home + 1, enemy) && !p.IsAttacked(home + 2, enemy))
                moves.Add(new Move(home, home + 2, MoveFlags.CastleKing));
            if (queenSide && !p.IsAttacked(home - 1, enemy) && !p.IsAttacked(home - 2, enemy))
                moves.Add(new Move(home, home - 2, MoveFlags.CastleQueen));
        }
    }
}
