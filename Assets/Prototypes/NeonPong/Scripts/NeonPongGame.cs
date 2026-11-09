using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Prototypes.NeonPong
{
    /// <summary>
    /// Owns the rules. Reads input, steps the simulation every frame, and exposes the state for the
    /// view. The rules split long steps themselves, so a variable frame time is safe, and no
    /// project time setting needs to change.
    /// </summary>
    public class NeonPongGame : MonoBehaviour
    {
        public NeonPongSettings settings;
        [Tooltip("0 picks a new random seed each run.")]
        public int seed;
        [Tooltip("Turn off to step the rules by hand with Tick (tests do this).")]
        public bool autoStep = true;

        /// <summary>Where input comes from. The default reads the keyboard. Tests replace it.</summary>
        public Func<NeonPongInput> InputSource;

        public NeonPongSimulation Simulation { get; private set; }

        void Awake()
        {
            Simulation = new NeonPongSimulation(settings, seed != 0 ? seed : Environment.TickCount);
            InputSource ??= () => NeonPongKeyboard.Read(Keyboard.current);
        }

        void Update()
        {
            if (autoStep) Tick(Mathf.Min(Time.unscaledDeltaTime, 0.05f));
        }

        public void Tick(float dt)
        {
            Simulation.Step(dt, InputSource());
        }
    }
}
