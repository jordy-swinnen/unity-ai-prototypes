using System.Collections.Generic;
using Shared.Feel;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Prototypes.MedievalChess
{
    /// <summary>
    /// The duel scene: pieces fight and move on a bare board so the animations can be judged before
    /// any chess exists. The default is a duel (one piece kills another). Keys: Space replays,
    /// 1 to 6 pick the attacker (pawn, knight, bishop, rook, queen, king), Q W E R T Y pick the victim,
    /// M plays the next demo (walk, leap, slide, en passant, castling, promotion, check, mate, resign),
    /// F1, F2 and F3 pick the speed. Everything is a plan played by a timeline, and this only shows it.
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
        readonly Dictionary<string, PieceActor> actors = new Dictionary<string, PieceActor>();
        ActorFrame[] frames = new ActorFrame[0];
        Vector3 cameraBase;
        Vector3 focus;
        float cameraPush;
        float startTimer;
        bool started;
        int shownStep = -1;
        int demoIndex = -1;
        string title = string.Empty;

        /// <summary>Everything that will be played.</summary>
        public ActionSequence Sequence { get; private set; }
        /// <summary>The duel's fight, or null while a demo is set up.</summary>
        public FightPlan Plan { get; private set; }
        public SequenceTimeline Timeline { get; private set; }
        public PieceActor Attacker => Actor("attacker");
        public PieceActor Victim => Actor("victim");
        public bool Playing => Timeline != null && !Timeline.Finished;
        public bool Started => started;
        public int DemoIndex => demoIndex;

        public PieceActor Actor(string id) => actors.TryGetValue(id, out var a) ? a : null;

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

        /// <summary>Sets up the duel: the two pieces at their squares, ready for a fight.</summary>
        public void SetUp()
        {
            demoIndex = -1;
            Plan = FightLibrary.Create(attackerType, victimType, attackerStart, victimPosition, 90f, -90f, settings.fight);
            title = $"{attackerType} kills {victimType}";
            Prepare(ActionSequence.Single(Plan));
        }

        /// <summary>Sets up a demo move (see <see cref="DuelDemos"/>), ready to play.</summary>
        public void SetUpDemo(int index)
        {
            demoIndex = ((index % DuelDemos.Count) + DuelDemos.Count) % DuelDemos.Count;
            Plan = null;
            title = DuelDemos.Names[demoIndex];
            Prepare(DuelDemos.Create(demoIndex, settings));
        }

        /// <summary>Plays a demo move from the start.</summary>
        public void PlayDemo(int index)
        {
            demoIndex = index;
            Play();
        }

        /// <summary>Plays the current duel or demo from the start.</summary>
        public void Play()
        {
            started = true;
            if (demoIndex >= 0) SetUpDemo(demoIndex);
            else SetUp();

            Timeline = new SequenceTimeline(Sequence, speed, settings.fight, settings.effectStrength);
            if (speed == AnimationSpeed.Off)
            {
                // Off skips the move: every step is shown at its end, in order.
                for (int i = 0; i < Sequence.Steps.Count; i++) ShowStep(i, Sequence.Steps[i].Duration);
                cameraPush = 0f;
            }
            else
            {
                ShowStep(0, 0f);
            }
            UpdateLabel();
        }

        void Prepare(ActionSequence sequence)
        {
            foreach (var a in actors.Values) if (a != null) Destroy(a.gameObject);
            actors.Clear();
            Sequence = sequence;
            Timeline = null;
            shownStep = -1;
            cameraPush = 0f;

            // A piece is created the first time it appears in a step, and is hidden if it is not there yet.
            var frame = new ActorFrame[8];
            for (int i = 0; i < frame.Length; i++) frame[i] = new ActorFrame();
            foreach (var step in sequence.Steps)
            {
                step.Evaluate(0f, frame);
                for (int i = 0; i < step.Actors.Count; i++)
                {
                    var spec = step.Actors[i];
                    if (actors.ContainsKey(spec.Id)) continue;
                    var actor = Spawn(spec);
                    actors[spec.Id] = actor;
                    actor.gameObject.SetActive(frame[i].Visible);
                }
            }
            UpdateLabel();
        }

        PieceActor Spawn(ActorSpec spec)
        {
            var instance = Instantiate(piecePrefabs[(int)spec.Type], spec.Start, Quaternion.identity);
            instance.name = spec.Type.ToString();
            var actor = PieceActor.Bind(instance, spec.Type, spec.Side == 0 ? bone : cinnabar, iron);
            actor.Place(spec.Start, spec.StartYaw);
            return actor;
        }

        /// <summary>Moves the plan and the camera on by a real frame time.</summary>
        public void Step(float dt)
        {
            if (Timeline != null)
            {
                int before = Timeline.StepIndex;
                Timeline.Advance(dt);
                if (Timeline.StepIndex != before) ShowStep(before, Sequence.Steps[before].Duration);
                ShowStep(Timeline.StepIndex, Timeline.PlanTime);
                cameraPush = Timeline.CameraPush;
                if (Timeline.BlowThisFrame && Timeline.BlowShake > 0f)
                    shake.Add(Timeline.BlowShake, Vector2.up, settings.fight.shakeDuration);
                if (Timeline.Finished) UpdateLabel();
            }
            UpdateCamera(dt);
        }

        /// <summary>Shows the first step at an exact moment, without playing. Captures use this on a duel.</summary>
        public void ShowAt(float planTime)
        {
            if (Sequence == null) return;
            ShowStep(0, planTime);
            cameraPush = Sequence.Steps[0].CameraPush(planTime) * (speed == AnimationSpeed.Full ? settings.effectStrength : 0f);
        }

        /// <summary>Shows one step of the sequence at an exact moment.</summary>
        public void ShowStep(int index, float planTime)
        {
            var plan = Sequence.Steps[index];
            if (frames.Length < plan.Actors.Count)
            {
                frames = new ActorFrame[plan.Actors.Count];
                for (int i = 0; i < frames.Length; i++) frames[i] = new ActorFrame();
            }
            if (shownStep != index)
            {
                shownStep = index;
                foreach (var spec in plan.Actors) actors[spec.Id].Place(spec.Start, spec.StartYaw);
            }
            plan.Evaluate(planTime, frames);
            for (int i = 0; i < plan.Actors.Count; i++) actors[plan.Actors[i].Id].Apply(frames[i]);
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

        static readonly Key[] AttackerKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6 };
        static readonly Key[] VictimKeys = { Key.Q, Key.W, Key.E, Key.R, Key.T, Key.Y };

        void HandleKeys()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            for (int i = 0; i < 6; i++)
            {
                if (kb[AttackerKeys[i]].wasPressedThisFrame) { attackerType = (PieceType)i; demoIndex = -1; Play(); }
                if (kb[VictimKeys[i]].wasPressedThisFrame) { victimType = (PieceType)i; demoIndex = -1; Play(); }
            }
            if (kb.f1Key.wasPressedThisFrame) speed = AnimationSpeed.Full;
            if (kb.f2Key.wasPressedThisFrame) speed = AnimationSpeed.Quick;
            if (kb.f3Key.wasPressedThisFrame) speed = AnimationSpeed.Off;
            if (kb.mKey.wasPressedThisFrame) { demoIndex = demoIndex + 1; Play(); }
            if (kb.dKey.wasPressedThisFrame) { demoIndex = -1; Play(); }
            if (kb.spaceKey.wasPressedThisFrame) Play();
        }

        void UpdateLabel()
        {
            if (label == null) return;
            label.text = $"{title}    speed: {speed}\n" +
                         "Space replay   1-6 attacker   Q-Y victim   M next demo   D duel   F1 full   F2 quick   F3 off";
        }
    }
}
