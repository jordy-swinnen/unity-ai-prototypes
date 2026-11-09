using UnityEngine;

namespace Prototypes.MedievalChess
{
    /// <summary>Turns a rules move into a description the move director can show.</summary>
    public static class MoveDescriber
    {
        /// <summary>
        /// Describes a move that is about to be made in <paramref name="before"/>. It does not change the position
        /// or the tracker. The tracker supplies the id of the piece on each square.
        /// </summary>
        public static MoveDescription Describe(Position before, Move move, BoardTracker tracker)
        {
            int from = move.From, to = move.To;
            int piece = before[from];
            var side = Pieces.SideOf(piece);
            var type = Pieces.TypeOf(piece);

            var d = new MoveDescription
            {
                PieceId = tracker.IdAt[from],
                Piece = type,
                Side = (int)side,
                From = BoardGeometry.Centre(from),
                To = BoardGeometry.Centre(to),
                Yaw = BoardGeometry.RestYaw(side),
            };

            if (move.IsCapture)
            {
                bool enPassant = (move.Flags & MoveFlags.EnPassant) != 0;
                int victimSquare = enPassant ? (side == ChessSide.White ? to - 8 : to + 8) : to;
                d.Capture = true;
                d.EnPassant = enPassant;
                d.VictimId = tracker.IdAt[victimSquare];
                d.Victim = Pieces.TypeOf(before[victimSquare]);
                d.VictimAt = BoardGeometry.Centre(victimSquare);
                d.VictimYaw = BoardGeometry.RestYaw(Pieces.Opposite(side));
            }

            if (move.IsCastle)
            {
                bool kingSide = (move.Flags & MoveFlags.CastleKing) != 0;
                int rookFrom = kingSide ? from + 3 : from - 4;
                int rookTo = kingSide ? from + 1 : from - 1;
                d.Castle = true;
                d.RookId = tracker.IdAt[rookFrom];
                d.RookFrom = BoardGeometry.Centre(rookFrom);
                d.RookTo = BoardGeometry.Centre(rookTo);
            }

            if (move.Promotion.HasValue)
            {
                d.Promotes = true;
                d.PromoteTo = move.Promotion.Value;
                d.PromotedId = tracker.NewPromotedId(d.PieceId);
            }

            // Does the move give check or mate? Look on a copy.
            var after = before.Clone();
            after.MakeMove(move);
            if (after.InCheck())
            {
                var enemy = after.SideToMove;
                int kingSquare = after.KingSquare(enemy);
                d.Check = true;
                d.Checkmate = MoveGenerator.Legal(after).Count == 0;
                d.KingId = tracker.IdAt[kingSquare];
                d.KingType = PieceType.King;
                d.KingAt = BoardGeometry.Centre(kingSquare);
                d.KingYaw = BoardGeometry.RestYaw(enemy);
                d.KingSide = (int)enemy;
            }
            return d;
        }
    }
}
