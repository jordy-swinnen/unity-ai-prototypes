using NUnit.Framework;

namespace Prototypes.DioramaTurn.Tests
{
    public sealed class JoinRuleTests
    {
        [Test]
        public void DepthStepsAreTheFourIsometricDirections()
        {
            Assert.AreEqual(new Cell(1, 1, 1), ViewRule.DepthStep(0));
            Assert.AreEqual(new Cell(-1, 1, 1), ViewRule.DepthStep(1));
            Assert.AreEqual(new Cell(-1, 1, -1), ViewRule.DepthStep(2));
            Assert.AreEqual(new Cell(1, 1, -1), ViewRule.DepthStep(3));
        }

        [Test]
        public void NeighboursAreJoinedInEveryView()
        {
            var a = new Cell(2, 1, 2);
            foreach (var n in ViewRule.HorizontalSteps)
            {
                for (int v = 0; v < ViewRule.ViewCount; v++)
                {
                    Assert.AreEqual(JoinKind.Adjacent, JoinRule.Classify(a, a + n, v), $"{n} view {v}");
                    Assert.IsTrue(JoinRule.AreJoined(a, a + n, v));
                }
            }
        }

        [Test]
        public void ADepthShiftedNeighbourIsJoinedOnlyInItsOwnView()
        {
            var a = new Cell(1, 0, 1);
            for (int v = 0; v < ViewRule.ViewCount; v++)
            {
                var r = ViewRule.DepthStep(v);
                foreach (int t in new[] { -2, -1, 1, 2 })
                {
                    foreach (var n in ViewRule.HorizontalSteps)
                    {
                        var b = a + n + r * t;
                        Assert.AreEqual(JoinKind.Screen, JoinRule.Classify(a, b, v), $"view {v} n {n} t {t}");
                        Assert.IsTrue(JoinRule.AreJoined(b, a, v), "joins are two-way");
                    }
                }
            }
        }

        [Test]
        public void ATableOfExamples()
        {
            var o = new Cell(0, 0, 0);
            // (1,0,0) + (1,1,1) in view 0, not in view 1.
            Assert.IsTrue(JoinRule.AreJoined(o, new Cell(2, 1, 1), 0));
            Assert.IsFalse(JoinRule.AreJoined(o, new Cell(2, 1, 1), 1));
            // (1,0,0) - (1,1,1) = (0,-1,-1) in view 0.
            Assert.IsTrue(JoinRule.AreJoined(o, new Cell(0, -1, -1), 0));
            // Two cells apart on one axis is never joined.
            for (int v = 0; v < 4; v++)
            {
                Assert.IsFalse(JoinRule.AreJoined(o, new Cell(2, 0, 0), v));
                Assert.IsFalse(JoinRule.AreJoined(o, new Cell(0, 1, 0), v), "straight above is not a join");
                Assert.IsFalse(JoinRule.AreJoined(o, o, v), "a cell is not joined to itself");
            }
        }

        [Test]
        public void JoinsAreSymmetric()
        {
            for (int v = 0; v < 4; v++)
            for (int x = 0; x < 3; x++)
            for (int y = 0; y < 3; y++)
            for (int z = 0; z < 3; z++)
            for (int x2 = 0; x2 < 3; x2++)
            for (int y2 = 0; y2 < 3; y2++)
            for (int z2 = 0; z2 < 3; z2++)
            {
                var a = new Cell(x, y, z);
                var b = new Cell(x2, y2, z2);
                Assert.AreEqual(JoinRule.AreJoined(a, b, v), JoinRule.AreJoined(b, a, v));
            }
        }

        [Test]
        public void TheDepthStepKeepsTheScreenPositionAndNeighboursMoveOneScreenStep()
        {
            for (int v = 0; v < 4; v++)
            {
                var a = new Cell(2, 1, 2);
                Assert.AreEqual(ViewRule.Screen(a, v), ViewRule.Screen(a + ViewRule.DepthStep(v) * 3, v), $"view {v}");
                var seen = new System.Collections.Generic.HashSet<(int, int)>();
                foreach (var n in ViewRule.HorizontalSteps)
                {
                    var d = ViewRule.Screen(a + n, v);
                    var s = ViewRule.Screen(a, v);
                    var step = (d.U - s.U, d.V - s.V);
                    Assert.AreEqual(1, System.Math.Abs(step.Item1));
                    Assert.AreEqual(1, System.Math.Abs(step.Item2));
                    Assert.IsTrue(seen.Add(step), "four distinct diagonal steps");
                }
            }
        }

        [Test]
        public void ARotatedViewWrapsAndFourTurnsReturn()
        {
            Assert.AreEqual(1, ViewRule.Rotate(0, 1));
            Assert.AreEqual(3, ViewRule.Rotate(0, -1));
            Assert.AreEqual(0, ViewRule.Rotate(3, 1));
            int v = 2;
            for (int i = 0; i < 4; i++) v = ViewRule.Rotate(v, 1);
            Assert.AreEqual(2, v);
        }
    }
}
