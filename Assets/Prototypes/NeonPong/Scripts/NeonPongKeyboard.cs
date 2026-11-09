using UnityEngine.InputSystem;

namespace Prototypes.NeonPong
{
    /// <summary>
    /// Keyboard mapping through the Input System. Player 1: W and S. Player 2: Up and Down arrows.
    /// Space restarts after a win. Up (positive) means toward the far side of the table.
    /// </summary>
    public static class NeonPongKeyboard
    {
        public static NeonPongInput Read(Keyboard keyboard)
        {
            if (keyboard == null) return default;
            return new NeonPongInput
            {
                Left = Axis(keyboard.wKey.isPressed, keyboard.sKey.isPressed),
                Right = Axis(keyboard.upArrowKey.isPressed, keyboard.downArrowKey.isPressed),
                Restart = keyboard.spaceKey.wasPressedThisFrame,
            };
        }

        static float Axis(bool up, bool down) => (up ? 1f : 0f) - (down ? 1f : 0f);
    }
}
