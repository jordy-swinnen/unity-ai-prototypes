using UnityEngine;

namespace Prototypes.MedievalChess
{
    /// <summary>
    /// Makes a torch flicker: its light and its flame wander a little around their base level. The wander is a plain
    /// function of time, so a test (or a frozen frame) can ask for any moment. The effect strength scales it, and at 0
    /// the torch burns steadily.
    /// </summary>
    public sealed class TorchFlicker : MonoBehaviour
    {
        public Light torchLight;
        public Transform flame;
        public MedievalChessSettings settings;
        public float seed;

        /// <summary>1 is normal. A draw lowers it, so the torches burn down a little.</summary>
        public float Dim { get; set; } = 1f;
        public float CurrentIntensity => torchLight ? torchLight.intensity : 0f;

        float baseIntensity;
        Vector3 baseScale;
        bool captured;
        float? frozenTime;

        /// <summary>
        /// The flicker factor at a time: 1 means the base level. It stays within 1 plus or minus the amplitude, and is
        /// exactly 1 when the amplitude is 0. Three sine waves of unrelated speeds give an uneven, flame-like wander.
        /// </summary>
        public static float Factor(float time, float seed, float amplitude, float speed)
        {
            if (amplitude <= 0f) return 1f;
            float t = time * speed;
            float wave = Mathf.Sin(t + seed * 12.9898f) * 0.5f
                       + Mathf.Sin(t * 2.31f + seed * 78.233f) * 0.3f
                       + Mathf.Sin(t * 5.17f + seed * 37.719f) * 0.2f;
            return 1f + wave * amplitude;
        }

        // The base level is read the first time it is needed, so the component also works in Edit Mode, where Awake does not run.
        void CaptureBase()
        {
            if (captured) return;
            captured = true;
            if (torchLight) baseIntensity = torchLight.intensity;
            if (flame) baseScale = flame.localScale;
        }

        /// <summary>Holds the flicker at one moment, for a frozen frame. Pass null to let it run again.</summary>
        public void Freeze(float? time)
        {
            frozenTime = time;
            Apply(time ?? Time.time);
        }

        void Update()
        {
            if (!frozenTime.HasValue) Apply(Time.time);
        }

        void Apply(float time)
        {
            CaptureBase();
            var look = settings.look;
            float amplitude = look.flickerAmplitude * settings.effectStrength;
            float f = Factor(time, seed, amplitude, look.flickerSpeed);
            if (torchLight) torchLight.intensity = baseIntensity * f * Dim;
            if (flame) flame.localScale = new Vector3(baseScale.x * (1f + (f - 1f) * 0.5f), baseScale.y * f * Mathf.Lerp(0.6f, 1f, Dim), baseScale.z * (1f + (f - 1f) * 0.5f));
        }
    }
}
