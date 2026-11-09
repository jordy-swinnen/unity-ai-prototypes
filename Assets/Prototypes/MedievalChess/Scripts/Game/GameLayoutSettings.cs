using System;
using UnityEngine;

namespace Prototypes.MedievalChess
{
    /// <summary>The camera, the trays and the highlight rings of the game. Starting values, tuned on renders.</summary>
    [Serializable]
    public class GameLayoutSettings
    {
        [Header("Camera")]
        [Tooltip("Degrees the camera is pitched down.")]
        public float cameraPitch = 50f;
        public float cameraFieldOfView = 35f;
        [Tooltip("Metres from the point the camera looks at.")]
        public float cameraDistance = 15f;
        [Tooltip("The height of the point the camera looks at.")]
        public float cameraFocusHeight = 0.3f;

        [Header("Captured pieces")]
        [Tooltip("The trays stand this far left and right of the middle of the board.")]
        public float trayX = 6f;
        [Tooltip("A captured piece stands at this fraction of its size.")]
        public float trayScale = 0.6f;

        [Header("Highlights")]
        public float ringSize = 0.88f;
        public float ringHeight = 0.03f;
        public Color selectionColor = new Color32(0xE3, 0xB3, 0x41, 0xFF);
        public Color moveColor = new Color32(0xE3, 0xB3, 0x41, 0xFF);
        public Color captureColor = new Color32(0xE0, 0x53, 0x3C, 0xFF);
        public Color lastMoveColor = new Color32(0x8A, 0x6B, 0x1F, 0xFF);
        public Color checkColor = new Color32(0xFF, 0x3B, 0x30, 0xFF);
        public Color hoverColor = new Color32(0xF1, 0xD5, 0x8A, 0xFF);

        [Header("Menu")]
        [Tooltip("The difficulty chosen when the menu opens: 0 Squire, 1 Knight, 2 Marshal.")]
        public int startingDifficulty = 1;
        public Color panelColor = new Color32(0x0F, 0x0C, 0x0A, 0xE6);
        public Color buttonColor = new Color32(0x3A, 0x2E, 0x24, 0xFF);
        public Color buttonSelectedColor = new Color32(0x8A, 0x6B, 0x1F, 0xFF);
        public Color textColor = new Color32(0xE9, 0xDF, 0xC5, 0xFF);
    }
}
