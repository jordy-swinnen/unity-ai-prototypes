using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Prototypes.DioramaTurn
{
    /// <summary>Draws one dotted ribbon per screen join of the current view. Stage 2: a dotted line, no texture.</summary>
    public sealed class LinkRibbons : MonoBehaviour
    {
        public DioramaTurnSettings settings;
        public RoomView room;
        public Material ribbonMaterial;

        GameObject root;
        readonly List<LineRenderer> lines = new List<LineRenderer>();

        public int Count => lines.Count;

        public void ShowJoins(IEnumerable<(Cell A, Cell B)> joins)
        {
            Clear();
            if (room == null) return;
            root = new GameObject("Ribbons");
            root.transform.SetParent(transform, false);
            int index = 0;
            foreach (var (a, b) in joins)
            {
                var line = new GameObject($"Join {index++}").AddComponent<LineRenderer>();
                line.transform.SetParent(root.transform, false);
                Configure(line);
                var from = RoomView.TopFace(a) + Vector3.up * 0.2f;
                var to = RoomView.TopFace(b) + Vector3.up * 0.2f;
                line.positionCount = 33;
                for (int i = 0; i <= 32; i++) line.SetPosition(i, Vector3.Lerp(from, to, i / 32f));
                lines.Add(line);
            }
        }

        public void Clear()
        {
            if (root != null)
            {
                if (Application.isPlaying) Destroy(root);
                else DestroyImmediate(root);
            }
            root = null;
            lines.Clear();
        }

        void Configure(LineRenderer line)
        {
            if (ribbonMaterial != null) line.sharedMaterial = ribbonMaterial;
            line.useWorldSpace = true;
            line.widthMultiplier = 0.08f;
            line.widthCurve = Beads();
            line.numCapVertices = 0;
            line.numCornerVertices = 0;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
        }

        /// <summary>A width curve that goes full width and to nothing, so the line reads as dots.</summary>
        static AnimationCurve Beads()
        {
            const int keys = 24;
            var frames = new Keyframe[keys + 1];
            for (int i = 0; i <= keys; i++)
                frames[i] = new Keyframe(i / (float)keys, i % 2 == 0 ? 1f : 0f);
            return new AnimationCurve(frames);
        }
    }
}
