using System.Collections.Generic;
using UnityEngine;

namespace Prototypes.DioramaTurn
{
    /// <summary>Builds a room's pieces, support pillars, 3D bridges, reachable markers and hover cursor.</summary>
    public sealed class RoomView : MonoBehaviour
    {
        public DioramaTurnSettings settings;
        public Material[] blockMaterials = new Material[4];
        public Material startMaterial;
        public Material goalMaterial;
        public Material plinthMaterial;
        public Material bridgeMaterial;
        public Material cursorMaterial;
        [Tooltip("Blender models; when missing a primitive cube stands in.")] public GameObject plinthModel;
        public GameObject blockModel;
        public GameObject goalModel;
        public GameObject pillarModel;
        public GameObject bridgeModel;
        [Tooltip("Fallback sprite fields kept for compatibility.")] public Material bridgeSpriteMaterial;
        public Texture2D bridgeTexture;
        public TextAsset bridgeJson;

        GameObject root;
        GameObject cursor;
        GameObject reachableRoot;
        RoomData room;
        readonly List<SpriteQuad> sprites = new List<SpriteQuad>();

        public RoomData Room => room;

        /// <summary>The centre of a cell in world space. The room's floor centre is the world origin.</summary>
        public static Vector3 CellCentre(Cell c) => new Vector3(c.X - 2f, c.Y, c.Z - 2f);

        /// <summary>The walkable top face of a cell.</summary>
        public static Vector3 TopFace(Cell c) => new Vector3(c.X - 2f, c.Y + 0.5f, c.Z - 2f);

        public void Build(RoomData data)
        {
            Clear();
            room = data;
            root = new GameObject("Pieces");
            root.transform.SetParent(transform, false);

            Model(plinthModel, "Plinth", new Vector3(0f, -1.1f, 0f), Vector3.one, plinthMaterial);

            int index = 0;
            foreach (var piece in data.Pieces)
            {
                var at = CellCentre(piece.Cell);
                switch (piece.Kind)
                {
                    case PieceKind.Bridge:
                        var bridgeGo = Model(bridgeModel, $"Bridge {piece.Cell}", at, Vector3.one, bridgeMaterial);
                        if (BridgeRunsAlongZ(data, piece.Cell))
                            bridgeGo.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                        break;
                    case PieceKind.Start:
                        Model(blockModel, $"Start {piece.Cell}", at, Vector3.one, startMaterial);
                        BuildSupportPillars(data, piece.Cell);
                        break;
                    case PieceKind.Goal:
                        Model(goalModel, $"Goal {piece.Cell}", at, Vector3.one, goalMaterial);
                        BuildSupportPillars(data, piece.Cell);
                        break;
                    default:
                        Model(blockModel, $"Block {piece.Cell}", at, Vector3.one, blockMaterials[index % blockMaterials.Length]);
                        BuildSupportPillars(data, piece.Cell);
                        break;
                }
                index++;
            }

            cursor = Cube("Cursor", TopFace(data.Start) + Vector3.up * 0.025f,
                new Vector3(0.66f, 0.035f, 0.66f), cursorMaterial);
            cursor.SetActive(false);
        }

        void BuildSupportPillars(RoomData data, Cell top)
        {
            for (int py = 0; py < top.Y; py++)
            {
                var below = new Cell(top.X, py, top.Z);
                if (data.Contains(below)) continue;
                Model(pillarModel, $"Pillar {below}", CellCentre(below), Vector3.one, plinthMaterial);
            }
        }

        static bool BridgeRunsAlongZ(RoomData data, Cell cell)
        {
            bool zNeighbour = data.Contains(cell + new Cell(0, 0, 1)) || data.Contains(cell + new Cell(0, 0, -1));
            bool xNeighbour = data.Contains(cell + new Cell(1, 0, 0)) || data.Contains(cell + new Cell(-1, 0, 0));
            return zNeighbour && !xNeighbour;
        }

        public void ShowReachable(IEnumerable<Cell> cells)
        {
            if (reachableRoot != null) Kill(reachableRoot);
            if (root == null || cells == null) return;
            reachableRoot = new GameObject("Reachable");
            reachableRoot.transform.SetParent(root.transform, false);
            int index = 0;
            foreach (var cell in cells)
            {
                var dot = Cube($"Reachable {index++}", TopFace(cell) + Vector3.up * 0.03f,
                    new Vector3(0.18f, 0.03f, 0.18f), cursorMaterial);
                dot.transform.SetParent(reachableRoot.transform, false);
                dot.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            }
        }

        public void ShowCursor(Cell? cell)
        {
            if (cursor == null) return;
            if (!cell.HasValue) { cursor.SetActive(false); return; }
            cursor.SetActive(true);
            cursor.transform.localPosition = TopFace(cell.Value) + Vector3.up * 0.025f;
        }

        /// <summary>Turns any sprite quads to face the camera.</summary>
        public void FaceSprites(float cameraYawDeg)
        {
            foreach (var sprite in sprites)
                if (sprite != null) sprite.Face(cameraYawDeg);
        }

        public void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                child.SetActive(false);
                Kill(child);
            }
            root = null;
            cursor = null;
            reachableRoot = null;
            room = null;
            sprites.Clear();
        }

        GameObject Cube(string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            var collider = go.GetComponent<Collider>();
            if (collider != null) Kill(collider);
            var renderer = go.GetComponent<Renderer>();
            if (material != null) renderer.sharedMaterial = material;
            return go;
        }

        /// <summary>Places a Blender model, or a cube when the model is missing (the greybox fallback).</summary>
        GameObject Model(GameObject model, string name, Vector3 position, Vector3 scale, Material material)
        {
            if (model == null) return Cube(name, position, scale, material);
            var go = Instantiate(model, root.transform, false);
            go.name = name;
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            SetMaterial(go, material);
            return go;
        }

        static void SetMaterial(GameObject go, Material material)
        {
            if (material == null) return;
            foreach (var renderer in go.GetComponentsInChildren<MeshRenderer>()) renderer.sharedMaterial = material;
        }

        static void Kill(Object target)
        {
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }
    }
}
