using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Prototypes.MedievalChess
{
    public enum GamePhase
    {
        Menu,
        PlayerTurn,
        ComputerThinking,
        Animating,
        ChoosingPromotion,
        GameOver,
    }

    public enum SideChoice
    {
        White,
        Black,
        Random,
    }

    /// <summary>
    /// Runs a game: the menu, the player's turn (select a piece, pick a square, choose a promotion), the computer's
    /// turn on another thread, the animation of each move, and the end. The rules decide everything, and the pieces
    /// on the board only show it. The state machine is Menu, PlayerTurn, ComputerThinking, Animating, ChoosingPromotion
    /// and GameOver.
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        public MedievalChessSettings settings;
        [Tooltip("One model per piece type, in the order of the PieceType enum.")]
        public GameObject[] piecePrefabs;
        public Material bone, cinnabar, iron;
        public UiView ui;
        public HighlightView highlights;
        public GameCameraRig cameraRig;
        public SoundBank sound;
        public EffectsView effects;
        [Tooltip("The torches, so a draw can dim them.")]
        public TorchFlicker[] torches;
        [Tooltip("Turn off to drive the game by hand with Step (tests and captures do this).")]
        public bool autoStep = true;
        [Tooltip("0 picks a new random seed each run.")]
        public int seed;
        [Tooltip("Tests use this to make the computer quick. Leave empty to use the chosen difficulty.")]
        public DifficultyProfile profileOverride;
        public AnimationSpeed speed;

        readonly Dictionary<string, PieceActor> actors = new Dictionary<string, PieceActor>();
        readonly List<PieceActor> tray = new List<PieceActor>();
        readonly int[] trayCount = new int[2];
        readonly List<Move> selectedMoves = new List<Move>();
        ComputerPlayer computer;
        SequenceStage stage;
        CancellationTokenSource cancel = new CancellationTokenSource();
        Task<ComputerChoice> thinking;
        int capturedShown;
        int selected = -1;
        int computerMoves;
        System.Random rng;
        readonly SlowMotion slowMotion = new SlowMotion();
        readonly Ease1D torchLevel = new Ease1D(1f);
        readonly Ease1D washLevel = new Ease1D(0f);
        Color washColour = Color.clear;
        bool mateFeelStarted;
        int lastHover = -1;

        public GamePhase Phase { get; private set; } = GamePhase.Menu;
        public ChessGame Game { get; private set; }
        public BoardTracker Tracker { get; private set; }
        public ChessSide PlayerSide { get; private set; } = ChessSide.White;
        public int DifficultyIndex { get; private set; } = 1;
        public int MenuDifficulty { get; private set; } = 1;
        public SideChoice MenuSide { get; private set; } = SideChoice.White;
        public SideChoice LastSideChoice { get; private set; } = SideChoice.White;
        public int SelectedSquare => selected;
        public IReadOnlyList<PieceActor> Tray => tray;
        public IReadOnlyDictionary<string, PieceActor> Actors => actors;
        /// <summary>1 while the torches burn normally, lower in a draw.</summary>
        public float TorchLevel => torchLevel.Value;
        public bool SlowMotionActive => slowMotion.Active;
        public float StrengthNow => Mathf.Clamp01(settings.effectStrength);
        public string DifficultyName => settings.difficulties[DifficultyIndex].name;

        public PieceActor ActorAt(int square)
        {
            var id = Tracker?.IdAt[square];
            return id != null && actors.TryGetValue(id, out var a) ? a : null;
        }

        // --- starting up ---

        void Awake()
        {
            computer = new ComputerPlayer(settings.evaluation);
            stage = new SequenceStage(id => actors[id]);
            rng = new System.Random(seed != 0 ? seed : Environment.TickCount);
        }

        void Start()
        {
            speed = settings.animationSpeed;
            MenuDifficulty = Mathf.Clamp(settings.game.startingDifficulty, 0, settings.difficulties.Length - 1);
            WireButtons();
            ShowMenu();
        }

        void WireButtons()
        {
            for (int i = 0; i < ui.difficultyButtons.Length; i++) { int n = i; ui.difficultyButtons[i].onClick.AddListener(() => SelectDifficulty(n)); }
            for (int i = 0; i < ui.sideButtons.Length; i++) { var s = (SideChoice)i; ui.sideButtons[i].onClick.AddListener(() => SelectSide(s)); }
            for (int i = 0; i < ui.speedButtons.Length; i++) { var s = (AnimationSpeed)i; ui.speedButtons[i].onClick.AddListener(() => SelectSpeed(s)); }
            for (int i = 0; i < ui.promotionButtons.Length; i++)
            {
                var type = new[] { PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight }[i];
                ui.promotionButtons[i].onClick.AddListener(() => ChoosePromotion(type));
            }
            ui.startButton.onClick.AddListener(StartFromMenu);
            ui.resignButton.onClick.AddListener(Resign);
            ui.menuButton.onClick.AddListener(ChangeDifficulty);
            ui.playAgainButton.onClick.AddListener(PlayAgain);
            ui.changeDifficultyButton.onClick.AddListener(ChangeDifficulty);

            // every button creaks a little when pressed
            foreach (var button in ui.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                button.onClick.AddListener(() => Play(SoundId.UiCreak, 0.6f));
        }

        void Play(SoundId id, float volume = 1f)
        {
            if (sound != null) sound.Play(id, volume, 1f + UnityEngine.Random.Range(-0.04f, 0.04f));
        }

        void ResetFeel()
        {
            slowMotion.Cancel();
            torchLevel.Snap(1f);
            washLevel.Snap(0f);
            mateFeelStarted = false;
            ApplyFeel();
        }

        void ApplyFeel()
        {
            if (torches != null) foreach (var t in torches) if (t != null) t.Dim = torchLevel.Value;
            if (ui != null) ui.SetWash(washColour, washLevel.Value);
        }

        void OnDestroy()
        {
            cancel?.Cancel();
        }

        // --- the menu ---

        public void SelectDifficulty(int index)
        {
            MenuDifficulty = Mathf.Clamp(index, 0, settings.difficulties.Length - 1);
            RefreshMenu();
        }

        public void SelectSide(SideChoice side)
        {
            MenuSide = side;
            RefreshMenu();
        }

        public void SelectSpeed(AnimationSpeed s)
        {
            speed = s;
            RefreshMenu();
        }

        void RefreshMenu()
        {
            ui.MarkSelection(MenuDifficulty, (int)MenuSide, (int)speed, Describe(settings.difficulties[MenuDifficulty]));
        }

        static string Describe(DifficultyProfile p)
        {
            switch (p.name)
            {
                case "Squire": return "Looks one move ahead and slips now and then. A good first opponent.";
                case "Knight": return "Looks a few moves ahead and sees simple tactics. A fair game.";
                default: return "Looks as far ahead as it can in a few seconds. Hard to beat.";
            }
        }

        public void ShowMenu()
        {
            Phase = GamePhase.Menu;
            ui.ShowMenu();
            RefreshMenu();
            highlights.ClearAll();
            cameraRig.SetPlayerSide(ChessSide.White);
            cameraRig.SnapMood(0f);
            ResetFeel();
        }

        public void StartFromMenu() => StartGame(MenuDifficulty, MenuSide);

        // --- a game ---

        /// <summary>Starts a new game. A start position can be given (tests and puzzles use this).</summary>
        public void StartGame(int difficulty, SideChoice sideChoice, Position startPosition = null)
        {
            cancel.Cancel();
            cancel = new CancellationTokenSource();
            thinking = null;
            ClearPieces();

            DifficultyIndex = Mathf.Clamp(difficulty, 0, settings.difficulties.Length - 1);
            LastSideChoice = sideChoice;
            PlayerSide = sideChoice == SideChoice.Random ? (rng.Next(2) == 0 ? ChessSide.White : ChessSide.Black)
                : sideChoice == SideChoice.White ? ChessSide.White : ChessSide.Black;
            Game = new ChessGame(startPosition ?? Position.Start());
            Tracker = new BoardTracker(Game.Position);
            computerMoves = 0;
            capturedShown = 0;
            tray.Clear();
            trayCount[0] = trayCount[1] = 0;

            foreach (var state in Tracker.Actors.Values)
                Spawn(new ActorSpec { Id = state.Id, Type = state.Type, Side = state.Side, Start = state.Position, StartYaw = state.Yaw });

            highlights.ClearAll();
            selected = -1;
            ui.ShowHud();
            cameraRig.SetPlayerSide(PlayerSide);

            // the camera starts back in the hall and walks in to the board
            ResetFeel();
            cameraRig.SnapMood(settings.feel.startMood);
            cameraRig.SetMood(0f, settings.feel.moodSeconds);
            cameraRig.Tick(0f, 0f, null); // the very first frame already shows the pulled-back view
            Play(SoundId.GameStart);
            lastHover = -1;

            if (Game.IsOver) EndGame();
            else NextTurn();
        }

        void ClearPieces()
        {
            foreach (var a in actors.Values) if (a != null) Destroy(a.gameObject);
            foreach (var a in tray) if (a != null) Destroy(a.gameObject);
            actors.Clear();
            tray.Clear();
        }

        PieceActor Spawn(ActorSpec spec)
        {
            var instance = Instantiate(piecePrefabs[(int)spec.Type]);
            instance.name = spec.Id;
            var actor = PieceActor.Bind(instance, spec.Type, spec.Side == 0 ? bone : cinnabar, iron, spec.Side);
            actor.Place(spec.Start, spec.StartYaw);
            actors[spec.Id] = actor;
            return actor;
        }

        void NextTurn()
        {
            if (Game.Position.SideToMove == PlayerSide)
            {
                Phase = GamePhase.PlayerTurn;
                ui.SetStatus(Game.Position.InCheck() ? "Your move. Your king is in check!" : "Your move");
                ui.SetResignAvailable(true);
            }
            else BeginComputerTurn();
        }

        // --- the player's turn ---

        public void Hover(int square)
        {
            highlights.ShowHover(Phase == GamePhase.PlayerTurn ? square : -1);
            if (Phase == GamePhase.PlayerTurn && square >= 0 && square != lastHover) Play(SoundId.HoverTick, 0.35f);
            lastHover = Phase == GamePhase.PlayerTurn ? square : -1;
        }

        public void Click(int square)
        {
            if (Phase != GamePhase.PlayerTurn || square < 0) return;

            if (selected >= 0)
            {
                var candidates = selectedMoves.Where(m => m.To == square).ToList();
                if (candidates.Count == 1)
                {
                    PlayMove(candidates[0]);
                    return;
                }
                if (candidates.Count > 1) // the four promotions
                {
                    Phase = GamePhase.ChoosingPromotion;
                    ui.ShowPromotion(true);
                    return;
                }
            }

            int piece = Game.Position[square];
            if (piece != Pieces.Empty && Pieces.SideOf(piece) == PlayerSide && Game.LegalMovesFrom(square).Any()) Select(square);
            else
            {
                // a click on a piece that cannot be picked or taken gets a dull knock; a click on an empty square just cancels
                if (piece != Pieces.Empty) Play(SoundId.IllegalKnock);
                Deselect();
            }
        }

        void Select(int square)
        {
            selected = square;
            Play(SoundId.SelectClink);
            selectedMoves.Clear();
            selectedMoves.AddRange(Game.LegalMovesFrom(square));
            highlights.ShowSelection(square);
            highlights.ShowMoves(
                selectedMoves.Where(m => !m.IsCapture).Select(m => (int)m.To).Distinct(),
                selectedMoves.Where(m => m.IsCapture).Select(m => (int)m.To).Distinct());
        }

        void Deselect()
        {
            selected = -1;
            selectedMoves.Clear();
            highlights.ClearSelection();
        }

        public void ChoosePromotion(PieceType type)
        {
            if (Phase != GamePhase.ChoosingPromotion || selected < 0) return;
            var move = selectedMoves.FirstOrDefault(m => m.Promotion == type);
            if (!move.IsValid) return;
            ui.ShowPromotion(false);
            PlayMove(move);
        }

        // --- playing a move (the player's or the computer's) ---

        void PlayMove(Move move)
        {
            var before = Game.Position.Clone();
            if (!Game.TryMake(move)) throw new InvalidOperationException($"{move} is not legal");
            var sequence = Tracker.Apply(before, move, settings.moves, settings.fight);

            // a promoted piece has no actor yet: make it, hidden until the step shows it
            foreach (var step in sequence.Steps)
                foreach (var spec in step.Actors)
                    if (!actors.ContainsKey(spec.Id)) Spawn(spec).gameObject.SetActive(false);

            Deselect();
            highlights.ClearAll();
            highlights.ShowLastMove(move.From, move.To);
            ui.SetStatus(string.Empty);
            ui.SetResignAvailable(false);
            mateFeelStarted = false;
            stage.Begin(sequence, speed, settings.fight, settings.effectStrength, settings.feel);
            Phase = GamePhase.Animating;
            PlayCues();
        }

        void FinishAnimation()
        {
            // captured pieces stand on the trays
            while (capturedShown < Tracker.Captured.Count)
            {
                AddToTray(Tracker.Captured[capturedShown]);
                capturedShown++;
            }

            if (Game.Status != GameStatus.Resignation && Game.Position.InCheck() && !Game.IsOver)
            {
                highlights.ShowCheck(Game.Position.KingSquare(Game.Position.SideToMove));
                Play(SoundId.CheckHorn);
                if (speed == AnimationSpeed.Full) cameraRig.AddShake(settings.feel.checkShake * StrengthNow);
            }
            else if (Game.IsOver && Game.Status == GameStatus.Checkmate)
            {
                highlights.ShowCheck(Game.Position.KingSquare(Game.Position.SideToMove));
                if (!mateFeelStarted) BeginMateFeel();
            }
            else if (Game.IsOver && Game.Status != GameStatus.Resignation) BeginDrawFeel();

            if (Game.IsOver) EndGame();
            else NextTurn();
        }

        void AddToTray(ActorState victim)
        {
            var side = (ChessSide)victim.Side;
            var spec = new ActorSpec { Id = $"tray:{tray.Count}", Type = victim.Type, Side = victim.Side };
            var instance = Instantiate(piecePrefabs[(int)victim.Type]);
            instance.name = $"Captured {victim.Type}";
            var actor = PieceActor.Bind(instance, victim.Type, victim.Side == 0 ? bone : cinnabar, iron, victim.Side);
            var slot = BoardGeometry.TraySlot(side, trayCount[victim.Side]++, settings.game.trayX);
            actor.Place(slot, BoardGeometry.RestYaw(side));
            instance.transform.localScale = Vector3.one * settings.game.trayScale;
            tray.Add(actor);
        }

        // --- the computer's turn ---

        void BeginComputerTurn()
        {
            Phase = GamePhase.ComputerThinking;
            ui.SetStatus($"The {DifficultyName} is thinking...");
            ui.SetResignAvailable(false);
            var profile = profileOverride ?? settings.difficulties[DifficultyIndex];
            int computerSeed = (seed != 0 ? seed : Environment.TickCount) + computerMoves++;
            thinking = computer.ChooseAsync(Game, profile, computerSeed, cancel.Token);
        }

        void OnComputerDone(Task<ComputerChoice> task)
        {
            Move move = default;
            if (task.IsFaulted)
                Debug.LogError($"The computer failed: {task.Exception?.GetBaseException().Message}");
            else if (!task.IsCanceled && task.Result != null && !task.Result.Cancelled) move = task.Result.Move;
            else return; // cancelled: the game was left or restarted

            // never get stuck: if the search gave nothing usable, play the first legal move
            if (!move.IsValid || !Game.LegalMoves.Any(m => m.Equals(move))) move = Game.LegalMoves[0];
            PlayMove(move);
        }

        // --- the end ---

        void EndGame()
        {
            Phase = GamePhase.GameOver;
            ui.SetStatus(string.Empty);
            ui.SetResignAvailable(false);
            ui.ShowResult(ResultText());
        }

        public string ResultText()
        {
            switch (Game.Status)
            {
                case GameStatus.Checkmate: return Game.Winner == PlayerSide ? "CHECKMATE\nYou win!" : "CHECKMATE\nYou lose.";
                case GameStatus.Resignation: return Game.Winner == PlayerSide ? "RESIGNED\nThe computer resigned." : "RESIGNED\nYou resigned.";
                case GameStatus.Stalemate: return "STALEMATE\nA draw.";
                case GameStatus.ThreefoldRepetition: return "DRAW\nThe same position three times.";
                case GameStatus.FiftyMoveRule: return "DRAW\nFifty moves without a capture or a pawn move.";
                case GameStatus.InsufficientMaterial: return "DRAW\nNeither side can win.";
                default: return string.Empty;
            }
        }

        public void Resign()
        {
            if (Phase != GamePhase.PlayerTurn) return;
            Game.Resign(PlayerSide);
            int kingSquare = Game.Position.KingSquare(PlayerSide);
            var king = Tracker.Actors[Tracker.IdAt[kingSquare]];
            var spec = new ActorSpec { Id = king.Id, Type = king.Type, Side = king.Side, Start = king.Position, StartYaw = king.Yaw };
            Deselect();
            highlights.ClearAll();
            ui.SetResignAvailable(false);
            stage.Begin(MoveDirector.Resign(spec, settings.moves), speed, settings.fight, settings.effectStrength, settings.feel);
            Phase = GamePhase.Animating;
            if (speed == AnimationSpeed.Full) cameraRig.SetMood(settings.feel.resignMood, settings.feel.moodSeconds, king.Position + Vector3.up * 0.4f);
            PlayCues();
        }

        // --- the endings: checkmate, draw ---

        // The mated king falls in slow motion, the camera walks toward him, a fanfare plays and a wash fades in.
        void BeginMateFeel()
        {
            mateFeelStarted = true;
            var f = settings.feel;
            var kingSquare = Game.Position.KingSquare(Game.Position.SideToMove);
            Play(SoundId.MateFanfare);
            if (speed == AnimationSpeed.Full)
            {
                slowMotion.Begin(f.slowMotionScale, f.slowMotionSeconds, StrengthNow);
                cameraRig.SetMood(f.mateMood, f.moodSeconds, BoardGeometry.Centre(kingSquare) + Vector3.up * 0.4f);
            }
            washColour = Game.Winner == PlayerSide ? f.washWin : f.washLose;
            washLevel.MoveTo(f.washAlpha * StrengthNow, speed == AnimationSpeed.Off ? 0.01f : f.washSeconds);
        }

        // A draw: a low bell, the torches burn down a little and the camera draws back.
        void BeginDrawFeel()
        {
            var f = settings.feel;
            Play(SoundId.DrawBell);
            torchLevel.MoveTo(Mathf.Lerp(1f, f.drawTorchLevel, StrengthNow), f.dimSeconds);
            if (speed == AnimationSpeed.Full) cameraRig.SetMood(f.drawMood, f.moodSeconds);
        }

        void PlayCues()
        {
            foreach (var c in stage.CuesThisFrame)
            {
                if (c.Cue.HasSound) Play(c.Cue.Sound);
                if (effects != null && c.Cue.Effect != EffectKind.None) effects.Burst(c.Cue.Effect, c.Position, StrengthNow);
            }
        }

        public void PlayAgain() => StartGame(DifficultyIndex, LastSideChoice);

        public void ChangeDifficulty()
        {
            cancel.Cancel();
            cancel = new CancellationTokenSource();
            thinking = null;
            ClearPieces();
            Game = null;
            Tracker = null;
            selected = -1;
            ShowMenu();
        }

        // --- every frame ---

        void Update()
        {
            if (autoStep) Step(Mathf.Min(Time.unscaledDeltaTime, 0.05f));
        }

        /// <summary>Moves the game on by a real frame time: the animation, the computer's answer and the camera.</summary>
        public void Step(float dt)
        {
            float push = 0f;
            Vector3? focus = null;
            if (Phase == GamePhase.Animating)
            {
                if (Game.Status == GameStatus.Checkmate && !mateFeelStarted && stage.CurrentStepName == "King falls") BeginMateFeel();
                stage.Step(slowMotion.Apply(dt));
                PlayCues();
                if (stage.BlowThisFrame && stage.BlowShake > 0f) cameraRig.AddShake(stage.BlowShake);
                push = stage.CameraPush;
                focus = stage.FightFocus;
                if (stage.Finished) FinishAnimation();
            }
            else if (Phase == GamePhase.ComputerThinking && thinking != null && thinking.IsCompleted)
            {
                var task = thinking;
                thinking = null;
                OnComputerDone(task);
            }
            cameraRig.Tick(dt, push, focus);
            torchLevel.Update(dt);
            washLevel.Update(dt);
            ApplyFeel();
        }
    }
}
