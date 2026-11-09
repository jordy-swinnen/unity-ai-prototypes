using UnityEngine;

namespace Prototypes.MedievalChess
{
    /// <summary>
    /// Small bursts of particles tied to the feel cues: dust at a footfall, sparks on a blow and embers from a collapse.
    /// The effect strength scales how many there are, and at 0 there are none. Particles are emitted by code, so a
    /// test can count what was emitted.
    /// </summary>
    public sealed class EffectsView : MonoBehaviour
    {
        public MedievalChessSettings settings;
        public ParticleSystem dust, embers, sparks;

        public int EmittedCount { get; private set; }
        public EffectKind LastKind { get; private set; }

        public void ResetCount()
        {
            EmittedCount = 0;
            LastKind = EffectKind.None;
        }

        public void Burst(EffectKind kind, Vector3 position, float strength)
        {
            if (kind == EffectKind.None || strength <= 0f) return;
            var f = settings.feel;
            switch (kind)
            {
                case EffectKind.Dust:
                    Emit(dust, position + Vector3.up * 0.03f, Mathf.CeilToInt(f.dustCount * strength), 0.5f, 0.15f, 0.7f * f.dustLife, f.dustLife, 0.25f, 0.45f, new Color(0.62f, 0.56f, 0.5f, 0.45f));
                    break;
                case EffectKind.Embers:
                    Emit(embers, position + Vector3.up * 0.3f, Mathf.CeilToInt(f.emberCount * strength), 0.6f, 1.3f, 0.7f * f.emberLife, f.emberLife, 0.07f, 0.14f, new Color(1f, 0.5f, 0.15f, 1f));
                    break;
                case EffectKind.Sparks:
                    Emit(sparks, position + Vector3.up * 0.5f, Mathf.CeilToInt(f.sparkCount * strength), 2.2f, 1.4f, 0.25f, 0.5f, 0.05f, 0.1f, new Color(1f, 0.85f, 0.45f, 1f));
                    break;
            }
            LastKind = kind;
        }

        void Emit(ParticleSystem system, Vector3 at, int count, float sideSpeed, float upSpeed, float lifeMin, float lifeMax, float sizeMin, float sizeMax, Color color)
        {
            if (system == null) { EmittedCount += count; return; }
            for (int i = 0; i < count; i++)
            {
                var p = new ParticleSystem.EmitParams
                {
                    position = at + Random.insideUnitSphere * 0.08f,
                    velocity = new Vector3(Random.Range(-sideSpeed, sideSpeed), Random.Range(0.3f, 1f) * upSpeed, Random.Range(-sideSpeed, sideSpeed)),
                    startLifetime = Random.Range(lifeMin, lifeMax),
                    startSize = Random.Range(sizeMin, sizeMax),
                    startColor = color,
                };
                system.Emit(p, 1);
            }
            EmittedCount += count;
        }
    }
}
