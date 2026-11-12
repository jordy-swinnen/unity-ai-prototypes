using UnityEngine;

namespace Prototypes.PixelInvaders
{
    /// <summary>
    /// Upright pixel-art quad tilted to face the 55-degree perspective camera.
    /// Uses a shared URP Lit (alpha-clipped, shadow-casting) or Unlit material and selects frames
    /// from an Aseprite sheet via _BaseMap_ST on a MaterialPropertyBlock.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class SpriteQuad : MonoBehaviour
    {
        public Vector2 size = Vector2.one;
        public Texture2D sheet;
        public TextAsset sheetJson;
        public int currentFrame;

        static Mesh sharedQuadMesh;
        MeshRenderer meshRenderer;
        MaterialPropertyBlock propBlock;
        SheetFrames parsedFrames;

        void OnEnable()
        {
            if (sheet != null && sheetJson != null)
            {
                ShowFrame(currentFrame);
            }
        }

        public SheetFrames Frames
        {
            get
            {
                EnsureInitialized();
                return parsedFrames;
            }
        }

        public void Configure(Material sharedMaterial, Texture2D texture, TextAsset json, Vector2 quadSize, float tiltXDeg = 35f)
        {
            EnsureInitialized();
            sheet = texture;
            sheetJson = json;
            parsedFrames = null;
            if (sharedMaterial != null) meshRenderer.sharedMaterial = sharedMaterial;
            SetSize(quadSize);
            transform.localRotation = Quaternion.Euler(tiltXDeg, 0f, 0f);
            BindSheet();
            ShowFrame(0);
        }

        public void SetSize(Vector2 quadSize)
        {
            size = quadSize;
            transform.localScale = new Vector3(quadSize.x, quadSize.y, 1f);
        }

        public void PlaceOnDeck(float x, float z, float yOffset = 0.02f)
        {
            transform.localPosition = new Vector3(x, yOffset, z);
        }

        public void ShowFrame(int frameIndex)
        {
            EnsureInitialized();
            BindSheet();
            if (parsedFrames == null || parsedFrames.FrameCount == 0) return;
            currentFrame = Mathf.Clamp(frameIndex, 0, parsedFrames.FrameCount - 1);
            parsedFrames.Uv(currentFrame, out Vector2 scale, out Vector2 offset);
            propBlock.SetVector("_BaseMap_ST", new Vector4(scale.x, scale.y, offset.x, offset.y));
            meshRenderer.SetPropertyBlock(propBlock);
        }

        public void SetEmission(Color hdrColor)
        {
            EnsureInitialized();
            BindSheet();
            propBlock.SetColor("_EmissionColor", hdrColor);
            meshRenderer.SetPropertyBlock(propBlock);
        }

        void EnsureInitialized()
        {
            if (meshRenderer != null && propBlock != null) return;
            meshRenderer = GetComponent<MeshRenderer>();
            var filter = GetComponent<MeshFilter>();
            if (filter.sharedMesh == null) filter.sharedMesh = GetOrCreateQuadMesh();
            if (propBlock == null) propBlock = new MaterialPropertyBlock();
        }

        void BindSheet()
        {
            if (propBlock == null) return;
            meshRenderer.GetPropertyBlock(propBlock);
            if (sheet != null) propBlock.SetTexture("_BaseMap", sheet);
            meshRenderer.SetPropertyBlock(propBlock);
            if (parsedFrames == null && sheetJson != null && !string.IsNullOrEmpty(sheetJson.text))
            {
                parsedFrames = SheetFrames.Parse(sheetJson.text);
            }
        }

        public static Mesh GetOrCreateQuadMesh()
        {
            if (sharedQuadMesh != null) return sharedQuadMesh;
            sharedQuadMesh = new Mesh { name = "PixelInvadersUprightQuad" };
            sharedQuadMesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, 0f),
                new Vector3(0.5f, 0f, 0f),
                new Vector3(0.5f, 1f, 0f),
                new Vector3(-0.5f, 1f, 0f),
            };
            sharedQuadMesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
            };
            sharedQuadMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            sharedQuadMesh.RecalculateNormals();
            sharedQuadMesh.RecalculateBounds();
            return sharedQuadMesh;
        }
    }
}
