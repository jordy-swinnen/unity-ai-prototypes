using System.Linq;
using NUnit.Framework;

namespace Prototypes.DioramaTurn.Tests
{
    public sealed class SimulationTests
    {
        static readonly SimulationTiming Timing = new SimulationTiming(0.45f, 0.22f, 0.35f, 1);

        // A walkable row, and a second island that one view joins to it.
        static RoomData Row(string row) => RoomParser.Parse(new[] { new[] { row } });

        static void Run(DioramaSimulation sim, float seconds)
        {
            for (float t = 0; t < seconds; t += 0.05f) sim.Tick(0.05f);
        }

        [Test]
        public void StartsIdleAtTheStartInViewZero()
        {
            var sim = new DioramaSimulation(Row("sbg"), Timing);
            Assert.AreEqual(Phase.Idle, sim.Phase);
            Assert.AreEqual(0, sim.View);
            Assert.AreEqual(new Cell(0, 0, 0), sim.PipCell);
            Assert.AreEqual(0, sim.TurnCount);
        }

        [Test]
        public void AWalkTakesOneStepTimePerNodeAndEmitsStepEvents()
        {
            var sim = new DioramaSimulation(Row("sbbg"), Timing);
            Assert.IsTrue(sim.PickNode(new Cell(2, 0, 0)));
            Assert.AreEqual(Phase.Walking, sim.Phase);
            sim.Tick(0.22f * 2 + 0.01f);
            Assert.AreEqual(Phase.Idle, sim.Phase);
            Assert.AreEqual(new Cell(2, 0, 0), sim.PipCell);
            var events = sim.DrainEvents();
            Assert.AreEqual(2, events.Count(e => e.Kind == GameEventKind.StepTaken));
        }

        [Test]
        public void ReachingTheGoalCompletesTheRoomAndEndsInput()
        {
            var sim = new DioramaSimulation(Row("sbg"), Timing);
            sim.PickNode(new Cell(2, 0, 0));
            Run(sim, 1f);
            Assert.AreEqual(Phase.Complete, sim.Phase);
            Assert.AreEqual(1, sim.DrainEvents().Count(e => e.Kind == GameEventKind.GoalReached));
            Assert.IsFalse(sim.PressTurn(1));
            Assert.IsFalse(sim.PickNode(new Cell(0, 0, 0)));
        }

        [Test]
        public void AnUnreachableNodeEmitsUnreachableAndDoesNotMove()
        {
            var sim = new DioramaSimulation(Row("sb.g"), Timing);
            Assert.IsFalse(sim.PickNode(new Cell(3, 0, 0)));
            Assert.AreEqual(Phase.Idle, sim.Phase);
            Assert.AreEqual(1, sim.DrainEvents().Count(e => e.Kind == GameEventKind.Unreachable));
        }

        [Test]
        public void ATurnChangesTheViewAfterItsDuration()
        {
            var sim = new DioramaSimulation(Row("sbg"), Timing);
            Assert.IsTrue(sim.PressTurn(1));
            Assert.AreEqual(Phase.Turning, sim.Phase);
            Assert.AreEqual(0, sim.View, "the view changes when the turn settles");
            Assert.AreEqual(1, sim.TargetView);
            sim.Tick(0.2f);
            Assert.AreEqual(Phase.Turning, sim.Phase);
            sim.Tick(0.3f);
            Assert.AreEqual(Phase.Idle, sim.Phase);
            Assert.AreEqual(1, sim.View);
            Assert.AreEqual(1, sim.TurnCount);
            var kinds = sim.DrainEvents().Select(e => e.Kind).ToList();
            Assert.AreEqual(GameEventKind.TurnStarted, kinds.First());
            Assert.AreEqual(GameEventKind.TurnSettled, kinds.Last());
        }

        [Test]
        public void OneExtraTurnIsQueuedAndTheRestAreIgnored()
        {
            var sim = new DioramaSimulation(Row("sbg"), Timing);
            Assert.IsTrue(sim.PressTurn(1));
            Assert.IsTrue(sim.PressTurn(1), "queued");
            Assert.IsFalse(sim.PressTurn(1), "queue is full");
            Run(sim, 2f);
            Assert.AreEqual(Phase.Idle, sim.Phase);
            Assert.AreEqual(2, sim.View);
            Assert.AreEqual(2, sim.TurnCount);
        }

        [Test]
        public void ATurnIsRefusedWhileWalking()
        {
            var sim = new DioramaSimulation(Row("sbbg"), Timing);
            sim.PickNode(new Cell(2, 0, 0));
            Assert.IsFalse(sim.PressTurn(1));
            Assert.AreEqual(Phase.Walking, sim.Phase);
            Assert.AreEqual(0, sim.TurnCount);
            Assert.AreEqual(1, sim.DrainEvents().Count(e => e.Kind == GameEventKind.TurnBlocked));
        }

        [Test]
        public void ANodePickIsIgnoredWhileTurning()
        {
            var sim = new DioramaSimulation(Row("sbbg"), Timing);
            sim.PressTurn(1);
            Assert.IsFalse(sim.PickNode(new Cell(1, 0, 0)));
            Assert.AreEqual(Phase.Turning, sim.Phase);
        }

        [Test]
        public void PipKeepsItsNodeThroughATurn()
        {
            var sim = new DioramaSimulation(Row("sbbg"), Timing);
            sim.PickNode(new Cell(1, 0, 0));
            Run(sim, 0.5f);
            var cell = sim.PipCell;
            sim.PressTurn(-1);
            Run(sim, 1f);
            Assert.AreEqual(cell, sim.PipCell);
        }

        [Test]
        public void ATurnReportsTheJoinsItFormsAndBreaks()
        {
            // Start block and a goal one neighbour step plus one view 0 depth step away.
            var room = new RoomData(3, 2, 2, new[]
            {
                new Piece(new Cell(0, 0, 0), PieceKind.Start),
                new Piece(new Cell(2, 1, 1), PieceKind.Goal),
            });
            var sim = new DioramaSimulation(room, Timing, 1);
            sim.PressTurn(-1); // view 1 to view 0: the join forms
            Run(sim, 1f);
            var formed = sim.DrainEvents().Where(e => e.Kind == GameEventKind.JoinFormed).ToList();
            Assert.AreEqual(1, formed.Count);
            sim.PressTurn(1); // and breaks again
            Run(sim, 1f);
            Assert.AreEqual(1, sim.DrainEvents().Count(e => e.Kind == GameEventKind.JoinBroken));
        }

        [Test]
        public void RestartGoesBackToTheStart()
        {
            var sim = new DioramaSimulation(Row("sbg"), Timing);
            sim.PressTurn(1);
            Run(sim, 1f);
            sim.PickNode(new Cell(1, 0, 0));
            Run(sim, 1f);
            sim.Restart();
            Assert.AreEqual(Phase.Idle, sim.Phase);
            Assert.AreEqual(0, sim.View);
            Assert.AreEqual(0, sim.TurnCount);
            Assert.AreEqual(new Cell(0, 0, 0), sim.PipCell);
            Assert.AreEqual(1, sim.DrainEvents().Count(e => e.Kind == GameEventKind.Restarted));
        }
    }
}
