using System;
using UnityEngine;

namespace Prototypes.MedievalChess
{
    /// <summary>The hall's light, the torches and the post-processing. Starting values, tuned on renders.</summary>
    [Serializable]
    public class LookSettings
    {
        [Header("Key light (a cool fill that casts the shadows, so the squares and sides keep their colour)")]
        public float keyLightIntensity = 1f;
        public Color keyLightColor = new Color(0.82f, 0.85f, 0.93f);

        [Header("Torches")]
        public Color torchColor = new Color32(0xFF, 0x9A, 0x3C, 0xFF);
        [Tooltip("The torches stand this far from the middle of the board, left and right.")]
        public float torchX = 6.2f;
        [Tooltip("The torches stand this far from the middle of the board, near and far.")]
        public float torchZ = 5.6f;
        public float torchHeight = 1.7f;
        public float torchLightIntensity = 38f;
        public float torchLightRange = 17f;
        [Tooltip("How far a flame's light wanders from its base level (0.2 is 20 percent).")]
        [Range(0f, 0.6f)] public float flickerAmplitude = 0.18f;
        [Tooltip("Roughly how many flickers a second.")]
        public float flickerSpeed = 7f;
        [Tooltip("The flame's glow, above 1 so that Bloom picks it up.")]
        public float flameEmission = 4f;

        [Header("Post-processing")]
        public float bloomThreshold = 1f;
        [Range(0f, 1f)] public float bloomIntensity = 0.5f;
        [Range(0f, 1f)] public float bloomScatter = 0.6f;
        [Range(0f, 1f)] public float vignetteIntensity = 0.38f;
        [Range(0.01f, 1f)] public float vignetteSmoothness = 0.45f;
        public float postExposure = 0f;
        public float contrast = 10f;
        public float saturation = -6f;
        [Tooltip("0 Neutral, 1 ACES.")]
        public int tonemapping = 1;

        [Header("The hall")]
        public Color ambientColor = new Color32(0x28, 0x2A, 0x32, 0xFF);
        public Color fogColor = new Color32(0x0F, 0x0C, 0x0A, 0xFF);
        [Range(0f, 0.2f)] public float fogDensity = 0.025f;
    }
}
