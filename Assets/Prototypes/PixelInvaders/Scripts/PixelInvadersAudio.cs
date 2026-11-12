using UnityEngine;

namespace Prototypes.PixelInvaders
{
    /// <summary>
    /// Audio director for PixelInvaders: plays the 4-note descending bass step heartbeat,
    /// laser shots, rank-pitched alien pops, UFO warble, power-up fanfare, and ducks the looping music track.
    /// Gracefully skips any null AudioClip so Stage 2 runs before Stage 3 generates the audio files.
    /// </summary>
    public sealed class PixelInvadersAudio : MonoBehaviour
    {
        public PixelInvadersSettings settings;
        public AudioSource sfxSource;
        public AudioSource ufoLoopSource;
        public AudioSource musicSource;

        public AudioClip[] stepNotes = new AudioClip[4];
        public AudioClip shootClip;
        public AudioClip twinShootClip;
        public AudioClip alienShootClip;
        public AudioClip alienPopClip;
        public AudioClip bunkerHitClip;
        public AudioClip ufoLoopClip;
        public AudioClip ufoBoomClip;
        public AudioClip powerupClip;
        public AudioClip cannonDeathClip;
        public AudioClip waveClearClip;
        public AudioClip gameOverClip;
        public AudioClip musicLoopClip;

        float duckTimer;

        public float CurrentMusicVolume => musicSource != null ? musicSource.volume : 0f;

        void Start()
        {
            SyncSettings();
        }

        public void SyncSettings()
        {
            if (settings == null) return;
            float baseMusic = settings.audio.masterVolume * settings.audio.musicVolume;
            if (musicSource != null)
            {
                if (musicLoopClip != null && musicSource.clip != musicLoopClip)
                {
                    musicSource.clip = musicLoopClip;
                    musicSource.loop = true;
                }
                float duckFactor = duckTimer > 0f ? Mathf.Pow(10f, settings.audio.musicDuckDb / 20f) : 1f;
                musicSource.volume = baseMusic * duckFactor;
                if (Application.isPlaying && musicSource.clip != null && !musicSource.isPlaying)
                {
                    musicSource.Play();
                }
            }
        }

        public void Tick(float dt, bool ufoActive)
        {
            if (duckTimer > 0f) duckTimer = Mathf.Max(0f, duckTimer - dt);
            SyncSettings();

            if (ufoLoopSource != null && ufoLoopClip != null && settings != null)
            {
                ufoLoopSource.clip = ufoLoopClip;
                ufoLoopSource.loop = true;
                ufoLoopSource.volume = settings.audio.masterVolume * 0.45f;
                if (ufoActive && Application.isPlaying)
                {
                    if (!ufoLoopSource.isPlaying) ufoLoopSource.Play();
                }
                else if (ufoLoopSource.isPlaying)
                {
                    ufoLoopSource.Stop();
                }
            }
        }

        public void HandleEvent(GameEvent ev)
        {
            if (settings == null || sfxSource == null) return;
            float vol = settings.audio.masterVolume;

            switch (ev.Kind)
            {
                case GameEventKind.FormationStepped:
                    if (stepNotes != null && stepNotes.Length > 0)
                    {
                        var clip = stepNotes[Mathf.Abs(ev.Value) % stepNotes.Length];
                        PlayOneShot(clip, vol * (ev.Flag ? 0.95f : 0.75f), 1f);
                    }
                    break;
                case GameEventKind.ShotFired:
                    PlayOneShot(ev.Powerup == PowerupKind.TwinShot && twinShootClip != null ? twinShootClip : shootClip, vol * 0.7f, 1f);
                    break;
                case GameEventKind.InvaderKilled:
                    float pitch = ev.Rank == InvaderRank.Scout ? 1.22f : (ev.Rank == InvaderRank.Guard ? 1.08f : 0.96f);
                    PlayOneShot(alienPopClip, vol * 0.8f, pitch);
                    break;
                case GameEventKind.BunkerEroded:
                case GameEventKind.ShotsCancelled:
                    PlayOneShot(bunkerHitClip, vol * 0.65f, 1f);
                    break;
                case GameEventKind.UfoDestroyed:
                    PlayOneShot(ufoBoomClip, vol * 0.95f, 1f);
                    break;
                case GameEventKind.PowerupCollected:
                    PlayOneShot(powerupClip, vol * 0.9f, 1f);
                    break;
                case GameEventKind.CannonDestroyed:
                    duckTimer = settings.audio.musicDuckDuration;
                    PlayOneShot(cannonDeathClip, vol, 1f);
                    break;
                case GameEventKind.WaveCleared:
                    duckTimer = settings.audio.musicDuckDuration;
                    PlayOneShot(waveClearClip, vol * 0.95f, 1f);
                    break;
                case GameEventKind.GameOver:
                    duckTimer = settings.audio.musicDuckDuration;
                    PlayOneShot(gameOverClip, vol, 1f);
                    break;
            }
        }

        void PlayOneShot(AudioClip clip, float volume, float pitch)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip, volume);
        }
    }
}
