using UnityEngine;

namespace Prototypes.NeonPong
{
    /// <summary>
    /// Plays one sound per feel-table row. The paddle sound's pitch climbs with the rally speed.
    /// Volume comes from the settings asset. The sounds are written by Tools/audio/NeonPong.
    /// </summary>
    public class NeonPongAudio : MonoBehaviour
    {
        public NeonPongGame game;
        public AudioSource sfx;
        public AudioSource paddleSfx;
        public AudioClip wall, paddle, serve, point, win, restart;

        public int PlayCount { get; private set; }
        public EventKind LastPlayed { get; private set; }
        public float LastPitch { get; private set; } = 1f;

        void OnEnable()
        {
            if (game != null) game.EventRaised += OnGameEvent;
        }

        void OnDisable()
        {
            if (game != null) game.EventRaised -= OnGameEvent;
        }

        void OnGameEvent(GameEvent e)
        {
            var s = game.settings;
            switch (e.Kind)
            {
                case EventKind.WallHit: Play(sfx, wall, e.Kind, 1f); break;
                case EventKind.Serve: Play(sfx, serve, e.Kind, 1f); break;
                case EventKind.Point: Play(sfx, point, e.Kind, 1f); break;
                case EventKind.Win: Play(sfx, win, e.Kind, 1f); break;
                case EventKind.Restart: Play(sfx, restart, e.Kind, 1f); break;
                case EventKind.PaddleHit:
                    float pitch = Mathf.Lerp(s.pitchMin, s.pitchMax, Mathf.InverseLerp(s.baseSpeed, s.maxSpeed, e.Speed));
                    Play(paddleSfx, paddle, e.Kind, pitch);
                    break;
            }
        }

        void Play(AudioSource source, AudioClip clip, EventKind kind, float pitch)
        {
            if (source == null || clip == null) return;
            source.pitch = pitch;
            source.PlayOneShot(clip, game.settings.masterVolume);
            PlayCount++;
            LastPlayed = kind;
            LastPitch = pitch;
        }
    }
}
