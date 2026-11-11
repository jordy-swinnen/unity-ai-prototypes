using NUnit.Framework;
using UnityEngine;

namespace Prototypes.DioramaTurn.Tests
{
    public sealed class SupportTests
    {
        [TestCase(1)]
        [TestCase(3)]
        public void StarsFollowParAndTheExtraTurns(int par)
        {
            Assert.AreEqual(3, Stars.For(par, par, 2));
            Assert.AreEqual(3, Stars.For(par - 1, par, 2));
            Assert.AreEqual(2, Stars.For(par + 1, par, 2));
            Assert.AreEqual(2, Stars.For(par + 2, par, 2));
            Assert.AreEqual(1, Stars.For(par + 3, par, 2));
        }

        [TestCase(1080, 3)]
        [TestCase(720, 2)]
        [TestCase(1440, 4)]
        [TestCase(2160, 4)]
        [TestCase(480, 1)]
        [TestCase(200, 1)]
        public void PixelScaleIsAWholeNumberThatFits(int height, int expected)
        {
            int scale = PixelScale.Scale(height, 32f, 10.5f, 4);
            Assert.AreEqual(expected, scale);
            Assert.GreaterOrEqual(scale, 1);
        }

        [TestCase(1080, 3)]
        [TestCase(720, 2)]
        [TestCase(1440, 4)]
        public void OrthographicSizeGivesWholeScreenPixelsPerSpritePixel(int height, int scale)
        {
            float size = PixelScale.OrthoSize(height, 32f, scale);
            // screen pixels per world unit = height / (2 * size)
            Assert.AreEqual(32f * scale, height / (2f * size), 1e-3f);
            Assert.AreEqual(5.625f, size, 1e-4f);
        }

        const string Json = @"{
  ""frames"": [
    { ""filename"": ""pip 0"", ""frame"": { ""x"": 0, ""y"": 0, ""w"": 32, ""h"": 32 }, ""duration"": 120 },
    { ""filename"": ""pip 1"", ""frame"": { ""x"": 32, ""y"": 0, ""w"": 32, ""h"": 32 }, ""duration"": 100 },
    { ""filename"": ""pip 2"", ""frame"": { ""x"": 64, ""y"": 0, ""w"": 32, ""h"": 32 }, ""duration"": 100 }
  ],
  ""meta"": {
    ""size"": { ""w"": 96, ""h"": 32 },
    ""frameTags"": [ { ""name"": ""idle"", ""from"": 0, ""to"": 1, ""direction"": ""forward"" }, { ""name"": ""walk"", ""from"": 2, ""to"": 2, ""direction"": ""forward"" } ]
  }
}";

        [Test]
        public void SheetFramesReadsRectsDurationsAndTags()
        {
            var sheet = SheetFrames.Parse(Json);
            Assert.AreEqual(3, sheet.FrameCount);
            Assert.AreEqual(96, sheet.Width);
            Assert.AreEqual(32, sheet.Height);
            Assert.AreEqual(new RectInt(32, 0, 32, 32), sheet.Rect(1));
            Assert.AreEqual(0.1f, sheet.Duration(1), 1e-5f);
            Assert.IsTrue(sheet.TryGetTag("idle", out var from, out var to));
            Assert.AreEqual((0, 1), (from, to));
            Assert.IsFalse(sheet.TryGetTag("hop", out _, out _));
        }

        [Test]
        public void SheetFramesGivesUnityUvTilingAndOffset()
        {
            var sheet = SheetFrames.Parse(Json);
            sheet.Uv(1, out var scale, out var offset);
            Assert.AreEqual(new Vector2(32f / 96f, 1f), scale);
            Assert.AreEqual(new Vector2(32f / 96f, 0f), offset);
        }

        [Test]
        public void SheetFramesRejectsEmptyJson()
        {
            Assert.Throws<System.ArgumentException>(() => SheetFrames.Parse("{}"));
        }

        [Test]
        public void SettingsClampToSafeRanges()
        {
            var s = ScriptableObject.CreateInstance<DioramaTurnSettings>();
            try
            {
                s.turn.duration = -1f;
                s.turn.queueMax = 99;
                s.walk.stepTime = 0f;
                s.camera.maxScale = 0;
                s.Clamp();
                Assert.GreaterOrEqual(s.turn.duration, 0.05f);
                Assert.LessOrEqual(s.turn.queueMax, 3);
                Assert.GreaterOrEqual(s.walk.stepTime, 0.05f);
                Assert.GreaterOrEqual(s.camera.maxScale, 1);
                var t = s.ToTiming();
                Assert.AreEqual(s.turn.duration, t.TurnDuration);
            }
            finally { Object.DestroyImmediate(s); }
        }

        [Test]
        public void TheDefaultsAreTheSpecsStartingValues()
        {
            var s = ScriptableObject.CreateInstance<DioramaTurnSettings>();
            try
            {
                Assert.AreEqual(0.45f, s.turn.duration);
                Assert.AreEqual(4f, s.turn.overshootDeg);
                Assert.AreEqual(0.22f, s.walk.stepTime);
                Assert.AreEqual(0.35f, s.walk.hopTime);
                Assert.AreEqual(32f, s.camera.pixelsPerUnit);
                Assert.AreEqual(35.264f, s.camera.pitchDeg, 1e-3f);
            }
            finally { Object.DestroyImmediate(s); }
        }
    }
}
