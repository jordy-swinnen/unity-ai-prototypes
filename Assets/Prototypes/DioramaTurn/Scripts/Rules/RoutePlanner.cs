using System.Collections.Generic;

namespace Prototypes.DioramaTurn
{
    public readonly struct RouteStep
    {
        public readonly Cell To;
        public readonly bool Hop;

        public RouteStep(Cell to, bool hop)
        {
            To = to;
            Hop = hop;
        }
    }

    public static class RoutePlanner
    {
        /// <summary>The shortest route over the joins of a view (breadth-first). Null if there is none, empty if already there.</summary>
        public static List<RouteStep> Plan(JoinGraph graph, Cell from, Cell to)
        {
            if (!graph.Contains(from) || !graph.Contains(to)) return null;
            if (from == to) return new List<RouteStep>();

            var parent = new Dictionary<Cell, (Cell From, bool Hop)>();
            var queue = new Queue<Cell>();
            queue.Enqueue(from);
            parent[from] = (from, false);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                foreach (var e in graph.Neighbours(c))
                {
                    if (parent.ContainsKey(e.To)) continue;
                    parent[e.To] = (c, e.Hop);
                    if (e.To == to) return Build(parent, from, to);
                    queue.Enqueue(e.To);
                }
            }
            return null;
        }

        static List<RouteStep> Build(Dictionary<Cell, (Cell From, bool Hop)> parent, Cell from, Cell to)
        {
            var steps = new List<RouteStep>();
            var c = to;
            while (c != from)
            {
                var p = parent[c];
                steps.Add(new RouteStep(c, p.Hop));
                c = p.From;
            }
            steps.Reverse();
            return steps;
        }
    }
}
