using System.Collections.Generic;
using UnityEngine;

namespace Prototypes.DioramaTurn
{
    /// <summary>Draws one dotted ribbon per screen join of the current view: small diamonds along the link.</summary>
    public sealed class LinkRibbons : MonoBehaviour
    {
        public DioramaTurnSettings settings;
        public RoomView room;
        public Material ribbonMaterial;

        GameObject root;
        int joinCount;

        public int Count => joinCount;

        public void ShowJoins(IEnumerable<(Cell A, Cell B)> joins)
        {
            Clear();
            if (room == null) return;
            root = new GameObject("Ribbons");
            root.transform.SetParent(transform, false);
            int index = 0;
            foreach (var (a, b) in joins)
            {
                var from = RoomView.TopFace(a) + Vector3.up * 0.24f;
                var to = RoomView.TopFace(b) + Vector3.up * 0.24f;
                float length = Vector3.Distance(from, to);
                int dots = Mathf.Clamp(Mathf.RoundToInt(length / 0.2f), 2, 24);
                for (int i = 0; i <= dots; i++)
                {
                    var dot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    dot.name = $"Join {index} dot {i}";
                    dot.transform.SetParent(root.transform, false);
                    dot.transform.position = Vector3.Lerp(from, to, i / (float)dots);
                    dot.transform.localScale = Vector3.one * 0.07f;
                    dot.transform.localRotation = Quaternion.Euler(45f, 45f, 45f);
                    var collider = dot.GetComponent<Collider>();
                    if (collider != null) Kill(collider);
                    dot.GetComponent<Renderer>().sharedMaterial = ribbonMaterial;
                }
                index++;
            }
            joinCount = index;
        }

        public void Clear()
        {
            if (root != null) Kill(root);
            root = null;
            joinCount = 0;
        }

        static void Kill(Object target)
        {
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }
    }
}
