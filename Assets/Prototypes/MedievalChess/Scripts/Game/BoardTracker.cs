using System.Collections.Generic;
using UnityEngine;

namespace Prototypes.MedievalChess
{
    /// <summary>
    /// Follows the pieces on the board. Every piece has an id (the name of the square it started on), and the
    /// tracker knows which id is on each square and where each piece stands, whether it is on the board, and how it
    /// lies. It turns each rules move into the animated steps, and the pieces it shows after a move must be the
    /// pieces the rules say are there (<see cref="Matches"/>).
    /// </summary>
    public sealed class BoardTracker
    {
        readonly Dictionary<string, ActorState> actors = new Dictionary<string, ActorState>();
        readonly List<ActorState> captured = new List<ActorState>();
        readonly string[] idAt = new string[64];
        int promotions;

        /// <summary>The id of the piece on each square, or null.</summary>
        public string[] IdAt => idAt;
        public IReadOnlyDictionary<string, ActorState> Actors => actors;
        /// <summary>The pieces that have been taken, in order, as they lay when they were taken.</summary>
        public IReadOnlyList<ActorState> Captured => captured;
        /// <summary>The description of the last move applied.</summary>
        public MoveDescription LastDescription { get; private set; }

        public int VisibleCount
        {
            get
            {
                int n = 0;
                foreach (var a in actors.Values) if (a.Visible) n++;
                return n;
            }
        }

        public BoardTracker(Position position)
        {
            for (int sq = 0; sq < 64; sq++)
            {
                int piece = position[sq];
                if (piece == Pieces.Empty) continue;
                var side = Pieces.SideOf(piece);
                string id = Squares.Name(sq);
                idAt[sq] = id;
                actors[id] = new ActorState
                {
                    Id = id, Type = Pieces.TypeOf(piece), Side = (int)side,
                    Position = BoardGeometry.Centre(sq), Yaw = BoardGeometry.RestYaw(side), Visible = true,
                };
            }
        }

        internal string NewPromotedId(string pawnId) => $"{pawnId}={++promotions}";

        /// <summary>
        /// Works out the animated steps for a move that is about to be made (or has just been made) in
        /// <paramref name="before"/>, and updates the tracker to where the pieces will stand when they finish.
        /// </summary>
        public ActionSequence Apply(Position before, Move move, MoveSettings moves, FightSettings fights)
        {
            var d = MoveDescriber.Describe(before, move, this);
            LastDescription = d;
            var sequence = MoveDirector.Plan(d, moves, fights);

            // Who stands where now: take the end of the sequence.
            foreach (var state in sequence.End.Values) actors[state.Id] = state.Clone();
            if (d.Capture) captured.Add(actors[d.VictimId].Clone());

            // The squares: the piece leaves its square, the victim's square is cleared, the piece arrives.
            int from = move.From, to = move.To;
            string mover = idAt[from];
            idAt[from] = null;
            if (d.Capture)
            {
                int victimSquare = d.EnPassant ? (Pieces.SideOf(before[from]) == ChessSide.White ? to - 8 : to + 8) : to;
                idAt[victimSquare] = null;
            }
            if (d.Castle)
            {
                bool kingSide = (move.Flags & MoveFlags.CastleKing) != 0;
                int rookFrom = kingSide ? from + 3 : from - 4;
                int rookTo = kingSide ? from + 1 : from - 1;
                idAt[rookTo] = idAt[rookFrom];
                idAt[rookFrom] = null;
            }
            idAt[to] = d.Promotes ? d.PromotedId : mover;
            return sequence;
        }

        /// <summary>
        /// True when the pieces shown are exactly the pieces of the position: each piece of the position has an id,
        /// on its square, of the right kind and colour, and nothing else is on the board.
        /// </summary>
        public bool Matches(Position position, out string problem)
        {
            problem = null;
            int expected = 0;
            for (int sq = 0; sq < 64; sq++)
            {
                int piece = position[sq];
                string id = idAt[sq];
                if (piece == Pieces.Empty)
                {
                    if (id != null && actors[id].Visible) { problem = $"{Squares.Name(sq)} should be empty but shows {id}"; return false; }
                    continue;
                }
                expected++;
                if (id == null) { problem = $"{Squares.Name(sq)} should hold {Pieces.ToChar(piece)} but nothing is there"; return false; }
                var a = actors[id];
                if (!a.Visible) { problem = $"{id} on {Squares.Name(sq)} is not on the board"; return false; }
                if (a.Type != Pieces.TypeOf(piece) || a.Side != (int)Pieces.SideOf(piece))
                { problem = $"{Squares.Name(sq)} should hold {Pieces.ToChar(piece)} but shows a {(a.Side == 0 ? "bone" : "cinnabar")} {a.Type} ({id})"; return false; }
                var want = BoardGeometry.Centre(sq);
                if (Mathf.Abs(a.Position.x - want.x) > 0.01f || Mathf.Abs(a.Position.z - want.z) > 0.01f || Mathf.Abs(a.Position.y) > 0.01f)
                { problem = $"{id} is at {a.Position} but {Squares.Name(sq)} is at {want}"; return false; }
            }
            if (VisibleCount != expected) { problem = $"{VisibleCount} pieces are shown but the position has {expected}"; return false; }
            return true;
        }
    }
}
