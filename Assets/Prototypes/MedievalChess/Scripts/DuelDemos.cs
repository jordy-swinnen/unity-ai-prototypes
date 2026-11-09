using UnityEngine;

namespace Prototypes.MedievalChess
{
    /// <summary>
    /// The moves the duel scene can show, each on the little strip of board (x from -3 to 3, z from -1 to 1).
    /// They go through the move director, the same way a real game will.
    /// </summary>
    public static class DuelDemos
    {
        public const int Count = 11;

        public static readonly string[] Names =
        {
            "Bishop walks two squares",
            "Knight leaps",
            "Rook slides",
            "Rook takes pawn (slides up, then fights)",
            "En passant",
            "Castling",
            "Pawn promotes to queen",
            "Pawn takes rook and promotes to knight",
            "Check (the king braces)",
            "Checkmate (the king falls)",
            "Resign",
        };

        static Vector3 Sq(float x, float z) => new Vector3(x, 0f, z);

        public static ActionSequence Create(int index, MedievalChessSettings s)
        {
            var m = s.moves;
            var f = s.fight;
            var d = new MoveDescription { PieceId = "mover", Side = 0, Yaw = 0f };
            switch (index)
            {
                case 0:
                    d.Piece = PieceType.Bishop; d.From = Sq(-3, -1); d.To = Sq(-1, 1);
                    break;
                case 1:
                    d.Piece = PieceType.Knight; d.From = Sq(-2, -1); d.To = Sq(0, 0);
                    break;
                case 2:
                    d.Piece = PieceType.Rook; d.From = Sq(-3, 0); d.To = Sq(2, 0);
                    break;
                case 3:
                    d.Piece = PieceType.Rook; d.From = Sq(-3, 0); d.To = Sq(1, 0);
                    d.Capture = true; d.Victim = PieceType.Pawn; d.VictimAt = Sq(1, 0);
                    break;
                case 4:
                    d.Piece = PieceType.Pawn; d.From = Sq(0, 0); d.To = Sq(1, 1);
                    d.Capture = true; d.EnPassant = true; d.Victim = PieceType.Pawn; d.VictimAt = Sq(1, 0);
                    break;
                case 5:
                    d.Piece = PieceType.King; d.From = Sq(0, 0); d.To = Sq(2, 0);
                    d.Castle = true; d.RookFrom = Sq(3, 0); d.RookTo = Sq(1, 0);
                    break;
                case 6:
                    d.Piece = PieceType.Pawn; d.From = Sq(0, 0); d.To = Sq(0, 1);
                    d.Promotes = true; d.PromoteTo = PieceType.Queen;
                    break;
                case 7:
                    d.Piece = PieceType.Pawn; d.From = Sq(0, 0); d.To = Sq(1, 1);
                    d.Capture = true; d.Victim = PieceType.Rook; d.VictimAt = Sq(1, 1);
                    d.Promotes = true; d.PromoteTo = PieceType.Knight;
                    break;
                case 8:
                    d.Piece = PieceType.Rook; d.From = Sq(-3, -1); d.To = Sq(-3, 1);
                    d.Check = true; d.KingAt = Sq(2, 0);
                    break;
                case 9:
                    d.Piece = PieceType.Queen; d.From = Sq(-3, -1); d.To = Sq(1, 1);
                    d.Check = true; d.Checkmate = true; d.KingAt = Sq(2, 0);
                    break;
                case 10:
                    return MoveDirector.Resign(new ActorSpec { Id = "king", Type = PieceType.King, Side = 0, Start = Sq(0, 0), StartYaw = 0f }, m);
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(index));
            }
            return MoveDirector.Plan(d, m, f);
        }
    }
}
