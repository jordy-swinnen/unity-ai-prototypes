using System;

namespace Prototypes.MedievalChess
{
    /// <summary>
    /// One of the three levels of the computer. The computer plays by the same rules as the player and sees
    /// only the position, and what differs between levels is how far it looks, how careful it is, and how long it pauses.
    /// </summary>
    [Serializable]
    public class DifficultyProfile
    {
        public string name = "Squire";
        [UnityEngine.Tooltip("How many half-moves (plies) ahead it looks, at most. The time cap may stop it sooner.")]
        public int depth = 1;
        [UnityEngine.Tooltip("Follow captures to the end of a line, so it does not stop in the middle of an exchange.")]
        public bool quiescence;
        [UnityEngine.Tooltip("It picks at random among moves within this many centipawns of its best. 0 means always the best.")]
        public int slackCentipawns;
        [UnityEngine.Tooltip("One move in this many is any legal move at all. 0 means never.")]
        public int blunderOneIn;
        [UnityEngine.Tooltip("It always pauses at least this long, so its move is easy to follow.")]
        public float minThinkSeconds = 0.8f;
        [UnityEngine.Tooltip("It never thinks longer than this.")]
        public float timeCapSeconds = 0.5f;
        [UnityEngine.Tooltip("The transposition table has 2 to the power of this many entries. 0 means none.")]
        public int tableLog2;

        public DifficultyProfile Clone() => (DifficultyProfile)MemberwiseClone();

        public static DifficultyProfile[] Defaults() => new[]
        {
            new DifficultyProfile { name = "Squire", depth = 1, quiescence = false, slackCentipawns = 120, blunderOneIn = 7, minThinkSeconds = 0.8f, timeCapSeconds = 0.5f, tableLog2 = 0 },
            new DifficultyProfile { name = "Knight", depth = 3, quiescence = true, slackCentipawns = 10, blunderOneIn = 0, minThinkSeconds = 0.5f, timeCapSeconds = 1.5f, tableLog2 = 16 },
            new DifficultyProfile { name = "Marshal", depth = 8, quiescence = true, slackCentipawns = 0, blunderOneIn = 0, minThinkSeconds = 0.3f, timeCapSeconds = 3f, tableLog2 = 18 },
        };
    }
}
