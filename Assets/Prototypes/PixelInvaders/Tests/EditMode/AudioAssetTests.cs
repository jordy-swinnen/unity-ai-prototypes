using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Prototypes.PixelInvaders.Tests
{
    public class AudioAssetTests
    {
        const string AudioFolder = "Assets/Prototypes/PixelInvaders/Audio";

        static readonly string[] SfxFiles =
        {
            "step_0.wav", "step_1.wav", "step_2.wav", "step_3.wav",
            "shoot.wav", "twin_shoot.wav", "alien_shoot.wav",
            "alien_pop.wav", "bunker_hit.wav",
            "ufo_loop.wav", "ufo_boom.wav",
            "powerup.wav", "cannon_death.wav", "wave_clear.wav", "game_over.wav",
        };

        [TestCaseSource(nameof(SfxFiles))]
        public void Sfx_clip_exists_is_not_silent_and_does_not_clip(string fileName)
        {
            string path = $"{AudioFolder}/{fileName}";
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            Assert.IsNotNull(clip, $"Missing AudioClip at {path}");
            Assert.Greater(clip.length, 0.05f);
            Assert.LessOrEqual(clip.length, 2.0f);

            var samples = new float[clip.samples * clip.channels];
            Assert.IsTrue(clip.GetData(samples, 0));

            float peak = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float a = Mathf.Abs(samples[i]);
                if (a > peak) peak = a;
            }
            Assert.Greater(peak, 0.2f, $"{fileName} is too quiet or silent");
            Assert.LessOrEqual(peak, 0.86f, $"{fileName} clips above target peak");
        }

        [Test]
        public void Music_track_streams_and_audio_director_binds_all_clips_and_ducks_on_wave_clear()
        {
            string musicPath = $"{AudioFolder}/music.wav";
            var musicClip = AssetDatabase.LoadAssetAtPath<AudioClip>(musicPath);
            Assert.IsNotNull(musicClip, $"Missing music clip at {musicPath}");
            Assert.AreEqual(48f, musicClip.length, 0.25f);

            var importer = (AudioImporter)AssetImporter.GetAtPath(musicPath);
            Assert.IsNotNull(importer);
            Assert.AreEqual(AudioClipLoadType.Streaming, importer.defaultSampleSettings.loadType);

            EditorSceneManager.OpenScene("Assets/Prototypes/PixelInvaders/Scenes/PixelInvaders.unity", OpenSceneMode.Single);
            var game = Object.FindAnyObjectByType<PixelInvadersGame>();
            Assert.IsNotNull(game);
            var audio = game.audioDirector;
            Assert.IsNotNull(audio);
            Assert.AreEqual(4, audio.stepNotes.Length);
            for (int i = 0; i < 4; i++) Assert.IsNotNull(audio.stepNotes[i], $"stepNotes[{i}] not bound");
            Assert.IsNotNull(audio.shootClip);
            Assert.IsNotNull(audio.twinShootClip);
            Assert.IsNotNull(audio.alienShootClip);
            Assert.IsNotNull(audio.alienPopClip);
            Assert.IsNotNull(audio.bunkerHitClip);
            Assert.IsNotNull(audio.ufoLoopClip);
            Assert.IsNotNull(audio.ufoBoomClip);
            Assert.IsNotNull(audio.powerupClip);
            Assert.IsNotNull(audio.cannonDeathClip);
            Assert.IsNotNull(audio.waveClearClip);
            Assert.IsNotNull(audio.gameOverClip);
            Assert.IsNotNull(audio.musicLoopClip);

            audio.SyncSettings();
            float baseVol = audio.CurrentMusicVolume;
            Assert.Greater(baseVol, 0.1f);

            audio.HandleEvent(new GameEvent { Kind = GameEventKind.WaveCleared, Value = 1 });
            audio.Tick(0.05f, ufoActive: false);
            Assert.Less(audio.CurrentMusicVolume, baseVol * 0.6f, "Music must duck on WaveCleared");

            audio.Tick(game.settings.audio.musicDuckDuration + 0.2f, ufoActive: false);
            Assert.AreEqual(baseVol, audio.CurrentMusicVolume, 1e-4f, "Music volume must recover after duck duration");
        }
    }
}
