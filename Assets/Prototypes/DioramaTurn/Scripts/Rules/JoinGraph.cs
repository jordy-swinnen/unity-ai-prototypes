using System.Collections.Generic;

namespace Prototypes.DioramaTurn
{
    public readonly struct JoinEdge
    {
        public readonly Cell To;
        /// <summary>True when the edge is a join made by the view (a hop), false for a neighbour.</summary>
        public readonly bool Hop;

        public JoinEdge(Cell to, bool hop)
        {
            To = to;
            Hop = hop;
        }
    }

    /// <summary>Which nodes of a room can be reached from which, in one view.</summary>
    public sealed class JoinGraph
    {
        static readonly IReadOnlyList<JoinEdge> NoEdges = new JoinEdge[0];
        readonly Dictionary<Cell, List<JoinEdge>> edges = new Dictionary<Cell, List<JoinEdge>>();

        public int View { get; }

        JoinGraph(int view) { View = view; }

        public static JoinGraph Build(RoomData room, int view)
        {
            var g = new JoinGraph(view);
            var cells = new List<Cell>();
            foreach (var p in room.Pieces) cells.Add(p.Cell);
            cells.Sort((a, b) => a.CompareTo(b));
            foreach (var c in cells) g.edges[c] = new List<JoinEdge>();
            foreach (var a in cells)
            {
                foreach (var b in cells)
                {
                    var kind = JoinRule.Classify(a, b, view);
                    if (kind != JoinKind.None) g.edges[a].Add(new JoinEdge(b, kind == JoinKind.Screen));
                }
            }
            return g;
        }

        public bool Contains(Cell c) => edges.ContainsKey(c);

        public IReadOnlyList<JoinEdge> Neighbours(Cell c) => edges.TryGetValue(c, out var list) ? list : NoEdges;

        public IEnumerable<Cell> Nodes => edges.Keys;
    }
}
