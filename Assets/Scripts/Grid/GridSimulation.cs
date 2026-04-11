// ============================================
// Grid Simulation
// ============================================
// PURPOSE: Runs the 3D cellular automata. Owns the double-buffered grid,
//          evaluates birth/survival/interaction rules each tick, and
//          exposes a public API for painting, pausing, and querying cells.
// USAGE:   Attach to an empty GameObject. Assign a GridConfig asset.
//          Call Randomize() to seed, or paint cells via SetCell().
//          The simulation ticks automatically when not paused.
// ============================================

using System.Collections.Generic;
using UnityEngine;

public class GridSimulation : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private GridConfig config;

    // --- Private State ---
    private GridCell[] _currentGrid;
    private GridCell[] _nextGrid;
    private int[] _neighborCounts;
    private float _tickTimer;
    private int _tickCount;
    private bool _paused;
    private Dictionary<(int, int), SpeciesInteraction> _interactionLookup;

    // --- Properties ---
    public int Width => config.Width;
    public int Height => config.Height;
    public int Depth => config.Depth;
    public int TickCount => _tickCount;
    public bool IsPaused => _paused;
    public GridConfig Config => config;

    // --- Lifecycle ---

    private void Awake()
    {
        InitializeGrid();
        BuildInteractionLookup();
    }

    private void Update()
    {
        if (_paused) return;

        _tickTimer += Time.deltaTime;
        if (_tickTimer < config.TickInterval) return;

        _tickTimer -= config.TickInterval;
        Tick();
    }

    // --- Public API ---

    public GridCell GetCell(int x, int y, int z)
    {
        if (!InBounds(x, y, z)) return GridCell.Empty;
        return _currentGrid[FlatIndex(x, y, z)];
    }

    public void SetCell(int x, int y, int z, int speciesIndex, float energy)
    {
        if (!InBounds(x, y, z)) return;
        _currentGrid[FlatIndex(x, y, z)] = new GridCell(speciesIndex, energy);
    }

    public void Pause() => _paused = true;
    public void Resume() => _paused = false;
    public void TogglePause() => _paused = !_paused;

    public void Step()
    {
        Tick();
    }

    public void Clear()
    {
        for (int i = 0; i < _currentGrid.Length; i++)
            _currentGrid[i] = GridCell.Empty;

        _tickCount = 0;
    }

    public void Randomize()
    {
        Randomize(config.SeedDensity);
    }

    public void Randomize(float density)
    {
        Clear();

        int speciesCount = config.Species.Count;
        if (speciesCount == 0) return;

        int totalCells = config.Width * config.Height * config.Depth;
        int cellsPerSpecies = Mathf.RoundToInt(totalCells * density);

        for (int s = 0; s < speciesCount; s++)
        {
            int speciesIndex = s + 1;
            float birthEnergy = config.Species[s].BirthEnergy;

            for (int i = 0; i < cellsPerSpecies; i++)
            {
                int x = Random.Range(0, config.Width);
                int y = Random.Range(0, config.Height);
                int z = Random.Range(0, config.Depth);

                int idx = FlatIndex(x, y, z);
                if (_currentGrid[idx].IsAlive) continue;

                _currentGrid[idx] = new GridCell(speciesIndex, birthEnergy);
            }
        }
    }

    public GridCell[] GetCurrentGrid()
    {
        return _currentGrid;
    }

    // --- Simulation ---

    private void Tick()
    {
        int w = config.Width;
        int h = config.Height;
        int d = config.Depth;
        int speciesCount = config.Species.Count;

        for (int z = 0; z < d; z++)
        {
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = FlatIndex(x, y, z);
                    GridCell current = _currentGrid[idx];

                    CountNeighbors(x, y, z, speciesCount);

                    if (!current.IsAlive)
                    {
                        _nextGrid[idx] = EvaluateBirth(speciesCount);
                    }
                    else
                    {
                        _nextGrid[idx] = EvaluateSurvival(current, speciesCount);
                    }
                }
            }
        }

        // Swap buffers
        var temp = _currentGrid;
        _currentGrid = _nextGrid;
        _nextGrid = temp;
        _tickCount++;
    }

    private void CountNeighbors(int cx, int cy, int cz, int speciesCount)
    {
        // Clear counts (index 0 unused, species are 1-based)
        for (int i = 0; i <= speciesCount; i++)
            _neighborCounts[i] = 0;

        // 26-neighbor Moore neighborhood
        for (int dz = -1; dz <= 1; dz++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0 && dz == 0) continue;

                    int nx = cx + dx;
                    int ny = cy + dy;
                    int nz = cz + dz;

                    if (config.WrapEdges)
                    {
                        nx = ((nx % config.Width) + config.Width) % config.Width;
                        ny = ((ny % config.Height) + config.Height) % config.Height;
                        nz = ((nz % config.Depth) + config.Depth) % config.Depth;
                    }
                    else if (!InBounds(nx, ny, nz))
                    {
                        continue;
                    }

                    int si = _currentGrid[FlatIndex(nx, ny, nz)].speciesIndex;
                    if (si > 0)
                        _neighborCounts[si]++;
                }
            }
        }
    }

    private GridCell EvaluateBirth(int speciesCount)
    {
        int bestSpecies = 0;
        int bestCount = 0;

        for (int s = 1; s <= speciesCount; s++)
        {
            int count = _neighborCounts[s];
            SpeciesConfig sc = config.Species[s - 1];

            if (count == sc.BirthCount && count > bestCount)
            {
                bestSpecies = s;
                bestCount = count;
            }
        }

        if (bestSpecies == 0) return GridCell.Empty;
        return new GridCell(bestSpecies, config.Species[bestSpecies - 1].BirthEnergy);
    }

    private GridCell EvaluateSurvival(GridCell cell, int speciesCount)
    {
        SpeciesConfig sc = config.Species[cell.speciesIndex - 1];
        int sameNeighbors = _neighborCounts[cell.speciesIndex];

        float energy = cell.energy;

        // Survival range check
        if (sameNeighbors >= sc.SurvivalMin && sameNeighbors <= sc.SurvivalMax)
            energy += sc.RecoveryRate;
        else
            energy -= sc.DecayRate;

        // Inter-species interactions
        for (int s = 1; s <= speciesCount; s++)
        {
            if (s == cell.speciesIndex) continue;
            if (_neighborCounts[s] == 0) continue;

            int myIndex = GetSpeciesListIndex(cell.speciesIndex);
            int otherIndex = GetSpeciesListIndex(s);

            if (_interactionLookup.TryGetValue((myIndex, otherIndex), out var interaction))
            {
                float effect = interaction.Strength * _neighborCounts[s];
                energy += GetInteractionEffect(interaction.Type, true, effect);
            }
            else if (_interactionLookup.TryGetValue((otherIndex, myIndex), out var reverseInteraction))
            {
                float effect = reverseInteraction.Strength * _neighborCounts[s];
                energy += GetInteractionEffect(reverseInteraction.Type, false, effect);
            }
        }

        energy = Mathf.Clamp01(energy);
        if (energy <= 0f) return GridCell.Empty;

        return new GridCell(cell.speciesIndex, energy);
    }

    private float GetInteractionEffect(InteractionType type, bool isSpeciesA, float effect)
    {
        switch (type)
        {
            case InteractionType.Neutral:
                return 0f;
            case InteractionType.Predatory:
                return isSpeciesA ? effect : -effect;
            case InteractionType.Symbiotic:
                return effect;
            case InteractionType.Competitive:
                return -effect;
            default:
                return 0f;
        }
    }

    // --- Helpers ---

    private void InitializeGrid()
    {
        int totalCells = config.Width * config.Height * config.Depth;
        _currentGrid = new GridCell[totalCells];
        _nextGrid = new GridCell[totalCells];
        _neighborCounts = new int[config.Species.Count + 1];

        for (int i = 0; i < totalCells; i++)
        {
            _currentGrid[i] = GridCell.Empty;
            _nextGrid[i] = GridCell.Empty;
        }
    }

    private void BuildInteractionLookup()
    {
        _interactionLookup = new Dictionary<(int, int), SpeciesInteraction>();

        for (int i = 0; i < config.Interactions.Count; i++)
        {
            var interaction = config.Interactions[i];
            if (!interaction.SpeciesA || !interaction.SpeciesB) continue;

            int idxA = FindSpeciesIndex(interaction.SpeciesA);
            int idxB = FindSpeciesIndex(interaction.SpeciesB);
            if (idxA < 0 || idxB < 0) continue;

            _interactionLookup[(idxA, idxB)] = interaction;
        }
    }

    private int GetSpeciesListIndex(int speciesIndex)
    {
        return speciesIndex - 1;
    }

    private int FindSpeciesIndex(SpeciesConfig species)
    {
        for (int i = 0; i < config.Species.Count; i++)
        {
            if (config.Species[i] == species) return i;
        }
        return -1;
    }

    private int FlatIndex(int x, int y, int z)
    {
        return x + y * config.Width + z * config.Width * config.Height;
    }

    private bool InBounds(int x, int y, int z)
    {
        return x >= 0 && x < config.Width
            && y >= 0 && y < config.Height
            && z >= 0 && z < config.Depth;
    }
}

// ============================================
// SETUP
// ============================================
// 1. Attach to an empty GameObject in the scene
// 2. Assign a GridConfig asset with species and interactions defined
// 3. On play, call Randomize() to seed the grid (or use GridPainter)
// 4. Simulation ticks automatically at the configured interval
// 5. Use Pause/Resume/Step for debugging single ticks
// 6. Tweak GridConfig and SpeciesConfig in Inspector during play
// ============================================
