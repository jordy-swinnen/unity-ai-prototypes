using System.Collections.Generic;
using UnityEngine;

namespace Prototypes.MedievalChess
{
    /// <summary>
    /// Plays the game's sounds. One clip per <see cref="SoundId"/>, a few audio sources so sounds can overlap, and the
    /// master volume from the settings. It remembers what it played, so a test can check the right sound played.
    /// </summary>
    public sealed class SoundBank : MonoBehaviour
    {
        public MedievalChessSettings settings;
        [Tooltip("One clip per SoundId, in the order of the enum.")]
        public AudioClip[] clips;
        public AudioSource[] sources;

        readonly List<SoundId> history = new List<SoundId>();
        int next;

        public IReadOnlyList<SoundId> History => history;
        public int PlayCount { get; private set; }

        public void ClearHistory()
        {
            history.Clear();
            PlayCount = 0;
        }

        public AudioClip Clip(SoundId id) => clips != null && (int)id < clips.Length ? clips[(int)id] : null;

        public void Play(SoundId id, float volume = 1f, float pitch = 1f)
        {
            history.Add(id);
            if (history.Count > 200) history.RemoveAt(0);
            PlayCount++;

            var clip = Clip(id);
            if (clip == null || sources == null || sources.Length == 0) return;
            var source = sources[next++ % sources.Length];
            source.pitch = pitch;
            source.PlayOneShot(clip, Mathf.Clamp01(settings.feel.masterVolume * volume));
        }
    }
}
