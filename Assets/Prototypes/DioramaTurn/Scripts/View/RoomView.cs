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

        GameObject root;
        GameObject cursor;
        RoomData room;

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

            Cube("Plinth", new Vector3(0f, -0.8f, 0f), new Vector3(7f, 0.6f, 7f), plinthMaterial);

            int index = 0;
            foreach (var piece in data.Pieces)
            {
                var at = CellCentre(piece.Cell);
                switch (piece.Kind)
                {
                    case PieceKind.Bridge:
                        // A plank whose top sits on the cell's top face, where Pip's feet go.
                        Cube($"Bridge {piece.Cell}", new Vector3(at.x, piece.Cell.Y + 0.44f, at.z),
                            new Vector3(1f, 0.12f, 0.5f), bridgeMaterial);
                        break;
                    case PieceKind.Start:
                        Cube($"Start {piece.Cell}", at, Vector3.one, startMaterial);
                        break;
                    case PieceKind.Goal:
                        Cube($"Goal {piece.Cell}", at, Vector3.one, goalMaterial);
                        break;
                    default:
                        Cube($"Block {piece.Cell}", at, Vector3.one, blockMaterials[index % blockMaterials.Length]);
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

        public void Clear()
        {
            if (root != null) Kill(root);
            root = null;
            cursor = null;
            room = null;
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

        static void Kill(Object target)
        {
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }
    }
}
