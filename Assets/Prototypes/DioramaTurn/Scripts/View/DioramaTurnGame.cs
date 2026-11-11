using System.Collections.Generic;
using UnityEngine;

namespace Prototypes.DioramaTurn
{
    public enum GameFlow { Title, Playing, Complete, Finished }

    /// <summary>
    /// Owns the simulation, the room flow and the translation of events into what the scene shows.
    /// Tests drive it with Step(dt) and the same commands the input uses.
    /// </summary>
    public sealed class DioramaTurnGame : MonoBehaviour
    {
        public DioramaTurnSettings settings;
        public CameraRig rig;
        public RoomView room;
        public PipView pip;
        public StarView star;
        public LinkRibbons ribbons;
        public Hud hud;
        public DioramaInput input;

        [System.NonSerialized] public bool autoStep = true;

        public GameFlow Flow { get; private set; }
        public DioramaSimulation Sim { get; private set; }
        public int RoomIndex { get; private set; }
        public Cell? Cursor { get; private set; }

        void Start()
        {
            WireButtons();
            ShowTitle();
        }

        void WireButtons()
        {
            if (hud == null) return;
            if (hud.turnLeftButton != null) hud.turnLeftButton.onClick.AddListener(() => PressTurn(-1));
            if (hud.turnRightButton != null) hud.turnRightButton.onClick.AddListener(() => PressTurn(1));
            if (hud.restartButton != null) hud.restartButton.onClick.AddListener(Restart);
            if (hud.nextButton != null) hud.nextButton.onClick.AddListener(NextRoom);
            if (hud.playAgainButton != null) hud.playAgainButton.onClick.AddListener(PlayAgain);
        }

        void Update()
        {
            if (autoStep) Step(Time.deltaTime);
        }

        /// <summary>Advance the simulation and the view by dt. The tests call this by hand.</summary>
        public void Step(float dt)
        {
            if (Sim == null) return;
            Sim.Tick(dt);
            FollowCamera();
            if (pip != null) pip.Show(Sim, dt);
            DrainEvents();
        }

        public void PressTurn(int direction)
        {
            if (Flow != GameFlow.Playing || Sim == null) return;
            Sim.PressTurn(direction);
            DrainEvents();
        }

        public void WalkTo(Cell target)
        {
            if (Flow != GameFlow.Playing || Sim == null) return;
            Sim.PickNode(target);
            DrainEvents();
        }

        public void Restart()
        {
            if (Flow != GameFlow.Playing || Sim == null) return;
            Sim.Restart();
            Cursor = Sim.Room.Start;
            if (room != null) room.ShowCursor(Cursor);
            if (hud != null) hud.SetTurns(Sim.TurnCount);
            if (pip != null) pip.SnapTo(Sim);
            DrainEvents();
        }

        /// <summary>Enter, Space, a click on the title, or the gamepad's south button.</summary>
        public void Confirm()
        {
            switch (Flow)
            {
                case GameFlow.Title: BeginRoom(0); break;
                case GameFlow.Complete: NextRoom(); break;
                case GameFlow.Finished: PlayAgain(); break;
                default: ConfirmCursor(); break;
            }
        }

        public void MoveCursor(ScreenDirection direction)
        {
            if (Sim == null) return;
            var candidates = new List<Cell>();
            foreach (var piece in Sim.Room.Pieces) candidates.Add(piece.Cell);
            Cursor = CursorNavigator.Move(Sim.View, Cursor ?? Sim.PipCell, direction, candidates);
            if (room != null) room.ShowCursor(Cursor);
        }

        public void ConfirmCursor()
        {
            if (Flow != GameFlow.Playing || Sim == null) return;
            if (Cursor.HasValue) WalkTo(Cursor.Value);
        }

        public void NextRoom()
        {
            if (RoomIndex + 1 < Rooms.All.Count) BeginRoom(RoomIndex + 1);
            else
            {
                Flow = GameFlow.Finished;
                if (hud != null) hud.ShowFinish();
            }
        }

        public void PlayAgain() => BeginRoom(0);

        /// <summary>The node under a screen point, within the click radius, or null.</summary>
        public Cell? NodeNearScreenPoint(Vector2 screenPoint)
        {
            if (Sim == null || rig == null || rig.camera == null) return null;
            var camera = rig.camera;
            var target = camera.ScreenToViewportPoint(screenPoint);
            float aspect = Mathf.Max(0.01f, camera.aspect);
            Cell? best = null;
            float bestDistance = float.MaxValue;
            foreach (var piece in Sim.Room.Pieces)
            {
                var viewport = camera.WorldToViewportPoint(RoomView.TopFace(piece.Cell) + Vector3.up * 0.3f);
                if (viewport.z <= 0f) continue;
                float dx = (viewport.x - target.x) * aspect;
                float dy = viewport.y - target.y;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                if (distance <= settings.camera.clickRadiusViewport && distance < bestDistance)
                {
                    bestDistance = distance;
                    best = piece.Cell;
                }
            }
            return best;
        }

        void ShowTitle()
        {
            Flow = GameFlow.Title;
            if (hud != null) hud.ShowTitle();
        }

        void BeginRoom(int index)
        {
            RoomIndex = Mathf.Clamp(index, 0, Rooms.All.Count - 1);
            var definition = Rooms.All[RoomIndex];
            Sim = new DioramaSimulation(Rooms.Build(RoomIndex), settings.ToTiming(), 0);

            if (room != null) room.Build(Sim.Room);
            if (star != null) star.Place(Sim.Room.Goal);
            if (ribbons != null) ribbons.ShowJoins(Sim.ScreenJoins(0));
            Cursor = Sim.Room.Start;
            if (room != null) room.ShowCursor(Cursor);
            if (rig != null) rig.SnapToView(0);

            Flow = GameFlow.Playing;
            if (hud != null)
            {
                hud.ShowPlaying(definition.Name);
                hud.SetTurns(0);
            }
            if (pip != null) pip.SnapTo(Sim);
        }

        void FollowCamera()
        {
            if (rig == null) return;
            if (Sim.Phase == Phase.Turning) rig.ShowTurn(Sim.View, Sim.TargetView, Sim.TurnProgress);
            else rig.SnapToView(Sim.View);
        }

        void DrainEvents()
        {
            foreach (var e in Sim.DrainEvents())
            {
                switch (e.Kind)
                {
                    case GameEventKind.TurnSettled:
                        if (ribbons != null) ribbons.ShowJoins(Sim.ScreenJoins(Sim.View));
                        if (hud != null) hud.SetTurns(Sim.TurnCount);
                        break;
                    case GameEventKind.GoalReached:
                        CompleteRoom();
                        break;
                }
            }
            if (hud != null && Flow == GameFlow.Playing) hud.SetTurns(Sim.TurnCount);
        }

        void CompleteRoom()
        {
            Flow = GameFlow.Complete;
            var definition = Rooms.All[RoomIndex];
            int stars = Stars.For(Sim.TurnCount, definition.Par, settings.rooms.parTwoStarsExtra);
            if (hud != null) hud.ShowComplete(definition.Name, Sim.TurnCount, definition.Par, stars);
        }
    }
}
