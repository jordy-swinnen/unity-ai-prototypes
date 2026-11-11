using NUnit.Framework;
using UnityEngine;

namespace Prototypes.DioramaTurn.Tests
{
    /// <summary>
    /// Silences the PlayMode test run: Unity plays the game's audio through the Editor's audio listener,
    /// so a room-completion test would otherwise make noise. The volume is restored afterwards.
    /// A SetUpFixture with this namespace applies to every PlayMode fixture in it.
    /// </summary>
    [SetUpFixture]
    public class TestAudio
    {
        float savedVolume;

        [OneTimeSetUp]
        public void Mute()
        {
            savedVolume = AudioListener.volume;
            AudioListener.volume = 0f;
        }

        [OneTimeTearDown]
        public void Restore()
        {
            AudioListener.volume = savedVolume;
        }
    }
}
