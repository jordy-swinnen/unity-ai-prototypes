using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Prototypes.DioramaTurn.Tests
{
    public class AudioAssetTests
    {
        const string Folder = "Assets/Prototypes/DioramaTurn/Audio";

        static readonly string[] Sounds =
        {
            "turn_whoosh", "turn_clunk", "join_up", "join_down", "tap_pluck", "step_a", "step_b",
            "hop_boing", "landing", "bonk", "tick", "star_arpeggio", "star_chime", "pop", "restart",
        };

        [Test]
        public void Every_sound_exists_and_imports_as_short_mono_44k_audio()
        {
            foreach (var name in Sounds)
            {
                string path = $"{Folder}/{name}.wav";
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                Assert.IsNotNull(clip, $"{path} is missing. Run Tools/audio/DioramaTurn/make_sounds.py");
                Assert.AreEqual(1, clip.channels, $"{name} must be mono");
                Assert.AreEqual(44100, clip.frequency, $"{name} must be 44.1 kHz");
                Assert.Greater(clip.length, 0.01f, $"{name} is empty");
                Assert.Less(clip.length, 2f, $"{name} is longer than the sound budget");
            }
        }
    }
}
