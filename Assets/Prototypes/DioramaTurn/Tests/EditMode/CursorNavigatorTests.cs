using System.Collections.Generic;
using NUnit.Framework;

namespace Prototypes.DioramaTurn.Tests
{
    public sealed class CursorNavigatorTests
    {
        [Test]
        public void RightPicksTheNearestNodeToTheRightOnScreen()
        {
            var from = new Cell(2, 0, 2);
            var near = new Cell(2, 0, 3);   // view 0: one step right and down
            var far = new Cell(2, 0, 4);    // the same direction, further
            var other = new Cell(3, 0, 2);  // left and down
            var pick = CursorNavigator.Move(0, from, ScreenDirection.Right, new[] { far, other, near });
            Assert.AreEqual(near, pick);
        }

        [Test]
        public void LeftPicksTheNodeOnTheLeft()
        {
            var from = new Cell(2, 0, 2);
            var left = new Cell(3, 0, 2);
            var right = new Cell(2, 0, 3);
            Assert.AreEqual(left, CursorNavigator.Move(0, from, ScreenDirection.Left, new[] { right, left }));
        }

        [Test]
        public void UpPicksTheNodeAbove()
        {
            var from = new Cell(2, 0, 2);
            var above = new Cell(2, 1, 2);
            var below = new Cell(2, -1, 2);
            Assert.AreEqual(above, CursorNavigator.Move(0, from, ScreenDirection.Up, new[] { below, above }));
            Assert.AreEqual(below, CursorNavigator.Move(0, from, ScreenDirection.Down, new[] { below, above }));
        }

        [Test]
        public void ReturnsNullWhenNothingLiesThatWay()
        {
            var from = new Cell(2, 0, 2);
            Assert.IsNull(CursorNavigator.Move(0, from, ScreenDirection.Up, new[] { new Cell(2, 0, 3) }));
            Assert.IsNull(CursorNavigator.Move(0, from, ScreenDirection.Right, new List<Cell>()));
        }

        [Test]
        public void TheSameCellIsNeverPicked()
        {
            var from = new Cell(2, 0, 2);
            Assert.IsNull(CursorNavigator.Move(0, from, ScreenDirection.Right, new[] { from }));
        }

        [Test]
        public void TheScreenDirectionDependsOnTheView()
        {
            var from = new Cell(2, 0, 2);
            var c = new Cell(2, 0, 3);
            Assert.AreEqual(c, CursorNavigator.Move(0, from, ScreenDirection.Right, new[] { c }));
            Assert.IsNull(CursorNavigator.Move(2, from, ScreenDirection.Right, new[] { c }));
        }
    }
}
