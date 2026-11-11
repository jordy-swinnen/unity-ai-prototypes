using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace Prototypes.DioramaTurn.Tests
{
    public class GamePlayTests
    {
        const string ScenePath = "Assets/Prototypes/DioramaTurn/Scenes/DioramaTurn.unity";
        const float Step = 1f / 60f;

        DioramaTurnGame game;
        Keyboard keyboard;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return null;
#endif
            game = UnityEngine.Object.FindAnyObjectByType<DioramaTurnGame>();
            Assert.IsNotNull(game, "the scene has no DioramaTurnGame");
            game.autoStep = false; // the tests step the game by hand
            yield return null;     // Start has run
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            if (keyboard != null)
            {
                InputSystem.RemoveDevice(keyboard);
                keyboard = null;
                yield return null;
            }
            yield return null;
        }

        IEnumerator StepUntil(Func<bool> done, string what, int maxFrames = 3000)
        {
            for (int i = 0; i < maxFrames && !done(); i++)
            {
                game.Step(Step);
                yield return null;
            }
            Assert.IsTrue(done(), $"timed out waiting for {what} (flow {game.Flow}, phase {(game.Sim != null ? game.Sim.Phase.ToString() : "none")})");
        }

        IEnumerator Settle() => StepUntil(
            () => game.Sim != null && game.Sim.Phase != Phase.Walking && game.Sim.Phase != Phase.Turning,
            "the move to settle");

        IEnumerator StartRoom()
        {
            game.Confirm();
            yield return null;
            Assert.AreEqual(GameFlow.Playing, game.Flow);
        }

        Cell FirstAdjacent(Cell from)
        {
            foreach (var edge in game.Sim.Graph.Neighbours(from))
                if (!edge.Hop) return edge.To;
            Assert.Fail($"no plain neighbour of {from}");
            return from;
        }

        [UnityTest]
        public IEnumerator The_scene_opens_on_the_title_card_with_no_simulation()
        {
            Assert.AreEqual(GameFlow.Title, game.Flow);
            Assert.IsNull(game.Sim);
            Assert.IsTrue(game.hud.titlePanel.activeSelf);
            Assert.IsFalse(game.hud.playingPanel.activeSelf);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Confirming_the_title_starts_room_one_in_view_zero()
        {
            yield return StartRoom();
            Assert.AreEqual(0, game.RoomIndex);
            Assert.AreEqual(0, game.Sim.View);
            Assert.AreEqual(game.Sim.Room.Start, game.Sim.PipCell);
            Assert.IsNotNull(game.room.Room, "the room's pieces are built");
            yield return null;
        }

        [UnityTest]
        public IEnumerator The_camera_ends_each_turn_on_the_exact_view_angle()
        {
            yield return StartRoom();
            for (int i = 0; i < 4; i++)
            {
                game.PressTurn(1);
                yield return Settle();
                float expected = Mathf.Repeat(CameraMath.YawForView(game.Sim.View, game.settings.camera.baseYawDeg), 360f);
                float actual = Mathf.Repeat(game.rig.Yaw, 360f);
                Assert.AreEqual(expected, actual, 0.05f, $"view {game.Sim.View}");
            }
            Assert.AreEqual(0, game.Sim.View, "four turns come back to view 0");
        }

        [UnityTest]
        public IEnumerator Pip_walks_to_a_reachable_node_and_stops()
        {
            yield return StartRoom();
            var target = FirstAdjacent(game.Sim.PipCell);
            game.WalkTo(target);
            Assert.AreEqual(Phase.Walking, game.Sim.Phase);
            yield return Settle();
            Assert.AreEqual(target, game.Sim.PipCell);
            Assert.AreEqual(Phase.Idle, game.Sim.Phase);
        }

        [UnityTest]
        public IEnumerator A_turn_is_refused_while_pip_walks()
        {
            yield return StartRoom();
            game.WalkTo(FirstAdjacent(game.Sim.PipCell));
            Assert.AreEqual(Phase.Walking, game.Sim.Phase);
            int turnsBefore = game.Sim.TurnCount;
            game.PressTurn(1);
            Assert.AreEqual(turnsBefore, game.Sim.TurnCount, "a turn during a walk must not start");
            Assert.AreEqual(game.Sim.View, game.Sim.TargetView);
            yield return Settle();
        }

        [UnityTest]
        public IEnumerator Every_room_completes_from_the_solvers_moves()
        {
            yield return StartRoom();
            for (int room = 0; room < Rooms.All.Count; room++)
            {
                if (room > 0)
                {
                    game.NextRoom();
                    yield return null;
                    Assert.AreEqual(room, game.RoomIndex);
                }

                var solve = Solver.Solve(Rooms.Build(room));
                Assert.IsTrue(solve.Solved, $"room {room} is not solvable");
                foreach (var move in solve.Moves)
                {
                    if (move.Kind == MoveKind.Turn) game.PressTurn(move.Direction);
                    else game.WalkTo(move.Target);
                    yield return Settle();
                }
                Assert.AreEqual(GameFlow.Complete, game.Flow, $"room {room} did not complete");
            }
        }

        [UnityTest]
        public IEnumerator The_next_room_loads_after_the_complete_card()
        {
            yield return StartRoom();
            var solve = Solver.Solve(Rooms.Build(0));
            foreach (var move in solve.Moves)
            {
                if (move.Kind == MoveKind.Turn) game.PressTurn(move.Direction);
                else game.WalkTo(move.Target);
                yield return Settle();
            }
            Assert.AreEqual(GameFlow.Complete, game.Flow);

            game.NextRoom();
            yield return null;
            Assert.AreEqual(GameFlow.Playing, game.Flow);
            Assert.AreEqual(1, game.RoomIndex);
            Assert.AreEqual(0, game.Sim.View);
            Assert.AreEqual(game.Sim.Room.Start, game.Sim.PipCell);
            int activePiecesRoots = 0;
            for (int i = 0; i < game.room.transform.childCount; i++)
                if (game.room.transform.GetChild(i).gameObject.activeSelf) activePiecesRoots++;
            Assert.AreEqual(1, activePiecesRoots, "the old room's Pieces hierarchy must be cleared when a new room builds");
        }

        [UnityTest]
        public IEnumerator Restart_restores_the_first_state()
        {
            yield return StartRoom();
            var start = game.Sim.Room.Start;
            game.PressTurn(1);
            yield return Settle();
            game.WalkTo(FirstAdjacent(game.Sim.PipCell));
            yield return Settle();
            Assert.AreNotEqual(start, game.Sim.PipCell);

            game.Restart();
            yield return null;
            Assert.AreEqual(start, game.Sim.PipCell);
            Assert.AreEqual(0, game.Sim.TurnCount);
            Assert.AreEqual(0, game.Sim.View);
            Assert.AreEqual(Phase.Idle, game.Sim.Phase);
        }

        [UnityTest]
        public IEnumerator A_click_maps_to_the_node_under_it()
        {
            yield return StartRoom();
            var cell = game.Sim.PipCell;
            var screen = game.rig.camera.WorldToScreenPoint(RoomView.TopFace(cell) + Vector3.up * 0.3f);
            var found = game.NodeNearScreenPoint(screen);
            Assert.IsTrue(found.HasValue, "the node under the screen point was not found");
            Assert.AreEqual(cell, found.Value);
        }

        [UnityTest]
        public IEnumerator The_keyboard_turns_the_room()
        {
            yield return StartRoom();
            keyboard = InputSystem.AddDevice<Keyboard>();
            yield return null;

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q));
            yield return null;
            yield return null;

            Assert.AreEqual(1, game.Sim.TurnCount, "Q must start a turn");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }

        [UnityTest]
        public IEnumerator Pressing_a_direction_walks_directly_to_the_reachable_neighbour()
        {
            yield return StartRoom();
            var start = game.Sim.PipCell;
            // In Room 0 view 0, (4,0,1) is one step in +X from Start (3,0,1), which is Left-Down on screen.
            game.MoveCursor(ScreenDirection.Left);
            Assert.AreEqual(Phase.Walking, game.Sim.Phase, "pressing a direction must start walking immediately");
            yield return Settle();
            Assert.AreNotEqual(start, game.Sim.PipCell);
        }
    }
}
