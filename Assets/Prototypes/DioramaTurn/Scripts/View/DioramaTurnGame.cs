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
        public DioramaAudio audio;
        public MusicPlayer music;

        [System.NonSerialized] public bool autoStep = true;

        float slowMoLeft;

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
            if (slowMoLeft > 0f)
            {
                slowMoLeft -= Time.unscaledDeltaTime;
                if (slowMoLeft <= 0f) Time.timeScale = 1f;
            }
            if (autoStep) Step(Time.deltaTime);
        }

        /// <summary>Advance the simulation and the view by dt. The tests call this by hand.</summary>
        public void Step(float dt)
        {
            if (Sim == null) return;
            Sim.Tick(dt);
            FollowCamera();
            if (pip != null) pip.Show(Sim, dt);
            if (star != null) star.Show(dt);
            if (room != null) room.FaceSprites(rig != null ? rig.Yaw : 45f);
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
            if (Sim.PickNode(target) && audio != null) audio.Tap();
            DrainEvents();
        }

        public void Restart()
        {
            if (Flow != GameFlow.Playing || Sim == null) return;
            Sim.Restart();
            Cursor = null;
            if (room != null)
            {
                room.ShowCursor(null);
                room.ShowReachable(RoutePlanner.Reachable(Sim.Graph, Sim.PipCell));
            }
            if (ribbons != null) ribbons.ShowJoins(Sim.ScreenJoins(Sim.View));
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
            if (Flow != GameFlow.Playing || Sim == null) return;
            var neighbours = new List<Cell>();
            foreach (var e in Sim.Graph.Neighbours(Sim.PipCell)) neighbours.Add(e.To);
            var next = CursorNavigator.Move(Sim.View, Sim.PipCell, direction, neighbours)
                       ?? CursorNavigator.Move(Sim.View, Sim.PipCell, direction, RoutePlanner.Reachable(Sim.Graph, Sim.PipCell));
            if (next.HasValue)
            {
                Cursor = next;
                if (room != null) room.ShowCursor(Cursor);
                WalkTo(next.Value);
            }
            else if (Sim.Phase == Phase.Idle)
            {
                if (pip != null) pip.Shake();
                if (audio != null) audio.Play(GameEventKind.Unreachable);
            }
        }

        public void HoverAtScreenPoint(Vector2 screenPoint)
        {
            if (Flow != GameFlow.Playing || Sim == null) return;
            var hit = NodeNearScreenPoint(screenPoint);
            if (hit.HasValue && hit.Value != Sim.PipCell && RoutePlanner.Plan(Sim.Graph, Sim.PipCell, hit.Value) != null)
                Cursor = hit;
            else
                Cursor = null;
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
            Time.timeScale = 1f;
            slowMoLeft = 0f;
            RoomIndex = Mathf.Clamp(index, 0, Rooms.All.Count - 1);
            var definition = Rooms.All[RoomIndex];
            Sim = new DioramaSimulation(Rooms.Build(RoomIndex), settings.ToTiming(), 0);

            if (room != null)
            {
                room.Build(Sim.Room);
                room.ShowCursor(null);
                room.ShowReachable(RoutePlanner.Reachable(Sim.Graph, Sim.PipCell));
            }
            if (star != null) star.Place(Sim.Room.Goal);
            if (ribbons != null) ribbons.ShowJoins(Sim.ScreenJoins(0));
            Cursor = null;
            if (rig != null) rig.SnapToView(0);

            Flow = GameFlow.Playing;
            if (hud != null)
            {
                hud.ShowPlaying(definition.Name);
                hud.SetTurns(0);
            }
            if (pip != null) pip.SnapTo(Sim);
            if (audio != null) audio.Pop();
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
                if (audio != null) audio.Play(e.Kind);
                switch (e.Kind)
                {
                    case GameEventKind.TurnSettled:
                        if (ribbons != null) ribbons.ShowJoins(Sim.ScreenJoins(Sim.View));
                        if (room != null) room.ShowReachable(RoutePlanner.Reachable(Sim.Graph, Sim.PipCell));
                        if (hud != null) hud.SetTurns(Sim.TurnCount);
                        break;
                    case GameEventKind.StepTaken:
                    case GameEventKind.HopTaken:
                        if (room != null) room.ShowReachable(RoutePlanner.Reachable(Sim.Graph, Sim.PipCell));
                        break;
                    case GameEventKind.GoalReached:
                        if (room != null) room.ShowReachable(null);
                        if (pip != null) pip.Cheer();
                        if (music != null) music.Duck();
                        CompleteRoom();
                        break;
                    case GameEventKind.Unreachable:
                        if (pip != null) pip.Shake();
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
            if (audio != null) audio.Chime();

            float slow = Feel.SlowMoScale(settings.feel.goalSlowMo, settings.feel.effectStrength);
            if (slow < 1f)
            {
                Time.timeScale = slow;
                slowMoLeft = settings.feel.goalSlowMoSeconds;
            }
        }
    }
}
