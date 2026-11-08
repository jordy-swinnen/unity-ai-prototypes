using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Prototypes.Pong
{
    /// <summary>
    /// Runs the Pong rules in the scene. It reads the keyboard, steps the simulation on every
    /// physics tick and restarts the game when Space is pressed after a win. It draws nothing:
    /// <see cref="PongView"/> does that.
    /// </summary>
    public sealed class PongGame : MonoBehaviour
    {
        [SerializeField] PongSettings settings;
        [Tooltip("Seed for the serve. 0 picks a new one every run.")]
        [SerializeField] int seed;

        /// <summary>Replaces the keyboard. Tests use this to steer the paddles.</summary>
        public Func<PongInput> InputSource { get; set; }

        public PongSettings Settings => settings;
        public PongSimulation Simulation { get; private set; }

        void Awake()
        {
            Simulation = new PongSimulation(settings, seed != 0 ? seed : Environment.TickCount);
        }

        void FixedUpdate()
        {
            Simulation.Step(Time.fixedDeltaTime, InputSource != null ? InputSource() : ReadKeyboard());
        }

        void Update()
        {
            if (Simulation.State.Phase == PongPhase.GameOver && RestartPressed())
            {
                Simulation.Restart();
            }
        }

        // Player 1: W and S. Player 2: Up and Down. Up means toward the top of the screen (+z).
        static PongInput ReadKeyboard()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return default;
            }

            float player1 = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            float player2 = (keyboard.upArrowKey.isPressed ? 1f : 0f) - (keyboard.downArrowKey.isPressed ? 1f : 0f);
            return new PongInput(player1, player2);
        }

        static bool RestartPressed()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
        }
    }
}
