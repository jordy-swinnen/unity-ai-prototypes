using NUnit.Framework;
using UnityEngine;

namespace Prototypes.PixelInvaders.Tests
{
    public class BunkerGridTests
    {
        [Test]
        public void Arch_mask_initializes_symmetrically_with_hollow_bottom_arch()
        {
            var grid = new BunkerGrid(0f, -2.8f, 12, 8, 0.10f);
            Assert.AreEqual(12, grid.Cols);
            Assert.AreEqual(8, grid.Rows);
            Assert.Greater(grid.IntactCount, 50);
            Assert.AreEqual(grid.InitialIntactCount, grid.IntactCount);

            // Top-left and top-right bevel corners are empty
            Assert.AreEqual(BunkerCellState.Empty, grid.GetCellVisualState(0, 7));
            Assert.AreEqual(BunkerCellState.Empty, grid.GetCellVisualState(11, 7));

            // Bottom-centre arch opening is empty
            Assert.AreEqual(BunkerCellState.Empty, grid.GetCellVisualState(5, 0));
            Assert.AreEqual(BunkerCellState.Empty, grid.GetCellVisualState(6, 0));

            // Solid shoulders are intact and symmetric
            for (int r = 0; r < grid.Rows; r++)
            {
                for (int c = 0; c < grid.Cols; c++)
                {
                    Assert.AreEqual(
                        grid.GetCellVisualState(c, r),
                        grid.GetCellVisualState(grid.Cols - 1 - c, r),
                        $"Bunker must be left-right symmetric at ({c},{r})");
                }
            }
        }

        [Test]
        public void HitAtWorld_erodes_struck_cell_and_marks_adjacent_cells_as_damaged_edge()
        {
            var grid = new BunkerGrid(0f, -2.8f, 12, 8, 0.10f);
            int before = grid.IntactCount;
            Vector2 cellCenter = grid.CellWorldCenter(3, 4);

            bool hit = grid.HitAtWorld(cellCenter.x, cellCenter.y, blastRadiusCells: 1);
            Assert.IsTrue(hit, "Striking an intact cell center must register a hit");
            Assert.Less(grid.IntactCount, before, "Erosion must reduce intact cell count");
            Assert.AreEqual(BunkerCellState.Empty, grid.GetCellVisualState(3, 4), "Struck cell must become empty");

            // At least one surviving cell bordering the blast radius should show DamagedEdge
            bool foundDamagedEdge = false;
            for (int r = 0; r < grid.Rows; r++)
            {
                for (int c = 0; c < grid.Cols; c++)
                {
                    if (grid.GetCellVisualState(c, r) == BunkerCellState.DamagedEdge)
                    {
                        foundDamagedEdge = true;
                        break;
                    }
                }
            }
            Assert.IsTrue(foundDamagedEdge, "Surviving cells next to an eroded hole must report DamagedEdge");
        }

        [Test]
        public void ScrapeBox_removes_overlapping_cells_and_RepairAll_restores_full_arch()
        {
            var grid = new BunkerGrid(0f, -2.8f, 12, 8, 0.10f);
            int fullCount = grid.InitialIntactCount;

            // Scrape across the top two rows of the bunker
            int scraped = grid.ScrapeBox(-0.6f, 0.6f, -2.55f, -2.35f);
            Assert.Greater(scraped, 0, "Scraping the top rows must remove cells");
            Assert.AreEqual(fullCount - scraped, grid.IntactCount);

            grid.RepairAll();
            Assert.AreEqual(fullCount, grid.IntactCount, "RepairAll must restore every original arch cell");
            for (int r = 0; r < grid.Rows; r++)
            {
                for (int c = 0; c < grid.Cols; c++)
                {
                    Assert.AreNotEqual(BunkerCellState.DamagedEdge, grid.GetCellVisualState(c, r),
                        "Repaired bunker must have no DamagedEdge cells");
                }
            }
        }
    }
}
