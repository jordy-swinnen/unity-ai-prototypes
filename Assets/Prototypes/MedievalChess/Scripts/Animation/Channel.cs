using System.Collections.Generic;
using UnityEngine;

namespace Prototypes.MedievalChess
{
    /// <summary>How a value moves toward a key. In starts slow, Out ends slow, InOut does both.</summary>
    public enum Ease
    {
        Linear,
        In,
        Out,
        InOut,
    }

    public struct Key
    {
        public float Time;
        public Vector3 Value;
        /// <summary>The easing of the stretch that ends at this key.</summary>
        public Ease Ease;
    }

    /// <summary>
    /// A value over time made of keys. Before the first key it holds the first value, after the last
    /// it holds the last. A one-number channel keeps its number in x.
    /// </summary>
    public sealed class Channel
    {
        readonly List<Key> keys = new List<Key>();

        public bool HasKeys => keys.Count > 0;

        public Channel Add(float time, Vector3 value, Ease ease = Ease.InOut)
        {
            if (keys.Count > 0 && time < keys[keys.Count - 1].Time)
                throw new System.ArgumentException($"Keys must be added in time order ({time} comes before {keys[keys.Count - 1].Time}).");
            keys.Add(new Key { Time = time, Value = value, Ease = ease });
            return this;
        }

        public Channel Add(float time, float value, Ease ease = Ease.InOut) => Add(time, new Vector3(value, 0f, 0f), ease);

        public Vector3 Evaluate(float t)
        {
            if (keys.Count == 0) return Vector3.zero;
            if (t <= keys[0].Time) return keys[0].Value;
            var last = keys[keys.Count - 1];
            if (t >= last.Time) return last.Value;

            for (int i = 1; i < keys.Count; i++)
            {
                if (t > keys[i].Time) continue;
                var a = keys[i - 1];
                var b = keys[i];
                float span = b.Time - a.Time;
                float u = span > 1e-6f ? (t - a.Time) / span : 1f;
                return Vector3.LerpUnclamped(a.Value, b.Value, Apply(b.Ease, u));
            }
            return last.Value;
        }

        public float EvaluateScalar(float t) => Evaluate(t).x;

        public static float Apply(Ease ease, float u)
        {
            u = Mathf.Clamp01(u);
            switch (ease)
            {
                case Ease.In: return u * u;
                case Ease.Out: return 1f - (1f - u) * (1f - u);
                case Ease.InOut: return u * u * (3f - 2f * u);
                default: return u;
            }
        }
    }
}
