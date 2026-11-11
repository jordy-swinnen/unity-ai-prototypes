using UnityEngine;
using UnityEngine.InputSystem;

namespace Prototypes.DioramaTurn
{
    /// <summary>Maps mouse, keyboard and gamepad to the game's commands. Tests drive the game directly.</summary>
    public sealed class DioramaInput : MonoBehaviour
    {
        public DioramaTurnGame game;

        Vector2 pressPoint;
        bool pressed;
        bool dragged;
        float stickCooldown;

        void Update()
        {
            if (game == null) return;
            KeyboardInput();
            MouseInput();
            GamepadInput();
        }

        void KeyboardInput()
        {
            var k = Keyboard.current;
            if (k == null) return;
            if (k.qKey.wasPressedThisFrame) game.PressTurn(-1);
            if (k.eKey.wasPressedThisFrame) game.PressTurn(1);
            bool shift = k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;
            if (k.leftArrowKey.wasPressedThisFrame)
            {
                if (shift) game.PressTurn(-1);
                else game.MoveCursor(ScreenDirection.Left);
            }
            if (k.rightArrowKey.wasPressedThisFrame)
            {
                if (shift) game.PressTurn(1);
                else game.MoveCursor(ScreenDirection.Right);
            }
            if (k.upArrowKey.wasPressedThisFrame) game.MoveCursor(ScreenDirection.Up);
            if (k.downArrowKey.wasPressedThisFrame) game.MoveCursor(ScreenDirection.Down);
            if (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame) game.Confirm();
            if (k.rKey.wasPressedThisFrame) game.Restart();
        }

        void MouseInput()
        {
            var m = Mouse.current;
            if (m == null) return;
            if (m.leftButton.wasPressedThisFrame)
            {
                pressPoint = m.position.ReadValue();
                pressed = true;
                dragged = false;
            }
            if (pressed && m.leftButton.isPressed && !dragged)
            {
                if (Mathf.Abs(m.position.ReadValue().x - pressPoint.x) > 60f)
                {
                    game.PressTurn(m.position.ReadValue().x > pressPoint.x ? 1 : -1);
                    dragged = true;
                }
            }
            if (pressed && m.leftButton.wasReleasedThisFrame)
            {
                pressed = false;
                if (dragged) return;
                if (game.Flow == GameFlow.Title)
                {
                    game.Confirm();
                    return;
                }
                var node = game.NodeNearScreenPoint(m.position.ReadValue());
                if (node.HasValue) game.WalkTo(node.Value);
            }
        }

        void GamepadInput()
        {
            var g = Gamepad.current;
            if (g == null) return;
            if (g.leftShoulder.wasPressedThisFrame) game.PressTurn(-1);
            if (g.rightShoulder.wasPressedThisFrame) game.PressTurn(1);
            if (g.buttonSouth.wasPressedThisFrame) game.Confirm();
            if (g.buttonNorth.wasPressedThisFrame) game.Restart();

            stickCooldown -= Time.deltaTime;
            var stick = g.leftStick.ReadValue();
            if (stickCooldown <= 0f && stick.magnitude > 0.6f)
            {
                if (Mathf.Abs(stick.x) > Mathf.Abs(stick.y))
                    game.MoveCursor(stick.x > 0f ? ScreenDirection.Right : ScreenDirection.Left);
                else
                    game.MoveCursor(stick.y > 0f ? ScreenDirection.Up : ScreenDirection.Down);
                stickCooldown = 0.18f;
            }
        }
    }
}
