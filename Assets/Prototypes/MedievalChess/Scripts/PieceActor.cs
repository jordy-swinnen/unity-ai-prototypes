using System.Collections.Generic;
using UnityEngine;

namespace Prototypes.MedievalChess
{
    /// <summary>
    /// One piece in the scene. It finds the model's named parts, sets the side colour, and applies a
    /// pose from a plan: a root move, a heading, a tilt, a size and a rotation for each part.
    /// It holds no rules.
    /// </summary>
    public sealed class PieceActor : MonoBehaviour
    {
        struct PartRest
        {
            public Transform Transform;
            public Quaternion Rotation;
        }

        readonly Dictionary<string, PartRest> parts = new Dictionary<string, PartRest>();
        Vector3 startPosition;
        float startYaw;
        Renderer[] renderers = new Renderer[0];
        MaterialPropertyBlock block;
        float appliedDissolve = -1f;
        static readonly int DissolveId = Shader.PropertyToID("_Dissolve");

        public PieceType Type { get; private set; }
        public Vector3 StartPosition => startPosition;

        /// <summary>
        /// Adds the actor to an instance of a piece model and sets its materials by the model's material names:
        /// "body" gets the side's material, "iron" the iron material and "trim" the gold trim (iron if none is given). The side (0 bone, 1 cinnabar) also picks
        /// the shoulders: round for bone, pointed for cinnabar, so the sides differ by shape and not only by colour.
        /// </summary>
        public static PieceActor Bind(GameObject instance, PieceType type, Material body, Material iron, int side = 0, Material trim = null)
        {
            var actor = instance.AddComponent<PieceActor>();
            actor.Type = type;
            foreach (var t in instance.GetComponentsInChildren<Transform>(true))
                actor.parts[t.name] = new PartRest { Transform = t, Rotation = t.localRotation };

            if (actor.parts.TryGetValue("ShoulderBone", out var roundShoulders)) roundShoulders.Transform.gameObject.SetActive(side == 0);
            if (actor.parts.TryGetValue("ShoulderRed", out var pointedShoulders)) pointedShoulders.Transform.gameObject.SetActive(side != 0);

            foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
            {
                var slots = r.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    string name = slots[i] != null ? slots[i].name : string.Empty;
                    if (name.StartsWith("body")) slots[i] = body;
                    else if (name.StartsWith("iron")) slots[i] = iron;
                    else if (name.StartsWith("trim")) slots[i] = trim != null ? trim : iron;
                }
                r.sharedMaterials = slots;
            }
            actor.renderers = instance.GetComponentsInChildren<Renderer>(true);
            return actor;
        }

        /// <summary>How burnt away the piece is, 0 whole to 1 gone. Set per renderer, so the side materials stay shared.</summary>
        void SetDissolve(float amount)
        {
            if (Mathf.Approximately(amount, appliedDissolve)) return;
            appliedDissolve = amount;
            if (block == null) block = new MaterialPropertyBlock();
            block.SetFloat(DissolveId, amount);
            foreach (var r in renderers) r.SetPropertyBlock(block);
        }

        public bool HasPart(string name) => parts.ContainsKey(name);

        public Transform Part(string name) => parts.TryGetValue(name, out var p) ? p.Transform : null;

        public void Place(Vector3 position, float yawDegrees)
        {
            startPosition = position;
            startYaw = yawDegrees;
            Apply(new ActorFrame { Yaw = yawDegrees });
        }

        public void Apply(ActorFrame frame)
        {
            gameObject.SetActive(frame.Visible);
            transform.position = startPosition + frame.RootOffset;
            transform.rotation = Quaternion.Euler(0f, frame.Yaw, 0f) * Quaternion.Euler(frame.Pitch, 0f, 0f);
            transform.localScale = Vector3.one * Mathf.Max(frame.Scale, 0.0001f);
            SetDissolve(frame.Dissolve);

            foreach (var kv in parts)
            {
                if (kv.Value.Transform == transform) continue;
                var euler = frame.PartEuler.TryGetValue(kv.Key, out var e) ? e : Vector3.zero;
                kv.Value.Transform.localRotation = kv.Value.Rotation * Quaternion.Euler(euler);
            }
        }
    }
}
