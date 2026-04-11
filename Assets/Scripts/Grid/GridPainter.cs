// ============================================
// Grid Painter
// ============================================
// PURPOSE: Lets the player interact with the grid — paint species into
//          the volume, erase cells, cycle species, pause, step, and
//          randomize. Raycasts into the grid's bounding volume.
// USAGE:   Attach to a GameObject. Assign simulation and camera refs.
//          Left-click = paint, Right-click = erase, Scroll = cycle species,
//          Space = pause, T = step, R = randomize, C = clear.
// ============================================

using UnityEngine;

public class GridPainter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridSimulation simulation;
    [SerializeField] private VoxelRenderer voxelRenderer;
    [SerializeField] private Camera cam;

    [Header("Brush")]
    [SerializeField, Range(1, 5)] private int brushRadius = 1;
    [SerializeField, Range(0f, 1f)] private float paintEnergy = 0.8f;

    [Header("Painting Mode")]
    [SerializeField] private PaintMode mode = PaintMode.Slice;
    [SerializeField, Range(0f, 1f), Tooltip("Y-layer as fraction of grid height (Slice mode).")]
    private float sliceHeight = 0.5f;

    // --- Private State ---
    private int _selectedSpecies = 1;
    private Vector3 _gridOrigin;
    private float _cellSpacing;

    // --- Properties ---
    public int SelectedSpecies => _selectedSpecies;

    private enum PaintMode { Slice, Surface }

    // --- Lifecycle ---

    private void Start()
    {
        if (!cam) cam = Camera.main;
        CacheGridLayout();
    }

    private void Update()
    {
        HandleInput();
        HandlePainting();
    }

    // --- Input ---

    private void HandleInput()
    {
        // Cycle species with scroll wheel
        float scroll = Input.mouseScrollDelta.y;
        if (scroll > 0f) CycleSpecies(1);
        else if (scroll < 0f) CycleSpecies(-1);

        // Pause
        if (Input.GetKeyDown(KeyCode.Space)) simulation.TogglePause();

        // Step (one tick while paused)
        if (Input.GetKeyDown(KeyCode.T)) simulation.Step();

        // Randomize
        if (Input.GetKeyDown(KeyCode.R)) simulation.Randomize();

        // Clear
        if (Input.GetKeyDown(KeyCode.C)) simulation.Clear();
    }

    private void HandlePainting()
    {
        bool painting = Input.GetMouseButton(0);
        bool erasing = Input.GetMouseButton(1);
        if (!painting && !erasing) return;

        Vector3Int? gridPos = GetGridPosition();
        if (!gridPos.HasValue) return;

        PaintBrush(gridPos.Value, painting ? _selectedSpecies : 0, painting ? paintEnergy : 0f);
    }

    // --- Painting ---

    private void PaintBrush(Vector3Int center, int speciesIndex, float energy)
    {
        int r = brushRadius - 1;

        for (int dz = -r; dz <= r; dz++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    if (dx * dx + dy * dy + dz * dz > r * r + r) continue;

                    int x = center.x + dx;
                    int y = center.y + dy;
                    int z = center.z + dz;

                    if (x < 0 || x >= simulation.Width) continue;
                    if (y < 0 || y >= simulation.Height) continue;
                    if (z < 0 || z >= simulation.Depth) continue;

                    simulation.SetCell(x, y, z, speciesIndex, energy);
                }
            }
        }
    }

    private void CycleSpecies(int direction)
    {
        int count = simulation.Config.Species.Count;
        if (count == 0) return;

        _selectedSpecies += direction;
        if (_selectedSpecies < 1) _selectedSpecies = count;
        if (_selectedSpecies > count) _selectedSpecies = 1;
    }

    // --- Raycasting ---

    private Vector3Int? GetGridPosition()
    {
        if (mode == PaintMode.Slice)
            return GetSlicePosition();

        return GetSurfacePosition();
    }

    private Vector3Int? GetSlicePosition()
    {
        int sliceY = Mathf.RoundToInt(sliceHeight * (simulation.Height - 1));

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        float planeY = _gridOrigin.y + sliceY * _cellSpacing;
        Plane plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));

        if (!plane.Raycast(ray, out float enter)) return null;

        Vector3 hitPoint = ray.GetPoint(enter);
        return WorldToGrid(hitPoint);
    }

    private Vector3Int? GetSurfacePosition()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // Raycast against the grid's bounding box
        Bounds bounds = GetGridBounds();
        if (!bounds.IntersectRay(ray, out float enter)) return null;

        Vector3 hitPoint = ray.GetPoint(enter);
        return WorldToGrid(hitPoint);
    }

    // --- Coordinate Conversion ---

    private Vector3Int? WorldToGrid(Vector3 worldPos)
    {
        Vector3 local = (worldPos - _gridOrigin) / _cellSpacing;
        int x = Mathf.RoundToInt(local.x);
        int y = Mathf.RoundToInt(local.y);
        int z = Mathf.RoundToInt(local.z);

        if (x < 0 || x >= simulation.Width) return null;
        if (y < 0 || y >= simulation.Height) return null;
        if (z < 0 || z >= simulation.Depth) return null;

        return new Vector3Int(x, y, z);
    }

    private Bounds GetGridBounds()
    {
        Vector3 cellExtent = new Vector3(
            simulation.Width * _cellSpacing,
            simulation.Height * _cellSpacing,
            simulation.Depth * _cellSpacing
        );

        // Center at midpoint of all cell positions (grid is symmetric around origin)
        Vector3 center = _gridOrigin + new Vector3(
            (simulation.Width - 1) * _cellSpacing * 0.5f,
            (simulation.Height - 1) * _cellSpacing * 0.5f,
            (simulation.Depth - 1) * _cellSpacing * 0.5f
        );

        return new Bounds(center, cellExtent);
    }

    // --- Helpers ---

    private void CacheGridLayout()
    {
        _cellSpacing = voxelRenderer ? voxelRenderer.CellSpacing : 1f;
        _gridOrigin = voxelRenderer ? voxelRenderer.GridOrigin : new Vector3(
            -(simulation.Width - 1) * _cellSpacing * 0.5f,
            -(simulation.Height - 1) * _cellSpacing * 0.5f,
            -(simulation.Depth - 1) * _cellSpacing * 0.5f
        );
    }

    private void OnGUI()
    {
        if (simulation.Config.Species.Count == 0) return;

        string speciesName = simulation.Config.Species[_selectedSpecies - 1].name;
        string status = simulation.IsPaused ? "PAUSED" : "RUNNING";

        GUI.Label(new Rect(10, 10, 300, 20), $"Species: {speciesName} (scroll to change)");
        GUI.Label(new Rect(10, 30, 300, 20), $"Tick: {simulation.TickCount} | {status}");
        GUI.Label(new Rect(10, 50, 300, 20), "LMB=Paint RMB=Erase Space=Pause T=Step R=Random C=Clear");
    }
}

// ============================================
// SETUP
// ============================================
// 1. Attach to a GameObject in the scene
// 2. Assign the GridSimulation, VoxelRenderer, and Camera references
// 3. Set brush radius and paint energy
// 4. Choose paint mode: Slice (paints on a Y-plane) or Surface
//    (paints on the grid bounding box surface)
// 5. Controls: LMB=paint, RMB=erase, Scroll=cycle species,
//    Space=pause, T=step, R=randomize, C=clear
// ============================================
