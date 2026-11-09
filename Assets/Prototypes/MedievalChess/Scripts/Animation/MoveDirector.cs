using UnityEngine;

namespace Prototypes.MedievalChess
{
    /// <summary>A move, described in world positions, for the director to show. The rules fill this in.</summary>
    public sealed class MoveDescription
    {
        public string PieceId = "mover";
        public PieceType Piece;
        /// <summary>0 for the bone side, 1 for the cinnabar side.</summary>
        public int Side;
        public Vector3 From;
        public Vector3 To;
        /// <summary>The heading the piece rests at (bone faces +z, cinnabar faces -z).</summary>
        public float Yaw;

        public bool Capture;
        public bool EnPassant;
        public string VictimId = "victim";
        public PieceType Victim;
        /// <summary>Where the captured piece stands. It is not the landing square for en passant.</summary>
        public Vector3 VictimAt;
        public float VictimYaw = 180f;

        public bool Castle;
        public string RookId = "rook";
        public Vector3 RookFrom;
        public Vector3 RookTo;

        public bool Promotes;
        public PieceType PromoteTo;
        public string PromotedId = "promoted";

        /// <summary>The move gives check (or mate) to the enemy king.</summary>
        public bool Check;
        public bool Checkmate;
        public string KingId = "king";
        public PieceType KingType = PieceType.King;
        public Vector3 KingAt;
        public float KingYaw = 180f;
        public int KingSide = 1;
    }

    /// <summary>
    /// Turns a described move into the steps the player sees: the piece travels, fights if it captures,
    /// steps on to an en passant square, castles, promotes, and the enemy king braces or falls.
    /// Each step starts where the last one left the pieces, and the sequence says where everything ends.
    /// </summary>
    public static class MoveDirector
    {
        public static ActionSequence Plan(MoveDescription d, MoveSettings m, FightSettings f)
        {
            var seq = new ActionSequence();
            var mover = new ActorSpec { Id = d.PieceId, Type = d.Piece, Side = d.Side, Start = d.From, StartYaw = d.Yaw };

            if (d.Castle)
            {
                var rook = new ActorSpec { Id = d.RookId, Type = PieceType.Rook, Side = d.Side, Start = d.RookFrom, StartYaw = d.Yaw };
                seq.Add(MoveLibrary.Castle(mover, d.To, rook, d.RookTo, m));
            }
            else if (d.Capture)
            {
                AddCapture(seq, d, mover, m, f);
            }
            else
            {
                seq.Add(MoveLibrary.Travel(mover, d.To, m));
            }

            if (d.Promotes)
            {
                var pawn = Spec(seq, d.PieceId);
                seq.Add(MoveLibrary.Promote(pawn, d.PromoteTo, d.PromotedId, m));
            }

            if (d.Check)
            {
                var king = new ActorSpec { Id = d.KingId, Type = d.KingType, Side = d.KingSide, Start = d.KingAt, StartYaw = d.KingYaw };
                seq.Add(d.Checkmate ? MoveLibrary.Fall(king, m) : MoveLibrary.Brace(king, m));
            }
            return seq;
        }

        public static ActionSequence Resign(ActorSpec king, MoveSettings m)
        {
            var seq = new ActionSequence();
            seq.Add(MoveLibrary.Resign(king, m));
            return seq;
        }

        static void AddCapture(ActionSequence seq, MoveDescription d, ActorSpec mover, MoveSettings m, FightSettings f)
        {
            var flat = d.VictimAt - d.From;
            flat.y = 0f;
            float distance = flat.magnitude;
            var dir = distance > 1e-4f ? flat / distance : Vector3.forward;
            float standOff = d.Piece == PieceType.Knight ? f.standOff + f.knightExtraStandOff : f.standOff;

            // A piece that is far from its victim travels up to a fighting distance first.
            var attacker = mover;
            if (distance > standOff + 0.05f)
            {
                var approach = d.VictimAt - dir * standOff;
                seq.Add(MoveLibrary.Travel(mover, approach, m));
                attacker = Spec(seq, d.PieceId);
            }

            seq.Add(FightLibrary.Create(d.Piece, d.Victim, attacker.Start, d.VictimAt, attacker.StartYaw, d.VictimYaw, f,
                d.PieceId, d.VictimId, d.Side, 1 - d.Side));

            // En passant: the pawn then steps on to the empty square behind the victim.
            var landed = Spec(seq, d.PieceId);
            var rest = new Vector3(d.To.x - landed.Start.x, 0f, d.To.z - landed.Start.z);
            if (rest.magnitude > 0.05f) seq.Add(MoveLibrary.Walk(landed, d.To, m));
        }

        // The state of a piece after the steps so far, as the start of its next step.
        static ActorSpec Spec(ActionSequence seq, string id)
        {
            var s = seq.Current(id);
            return new ActorSpec { Id = s.Id, Type = s.Type, Side = s.Side, Start = s.Position, StartYaw = s.Yaw };
        }
    }
}
