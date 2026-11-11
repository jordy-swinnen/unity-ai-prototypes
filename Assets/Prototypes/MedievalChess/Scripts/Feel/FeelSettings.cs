using System;
using UnityEngine;

namespace Prototypes.MedievalChess
{
    /// <summary>Sound, effects, camera moods and the endings. Starting values, tuned on renders and in play.</summary>
    [Serializable]
    public class FeelSettings
    {
        [Header("Sound")]
        [Range(0f, 1f)] public float masterVolume = 0.8f;
        [Range(0f, 1f)] public float musicVolume = 0.35f;
        [Tooltip("The swing sounds this long before the blow lands.")]
        public float swingLead = 0.22f;
        [Tooltip("The victim's collapse starts this long after the blow.")]
        public float collapseDelay = 0.08f;

        [Header("Check")]
        public float checkShake = 0.06f;
        [Tooltip("How fast the red ring on a king in check pulses, per second.")]
        public float checkPulseSpeed = 1.6f;
        [Tooltip("How far the ring's size swings: 0.12 is 12 percent either way.")]
        [Range(0f, 0.5f)] public float checkPulseAmount = 0.12f;

        [Header("Checkmate")]
        [Range(0.05f, 1f)] public float slowMotionScale = 0.3f;
        [Tooltip("Real seconds the checkmate plays in slow motion.")]
        public float slowMotionSeconds = 1.2f;
        [Range(0f, 1f)] public float washAlpha = 0.03f;
        public float washSeconds = 1.4f;
        public Color washWin = new Color32(0xE3, 0xB3, 0x41, 0xFF);
        public Color washLose = new Color32(0x5A, 0x0C, 0x08, 0xFF);

        [Header("Camera moods (how far the camera moves toward what matters, 0 to 1; below 0 pulls back)")]
        public float startMood = -0.35f;
        public float mateMood = 0.45f;
        public float resignMood = 0.3f;
        public float drawMood = -0.2f;
        public float moodSeconds = 1.4f;

        [Header("Draw")]
        [Tooltip("The torches burn this bright in a draw: 1 is unchanged.")]
        [Range(0f, 1f)] public float drawTorchLevel = 0.45f;
        public float dimSeconds = 1.5f;

        [Header("Particles")]
        public int dustCount = 7;
        public int emberCount = 28;
        public int sparkCount = 10;
        public float dustLife = 0.8f;
        public float emberLife = 1.3f;
    }
}
