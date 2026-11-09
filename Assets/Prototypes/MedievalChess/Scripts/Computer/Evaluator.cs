using System;

namespace Prototypes.MedievalChess
{
    /// <summary>
    /// Scores a position for the side to move, in centipawns: material, where the pieces stand, pawn
    /// structure and king safety. The position tables are made from the numbers in <see cref="EvaluationSettings"/>.
    /// One evaluator is for one thread, because it keeps scratch arrays.
    /// </summary>
    public sealed class Evaluator
    {
        readonly EvaluationSettings s;
        // Tables are from White's point of view, by square. Black uses the same table with the rank flipped.
        readonly int[] knightTable = new int[64];
        readonly int[] bishopTable = new int[64];
        readonly int[] queenTable = new int[64];
        readonly int[] rookTable = new int[64];
        readonly int[] pawnTable = new int[64];
        readonly int[] kingMiddle = new int[64];
        readonly int[] kingEnd = new int[64];

        // scratch
        readonly int[,] pawnsOnFile = new int[2, 8];
        readonly int[] maxPawnRank = new int[2 * 8];   // [side * 8 + file], -1 if none
        readonly int[] minPawnRank = new int[2 * 8];   // 8 if none

        public Evaluator(EvaluationSettings settings)
        {
            s = settings;
            for (int sq = 0; sq < 64; sq++)
            {
                int file = Squares.FileOf(sq), rank = Squares.RankOf(sq);
                // distance from the middle of the board: about 0.7 in the middle, 3.5 on the rim, 4.9 in a corner
                double d = Math.Sqrt((file - 3.5) * (file - 3.5) + (rank - 3.5) * (rank - 3.5));
                double nearness = 3.0 - d;
                knightTable[sq] = (int)Math.Round(s.knightCentre * nearness);
                bishopTable[sq] = (int)Math.Round(s.bishopCentre * nearness);
                queenTable[sq] = (int)Math.Round(s.queenCentre * nearness);
                kingEnd[sq] = (int)Math.Round(s.kingEndgameCentre * nearness);

                int rook = (file == 3 || file == 4) ? s.rookOnCentralFile : 0;
                if (rank == 6) rook += s.rookOnSeventh;
                rookTable[sq] = rook;

                int pawn = s.pawnRankBonus[rank];
                bool centre = file == 3 || file == 4;
                if (rank == 1 && centre) pawn -= 12;       // leave the centre pawns at home and the game is slow
                else if (rank == 2 && centre) pawn += 6;
                else if (rank == 3 && centre) pawn += 14;
                else if (rank == 3 && (file == 2 || file == 5)) pawn += 4;
                pawnTable[sq] = pawn;

                kingMiddle[sq] = rank == 0 ? s.kingBackRank[file] : rank == 1 ? s.kingSecondRank[file] : s.kingAdvanced[Math.Min(rank - 2, s.kingAdvanced.Length - 1)];
            }
        }

