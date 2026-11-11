using UnityEngine;

namespace Prototypes.PixelInvaders
{
    public enum BunkerCellState
    {
        Empty = 0,
        Intact = 1,
        DamagedEdge = 2,
    }

    /// <summary>
    /// Deterministic 12x8 pixel-art bunker grid. Top rows have bevelled corners, bottom-centre has an arch cutout.
    /// Cells erode when struck by player/alien projectiles or scraped by low-marching invaders.
    /// </summary>
    public sealed class BunkerGrid
    {
        // Row 7 is the top of the bunker, row 0 is the bottom feet.
        static readonly string[] DefaultArchRowsBottomToTop =
        {
            "####....####", // row 0 (bottom feet)
            "####....####", // row 1
            "#####..#####", // row 2 (arch inner shoulders)
            "############", // row 3
            "############", // row 4
            "############", // row 5
            ".##########.", // row 6 (bevel)
            "..########..", // row 7 (top bevel)
        };

        readonly bool[,] initialMask;
        readonly bool[,] currentMask;

        public float CenterX { get; }
        public float CenterZ { get; }
        public int Cols { get; }
        public int Rows { get; }
        public float CellSize { get; }
        public int InitialIntactCount { get; private set; }
        public int IntactCount { get; private set; }
        public int Version { get; private set; }

        public float Width => Cols * CellSize;
        public float Height => Rows * CellSize;
        public float MinX => CenterX - Width * 0.5f;
        public float MaxX => CenterX + Width * 0.5f;
        public float MinZ => CenterZ - Height * 0.5f;
        public float MaxZ => CenterZ + Height * 0.5f;

        public BunkerGrid(float centerX, float centerZ, int cols = 12, int rows = 8, float cellSize = 0.10f)
        {
            CenterX = centerX;
            CenterZ = centerZ;
            Cols = cols;
            Rows = rows;
            CellSize = cellSize;
            initialMask = new bool[cols, rows];
            currentMask = new bool[cols, rows];
            BuildInitialMask();
            ResetToArch();
        }

        void BuildInitialMask()
        {
            InitialIntactCount = 0;
            for (int r = 0; r < Rows; r++)
            {
                string rowPattern = r < DefaultArchRowsBottomToTop.Length
                    ? DefaultArchRowsBottomToTop[r]
                    : DefaultArchRowsBottomToTop[DefaultArchRowsBottomToTop.Length - 1];
                for (int c = 0; c < Cols; c++)
                {
                    char ch = c < rowPattern.Length ? rowPattern[c] : '#';
                    bool solid = ch == '#';
                    initialMask[c, r] = solid;
                    if (solid) InitialIntactCount++;
                }
            }
        }

        public void ResetToArch()
        {
            IntactCount = 0;
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    currentMask[c, r] = initialMask[c, r];
                    if (currentMask[c, r]) IntactCount++;
                }
            }
            Version++;
        }

        public void RepairAll() => ResetToArch();

        public void ClearAll()
        {
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    currentMask[c, r] = false;
            IntactCount = 0;
            Version++;
        }

        public Vector2 CellWorldCenter(int col, int row)
        {
            return new Vector2(
                MinX + (col + 0.5f) * CellSize,
                MinZ + (row + 0.5f) * CellSize);
        }

        public bool HitAtWorld(float worldX, float worldZ, int blastRadiusCells)
        {
            if (IntactCount == 0) return false;
            if (worldX < MinX - CellSize || worldX > MaxX + CellSize ||
                worldZ < MinZ - CellSize || worldZ > MaxZ + CellSize)
            {
                return false;
            }

            int hitCol = Mathf.Clamp(Mathf.FloorToInt((worldX - MinX) / CellSize), 0, Cols - 1);
            int hitRow = Mathf.Clamp(Mathf.FloorToInt((worldZ - MinZ) / CellSize), 0, Rows - 1);

            // If the exact cell is already empty, check direct 1-cell neighbours along the shot path
            if (!currentMask[hitCol, hitRow])
            {
                bool foundNeighbour = false;
                for (int dr = -1; dr <= 1 && !foundNeighbour; dr++)
                {
                    for (int dc = -1; dc <= 1 && !foundNeighbour; dc++)
                    {
                        int nc = hitCol + dc;
                        int nr = hitRow + dr;
                        if (nc >= 0 && nc < Cols && nr >= 0 && nr < Rows && currentMask[nc, nr])
                        {
                            Vector2 center = CellWorldCenter(nc, nr);
                            if (Mathf.Abs(worldX - center.x) <= CellSize * 0.85f &&
                                Mathf.Abs(worldZ - center.y) <= CellSize * 0.85f)
                            {
                                hitCol = nc;
                                hitRow = nr;
                                foundNeighbour = true;
                            }
                        }
                    }
                }
                if (!foundNeighbour) return false;
            }

            ErodeAround(hitCol, hitRow, blastRadiusCells);
            return true;
        }

        void ErodeAround(int centerCol, int centerRow, int radius)
        {
            int removed = 0;
            for (int r = centerRow - radius; r <= centerRow + radius; r++)
            {
                for (int c = centerCol - radius; c <= centerCol + radius; c++)
                {
                    if (c < 0 || c >= Cols || r < 0 || r >= Rows) continue;
                    // Manhattan distance <= radius + 0 (cross/diamond blast) keeps crisp pixel chunks
                    int manhattan = Mathf.Abs(c - centerCol) + Mathf.Abs(r - centerRow);
                    if (manhattan <= radius && currentMask[c, r])
                    {
                        currentMask[c, r] = false;
                        removed++;
                    }
                }
            }
            if (removed > 0)
            {
                IntactCount -= removed;
                Version++;
            }
        }

        public int ScrapeBox(float boxMinX, float boxMaxX, float boxMinZ, float boxMaxZ)
        {
            if (IntactCount == 0) return 0;
            if (boxMaxX < MinX || boxMinX > MaxX || boxMaxZ < MinZ || boxMinZ > MaxZ) return 0;

            int removed = 0;
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    if (!currentMask[c, r]) continue;
                    Vector2 cell = CellWorldCenter(c, r);
                    if (cell.x >= boxMinX && cell.x <= boxMaxX && cell.y >= boxMinZ && cell.y <= boxMaxZ)
                    {
                        currentMask[c, r] = false;
                        removed++;
                    }
                }
            }
            if (removed > 0)
            {
                IntactCount -= removed;
                Version++;
            }
            return removed;
        }

        public BunkerCellState GetCellVisualState(int col, int row)
        {
            if (col < 0 || col >= Cols || row < 0 || row >= Rows) return BunkerCellState.Empty;
            if (!currentMask[col, row]) return BunkerCellState.Empty;

            // If any orthogonal neighbour was originally solid in the arch and is now eroded, show DamagedEdge
            if (WasEroded(col - 1, row) || WasEroded(col + 1, row) ||
                WasEroded(col, row - 1) || WasEroded(col, row + 1))
            {
                return BunkerCellState.DamagedEdge;
            }
            return BunkerCellState.Intact;
        }

        bool WasEroded(int col, int row)
        {
            if (col < 0 || col >= Cols || row < 0 || row >= Rows) return false;
            return initialMask[col, row] && !currentMask[col, row];
        }
    }
}
