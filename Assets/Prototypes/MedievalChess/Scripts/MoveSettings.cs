using System;

namespace Prototypes.MedievalChess
{
    /// <summary>Every number that shapes a move that is not a fight. Times in seconds, distances in metres.</summary>
    [Serializable]
    public class MoveSettings
    {
        [UnityEngine.Header("Walking")]
        [UnityEngine.Tooltip("Seconds for each square a piece walks. Each step ends with a thud.")]
        public float stepTime = 0.35f;
        [UnityEngine.Tooltip("A long walk is sped up so it never takes longer than this many seconds.")]
        public float maxWalkTime = 1.4f;
        [UnityEngine.Tooltip("Seconds a piece takes to turn toward where it goes, and to turn back at the end.")]
        public float turnTime = 0.15f;
        [UnityEngine.Tooltip("How high the body rises on each step.")]
        public float bobHeight = 0.04f;

        [UnityEngine.Header("Knight and rook")]
        public float leapTime = 0.55f;
        public float leapHeight = 0.5f;
        [UnityEngine.Tooltip("A rook slides: this long for a short move, plus the extra per square.")]
        public float slideBase = 0.3f;
        public float slidePerSquare = 0.08f;

        [UnityEngine.Header("Castling")]
        [UnityEngine.Tooltip("The rook starts its hop this long after the king starts to move.")]
        public float castleRookDelay = 0.15f;
        public float castleHopHeight = 0.35f;

        [UnityEngine.Header("Promotion")]
        public float kneelTime = 0.4f;
        public float riseTime = 0.45f;

        [UnityEngine.Header("Check, checkmate and resigning")]
        public float braceTime = 0.5f;
        public float fallTime = 0.9f;
        public float resignTime = 1f;
    }
}
