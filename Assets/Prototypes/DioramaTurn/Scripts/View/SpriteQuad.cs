using UnityEngine;

namespace Prototypes.DioramaTurn
{
    /// <summary>
    /// One pixel-art sprite: a quad with a URP Lit, alpha-clipped material. The sheet's JSON gives the
    /// frames; a frame is shown by tiling and offset, so the sheet stays one Single sprite in Unity.
    /// The quad's pivot is the bottom centre (the sprite's feet).
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class SpriteQuad : MonoBehaviour
    {
        public Vector2 size = Vector2.one;
        [Tooltip("The sheet texture, bound on first use so it survives a scene load.")] public Texture2D sheet;
        [Tooltip("The sheet's Aseprite json-array data.")] public TextAsset sheetJson;

        static Mesh quadMesh;
        MeshRenderer meshRenderer;
        MaterialPropertyBlock block;
        SheetFrames frames;

        public SheetFrames Frames => frames;

        public void SetSheet(Material sharedMaterial, Texture2D texture, TextAsset json, Vector2? newSize = null)
        {
            Ensure();
            sheet = texture;
            sheetJson = json;
            if (sharedMaterial != null) meshRenderer.sharedMaterial = sharedMaterial;
            Bind();
            SetSize(newSize ?? size);
        }

        public void SetSize(Vector2 newSize)
        {
            size = newSize;
            transform.localScale = new Vector3(newSize.x, newSize.y, 1f);
        }

        /// <summary>Show one frame of the sheet (material tiling and offset).</summary>
        public void ShowFrame(int index)
        {
            Ensure();
            Bind();
            if (frames == null || frames.FrameCount == 0) return;
            index = Mathf.Clamp(index, 0, frames.FrameCount - 1);
            frames.Uv(index, out var scale, out var offset);
            block.SetVector("_BaseMap_ST", new Vector4(scale.x, scale.y, offset.x, offset.y));
            meshRenderer.SetPropertyBlock(block);
        }

        /// <summary>Turn around Y only, so sprites always face the camera and never lie flat.</summary>
        public void Face(float cameraYawDeg)
        {
            transform.rotation = Quaternion.Euler(0f, cameraYawDeg, 0f);
        }

        /// <summary>Place the feet at a point, pushed toward the camera so they do not sink into a block.</summary>
        public void Place(Vector3 feet, Vector3 pushDirection, float pushAmount)
        {
            transform.position = feet + pushDirection * pushAmount;
        }

        void Ensure()
        {
            if (meshRenderer != null) return;
            meshRenderer = GetComponent<MeshRenderer>();
            var filter = GetComponent<MeshFilter>();
            if (filter.sharedMesh == null) filter.sharedMesh = QuadMesh();
            // A MonoBehaviour field initializer may not create a property block (pitfall 44).
            block = new MaterialPropertyBlock();
        }

        /// <summary>Rebinds the serialized texture and parses the sheet. A property block is not saved with a scene, so this runs again after a load.</summary>
        void Bind()
        {
            if (block == null) return;
            if (sheet != null) block.SetTexture("_BaseMap", sheet);
            meshRenderer.SetPropertyBlock(block);
            if (frames == null && sheetJson != null) frames = SheetFrames.Parse(sheetJson.text);
        }

        static Mesh QuadMesh()
        {
            if (quadMesh != null) return quadMesh;
            quadMesh = new Mesh { name = "DioramaTurn Sprite Quad" };
            quadMesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, 0f),
                new Vector3(0.5f, 0f, 0f),
                new Vector3(0.5f, 1f, 0f),
                new Vector3(-0.5f, 1f, 0f),
            };
            quadMesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            };
            quadMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            quadMesh.RecalculateNormals();
            quadMesh.RecalculateBounds();
            return quadMesh;
        }
    }
}