        public int Evaluate(Position p)
        {
            Array.Clear(pawnsOnFile, 0, pawnsOnFile.Length);
            for (int i = 0; i < 16; i++) { maxPawnRank[i] = -1; minPawnRank[i] = 8; }

            int[] queens = new int[2], rooks = new int[2], minors = new int[2], bishops = new int[2];
            int pawns = 0, knights = 0, darkBishops = 0, lightBishops = 0;

            // first pass: count what is there
            for (int sq = 0; sq < 64; sq++)
            {
                int piece = p[sq];
                if (piece == Pieces.Empty) continue;
                int side = (int)Pieces.SideOf(piece);
                switch (Pieces.TypeOf(piece))
                {
                    case PieceType.Pawn:
                        {
                            int file = Squares.FileOf(sq), rank = Squares.RankOf(sq);
                            pawns++;
                            pawnsOnFile[side, file]++;
                            if (rank > maxPawnRank[side * 8 + file]) maxPawnRank[side * 8 + file] = rank;
                            if (rank < minPawnRank[side * 8 + file]) minPawnRank[side * 8 + file] = rank;
                            break;
                        }
                    case PieceType.Knight: minors[side]++; knights++; break;
                    case PieceType.Bishop:
                        minors[side]++;
                        bishops[side]++;
                        if (Squares.IsDark(sq)) darkBishops++; else lightBishops++;
                        break;
                    case PieceType.Rook: rooks[side]++; break;
                    case PieceType.Queen: queens[side]++; break;
                }
            }

            // Neither side can ever give mate: a dead draw, whatever the pieces are worth.
            if (pawns == 0 && queens[0] + queens[1] + rooks[0] + rooks[1] == 0)
            {
                int totalMinors = minors[0] + minors[1];
                if (totalMinors <= 1 || (knights == 0 && (lightBishops == 0 || darkBishops == 0))) return 0;
            }

            // An endgame: no queens, or every side with a queen has no rook and at most one minor piece.
            bool endgame = QuietSide(queens[0], rooks[0], minors[0]) && QuietSide(queens[1], rooks[1], minors[1]);
            var kingTable = endgame ? kingEnd : kingMiddle;

            // second pass: score it from White's point of view
            int score = 0;
            for (int sq = 0; sq < 64; sq++)
            {
                int piece = p[sq];
                if (piece == Pieces.Empty) continue;
                var side = Pieces.SideOf(piece);
                int sign = side == ChessSide.White ? 1 : -1;
                int index = side == ChessSide.White ? sq : sq ^ 56;
                int value;
                switch (Pieces.TypeOf(piece))
                {
                    case PieceType.Pawn: value = s.pawn + pawnTable[index] + PawnStructure(sq, side); break;
                    case PieceType.Knight: value = s.knight + knightTable[index]; break;
                    case PieceType.Bishop: value = s.bishop + bishopTable[index]; break;
                    case PieceType.Rook: value = s.rook + rookTable[index]; break;
                    case PieceType.Queen: value = s.queen + queenTable[index]; break;
                    default: value = kingTable[index]; break;
                }
                score += sign * value;
            }
            if (bishops[0] >= 2) score += s.bishopPair;
            if (bishops[1] >= 2) score -= s.bishopPair;

            // doubled pawns
            for (int f = 0; f < 8; f++)
            {
                if (pawnsOnFile[0, f] > 1) score -= s.doubledPawn * (pawnsOnFile[0, f] - 1);
                if (pawnsOnFile[1, f] > 1) score += s.doubledPawn * (pawnsOnFile[1, f] - 1);
            }

            return (p.SideToMove == ChessSide.White ? score : -score) + s.tempo;
        }

        static bool QuietSide(int queens, int rooks, int minors) => queens == 0 || (rooks == 0 && minors <= 1);

        // Isolated and passed pawns, from the pawn's own side (a positive number is good for that side).
        int PawnStructure(int sq, ChessSide side)
        {
            int file = Squares.FileOf(sq), rank = Squares.RankOf(sq);
            int mine = (int)side, theirs = 1 - mine;
            int bonus = 0;

            bool neighbours = (file > 0 && pawnsOnFile[mine, file - 1] > 0) || (file < 7 && pawnsOnFile[mine, file + 1] > 0);
            if (!neighbours) bonus -= s.isolatedPawn;

            bool passed = true;
            for (int g = Math.Max(0, file - 1); g <= Math.Min(7, file + 1) && passed; g++)
            {
                if (side == ChessSide.White) { if (maxPawnRank[theirs * 8 + g] > rank) passed = false; }
                else if (minPawnRank[theirs * 8 + g] < rank) passed = false;
            }
            if (passed)
            {
                int ownRank = side == ChessSide.White ? rank : 7 - rank;
                bonus += s.passedPawnBonus[ownRank];
            }
            return bonus;
        }
    }
}
