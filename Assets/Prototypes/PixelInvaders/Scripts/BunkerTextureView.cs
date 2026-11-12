using UnityEngine;

namespace Prototypes.PixelInvaders
{
    /// <summary>
    /// Renders a single 12x8 BunkerGrid onto one upright alpha-clipped URP Lit quad using a live 24x16
    /// Point-filtered Texture2D (2x2 pixels per cell with dark outlines). Casts eroded 3D shadows onto the deck.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class BunkerTextureView : MonoBehaviour
    {
        static readonly Color32 ClearColor = new Color32(0, 0, 0, 0);
        static readonly Color32 OutlineBlack = new Color32(9, 12, 18, 255);     // #090C12
        static readonly Color32 IntactEmerald = new Color32(56, 217, 115, 255); // #38D973
        static readonly Color32 DamagedMoss = new Color32(30, 130, 76, 255);    // #1E824C

        MeshRenderer meshRenderer;
        MaterialPropertyBlock propBlock;
        Texture2D runtimeTexture;
        Color32[] pixelBuffer;
        BunkerGrid boundGrid;
        int lastVersion = -1;

        public int LastRenderedVersion => lastVersion;
        public Texture2D RuntimeTexture => runtimeTexture;

        void OnEnable()
        {
            if (runtimeTexture == null)
            {
                Sync(new BunkerGrid(transform.localPosition.x, transform.localPosition.z));
            }
            else
            {
                BindTextureBlock();
            }
        }

        public void Initialize(Material sharedLitMaterial, Vector2 worldSize, float x, float z, float tiltXDeg = 35f)
        {
            EnsureComponents();
            if (sharedLitMaterial != null) meshRenderer.sharedMaterial = sharedLitMaterial;
            transform.localPosition = new Vector3(x, 0.02f, z);
            transform.localRotation = Quaternion.Euler(tiltXDeg, 0f, 0f);
            transform.localScale = new Vector3(worldSize.x, worldSize.y, 1f);
            boundGrid = null;
            lastVersion = -1;
        }

        public void Sync(BunkerGrid grid)
        {
            if (grid == null) return;
            EnsureComponents();
            int texW = grid.Cols * 2;
            int texH = grid.Rows * 2;
            if (runtimeTexture == null || runtimeTexture.width != texW || runtimeTexture.height != texH)
            {
                if (runtimeTexture != null) DestroyImmediate(runtimeTexture);
                runtimeTexture = new Texture2D(texW, texH, TextureFormat.RGBA32, false)
                {
                    name = $"BunkerRuntimeTex_{name}",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                };
                pixelBuffer = new Color32[texW * texH];
                boundGrid = null;
                lastVersion = -1;
            }

            if (boundGrid == grid && lastVersion == grid.Version)
            {
                BindTextureBlock();
                return;
            }
            boundGrid = grid;
            lastVersion = grid.Version;

            for (int i = 0; i < pixelBuffer.Length; i++) pixelBuffer[i] = ClearColor;

            for (int r = 0; r < grid.Rows; r++)
            {
                for (int c = 0; c < grid.Cols; c++)
                {
                    var state = grid.GetCellVisualState(c, r);
                    if (state == BunkerCellState.Empty) continue;

                    Color32 fill = state == BunkerCellState.DamagedEdge ? DamagedMoss : IntactEmerald;
                    bool emptyLeft = grid.GetCellVisualState(c - 1, r) == BunkerCellState.Empty;
                    bool emptyRight = grid.GetCellVisualState(c + 1, r) == BunkerCellState.Empty;
                    bool emptyDown = grid.GetCellVisualState(c, r - 1) == BunkerCellState.Empty;
                    bool emptyUp = grid.GetCellVisualState(c, r + 1) == BunkerCellState.Empty;

                    int px0 = c * 2;
                    int py0 = r * 2;
                    for (int dy = 0; dy < 2; dy++)
                    {
                        for (int dx = 0; dx < 2; dx++)
                        {
                            bool isOuterBorder =
                                (dx == 0 && emptyLeft && c == 0) ||
                                (dx == 1 && emptyRight && c == grid.Cols - 1) ||
                                (dy == 0 && emptyDown && r == 0) ||
                                (dy == 1 && emptyUp && r == grid.Rows - 1);
                            Color32 col = isOuterBorder ? OutlineBlack : fill;
                            pixelBuffer[(py0 + dy) * texW + (px0 + dx)] = col;
                        }
                    }
                }
            }

            runtimeTexture.SetPixels32(pixelBuffer);
            runtimeTexture.Apply(false, false);

            BindTextureBlock();
        }

        void BindTextureBlock()
        {
            EnsureComponents();
            if (runtimeTexture == null) return;
            meshRenderer.GetPropertyBlock(propBlock);
            propBlock.SetTexture("_BaseMap", runtimeTexture);
            propBlock.SetVector("_BaseMap_ST", new Vector4(1f, 1f, 0f, 0f));
            meshRenderer.SetPropertyBlock(propBlock);
        }

        void EnsureComponents()
        {
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            var filter = GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh == null) filter.sharedMesh = SpriteQuad.GetOrCreateQuadMesh();
            if (propBlock == null) propBlock = new MaterialPropertyBlock();
        }

        void OnDestroy()
        {
            if (runtimeTexture != null) DestroyImmediate(runtimeTexture);
        }
    }
}
