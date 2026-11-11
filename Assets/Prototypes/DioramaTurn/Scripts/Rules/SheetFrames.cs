using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prototypes.DioramaTurn
{
    /// <summary>
    /// The frames of an Aseprite sprite sheet, read from its json-array data file. A frame is shown on a
    /// quad by tiling and offset, so the sheet stays one Single sprite in Unity.
    /// </summary>
    public sealed class SheetFrames
    {
        [Serializable] class JRect { public int x, y, w, h; }
        [Serializable] class JFrame { public string filename; public JRect frame; public int duration; }
        [Serializable] class JSize { public int w, h; }
        [Serializable] class JTag { public string name; public int from, to; public string direction; }
        [Serializable] class JMeta { public JSize size; public JTag[] frameTags; }
        [Serializable] class JSheet { public JFrame[] frames; public JMeta meta; }

        readonly List<RectInt> rects = new List<RectInt>();
        readonly List<float> durations = new List<float>();
        readonly Dictionary<string, (int From, int To)> tags = new Dictionary<string, (int, int)>();

        public int FrameCount => rects.Count;
        public int Width { get; private set; }
        public int Height { get; private set; }

        public static SheetFrames Parse(string json)
        {
            var data = JsonUtility.FromJson<JSheet>(json);
            if (data == null || data.frames == null || data.frames.Length == 0 || data.meta == null || data.meta.size == null)
            {
                throw new ArgumentException("not an Aseprite json-array sheet: no frames or no meta.size");
            }
            var s = new SheetFrames { Width = data.meta.size.w, Height = data.meta.size.h };
            foreach (var f in data.frames)
            {
                s.rects.Add(new RectInt(f.frame.x, f.frame.y, f.frame.w, f.frame.h));
                s.durations.Add(f.duration / 1000f);
            }
            if (data.meta.frameTags != null)
            {
                foreach (var t in data.meta.frameTags) s.tags[t.name] = (t.from, t.to);
            }
            return s;
        }

        public RectInt Rect(int frame) => rects[frame];
        public float Duration(int frame) => durations[frame];

        public bool TryGetTag(string name, out int from, out int to)
        {
            if (tags.TryGetValue(name, out var t)) { from = t.From; to = t.To; return true; }
            from = to = 0;
            return false;
        }

        /// <summary>Material tiling and offset that show one frame (Unity's UV origin is bottom left).</summary>
        public void Uv(int frame, out Vector2 scale, out Vector2 offset)
        {
            var r = rects[frame];
            scale = new Vector2((float)r.width / Width, (float)r.height / Height);
            offset = new Vector2((float)r.x / Width, 1f - (float)(r.y + r.height) / Height);
        }
    }
}
