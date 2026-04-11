// ============================================
// Voxel Renderer
// ============================================
// PURPOSE: Renders all living grid cells as GPU-instanced meshes in a
//          single pass. Cell scale and color are modulated by energy,
//          producing pulsing, bioluminescent visuals.
// USAGE:   Attach to a GameObject. Assign the simulation, a mesh (cube
//          or sphere), and a material with GPU instancing enabled.
//          Renders automatically each frame — no per-cell GameObjects.
// ============================================

using System.Collections.Generic;
using UnityEngine;

public class VoxelRenderer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridSimulation simulation;

    [Header("Mesh")]
    [SerializeField] private Mesh mesh;
    [SerializeField] private Material material;

    [Header("Layout")]
    [SerializeField] private float cellSize = 0.5f;
    [SerializeField] private float cellSpacing = 1f;

    // --- Properties ---
    public float CellSpacing => cellSpacing;
    public Vector3 GridOrigin => _gridOrigin;

    // --- Private State ---
    private const int BATCH_SIZE = 1023;
    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");

    private List<Matrix4x4> _matrices;
    private List<Vector4> _colors;
    private Matrix4x4[] _batchMatrices;
    private Vector4[] _batchColors;
    private MaterialPropertyBlock _propertyBlock;
    private RenderParams _renderParams;
    private Vector3 _gridOrigin;

    // --- Lifecycle ---

    private void Awake()
    {
        CalculateGridOrigin();
    }

    private void Start()
    {
        _matrices = new List<Matrix4x4>(4096);
        _colors = new List<Vector4>(4096);
        _batchMatrices = new Matrix4x4[BATCH_SIZE];
        _batchColors = new Vector4[BATCH_SIZE];
        _propertyBlock = new MaterialPropertyBlock();

        _renderParams = new RenderParams(material)
        {
            matProps = _propertyBlock
        };
    }

    private void LateUpdate()
    {
        if (!simulation || !mesh || !material) return;

        CollectVisibleCells();
        DrawBatches();
    }

    // --- Rendering ---

    private void CollectVisibleCells()
    {
        _matrices.Clear();
        _colors.Clear();

        GridCell[] grid = simulation.GetCurrentGrid();
        if (grid == null) return;

        int w = simulation.Width;
        int h = simulation.Height;
        int d = simulation.Depth;
        var species = simulation.Config.Species;

        for (int z = 0; z < d; z++)
        {
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = x + y * w + z * w * h;
                    GridCell cell = grid[idx];
                    if (!cell.IsAlive) continue;

                    float scale = cell.energy * cellSize;
                    Vector3 position = _gridOrigin + new Vector3(x, y, z) * cellSpacing;

                    _matrices.Add(Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * scale));

                    SpeciesConfig sc = species[cell.speciesIndex - 1];
                    Color color = Color.Lerp(sc.EmissionColor, sc.Color, cell.energy);
                    _colors.Add(color);
                }
            }
        }
    }

    private void DrawBatches()
    {
        int total = _matrices.Count;
        int offset = 0;

        while (offset < total)
        {
            int count = Mathf.Min(BATCH_SIZE, total - offset);

            for (int i = 0; i < count; i++)
            {
                _batchMatrices[i] = _matrices[offset + i];
                _batchColors[i] = _colors[offset + i];
            }

            _propertyBlock.SetVectorArray(BaseColorProperty, _batchColors);
            _renderParams.matProps = _propertyBlock;

            Graphics.RenderMeshInstanced(_renderParams, mesh, 0, _batchMatrices, count);
            offset += count;
        }
    }

    // --- Helpers ---

    private void CalculateGridOrigin()
    {
        _gridOrigin = new Vector3(
            -(simulation.Width - 1) * cellSpacing * 0.5f,
            -(simulation.Height - 1) * cellSpacing * 0.5f,
            -(simulation.Depth - 1) * cellSpacing * 0.5f
        );
    }
}

// ============================================
// SETUP
// ============================================
// 1. Attach to a GameObject in the scene
// 2. Assign the GridSimulation reference
// 3. Assign a mesh (cube primitive recommended)
// 4. Create a material with GPU Instancing enabled
//    - URP/Lit or URP/Unlit, check "Enable GPU Instancing"
// 5. Adjust cellSize (visual scale) and cellSpacing (gap between cells)
// 6. For glow effects: enable emission on the material + add Bloom
//    in a Post-Processing Volume
// ============================================
