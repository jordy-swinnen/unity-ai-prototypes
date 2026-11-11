using UnityEngine;

namespace Prototypes.DioramaTurn
{
    /// <summary>Plays the generated sounds for the simulation's events. The music comes later (FluidSynth).</summary>
    public sealed class DioramaAudio : MonoBehaviour
    {
        public DioramaTurnSettings settings;
        public AudioSource source;

        public AudioClip turnWhoosh;
        public AudioClip turnClunk;
        public AudioClip joinUp;
        public AudioClip joinDown;
        public AudioClip tapPluck;
        public AudioClip stepA;
        public AudioClip stepB;
        public AudioClip hopBoing;
        public AudioClip landing;
        public AudioClip bonk;
        public AudioClip tick;
        public AudioClip starArpeggio;
        public AudioClip starChime;
        public AudioClip pop;
        public AudioClip restart;

        int stepFlip;

        public void Play(GameEventKind kind)
        {
            switch (kind)
            {
                case GameEventKind.TurnStarted: One(turnWhoosh); break;
                case GameEventKind.TurnSettled: One(turnClunk); break;
                case GameEventKind.JoinFormed: One(joinUp); break;
                case GameEventKind.JoinBroken: One(joinDown); break;
                case GameEventKind.StepTaken: One(stepFlip++ % 2 == 0 ? stepA : stepB); break;
                case GameEventKind.HopTaken: One(hopBoing); break;
                case GameEventKind.Unreachable: One(bonk); break;
                case GameEventKind.TurnBlocked: One(tick); break;
                case GameEventKind.GoalReached: One(starArpeggio); break;
                case GameEventKind.Restarted: One(restart); break;
            }
        }

        public void Chime() => One(starChime);

        public void Pop() => One(pop);

        public void Tap() => One(tapPluck);

        void One(AudioClip clip)
        {
            if (source == null || clip == null) return;
            source.PlayOneShot(clip, settings != null ? settings.audio.sfx * settings.audio.master : 1f);
        }
    }
}
