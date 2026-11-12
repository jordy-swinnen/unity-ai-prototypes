using System;
using NUnit.Framework;
using UnityEngine;

namespace Prototypes.PixelInvaders.Tests
{
    public class SheetFramesTests
    {
        const string SampleJson = @"{
          ""frames"": [
            {
              ""filename"": ""invaders 0.aseprite"",
              ""frame"": { ""x"": 0, ""y"": 0, ""w"": 16, ""h"": 16 },
              ""duration"": 250
            },
            {
              ""filename"": ""invaders 1.aseprite"",
              ""frame"": { ""x"": 16, ""y"": 0, ""w"": 16, ""h"": 16 },
              ""duration"": 250
            }
          ],
          ""meta"": {
            ""size"": { ""w"": 32, ""h"": 16 },
            ""frameTags"": [
              { ""name"": ""scout"", ""from"": 0, ""to"": 1, ""direction"": ""forward"" }
            ]
          }
        }";

        [Test]
        public void Parse_reads_frames_durations_tags_and_bottom_left_uvs()
        {
            var sheet = SheetFrames.Parse(SampleJson);
            Assert.AreEqual(2, sheet.FrameCount);
            Assert.AreEqual(32, sheet.Width);
            Assert.AreEqual(16, sheet.Height);
            Assert.AreEqual(new RectInt(16, 0, 16, 16), sheet.Rect(1));
            Assert.AreEqual(0.25f, sheet.Duration(0), 1e-4f);

            Assert.IsTrue(sheet.TryGetTag("scout", out int from, out int to));
            Assert.AreEqual(0, from);
            Assert.AreEqual(1, to);
            Assert.IsFalse(sheet.TryGetTag("missing", out _, out _));

            sheet.Uv(1, out Vector2 scale, out Vector2 offset);
            Assert.AreEqual(new Vector2(0.5f, 1f), scale);
            Assert.AreEqual(new Vector2(0.5f, 0f), offset);
        }

        [Test]
        public void Parse_rejects_empty_or_malformed_json()
        {
            Assert.Throws<ArgumentException>(() => SheetFrames.Parse("{}"));
            Assert.Throws<ArgumentException>(() => SheetFrames.Parse(""));
        }
    }
}
