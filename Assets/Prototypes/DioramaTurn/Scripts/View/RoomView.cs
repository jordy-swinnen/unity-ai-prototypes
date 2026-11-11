using UnityEngine;

namespace Prototypes.DioramaTurn
{
    /// <summary>Builds a room's pieces as simple shapes and keeps their cell positions. The stage 2 greybox.</summary>
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
        [Tooltip("The plank sprite for Bridge pieces.")] public Material bridgeSpriteMaterial;
        public Texture2D bridgeTexture;
        public TextAsset bridgeJson;

        GameObject root;
        GameObject cursor;
        RoomData room;
        readonly System.Collections.Generic.List<SpriteQuad> sprites = new System.Collections.Generic.List<SpriteQuad>();

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

            var plinth = Model(plinthModel, "Plinth", new Vector3(0f, -1.1f, 0f), Vector3.one, plinthMaterial);

            int index = 0;
            foreach (var piece in data.Pieces)
            {
                var at = CellCentre(piece.Cell);
                switch (piece.Kind)
                {
                    case PieceKind.Bridge:
                        var bridgeGo = new GameObject($"Bridge {piece.Cell}");
                        bridgeGo.transform.SetParent(root.transform, false);
                        var bridge = bridgeGo.AddComponent<SpriteQuad>();
                        bridge.SetSheet(bridgeSpriteMaterial, bridgeTexture, bridgeJson, new Vector2(1f, 1f));
                        bridgeGo.transform.position = new Vector3(at.x, piece.Cell.Y + 0.25f, at.z);
                        sprites.Add(bridge);
                        break;
                    case PieceKind.Start:
                        Model(blockModel, $"Start {piece.Cell}", at, Vector3.one, startMaterial);
                        break;
                    case PieceKind.Goal:
                        Model(goalModel, $"Goal {piece.Cell}", at, Vector3.one, goalMaterial);
                        break;
                    default:
                        Model(blockModel, $"Block {piece.Cell}", at, Vector3.one, blockMaterials[index % blockMaterials.Length]);
                        break;
                }
                index++;
            }

            cursor = Cube("Cursor", TopFace(data.Start) + Vector3.up * 0.02f,
                new Vector3(0.92f, 0.04f, 0.92f), cursorMaterial);
            cursor.SetActive(false);
        }

        public void ShowCursor(Cell? cell)
        {
            if (cursor == null) return;
            if (!cell.HasValue) { cursor.SetActive(false); return; }
            cursor.SetActive(true);
            cursor.transform.localPosition = TopFace(cell.Value) + Vector3.up * 0.02f;
        }

        /// <summary>Turns the bridge sprites to face the camera, like Pip and the star.</summary>
        public void FaceSprites(float cameraYawDeg)
        {
            foreach (var sprite in sprites)
                if (sprite != null) sprite.Face(cameraYawDeg);
        }

        public void Clear()
        {
            if (root != null) Kill(root);
            root = null;
            cursor = null;
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
