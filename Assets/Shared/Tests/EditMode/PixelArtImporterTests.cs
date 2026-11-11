using NUnit.Framework;
using Shared.Editor;

namespace Shared.Tests
{
    /// <summary>
    /// The import rule for pixel art is path based: only textures under an Art/Sprites folder are changed.
    /// These tests pin that path rule down, so other textures (Art/Textures) are never switched to Point filtering.
    /// </summary>
    public class PixelArtImporterTests
    {
        [TestCase("Assets/Prototypes/Demo/Art/Sprites/intent_attack.png")]
        [TestCase("Assets/Prototypes/Demo/Art/Sprites/Hero/idle.png")]
        [TestCase("Assets/Shared/Art/Sprites/cursor.png")]
        public void Textures_under_Art_Sprites_get_the_pixel_art_settings(string path)
        {
            Assert.IsTrue(PixelArtTextureImporter.IsSpritePath(path));
        }

        [TestCase("Assets/Prototypes/Demo/Art/Textures/stone_albedo.png")]
        [TestCase("Assets/Prototypes/Demo/Art/Exports/king.glb")]
        [TestCase("Assets/Prototypes/Demo/Sprites/idle.png")]
        [TestCase("Assets/Prototypes/Demo/Art/SpritesOld/idle.png")]
        [TestCase("Assets/Prototypes/Demo/Art/Sprites")]
        [TestCase("")]
        public void Other_paths_are_left_alone(string path)
        {
            Assert.IsFalse(PixelArtTextureImporter.IsSpritePath(path));
        }

        [Test]
        public void A_null_path_is_left_alone()
        {
            Assert.IsFalse(PixelArtTextureImporter.IsSpritePath(null));
        }
    }
}
