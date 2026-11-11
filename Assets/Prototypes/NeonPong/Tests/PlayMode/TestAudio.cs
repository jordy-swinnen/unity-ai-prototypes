using NUnit.Framework;
using UnityEngine;

namespace Prototypes.NeonPong.Tests
{
    /// <summary>Silences the PlayMode test run and restores the volume afterwards.</summary>
    [SetUpFixture]
    public class TestAudio
    {
        float savedVolume = 1f;

        [OneTimeSetUp]
        public void Mute()
        {
            savedVolume = AudioListener.volume;
            AudioListener.volume = 0f;
        }

        [OneTimeTearDown]
        public void Restore() => AudioListener.volume = savedVolume;
    }
}
