namespace Prototypes.MedievalChess
{
    /// <summary>Random numbers used to give each position a number (a hash) that can be updated move by move.</summary>
    static class Zobrist
    {
        public static readonly ulong[,] Piece = new ulong[13, 64];
        public static readonly ulong[] Castling = new ulong[16];
        public static readonly ulong[] EnPassantFile = new ulong[8];
        public static readonly ulong SideToMove;

        static Zobrist()
        {
            // SplitMix64 from a fixed seed, so the numbers (and so the hashes) are the same on every run.
            ulong state = 0x9E3779B97F4A7C15UL;
            ulong Next()
            {
                state += 0x9E3779B97F4A7C15UL;
                ulong z = state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
            for (int p = 1; p < 13; p++)
                for (int s = 0; s < 64; s++) Piece[p, s] = Next();
            for (int i = 0; i < 16; i++) Castling[i] = Next();
            for (int i = 0; i < 8; i++) EnPassantFile[i] = Next();
            SideToMove = Next();
        }
    }
}
