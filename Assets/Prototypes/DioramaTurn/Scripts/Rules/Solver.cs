using System.Collections.Generic;

namespace Prototypes.DioramaTurn
{
    public enum MoveKind { Turn, Walk }

    public readonly struct SolverMove
    {
        public readonly MoveKind Kind;
        /// <summary>For a turn: +1 or -1.</summary>
        public readonly int Direction;
        /// <summary>For a walk: the node to walk to.</summary>
        public readonly Cell Target;

        SolverMove(MoveKind kind, int direction, Cell target)
        {
            Kind = kind;
            Direction = direction;
            Target = target;
        }

        public static SolverMove Turn(int direction) => new SolverMove(MoveKind.Turn, direction, default);
        public static SolverMove WalkTo(Cell target) => new SolverMove(MoveKind.Walk, 0, target);
    }

    public sealed class SolveResult
    {
        public bool Solved;
        public int MinTurns;
        public List<SolverMove> Moves = new List<SolverMove>();
    }

    /// <summary>
    /// Finds the least number of turns that gets Pip from the Start to the Goal. A state is a node and a
    /// view. Walking over the joins of the current view costs nothing, a quarter turn costs one.
    /// </summary>
    public static class Solver
    {
        public static SolveResult Solve(RoomData room, int startView = 0)
        {
            var graphs = new JoinGraph[ViewRule.ViewCount];
            for (int v = 0; v < ViewRule.ViewCount; v++) graphs[v] = JoinGraph.Build(room, v);

            var dist = new Dictionary<(int, Cell), int>();
            var parent = new Dictionary<(int, Cell), ((int, Cell) From, bool Turn, int Dir)>();
            var deque = new LinkedList<(int View, Cell Cell)>();
            var first = (startView, room.Start);
            dist[first] = 0;
            deque.AddFirst(first);

            while (deque.Count > 0)
            {
                var (view, cell) = deque.First.Value;
                deque.RemoveFirst();
                int d = dist[(view, cell)];

                foreach (var e in graphs[view].Neighbours(cell))
                {
                    var next = (view, e.To);
                    if (dist.TryGetValue(next, out var old) && old <= d) continue;
                    dist[next] = d;
                    parent[next] = ((view, cell), false, 0);
                    deque.AddFirst(next);
                }
                foreach (int dir in new[] { 1, -1 })
                {
                    var next = (ViewRule.Rotate(view, dir), cell);
                    if (dist.TryGetValue(next, out var old) && old <= d + 1) continue;
                    dist[next] = d + 1;
                    parent[next] = ((view, cell), true, dir);
                    deque.AddLast(next);
                }
            }

            var result = new SolveResult();
            int best = int.MaxValue;
            (int, Cell) bestState = default;
            for (int v = 0; v < ViewRule.ViewCount; v++)
            {
                if (dist.TryGetValue((v, room.Goal), out var d) && d < best)
                {
                    best = d;
                    bestState = (v, room.Goal);
                }
            }
            if (best == int.MaxValue) return result;

            result.Solved = true;
            result.MinTurns = best;

            var path = new List<((int, Cell) To, bool Turn, int Dir)>();
            var s = bestState;
            while (!s.Equals(first))
            {
                var p = parent[s];
                path.Add((s, p.Turn, p.Dir));
                s = p.From;
            }
            path.Reverse();

            // Join runs of walking steps into one Walk to the last node of the run.
            Cell? pendingWalk = null;
            foreach (var step in path)
            {
                if (step.Turn)
                {
                    if (pendingWalk.HasValue) { result.Moves.Add(SolverMove.WalkTo(pendingWalk.Value)); pendingWalk = null; }
                    result.Moves.Add(SolverMove.Turn(step.Dir));
                }
                else
                {
                    pendingWalk = step.To.Item2;
                }
            }
            if (pendingWalk.HasValue) result.Moves.Add(SolverMove.WalkTo(pendingWalk.Value));
            return result;
        }
    }
}
