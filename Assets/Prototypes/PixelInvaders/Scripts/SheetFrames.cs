using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prototypes.PixelInvaders
{
    /// <summary>
    /// Parses an Aseprite json-array sprite sheet export into frame rectangles, durations and tags,
    /// and computes bottom-left UV scale/offset vectors for URP Lit quads.
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
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Aseprite sheet json is empty");
            }
            var data = JsonUtility.FromJson<JSheet>(json);
            if (data == null || data.frames == null || data.frames.Length == 0 ||
                data.meta == null || data.meta.size == null || data.meta.size.w <= 0 || data.meta.size.h <= 0)
            {
                throw new ArgumentException("Not a valid Aseprite json-array sheet: missing frames or meta.size");
            }
            var sheet = new SheetFrames
            {
                Width = data.meta.size.w,
                Height = data.meta.size.h,
            };
            foreach (var f in data.frames)
            {
                sheet.rects.Add(new RectInt(f.frame.x, f.frame.y, f.frame.w, f.frame.h));
                sheet.durations.Add(f.duration / 1000f);
            }
            if (data.meta.frameTags != null)
            {
                foreach (var t in data.meta.frameTags)
                {
                    if (!string.IsNullOrEmpty(t.name))
                    {
                        sheet.tags[t.name] = (t.from, t.to);
                    }
                }
            }
            return sheet;
        }

        public RectInt Rect(int frame) => rects[Mathf.Clamp(frame, 0, rects.Count - 1)];
        public float Duration(int frame) => durations[Mathf.Clamp(frame, 0, durations.Count - 1)];

        public bool TryGetTag(string name, out int from, out int to)
        {
            if (tags.TryGetValue(name, out var range))
            {
                from = range.From;
                to = range.To;
                return true;
            }
            from = 0;
            to = 0;
            return false;
        }

        public void Uv(int frame, out Vector2 scale, out Vector2 offset)
        {
            var r = Rect(frame);
            scale = new Vector2((float)r.width / Width, (float)r.height / Height);
            offset = new Vector2((float)r.x / Width, 1f - (float)(r.y + r.height) / Height);
        }
    }
}
