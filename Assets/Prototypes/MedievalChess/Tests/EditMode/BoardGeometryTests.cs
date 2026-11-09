using NUnit.Framework;
using UnityEngine;

namespace Prototypes.MedievalChess.Tests
{
    public class BoardGeometryTests
    {
        [Test]
        public void Each_square_has_a_centre_one_metre_from_its_neighbours_and_the_board_is_centred()
        {
            Assert.AreEqual(new Vector3(-3.5f, 0f, -3.5f), BoardGeometry.Centre(Squares.Parse("a1")));
            Assert.AreEqual(new Vector3(3.5f, 0f, 3.5f), BoardGeometry.Centre(Squares.Parse("h8")));
            Assert.AreEqual(1f, BoardGeometry.Centre(1).x - BoardGeometry.Centre(0).x);
            Assert.AreEqual(1f, BoardGeometry.Centre(8).z - BoardGeometry.Centre(0).z);
        }

        [Test]
        public void A_point_anywhere_on_a_square_gives_that_square()
        {
            for (int sq = 0; sq < 64; sq++)
            {
                var c = BoardGeometry.Centre(sq);
                Assert.AreEqual(sq, BoardGeometry.SquareAt(c));
                Assert.AreEqual(sq, BoardGeometry.SquareAt(c + new Vector3(0.45f, 0f, -0.45f)));
                Assert.AreEqual(sq, BoardGeometry.SquareAt(c + new Vector3(-0.45f, 3f, 0.45f)), "height does not matter");
            }
        }

        [Test]
        public void A_point_off_the_board_gives_no_square()
        {
            Assert.AreEqual(-1, BoardGeometry.SquareAt(new Vector3(4.1f, 0f, 0f)));
            Assert.AreEqual(-1, BoardGeometry.SquareAt(new Vector3(0f, 0f, -4.1f)));
            Assert.AreEqual(-1, BoardGeometry.SquareAt(new Vector3(-9f, 0f, 9f)));
        }

        [Test]
        public void White_faces_forward_and_black_faces_back()
        {
            Assert.AreEqual(0f, BoardGeometry.RestYaw(ChessSide.White));
            Assert.AreEqual(180f, BoardGeometry.RestYaw(ChessSide.Black));
        }

        [Test]
        public void Tray_slots_are_beside_the_board_in_rows_and_do_not_overlap()
        {
            var seen = new System.Collections.Generic.HashSet<Vector3>();
            for (int i = 0; i < 16; i++)
            {
                var slot = BoardGeometry.TraySlot(ChessSide.White, i, 6f);
                Assert.IsTrue(slot.x < -4.5f, "the bone side's captured pieces stand on the left");
                Assert.IsTrue(slot.z >= -4f && slot.z <= 4f, $"slot {i} is off the tray");
                Assert.IsTrue(seen.Add(slot), $"slot {i} is taken");
                Assert.IsTrue(BoardGeometry.TraySlot(ChessSide.Black, i, 6f).x > 4.5f);
            }
        }
    }
}
