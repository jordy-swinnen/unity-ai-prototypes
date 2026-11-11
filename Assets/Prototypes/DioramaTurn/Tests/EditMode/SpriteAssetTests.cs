using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Prototypes.DioramaTurn.Tests
{
    public class SpriteAssetTests
    {
        const string SheetPath = "Assets/Prototypes/DioramaTurn/Art/Sprites/pip_sheet.png";
        const string JsonPath = "Assets/Prototypes/DioramaTurn/Art/Sprites/pip_sheet.json";

        [Test]
        public void The_sheet_imports_as_a_point_filtered_sprite()
        {
            var importer = AssetImporter.GetAtPath(SheetPath) as TextureImporter;
            Assert.IsNotNull(importer, $"no importer at {SheetPath}");
            Assert.AreEqual(TextureImporterType.Sprite, importer.textureType);
            Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode);
            Assert.AreEqual(FilterMode.Point, importer.filterMode);
            Assert.IsFalse(importer.mipmapEnabled);
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
        }

        [Test]
        public void The_sheet_json_reads_into_fourteen_frames_with_the_five_tags()
        {
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(JsonPath);
            Assert.IsNotNull(json, $"no sheet data at {JsonPath}");
            var sheet = SheetFrames.Parse(json.text);

            Assert.AreEqual(14, sheet.FrameCount);
            Assert.AreEqual(14 * 32, sheet.Width);
            Assert.AreEqual(32, sheet.Height);
            for (int i = 0; i < sheet.FrameCount; i++)
            {
                var rect = sheet.Rect(i);
                Assert.AreEqual(32, rect.width, $"frame {i} width");
                Assert.AreEqual(32, rect.height, $"frame {i} height");
            }

            Assert.IsTrue(sheet.TryGetTag("idle", out int idleFrom, out int idleTo));
            Assert.AreEqual(0, idleFrom);
            Assert.AreEqual(1, idleTo);
            Assert.IsTrue(sheet.TryGetTag("walk", out int walkFrom, out int walkTo));
            Assert.AreEqual(2, walkFrom);
            Assert.AreEqual(5, walkTo);
            Assert.IsTrue(sheet.TryGetTag("hop", out int hopFrom, out int hopTo));
            Assert.AreEqual(6, hopFrom);
            Assert.AreEqual(8, hopTo);
            Assert.IsTrue(sheet.TryGetTag("cheer", out int cheerFrom, out int cheerTo));
            Assert.AreEqual(9, cheerFrom);
            Assert.AreEqual(11, cheerTo);
            Assert.IsTrue(sheet.TryGetTag("shake", out int shakeFrom, out int shakeTo));
            Assert.AreEqual(12, shakeFrom);
            Assert.AreEqual(13, shakeTo);
        }

        [Test]
        public void The_sprite_frames_tile_and_offset_within_the_sheet()
        {
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(JsonPath);
            var sheet = SheetFrames.Parse(json.text);
            sheet.Uv(0, out var scale0, out var offset0);
            sheet.Uv(1, out var scale1, out var offset1);
            Assert.AreEqual(1f / 14f, scale0.x, 0.0001f);
            Assert.AreEqual(1f, scale0.y, 0.0001f);
            Assert.AreEqual(0f, offset0.x, 0.0001f);
            Assert.AreEqual(1f / 14f, offset1.x, 0.0001f, "the second frame starts one frame to the right");
        }
    }
}
