using System;

namespace Prototypes.MedievalChess
{
    /// <summary>
    /// How the computer judges a position, in centipawns (a hundredth of a pawn). Plain data, so tests can
    /// build it without a scene. The position tables are made from the numbers here, so they are original.
    /// </summary>
    [Serializable]
    public class EvaluationSettings
    {
        [UnityEngine.Header("Material")]
        public int pawn = 100;
        public int knight = 320;
        public int bishop = 330;
        public int rook = 500;
        public int queen = 900;
        [UnityEngine.Tooltip("The score of a checkmate. A mate in n moves scores a little less, so quicker mates win.")]
        public int mateScore = 100000;

        [UnityEngine.Header("Pieces")]
        public int bishopPair = 30;
        public int rookOnSeventh = 15;
        public int rookOnCentralFile = 4;
        [UnityEngine.Tooltip("A knight gains this for each step nearer the middle of the board.")]
        public int knightCentre = 14;
        public int bishopCentre = 5;
        public int queenCentre = 3;
        public int kingEndgameCentre = 16;
        [UnityEngine.Tooltip("A point for the side to move.")]
        public int tempo = 10;

        [UnityEngine.Header("Pawns")]
        public int doubledPawn = 10;
        public int isolatedPawn = 8;
        [UnityEngine.Tooltip("Bonus for advancing a pawn, by the rank it stands on (0 to 7, from its own side).")]
        public int[] pawnRankBonus = { 0, 0, 5, 12, 25, 45, 80, 0 };
        [UnityEngine.Tooltip("Extra bonus when no enemy pawn can stop it, by rank.")]
        public int[] passedPawnBonus = { 0, 0, 10, 20, 35, 60, 100, 0 };

        [UnityEngine.Header("The king in the middlegame, by file a to h")]
        public int[] kingBackRank = { 5, 25, 20, -10, -15, 5, 30, 15 };
        public int[] kingSecondRank = { -5, 0, -5, -20, -25, -10, -5, -5 };
        [UnityEngine.Tooltip("Ranks 3 to 8: the further from home the worse.")]
        public int[] kingAdvanced = { -40, -60, -80, -80, -80, -80 };

        [UnityEngine.Header("Search")]
        [UnityEngine.Tooltip("How many captures deep the search follows a line after its depth runs out.")]
        public int maxQuiescenceDepth = 8;
    }
}
