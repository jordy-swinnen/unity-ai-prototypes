using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Prototypes.PixelInvaders.Tests
{
    public class SpriteAssetTests
    {
        const string SpriteFolder = "Assets/Prototypes/PixelInvaders/Art/Sprites";

        static readonly HashSet<Color32> AllowedPalette = new HashSet<Color32>
        {
            new Color32(0, 0, 0, 0),       // Transparent
            new Color32(9, 12, 18, 255),   // #090C12 Void black
            new Color32(22, 30, 43, 255),  // #161E2B Trench steel
            new Color32(44, 57, 75, 255),  // #2C394B Hull slate
            new Color32(99, 118, 141, 255),// #63768D Highlight steel
            new Color32(240, 246, 255, 255),// #F0F6FF Phosphor white
            new Color32(217, 130, 43, 255),// #D9822B Amber grid
            new Color32(255, 209, 71, 255),// #FFD147 Solar gold
            new Color32(44, 232, 245, 255),// #2CE8F5 Cyan turret
            new Color32(27, 118, 181, 255),// #1B76B5 Deep cobalt
            new Color32(56, 217, 115, 255),// #38D973 Bunker emerald
            new Color32(30, 130, 76, 255), // #1E824C Bunker moss
            new Color32(184, 92, 255, 255),// #B85CFF Guard violet
            new Color32(107, 47, 163, 255),// #6B2FA3 Guard plum
            new Color32(255, 71, 105, 255),// #FF4769 Scout coral
            new Color32(168, 29, 57, 255), // #A81D39 Crimson ember
        };

        [TestCase("invaders_sheet", 12, new[] { "scout", "guard", "heavy", "ufo" })]
        [TestCase("cannon_and_shots_sheet", 13, new[] { "cannon", "cannon_shatter", "shots" })]
        [TestCase("powerups_and_fx_sheet", 15, new[] { "capsule_rapid", "capsule_twin", "capsule_shield", "alien_burst", "ufo_burst", "spark" })]
        [TestCase("bunker_and_hud_sheet", 6, new[] { "bunker_icon", "life_icon", "crown_icon", "badge_rapid", "badge_twin", "badge_shield" })]
        public void Sprite_sheet_is_point_filtered_uncompressed_in_palette_and_matches_json(
            string name, int expectedFrames, string[] expectedTags)
        {
            string pngPath = $"{SpriteFolder}/{name}.png";
            string jsonPath = $"{SpriteFolder}/{name}.json";

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
            Assert.IsNotNull(tex, $"Missing sprite texture at {pngPath}");
            Assert.AreEqual(FilterMode.Point, tex.filterMode, $"{name} must use Point filter");

            var importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
            Assert.IsNotNull(importer);
            Assert.AreEqual(TextureImporterType.Sprite, importer.textureType);
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
            Assert.IsFalse(importer.mipmapEnabled);

            var rawTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            rawTex.LoadImage(File.ReadAllBytes(pngPath));
            foreach (var c in rawTex.GetPixels32())
            {
                if (c.a == 0) continue;
                Assert.IsTrue(AllowedPalette.Contains(c),
                    $"{name}.png contains off-palette pixel RGBA({c.r},{c.g},{c.b},{c.a})");
            }
            Object.DestroyImmediate(rawTex);

            var jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
            Assert.IsNotNull(jsonAsset, $"Missing sheet JSON at {jsonPath}");
            var sheet = SheetFrames.Parse(jsonAsset.text);
            Assert.AreEqual(expectedFrames, sheet.FrameCount, $"{name} frame count mismatch");
            foreach (string tag in expectedTags)
            {
                Assert.IsTrue(sheet.TryGetTag(tag, out _, out _), $"{name}.json missing tag '{tag}'");
            }
        }
    }
}
