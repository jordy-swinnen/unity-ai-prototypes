using UnityEngine;

namespace Prototypes.MedievalChess
{
    /// <summary>
    /// Where the squares are in the world. The board top is the plane y = 0 and is centred on the origin.
    /// Files a to h run along +x and ranks 1 to 8 run along +z, so a1 is at (-3.5, 0, -3.5).
    /// </summary>
    public static class BoardGeometry
    {
        public static Vector3 Centre(int square) =>
            new Vector3(Squares.FileOf(square) - 3.5f, 0f, Squares.RankOf(square) - 3.5f);

        /// <summary>The square under a point (the height is ignored), or -1 if the point is off the board.</summary>
        public static int SquareAt(Vector3 point)
        {
            int file = Mathf.FloorToInt(point.x + 4f);
            int rank = Mathf.FloorToInt(point.z + 4f);
            if (file < 0 || file > 7 || rank < 0 || rank > 7) return -1;
            return Squares.Make(file, rank);
        }

        /// <summary>The heading a side's pieces rest at: bone faces +z, cinnabar faces -z.</summary>
        public static float RestYaw(ChessSide side) => side == ChessSide.White ? 0f : 180f;

        /// <summary>
        /// Where a captured piece stands: the bone side's on a tray left of the board, the cinnabar side's on the right,
        /// two to a row.
        /// </summary>
        public static Vector3 TraySlot(ChessSide capturedSide, int index, float trayX)
        {
            float side = capturedSide == ChessSide.White ? -1f : 1f;
            float column = (index % 2 == 0) ? -0.35f : 0.35f;
            return new Vector3(side * trayX + column, 0f, -3.5f + (index / 2) * 0.9f);
        }
    }
}
