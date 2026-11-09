using Shared.Feel;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Prototypes.MedievalChess
{
    /// <summary>
    /// The duel scene: one piece kills another on a bare board, so the fight can be judged before any
    /// chess exists. Keys: Space replays, 1 and 2 pick who kills whom, F, Q and O pick the speed
    /// (Full, Quick, Off). The fight is a plan, and this only plays it and moves the camera.
    /// </summary>
    public class DuelController : MonoBehaviour
    {
        public MedievalChessSettings settings;
        [Tooltip("One model per piece type, in the order of the PieceType enum.")]
        public GameObject[] piecePrefabs;
        public Material bone;
        public Material cinnabar;
        public Material iron;
        public Camera cam;
        public Text label;
        [Tooltip("Turn off to drive the duel by hand with Step and ShowAt (tests and captures do this).")]
        public bool autoStep = true;
        [Tooltip("The speed in use. It starts from the settings asset and the keys change it here, not in the asset.")]
        public AnimationSpeed speed;

        public PieceType attackerType = PieceType.Rook;
        public PieceType victimType = PieceType.Pawn;
        public Vector3 attackerStart = new Vector3(-1.5f, 0f, 0f);
        public Vector3 victimPosition = new Vector3(1f, 0f, 0f);

        readonly ShakeState shake = new ShakeState();
        readonly FightFrame frame = new FightFrame();
        PieceActor attacker;
        PieceActor victim;
        Vector3 cameraBase;
        Vector3 focus;
        float cameraPush;
        float startTimer;
        bool started;

        public FightPlan Plan { get; private set; }
        public FightTimeline Timeline { get; private set; }
        public PieceActor Attacker => attacker;
        public PieceActor Victim => victim;
        public bool Playing => Timeline != null && !Timeline.Finished;
        public bool Started => started;

        void Start()
        {
            speed = settings.animationSpeed;
            cameraBase = cam.transform.position;
            focus = cam.transform.position + cam.transform.forward * settings.duelCameraDistance;
            SetUp();
            startTimer = settings.duelStartDelay;
        }

        void Update()
        {
            HandleKeys();
            if (!autoStep) return;

            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (!started)
            {
                startTimer -= dt;
                if (startTimer <= 0f) Play();
            }
            Step(dt);
        }

        // --- setting up and playing ---

        /// <summary>Puts the two pieces at their squares, ready for a fight.</summary>
        public void SetUp()
        {
            if (attacker != null) Destroy(attacker.gameObject);
            if (victim != null) Destroy(victim.gameObject);
            attacker = Spawn(attackerType, bone, attackerStart, 90f);
            victim = Spawn(victimType, cinnabar, victimPosition, -90f);
            Plan = null;
            Timeline = null;
            cameraPush = 0f;
            UpdateLabel();
        }

        PieceActor Spawn(PieceType type, Material side, Vector3 position, float yaw)
        {
            var instance = Instantiate(piecePrefabs[(int)type], position, Quaternion.identity);
            instance.name = type.ToString();
            var actor = PieceActor.Bind(instance, type, side, iron);
            actor.Place(position, yaw);
            return actor;
        }

        /// <summary>Sets up the pieces again and plays the fight from the start.</summary>
        public void Play()
        {
            started = true;
            SetUp();
            Plan = FightLibrary.Create(attackerType, victimType, attackerStart, victimPosition, 90f, -90f, settings.fight);
            Timeline = new FightTimeline(Plan, speed, settings.fight, settings.effectStrength);
            ShowAt(0f);
            if (speed == AnimationSpeed.Off) Step(0f); // an Off fight is over at once
        }

        /// <summary>Moves the fight and the camera on by a real frame time.</summary>
        public void Step(float dt)
        {
            if (Timeline != null)
            {
                Timeline.Advance(dt);
                ShowAt(Timeline.PlanTime);
                cameraPush = Timeline.CameraPush;
                if (Timeline.BlowThisFrame && Timeline.BlowShake > 0f)
                    shake.Add(Timeline.BlowShake, Vector2.up, settings.fight.shakeDuration);
                if (Timeline.Finished) UpdateLabel();
            }
            UpdateCamera(dt);
        }

        /// <summary>Shows the fight at an exact moment, without playing it. Captures use this.</summary>
        public void ShowAt(float planTime)
        {
            if (Plan == null) return;
            Plan.Evaluate(planTime, frame);
            attacker.Apply(frame.Attacker);
            victim.Apply(frame.Victim);
            cameraPush = frame.CameraPush * (speed == AnimationSpeed.Full ? settings.effectStrength : 0f);
        }

        // --- the camera ---

        void UpdateCamera(float dt)
        {
            shake.Update(dt);
            var f = settings.fight;
            // Push toward the focus and lower a little, keep looking at the focus, then add the shake.
            var toFocus = focus - cameraBase;
            var position = cameraBase + toFocus * (f.cameraPush * cameraPush);
            position.y -= cameraBase.y * f.cameraLower * cameraPush;
            cam.transform.position = position;
            cam.transform.rotation = Quaternion.LookRotation(focus - position);
            cam.transform.position = position + cam.transform.right * shake.Offset.x + cam.transform.up * shake.Offset.y;
        }

        /// <summary>Applies the camera for the current push and shake. Tests and captures call it after ShowAt.</summary>
        public void ApplyCamera() => UpdateCamera(0f);

        public Vector3 CameraPosition => cam.transform.position;
        public Vector3 CameraBasePosition => cameraBase;
        public float CameraPush => cameraPush;

        // --- keys ---

        void HandleKeys()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.digit1Key.wasPressedThisFrame) Choose(PieceType.Rook, PieceType.Pawn);
            if (kb.digit2Key.wasPressedThisFrame) Choose(PieceType.Pawn, PieceType.Rook);
            if (kb.fKey.wasPressedThisFrame) speed = AnimationSpeed.Full;
            if (kb.qKey.wasPressedThisFrame) speed = AnimationSpeed.Quick;
            if (kb.oKey.wasPressedThisFrame) speed = AnimationSpeed.Off;
            if (kb.spaceKey.wasPressedThisFrame || kb.digit1Key.wasPressedThisFrame || kb.digit2Key.wasPressedThisFrame) Play();
        }

        void Choose(PieceType attackerKind, PieceType victimKind)
        {
            attackerType = attackerKind;
            victimType = victimKind;
        }

        void UpdateLabel()
        {
            if (label == null) return;
            label.text = $"{attackerType} kills {victimType}    speed: {speed}\n" +
                         "Space replay   1 rook kills pawn   2 pawn kills rook   F full   Q quick   O off";
        }
    }
}
