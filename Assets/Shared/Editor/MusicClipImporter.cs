using UnityEditor;
using UnityEngine;

namespace Shared.Editor
{
    /// <summary>
    /// Music clips are long, so they stream from disk instead of decompressing into memory. Every
    /// prototype's rendered track is <prototype>/Audio/music.wav (Tools/audio/render_music.sh). Sound
    /// effects keep their default import.
    /// </summary>
    public sealed class MusicClipImporter : AssetPostprocessor
    {
        /// <summary>True for a rendered music clip such as Assets/Prototypes/Demo/Audio/music.wav.</summary>
        public static bool IsMusicPath(string assetPath)
        {
            return !string.IsNullOrEmpty(assetPath) &&
                   assetPath.Replace('\\', '/').EndsWith("/Audio/music.wav");
        }

        void OnPreprocessAudio()
        {
            if (!IsMusicPath(assetPath)) return;
            var importer = (AudioImporter)assetImporter;
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = false;
            importer.loadInBackground = true;
        }
    }
}
