using UnityEngine;

namespace Prototypes.DioramaTurn
{
    /// <summary>Keeps a sprite pixel a whole number of screen pixels, so pixel art stays crisp.</summary>
    public static class PixelScale
    {
        /// <summary>The largest whole number of screen pixels per sprite pixel at which the room still fits (1 at the least).</summary>
        public static int Scale(float screenHeight, float pixelsPerUnit, float roomExtentUnits, int maxScale)
        {
            int fit = Mathf.FloorToInt(screenHeight / (pixelsPerUnit * roomExtentUnits));
            return Mathf.Clamp(fit, 1, Mathf.Max(1, maxScale));
        }

        /// <summary>Half the visible height in world units, for an orthographic camera.</summary>
        public static float OrthoSize(float screenHeight, float pixelsPerUnit, int scale)
        {
            return screenHeight / 2f / (pixelsPerUnit * scale);
        }
    }
}
