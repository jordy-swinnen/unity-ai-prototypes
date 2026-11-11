using Shared.Feel;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Prototypes.PixelInvaders
{
    /// <summary>
    /// Top-level runtime MonoBehaviour for PixelInvaders. Reads Input System keyboard/gamepad,
    /// steps PixelInvadersSimulation with TimeFeel hit-stop/slow-mo and ShakeState camera shake,
    /// and updates PixelInvadersView and PixelInvadersAudio.
    /// </summary>
    public sealed class PixelInvadersGame : MonoBehaviour
    {
        public PixelInvadersSettings settings;
        public PixelInvadersView view;
        public PixelInvadersAudio audioDirector;
        public bool autoStep = true;
        public int randomSeed = 1337;

        readonly ShakeState shake = new ShakeState();
        readonly TimeFeel timeFeel = new TimeFeel();
        PixelInvadersSimulation simulation;
        bool hasInjectedInput;
        PixelInvadersInput injectedInput;

        public PixelInvadersSimulation Simulation
        {
            get
            {
                EnsureInitialized();
                return simulation;
            }
        }

        public ShakeState Shake => shake;
        public TimeFeel TimeFeel => timeFeel;

        void Awake()
        {
            EnsureInitialized();
            if (view != null) view.Sync(simulation, 0f, Vector2.zero);
            if (audioDirector != null) audioDirector.SyncSettings();
        }

        void Update()
        {
            if (!autoStep) return;
            StepFrame(Time.deltaTime, ReadHardwareInput());
        }

        public void EnsureInitialized()
        {
            if (simulation != null) return;
            if (settings == null) settings = ScriptableObject.CreateInstance<PixelInvadersSettings>();
            settings.Validate();
            simulation = new PixelInvadersSimulation(settings, randomSeed);
        }

        public void InjectInputForTest(PixelInvadersInput input)
        {
            hasInjectedInput = true;
            injectedInput = input;
        }

        public void StepFrame(float realDt, PixelInvadersInput? manualInput = null)
        {
            EnsureInitialized();
            PixelInvadersInput input;
            if (manualInput.HasValue)
            {
                input = manualInput.Value;
            }
            else if (hasInjectedInput)
            {
                input = injectedInput;
                hasInjectedInput = false;
            }
            else
            {
                input = default;
            }

            timeFeel.UpdateSlow(realDt);
            float simDt = timeFeel.Advance(realDt) * timeFeel.VisualScale;
            if (simDt > 0f)
            {
                simulation.Step(simDt, input);
                ProcessEvents();
            }

            shake.Update(realDt);
            Vector2 shakeOffset = settings.feel.effectStrength > 0f ? shake.Offset : Vector2.zero;

            if (view != null) view.Sync(simulation, realDt * timeFeel.VisualScale, shakeOffset);
            if (audioDirector != null) audioDirector.Tick(realDt, simulation.ActiveUfo.Active);
        }

        void ProcessEvents()
        {
            var events = simulation.Events;
            for (int i = 0; i < events.Count; i++)
            {
                var ev = events[i];
                ApplyFeel(ev);
                if (view != null) view.HandleEvent(ev);
                if (audioDirector != null) audioDirector.HandleEvent(ev);
            }
        }

        void ApplyFeel(GameEvent ev)
        {
            if (settings == null || settings.feel.effectStrength <= 0f)
            {
                timeFeel.Clear();
                return;
            }

            switch (ev.Kind)
            {
                case GameEventKind.ShotFired:
                    shake.Add(settings.EffectiveShake(settings.feel.shakeFire), Vector2.down, 0.08f);
                    break;
                case GameEventKind.FormationStepped:
                    if (ev.Flag)
                    {
                        shake.Add(settings.EffectiveShake(0.03f), Vector2.down, 0.12f);
                    }
                    break;
                case GameEventKind.InvaderKilled:
                    shake.Add(settings.EffectiveShake(settings.feel.shakeAlienKill), Vector2.up, 0.14f);
                    timeFeel.HitStop(settings.EffectiveHitStop(settings.feel.alienHitStop));
                    break;
                case GameEventKind.BunkerEroded:
                    shake.Add(settings.EffectiveShake(settings.feel.shakeBunkerHit), Vector2.down, 0.10f);
                    break;
                case GameEventKind.UfoDestroyed:
                    shake.Add(settings.EffectiveShake(settings.feel.shakeUfoKill), Vector2.up, 0.22f);
                    timeFeel.HitStop(settings.EffectiveHitStop(settings.feel.ufoHitStop));
                    break;
                case GameEventKind.CannonDestroyed:
                    shake.Add(settings.EffectiveShake(settings.feel.shakeDeath), Vector2.down, 0.28f);
                    timeFeel.HitStop(settings.EffectiveHitStop(settings.feel.deathHitStop));
                    break;
                case GameEventKind.WaveCleared:
                    timeFeel.SlowMotion(settings.feel.waveClearSlowMoScale, settings.EffectiveSlowMoDuration());
                    break;
            }
        }

        static PixelInvadersInput ReadHardwareInput()
        {
            var input = new PixelInvadersInput();
            var kb = Keyboard.current;
            if (kb != null)
            {
                float left = (kb.aKey.isPressed || kb.leftArrowKey.isPressed) ? -1f : 0f;
                float right = (kb.dKey.isPressed || kb.rightArrowKey.isPressed) ? 1f : 0f;
                input.moveAxis = Mathf.Clamp(left + right, -1f, 1f);
                input.firePressed = kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame;
                input.restartPressed = kb.spaceKey.wasPressedThisFrame || kb.rKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame;
            }

            var pad = Gamepad.current;
            if (pad != null)
            {
                float stick = pad.leftStick.x.ReadValue();
                float dpad = pad.dpad.x.ReadValue();
                if (Mathf.Abs(stick) > 0.15f) input.moveAxis = Mathf.Clamp(stick, -1f, 1f);
                else if (Mathf.Abs(dpad) > 0.15f) input.moveAxis = Mathf.Clamp(dpad, -1f, 1f);
                input.firePressed |= pad.buttonSouth.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame;
                input.restartPressed |= pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame;
            }
            return input;
        }
    }
}
