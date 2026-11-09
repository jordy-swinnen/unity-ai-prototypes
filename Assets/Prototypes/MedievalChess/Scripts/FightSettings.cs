using System;

namespace Prototypes.MedievalChess
{
    /// <summary>How fast the fights play. Full is the real fight. Quick is faster with no camera cut. Off skips it.</summary>
    public enum AnimationSpeed
    {
        Full,
        Quick,
        Off,
    }

    /// <summary>
    /// Every number that shapes a fight. Plain data, so tests can build it without a scene.
    /// Times are in seconds, distances in metres, angles in degrees.
    /// </summary>
    [Serializable]
    public class FightSettings
    {
        [UnityEngine.Header("Timing")]
        public float approachTime = 0.35f;
        public float windUpPawn = 0.3f;
        public float windUpKnight = 0.5f;
        public float windUpBishop = 0.4f;
        public float windUpRook = 0.45f;
        public float windUpQueen = 0.35f;
        public float windUpKing = 0.55f;
        public float collapseTime = 0.6f;
        public float stepInTime = 0.3f;
        public float settleTime = 0.2f;
        [UnityEngine.Tooltip("Quick plays the fight this many times faster.")]
        public float quickFactor = 3f;

        [UnityEngine.Header("Positions")]
        [UnityEngine.Tooltip("How far from the victim the attacker stops to strike.")]
        public float standOff = 0.8f;
        [UnityEngine.Tooltip("How far the attacker lunges forward on the blow.")]
        public float lungeDistance = 0.25f;
        [UnityEngine.Tooltip("A knight's lance is long, so it stops this much further back and charges further.")]
        public float knightExtraStandOff = 0.3f;
        public float knightLunge = 0.5f;
        [UnityEngine.Tooltip("How far the blow knocks the victim back.")]
        public float pushBack = 0.3f;

        [UnityEngine.Header("Feel")]
        [UnityEngine.Tooltip("The rules and the fight freeze this long on the blow.")]
        public float hitStop = 0.07f;
        [UnityEngine.Range(0f, 1f)] public float cameraPush = 0.35f;
        [UnityEngine.Range(0f, 1f)] public float cameraLower = 0.25f;
        public float shakeStrength = 0.12f;
        public float shakeDuration = 0.3f;
    }
}
