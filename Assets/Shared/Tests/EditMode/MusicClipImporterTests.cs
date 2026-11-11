using NUnit.Framework;
using Shared.Editor;

namespace Shared.Tests
{
    public class MusicClipImporterTests
    {
        [Test]
        public void A_rendered_music_clip_is_recognised()
        {
            Assert.IsTrue(MusicClipImporter.IsMusicPath("Assets/Prototypes/Demo/Audio/music.wav"));
            Assert.IsTrue(MusicClipImporter.IsMusicPath("Assets/Prototypes/Demo/Audio/music.wav".Replace('/', '\\')));
        }

        [Test]
        public void Sound_effects_and_other_files_are_left_alone()
        {
            Assert.IsFalse(MusicClipImporter.IsMusicPath("Assets/Prototypes/Demo/Audio/wall.wav"));
            Assert.IsFalse(MusicClipImporter.IsMusicPath("Assets/Prototypes/Demo/Audio/music2.wav"));
            Assert.IsFalse(MusicClipImporter.IsMusicPath("Assets/Prototypes/Demo/Art/Sprites/music.png"));
            Assert.IsFalse(MusicClipImporter.IsMusicPath(null));
        }
    }
}
