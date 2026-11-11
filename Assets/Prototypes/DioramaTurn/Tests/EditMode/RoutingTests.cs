using System.Linq;
using NUnit.Framework;

namespace Prototypes.DioramaTurn.Tests
{
    public sealed class RoutingTests
    {
        static RoomData Row(string row) => RoomParser.Parse(new[] { new[] { row } });

        [Test]
        public void GraphHasNeighbourEdgesAndNoHops()
        {
            var room = Row("sbbg");
            var g = JoinGraph.Build(room, 0);
            Assert.AreEqual(1, g.Neighbours(new Cell(0, 0, 0)).Count);
            Assert.AreEqual(2, g.Neighbours(new Cell(1, 0, 0)).Count);
            Assert.IsFalse(g.Neighbours(new Cell(1, 0, 0)).Any(e => e.Hop));
        }

        [Test]
        public void RouteIsShortestAndInOrder()
        {
            var room = Row("sbbg");
            var route = RoutePlanner.Plan(JoinGraph.Build(room, 0), room.Start, room.Goal);
            Assert.AreEqual(3, route.Count);
            Assert.AreEqual(new Cell(1, 0, 0), route[0].To);
            Assert.AreEqual(new Cell(3, 0, 0), route[2].To);
            Assert.IsFalse(route.Any(s => s.Hop));
        }

        [Test]
        public void RouteToTheSameCellIsEmpty()
        {
            var room = Row("sbg");
            var route = RoutePlanner.Plan(JoinGraph.Build(room, 0), room.Start, room.Start);
            Assert.IsNotNull(route);
            Assert.AreEqual(0, route.Count);
        }

        [Test]
        public void ARoomWithAGapHasNoRoute()
        {
            var room = Row("sb.g");
            Assert.IsNull(RoutePlanner.Plan(JoinGraph.Build(room, 0), room.Start, room.Goal));
        }

        [Test]
        public void AScreenJoinIsAHopAndOnlyInItsView()
        {
            // The goal sits one neighbour step plus one view 0 depth step from the block.
            var block = new Cell(0, 0, 0);
            var goal = new Cell(2, 1, 1);
            var room = new RoomData(3, 2, 2, new[]
            {
                new Piece(block, PieceKind.Start),
                new Piece(goal, PieceKind.Goal),
            });
            var view0 = RoutePlanner.Plan(JoinGraph.Build(room, 0), block, goal);
            Assert.IsNotNull(view0);
            Assert.AreEqual(1, view0.Count);
            Assert.IsTrue(view0[0].Hop);
            Assert.IsNull(RoutePlanner.Plan(JoinGraph.Build(room, 1), block, goal));
        }

        [Test]
        public void SolverFindsOneTurnForATwoIslandRoom()
        {
            var room = RoomParser.Parse(new[]
            {
                new[] { ".....", ".....", ".....", ".....", "....." },
                new[] { ".....", ".....", "...sb", ".....", "....." },
                new[] { ".....", ".....", ".....", ".....", "....." },
                new[] { "....b", "....g", ".....", ".....", "....." },
            });
            var result = Solver.Solve(room);
            Assert.IsTrue(result.Solved);
            Assert.AreEqual(1, result.MinTurns);
            Assert.AreEqual(1, result.Moves.Count(m => m.Kind == MoveKind.Turn));
        }

        [Test]
        public void SolverReportsAnUnsolvableRoom()
        {
            var room = Row("sb..g");
            var result = Solver.Solve(room);
            Assert.IsFalse(result.Solved);
        }

        [Test]
        public void SolverNeedsNoTurnWhenTheRoomIsAlreadyConnected()
        {
            var room = Row("sbg");
            var result = Solver.Solve(room);
            Assert.IsTrue(result.Solved);
            Assert.AreEqual(0, result.MinTurns);
        }
    }
}
