using UnityEngine;

namespace Prototypes.DioramaTurn
{
    /// <summary>The looping music. Ducks under the goal moment, as the spec's audio direction asks.</summary>
    public sealed class MusicPlayer : MonoBehaviour
    {
        public DioramaTurnSettings settings;
        public AudioSource source;

        float duckLevel = 1f;
        float duckLeft;

        void Start()
        {
            if (source == null) return;
            source.loop = true;
            if (source.clip != null && !source.isPlaying) source.Play();
            ApplyVolume();
        }

        /// <summary>Dip the music for the tuned time (the star moment).</summary>
        public void Duck()
        {
            if (settings == null) return;
            if (settings.feel.effectStrength <= 0f) return;
            duckLeft = settings.audio.musicDuckSeconds;
        }

        void Update()
        {
            if (settings == null || source == null) return;
            float target = duckLeft > 0f ? Mathf.Pow(10f, settings.audio.musicDuckDb / 20f) : 1f;
            duckLevel = Mathf.MoveTowards(duckLevel, target, Time.unscaledDeltaTime * 3f);
            duckLeft = Mathf.Max(0f, duckLeft - Time.unscaledDeltaTime);
            ApplyVolume();
        }

        void ApplyVolume()
        {
            if (source == null || settings == null) return;
            source.volume = settings.audio.music * settings.audio.master * duckLevel;
        }
    }
}
