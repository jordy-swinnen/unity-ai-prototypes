using System.Collections.Generic;

namespace Prototypes.MedievalChess
{
    /// <summary>Squares a knight or a king can reach from each square, and the lines a sliding piece travels.</summary>
    static class Tables
    {
        public struct Ray
        {
            public int[] Squares;
            /// <summary>True for a rank or file, false for a diagonal.</summary>
            public bool Straight;
        }

        public static readonly int[][] KnightTargets = new int[64][];
        public static readonly int[][] KingTargets = new int[64][];
        public static readonly Ray[][] Rays = new Ray[64][];

        static Tables()
        {
            int[,] knight = { { 1, 2 }, { 2, 1 }, { 2, -1 }, { 1, -2 }, { -1, -2 }, { -2, -1 }, { -2, 1 }, { -1, 2 } };
            int[,] straight = { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } };
            int[,] diagonal = { { 1, 1 }, { 1, -1 }, { -1, 1 }, { -1, -1 } };

            for (int sq = 0; sq < 64; sq++)
            {
                int file = sq & 7, rank = sq >> 3;
                var k = new List<int>();
                for (int i = 0; i < 8; i++) Add(k, file + knight[i, 0], rank + knight[i, 1]);
                KnightTargets[sq] = k.ToArray();

                var kg = new List<int>();
                for (int df = -1; df <= 1; df++)
                    for (int dr = -1; dr <= 1; dr++)
                        if (df != 0 || dr != 0) Add(kg, file + df, rank + dr);
                KingTargets[sq] = kg.ToArray();

                var rays = new List<Ray>();
                for (int i = 0; i < 4; i++) rays.Add(MakeRay(file, rank, straight[i, 0], straight[i, 1], true));
                for (int i = 0; i < 4; i++) rays.Add(MakeRay(file, rank, diagonal[i, 0], diagonal[i, 1], false));
                Rays[sq] = rays.ToArray();
            }
        }

        static void Add(List<int> list, int file, int rank)
        {
            if (file >= 0 && file < 8 && rank >= 0 && rank < 8) list.Add(rank * 8 + file);
        }

        static Ray MakeRay(int file, int rank, int df, int dr, bool straight)
        {
            var squares = new List<int>();
            for (int f = file + df, r = rank + dr; f >= 0 && f < 8 && r >= 0 && r < 8; f += df, r += dr) squares.Add(r * 8 + f);
            return new Ray { Squares = squares.ToArray(), Straight = straight };
        }
    }
}
