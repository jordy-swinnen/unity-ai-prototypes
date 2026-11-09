using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Prototypes.NeonPong.Tests
{
    /// <summary>
    /// Checks the key mapping with a simulated keyboard. A real keyboard is still a person's check.
    /// </summary>
    public class NeonPongKeyboardTests
    {
        Keyboard keyboard;

        [SetUp]
        public void SetUp()
        {
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        [TearDown]
        public void TearDown()
        {
            InputSystem.RemoveDevice(keyboard);
        }

        NeonPongInput Press(params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            InputSystem.Update();
            return NeonPongKeyboard.Read(keyboard);
        }

        [Test]
        public void Nothing_pressed_gives_zero_for_both_players()
        {
            var input = Press();
            Assert.AreEqual(0f, input.Left);
            Assert.AreEqual(0f, input.Right);
            Assert.IsFalse(input.Restart);
        }

        [Test]
        public void W_and_S_move_player_one()
        {
            Assert.AreEqual(1f, Press(Key.W).Left);
            Assert.AreEqual(-1f, Press(Key.S).Left);
            Assert.AreEqual(0f, Press(Key.W).Right);
        }

        [Test]
        public void Up_and_Down_move_player_two()
        {
            Assert.AreEqual(1f, Press(Key.UpArrow).Right);
            Assert.AreEqual(-1f, Press(Key.DownArrow).Right);
            Assert.AreEqual(0f, Press(Key.UpArrow).Left);
        }

        [Test]
        public void Opposite_keys_cancel_out()
        {
            Assert.AreEqual(0f, Press(Key.W, Key.S).Left);
            Assert.AreEqual(0f, Press(Key.UpArrow, Key.DownArrow).Right);
        }

        [Test]
        public void Both_players_can_move_at_the_same_time()
        {
            var input = Press(Key.W, Key.DownArrow);
            Assert.AreEqual(1f, input.Left);
            Assert.AreEqual(-1f, input.Right);
        }

        // Space uses wasPressedThisFrame, which only updates in Play Mode.
        // It is tested in PlayMode (NeonPongPlayTests).

        [Test]
        public void A_missing_keyboard_gives_no_input()
        {
            var input = NeonPongKeyboard.Read(null);
            Assert.AreEqual(0f, input.Left);
            Assert.AreEqual(0f, input.Right);
            Assert.IsFalse(input.Restart);
        }
    }
}
