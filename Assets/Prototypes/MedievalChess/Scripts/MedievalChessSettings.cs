using UnityEngine;

namespace Prototypes.MedievalChess
{
    /// <summary>
    /// Every tuning value for Medieval Chess. More sections are added in the later stages
    /// (the rules, the computer, the look and the feel).
    /// </summary>
    [CreateAssetMenu(menuName = "Prototypes/Medieval Chess/Settings", fileName = "MedievalChessSettings")]
    public class MedievalChessSettings : ScriptableObject
    {
        [Header("Fights")]
        public FightSettings fight = new FightSettings();
        public MoveSettings moves = new MoveSettings();
        public AnimationSpeed animationSpeed = AnimationSpeed.Full;
        [Tooltip("0 turns off the shake, the hit-stop and the camera push. 1 is full.")]
        [Range(0f, 1f)] public float effectStrength = 1f;

        [Header("The computer")]
        public DifficultyProfile[] difficulties = DifficultyProfile.Defaults();
        public EvaluationSettings evaluation = new EvaluationSettings();

        [Header("Palette (from the spec)")]
        public Color boneColor = new Color32(0xE8, 0xDC, 0xC0, 0xFF);
        public Color cinnabarColor = new Color32(0xB8, 0x30, 0x1F, 0xFF);
        public Color ironColor = new Color32(0x5A, 0x5F, 0x63, 0xFF);
        public Color lightSquareColor = new Color32(0x9A, 0x9C, 0x94, 0xFF);
        public Color darkSquareColor = new Color32(0x5E, 0x66, 0x60, 0xFF);
        public Color floorColor = new Color32(0x2B, 0x27, 0x24, 0xFF);
        public Color keyLightColor = new Color32(0xFF, 0xD9, 0xA0, 0xFF);

        [Header("Duel scene (stage 1)")]
        [Tooltip("Degrees the camera is pitched down.")]
        public float duelCameraPitch = 32f;
        public float duelCameraDistance = 6.5f;
        public float duelCameraFieldOfView = 35f;
        [Tooltip("Seconds before the duel scene plays the first fight by itself.")]
        public float duelStartDelay = 1.2f;
    }
}
