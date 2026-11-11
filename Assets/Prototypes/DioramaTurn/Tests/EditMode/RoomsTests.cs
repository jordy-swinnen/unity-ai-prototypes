using NUnit.Framework;

namespace Prototypes.DioramaTurn.Tests
{
    public sealed class RoomsTests
    {
        [Test]
        public void ThereAreThreeRooms()
        {
            Assert.AreEqual(3, Rooms.All.Count);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void EveryRoomIsValidAndSolvableWithTheRightPar(int index)
        {
            var def = Rooms.All[index];
            var room = Rooms.Build(index);
            Assert.IsEmpty(RoomValidator.Validate(room, 5, 5, 5), def.Name);
            var result = Solver.Solve(room);
            Assert.IsTrue(result.Solved, def.Name);
            Assert.AreEqual(def.Par, result.MinTurns, def.Name);
            Assert.GreaterOrEqual(result.MinTurns, 1, $"{def.Name} must need a turn");
        }

        [Test]
        public void ParsGrowFromRoomToRoom()
        {
            Assert.AreEqual(1, Rooms.All[0].Par);
            Assert.That(Rooms.All[1].Par, Is.InRange(2, 3));
            Assert.That(Rooms.All[2].Par, Is.InRange(4, 6));
        }

        [Test]
        public void RoomTwoHasABridgePiece()
        {
            var room = Rooms.Build(1);
            Assert.IsTrue(System.Linq.Enumerable.Any(room.Pieces, p => p.Kind == PieceKind.Bridge));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void TheSolversMovesFinishTheRoomInTheSimulation(int index)
        {
            var room = Rooms.Build(index);
            var timing = new SimulationTiming(0.45f, 0.22f, 0.35f, 1);
            var sim = new DioramaSimulation(room, timing);
            foreach (var move in Solver.Solve(room).Moves)
            {
                if (move.Kind == MoveKind.Turn)
                {
                    Assert.IsTrue(sim.PressTurn(move.Direction));
                    sim.Tick(1f);
                }
                else
                {
                    Assert.IsTrue(sim.PickNode(move.Target), $"walk to {move.Target}");
                    for (int i = 0; i < 200 && sim.Phase == Phase.Walking; i++) sim.Tick(0.1f);
                }
            }
            Assert.AreEqual(Phase.Complete, sim.Phase);
            Assert.AreEqual(Rooms.All[index].Par, sim.TurnCount);
        }
    }
}
