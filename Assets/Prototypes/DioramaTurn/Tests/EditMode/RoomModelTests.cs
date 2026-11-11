using System;
using System.Linq;
using NUnit.Framework;

namespace Prototypes.DioramaTurn.Tests
{
    public sealed class RoomModelTests
    {
        static string[] Layer(params string[] rows) => rows;

        [Test]
        public void ParsesLayersBottomToTop()
        {
            var room = RoomParser.ParseText("s b .\n. . .\n---\n. . g\n. . .");
            Assert.AreEqual(3, room.SizeX);
            Assert.AreEqual(2, room.SizeY);
            Assert.AreEqual(2, room.SizeZ);
            Assert.AreEqual(new Cell(0, 0, 0), room.Start);
            Assert.AreEqual(new Cell(2, 1, 0), room.Goal);
            Assert.AreEqual(3, room.Pieces.Count);
            Assert.IsTrue(room.TryGetPiece(new Cell(1, 0, 0), out var p));
            Assert.AreEqual(PieceKind.Block, p.Kind);
        }

        [Test]
        public void ParsesBridges()
        {
            var room = RoomParser.Parse(new[] { Layer("sr.g") });
            Assert.IsTrue(room.TryGetPiece(new Cell(1, 0, 0), out var p));
            Assert.AreEqual(PieceKind.Bridge, p.Kind);
        }

        [Test]
        public void RejectsABadRowLength()
        {
            Assert.Throws<RoomFormatException>(() => RoomParser.Parse(new[] { Layer("s.g", "..") }));
        }

        [Test]
        public void RejectsAnUnknownCharacter()
        {
            var e = Assert.Throws<RoomFormatException>(() => RoomParser.Parse(new[] { Layer("s?g") }));
            StringAssert.Contains("?", e.Message);
        }

        [Test]
        public void RejectsAMissingStartOrGoal()
        {
            Assert.Throws<RoomFormatException>(() => RoomParser.Parse(new[] { Layer("b.g") }));
            Assert.Throws<RoomFormatException>(() => RoomParser.Parse(new[] { Layer("s.b") }));
        }

        [Test]
        public void RejectsTwoStartsOrTwoGoals()
        {
            Assert.Throws<RoomFormatException>(() => RoomParser.Parse(new[] { Layer("ssg") }));
            Assert.Throws<RoomFormatException>(() => RoomParser.Parse(new[] { Layer("sgg") }));
        }

        [Test]
        public void RejectsLayersOfDifferentSizes()
        {
            Assert.Throws<RoomFormatException>(() => RoomParser.Parse(new[] { Layer("sg"), Layer("...") }));
        }

        [Test]
        public void ValidatorAcceptsAGoodRoom()
        {
            var room = RoomParser.Parse(new[] { Layer("sb.g") });
            Assert.IsEmpty(RoomValidator.Validate(room, 5, 5, 5));
        }

        [Test]
        public void ValidatorRejectsARoomThatIsTooBig()
        {
            var room = RoomParser.Parse(new[] { Layer("sb.g") });
            Assert.IsNotEmpty(RoomValidator.Validate(room, 3, 5, 5));
        }

        [Test]
        public void ValidatorRejectsTwoNodesOnOneScreenSpot()
        {
            // (0,0,0) and (1,1,1) differ by the view 0 depth step.
            var room = new RoomData(2, 2, 2, new[]
            {
                new Piece(new Cell(0, 0, 0), PieceKind.Start),
                new Piece(new Cell(1, 1, 1), PieceKind.Goal),
            });
            var errors = RoomValidator.Validate(room, 5, 5, 5);
            Assert.IsNotEmpty(errors);
            StringAssert.Contains("screen", errors[0]);
        }

        [Test]
        public void ValidatorRejectsAnOverlapInAnotherView()
        {
            // (0,0,0) and (-1,1,1) differ by the view 1 depth step.
            var room = new RoomData(3, 2, 3, new[]
            {
                new Piece(new Cell(1, 0, 0), PieceKind.Start),
                new Piece(new Cell(0, 1, 1), PieceKind.Goal),
            });
            Assert.IsNotEmpty(RoomValidator.Validate(room, 5, 5, 5));
        }

        [Test]
        public void RoomDataRejectsDuplicateCells()
        {
            Assert.Throws<RoomFormatException>(() => new RoomData(2, 1, 1, new[]
            {
                new Piece(new Cell(0, 0, 0), PieceKind.Start),
                new Piece(new Cell(0, 0, 0), PieceKind.Goal),
            }));
        }
    }
}
