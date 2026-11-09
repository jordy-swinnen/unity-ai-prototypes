using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Prototypes.NeonPong.Tests
{
    /// <summary>Checks the sound files the script wrote. Whether they sound good is a person's call.</summary>
    public class NeonPongAudioFileTests
    {
        const string Folder = "Assets/Prototypes/NeonPong/Audio";
        static readonly string[] Names = { "wall", "paddle", "serve", "point", "win", "restart" };

        public static string[] SoundNames => Names;

        // Reads a 16-bit mono PCM wav: returns the sample rate and the samples in -1..1.
        static (int rate, float[] samples) ReadWav(string path)
        {
            var bytes = File.ReadAllBytes(path);
            Assert.AreEqual("RIFF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
            Assert.AreEqual("WAVE", System.Text.Encoding.ASCII.GetString(bytes, 8, 4));
            int channels = BitConverter.ToInt16(bytes, 22);
            int rate = BitConverter.ToInt32(bytes, 24);
            int bits = BitConverter.ToInt16(bytes, 34);
            Assert.AreEqual(1, channels, "mono");
            Assert.AreEqual(16, bits, "16-bit");

            // Find the "data" chunk.
            int pos = 12;
            while (pos + 8 <= bytes.Length)
            {
                string id = System.Text.Encoding.ASCII.GetString(bytes, pos, 4);
                int size = BitConverter.ToInt32(bytes, pos + 4);
                if (id == "data")
                {
                    var samples = new float[size / 2];
                    for (int i = 0; i < samples.Length; i++)
                        samples[i] = BitConverter.ToInt16(bytes, pos + 8 + i * 2) / 32768f;
                    return (rate, samples);
                }
                pos += 8 + size;
            }
            Assert.Fail("no data chunk");
            return (0, null);
        }

        [TestCaseSource(nameof(SoundNames))]
        public void The_sound_exists_is_short_audible_does_not_clip_and_ends_near_zero(string name)
        {
            string path = $"{Folder}/{name}.wav";
            Assert.IsTrue(File.Exists(path), $"{path} is missing. Run Tools/audio/NeonPong/make_sounds.py");

            var (rate, samples) = ReadWav(path);
            Assert.AreEqual(44100, rate);
            Assert.Less(samples.Length / (float)rate, 1.5f, "longer than 1.5 s");
            Assert.Greater(samples.Length, 100, "too short");

            float peak = 0f;
            foreach (var s in samples) peak = Mathf.Max(peak, Mathf.Abs(s));
            Assert.Greater(peak, 0.1f, "silent");
            Assert.Less(peak, 0.95f, "clips");

            float tail = 0f;
            for (int i = samples.Length - 20; i < samples.Length; i++) tail = Mathf.Max(tail, Mathf.Abs(samples[i]));
            Assert.Less(tail, 0.01f, "does not end near zero (it would click)");
        }

        [TestCaseSource(nameof(SoundNames))]
        public void Unity_imports_the_sound_as_an_audio_clip(string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Folder}/{name}.wav");
            Assert.IsNotNull(clip, name);
            Assert.Less(clip.length, 1.5f);
        }
    }
}
