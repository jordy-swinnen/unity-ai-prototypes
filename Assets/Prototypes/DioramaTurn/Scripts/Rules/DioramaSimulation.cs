using System.Collections.Generic;

namespace Prototypes.DioramaTurn
{
    public enum Phase { Idle, Walking, Turning, Complete }

    public readonly struct SimulationTiming
    {
        public readonly float TurnDuration;
        public readonly float StepTime;
        public readonly float HopTime;
        public readonly int QueueMax;

        public SimulationTiming(float turnDuration, float stepTime, float hopTime, int queueMax)
        {
            TurnDuration = turnDuration;
            StepTime = stepTime;
            HopTime = hopTime;
            QueueMax = queueMax;
        }
    }

    /// <summary>
    /// The state machine of one room. Plain C#: no engine objects, so tests step it with numbers. It
    /// reports what happened as events, and the scene only shows them.
    /// </summary>
    public sealed class DioramaSimulation
    {
        readonly RoomData room;
        readonly SimulationTiming timing;
        readonly int startView;
        readonly List<GameEvent> events = new List<GameEvent>();
        readonly Queue<int> queuedTurns = new Queue<int>();
        readonly JoinGraph[] graphs = new JoinGraph[ViewRule.ViewCount];

        List<RouteStep> route;
        int routeIndex;
        float stepTimer;
        float turnTimer;

        public Phase Phase { get; private set; }
        /// <summary>The view the joins are read from. It changes when a turn settles.</summary>
        public int View { get; private set; }
        public int TargetView { get; private set; }
        public Cell PipCell { get; private set; }
        public int TurnCount { get; private set; }
        public int QueuedTurns => queuedTurns.Count;
        public RoomData Room => room;
        public JoinGraph Graph => graphs[View];

        /// <summary>0 to 1 through the current turn.</summary>
        public float TurnProgress => Phase == Phase.Turning && timing.TurnDuration > 0f ? Clamp01(turnTimer / timing.TurnDuration) : 0f;
        public Cell WalkFrom { get; private set; }
        public Cell WalkTo { get; private set; }
        public bool StepIsHop { get; private set; }
        /// <summary>0 to 1 through the current step.</summary>
        public float StepProgress => Phase == Phase.Walking ? Clamp01(stepTimer / StepDuration) : 0f;

        float StepDuration => StepIsHop ? timing.HopTime : timing.StepTime;

        public DioramaSimulation(RoomData room, SimulationTiming timing, int startView = 0)
        {
            this.room = room;
            this.timing = timing;
            this.startView = ViewRule.Wrap(startView);
            for (int v = 0; v < ViewRule.ViewCount; v++) graphs[v] = JoinGraph.Build(room, v);
            Reset();
        }

        void Reset()
        {
            Phase = Phase.Idle;
            View = startView;
            TargetView = startView;
            PipCell = room.Start;
            TurnCount = 0;
            route = null;
            queuedTurns.Clear();
            turnTimer = 0f;
            stepTimer = 0f;
        }

        public List<GameEvent> DrainEvents()
        {
            var copy = new List<GameEvent>(events);
            events.Clear();
            return copy;
        }

        public void Restart()
        {
            Reset();
            events.Add(new GameEvent(GameEventKind.Restarted, PipCell, PipCell, View));
        }

        /// <summary>A quarter turn: +1 or -1. Returns true if it started or was queued.</summary>
        public bool PressTurn(int direction)
        {
            if (Phase == Phase.Complete) return false;
            if (Phase == Phase.Walking)
            {
                events.Add(new GameEvent(GameEventKind.TurnBlocked, PipCell, PipCell, View));
                return false;
            }
            if (Phase == Phase.Turning)
            {
                if (queuedTurns.Count >= timing.QueueMax) return false;
                queuedTurns.Enqueue(direction >= 0 ? 1 : -1);
                return true;
            }
            StartTurn(direction >= 0 ? 1 : -1);
            return true;
        }

        void StartTurn(int direction)
        {
            Phase = Phase.Turning;
            TargetView = ViewRule.Rotate(View, direction);
            TurnCount++;
            turnTimer = 0f;
            events.Add(new GameEvent(GameEventKind.TurnStarted, PipCell, PipCell, TargetView));
        }

        /// <summary>Pick a node to walk to. Returns true if Pip starts walking.</summary>
        public bool PickNode(Cell target)
        {
            if (Phase != Phase.Idle) return false;
            if (target == PipCell) return false;
            var plan = room.Contains(target) ? RoutePlanner.Plan(Graph, PipCell, target) : null;
            if (plan == null || plan.Count == 0)
            {
                events.Add(new GameEvent(GameEventKind.Unreachable, target, PipCell, View));
                return false;
            }
            route = plan;
            routeIndex = 0;
            BeginStep();
            Phase = Phase.Walking;
            return true;
        }

        void BeginStep()
        {
            var step = route[routeIndex];
            WalkFrom = PipCell;
            WalkTo = step.To;
            StepIsHop = step.Hop;
            stepTimer = 0f;
        }

        public void Tick(float dt)
        {
            int guard = 0;
            while (dt > 0f && guard++ < 64)
            {
                if (Phase == Phase.Turning)
                {
                    float left = timing.TurnDuration - turnTimer;
                    if (dt < left) { turnTimer += dt; return; }
                    dt -= left;
                    SettleTurn();
                }
                else if (Phase == Phase.Walking)
                {
                    float left = StepDuration - stepTimer;
                    if (dt < left) { stepTimer += dt; return; }
                    dt -= left;
                    FinishStep();
                }
                else
                {
                    return;
                }
            }
        }

        void SettleTurn()
        {
            int from = View;
            View = TargetView;
            foreach (var pair in ScreenJoins(from))
            {
                if (!JoinRule.AreJoined(pair.A, pair.B, View))
                {
                    events.Add(new GameEvent(GameEventKind.JoinBroken, pair.A, pair.B, View));
                }
            }
            foreach (var pair in ScreenJoins(View))
            {
                if (!JoinRule.AreJoined(pair.A, pair.B, from))
                {
                    events.Add(new GameEvent(GameEventKind.JoinFormed, pair.A, pair.B, View));
                }
            }
            events.Add(new GameEvent(GameEventKind.TurnSettled, PipCell, PipCell, View));
            Phase = Phase.Idle;
            if (queuedTurns.Count > 0) StartTurn(queuedTurns.Dequeue());
        }

        void FinishStep()
        {
            var from = PipCell;
            PipCell = WalkTo;
            events.Add(new GameEvent(StepIsHop ? GameEventKind.HopTaken : GameEventKind.StepTaken, PipCell, from, View));
            routeIndex++;
            if (PipCell == room.Goal)
            {
                route = null;
                Phase = Phase.Complete;
                events.Add(new GameEvent(GameEventKind.GoalReached, PipCell, PipCell, View));
            }
            else if (routeIndex < route.Count)
            {
                BeginStep();
            }
            else
            {
                route = null;
                Phase = Phase.Idle;
            }
        }

        /// <summary>The pairs of nodes joined by the view itself (not plain neighbours), each pair once.</summary>
        public IEnumerable<(Cell A, Cell B)> ScreenJoins(int view)
        {
            var g = graphs[view];
            foreach (var a in g.Nodes)
            {
                foreach (var e in g.Neighbours(a))
                {
                    if (e.Hop && a.CompareTo(e.To) < 0) yield return (a, e.To);
                }
            }
        }

        static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
