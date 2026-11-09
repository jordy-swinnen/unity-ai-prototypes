using System.Collections.Generic;

namespace Prototypes.MedievalChess
{
    /// <summary>Counts the legal move sequences of a given length. Used to check the move generator against published numbers.</summary>
    public static class Perft
    {
        public static long Count(Position position, int depth)
        {
            var buffers = new List<Move>[depth + 1];
            for (int i = 0; i <= depth; i++) buffers[i] = new List<Move>(64);
            return Count(position, depth, buffers);
        }

        static long Count(Position p, int depth, List<Move>[] buffers)
        {
            if (depth == 0) return 1;
            var moves = buffers[depth];
            MoveGenerator.GenerateLegal(p, moves);
            if (depth == 1) return moves.Count;
            long total = 0;
            for (int i = 0; i < moves.Count; i++)
            {
                p.MakeMove(moves[i]);
                total += Count(p, depth - 1, buffers);
                p.UnmakeMove();
            }
            return total;
        }
    }
}
