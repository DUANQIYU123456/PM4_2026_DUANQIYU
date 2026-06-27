// ============================================
// Backrooms Layout
// ============================================
// Plain-C# procedural layout model + generation passes
// for the Backrooms generator. No UnityEditor dependency,
// so it stays unit-testable and runtime-reusable.
//
// Grid model (cellSize lives on the tile set):
//   cells[W, D]      cell kind + ceiling tier
//   hEdges[W, D+1]   horizontal edges (lines of constant Z)
//   vEdges[W+1, D]   vertical   edges (lines of constant X)
//
// An edge separates two cells along one axis:
//   hEdges[col,row] separates cell (col,row-1) and (col,row)
//   vEdges[col,row] separates cell (col-1,row) and (col,row)
//
// Boundary handling is computed, not stored: see Effective*().
// An edge between a Floor and a non-Floor cell is always a
// solid Full wall; an edge between two non-Floor cells is nothing.
// ============================================

using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Ludocore
{
    public enum CellKind { Empty, Floor, Column }
    public enum Barrier { None, Full }   // None = open passage, Full = 3m wall
    public enum Opening { None, Door, Window }

    public struct Cell { public CellKind kind; public byte tier; }   // tier: ceiling height multiple (1 = base 3m)
    public struct Edge { public Barrier barrier; public Opening opening; }

    public enum LayoutMode { Maze, OpenRoom, RoomsAndCorridors, Halls }
    public enum ColumnPattern { None, Lattice, Clusters }

    [Serializable]
    public struct LayoutSettings
    {
        public int width;
        public int depth;
        public LayoutMode mode;
        public int extraOpenings;   // %  internal walls removed for loops
        public int doorChance;      // %  of open passages that get a door
        public int windowChance;    // %  of solid internal walls that get a window
        public int maxRoomSize;
        public int straightness;    // %  maze corridor straight-bias (longer straight runs)
        public int hallCount;       //    number of halls (Halls mode)
        public int corridorWidth;   //    corridor width 1-3 (Halls mode)
        public int maxCeilingTier;  //    max ceiling height multiple per room/hall (1 = uniform base)
        public int seed;            // 0 = random

        // --- Footprint & massing ---
        public int irregularity;        // %  bites taken out of the rectangular outline
        public int courtyards;          //    interior voids carved into the space
        public ColumnPattern columnPattern;
        public int columnDensity;       // %
        public int columnBigChance;     // %  chance a column is a thick 2x2 block
    }

    public class BackroomsLayout
    {
        public readonly int W;
        public readonly int D;
        public readonly Cell[,] cells;   // [W, D]
        public readonly Edge[,] hEdges;  // [W, D+1]
        public readonly Edge[,] vEdges;  // [W+1, D]

        // Empty cells reachable from the grid border (the building "outside").
        // Interior Empty cells (courtyards) are NOT exterior. Computed after generation.
        private bool[,] _exterior;

        public BackroomsLayout(int w, int d)
        {
            W = Mathf.Max(1, w);
            D = Mathf.Max(1, d);
            cells = new Cell[W, D];
            hEdges = new Edge[W, D + 1];
            vEdges = new Edge[W + 1, D];
        }

        //==================== ENTRY ====================

        public static BackroomsLayout Generate(LayoutSettings s)
        {
            var L = new BackroomsLayout(s.width, s.depth);
            Random.InitState(s.seed != 0 ? s.seed : Environment.TickCount);
            L.Init();
            L.CarveFootprint(s.irregularity, s.courtyards);
            L.PlaceColumns(s.columnPattern, s.columnDensity, s.columnBigChance);

            switch (s.mode)
            {
                case LayoutMode.Maze:
                    L.CarveMaze(s.straightness);
                    L.CarveExtraOpenings(s.extraOpenings);
                    break;
                case LayoutMode.OpenRoom:
                    L.OpenAllInternal();
                    break;
                case LayoutMode.RoomsAndCorridors:
                    L.PlaceRooms(s.maxRoomSize, s.maxCeilingTier);
                    L.CarveMaze(s.straightness);
                    L.CarveExtraOpenings(s.extraOpenings);
                    break;
                case LayoutMode.Halls:
                    L.CarveHalls(s.hallCount, s.corridorWidth, s.maxCeilingTier);
                    break;
            }

            L.AssignDoors(s.doorChance);
            L.AssignWindows(s.windowChance);
            L.ComputeExterior();
            return L;
        }

        //==================== INIT ====================

        private void Init()
        {
            for (int x = 0; x < W; x++)
                for (int z = 0; z < D; z++)
                    cells[x, z] = new Cell { kind = CellKind.Floor, tier = 1 };

            var wall = new Edge { barrier = Barrier.Full, opening = Opening.None };
            for (int x = 0; x < W; x++)
                for (int z = 0; z <= D; z++)
                    hEdges[x, z] = wall;
            for (int x = 0; x <= W; x++)
                for (int z = 0; z < D; z++)
                    vEdges[x, z] = wall;
        }

        //==================== QUERIES ====================

        public bool InBounds(int x, int z) => x >= 0 && x < W && z >= 0 && z < D;
        public bool IsFloor(int x, int z) => InBounds(x, z) && cells[x, z].kind == CellKind.Floor;

        /// Effective horizontal edge: boundary (Floor<->non-Floor) is a solid Full wall;
        /// between two non-Floor cells it is nothing; interior uses the stored state.
        public Edge EffectiveH(int col, int row)
        {
            bool a = IsFloor(col, row - 1);
            bool b = IsFloor(col, row);
            if (!a && !b) return new Edge { barrier = Barrier.None, opening = Opening.None };
            if (a != b) return new Edge { barrier = Barrier.Full, opening = Opening.None };
            return hEdges[col, row];
        }

        /// Effective vertical edge (see EffectiveH).
        public Edge EffectiveV(int col, int row)
        {
            bool a = IsFloor(col - 1, row);
            bool b = IsFloor(col, row);
            if (!a && !b) return new Edge { barrier = Barrier.None, opening = Opening.None };
            if (a != b) return new Edge { barrier = Barrier.Full, opening = Opening.None };
            return vEdges[col, row];
        }

        // Flood-fill Empty cells inward from the grid border to mark the "outside".
        public void ComputeExterior()
        {
            _exterior = new bool[W, D];
            var q = new Queue<Vector2Int>();
            void Seed(int x, int z)
            {
                if (InBounds(x, z) && cells[x, z].kind == CellKind.Empty && !_exterior[x, z])
                { _exterior[x, z] = true; q.Enqueue(new Vector2Int(x, z)); }
            }
            for (int x = 0; x < W; x++) { Seed(x, 0); Seed(x, D - 1); }
            for (int z = 0; z < D; z++) { Seed(0, z); Seed(W - 1, z); }

            while (q.Count > 0)
            {
                var c = q.Dequeue();
                Seed(c.x - 1, c.y); Seed(c.x + 1, c.y); Seed(c.x, c.y - 1); Seed(c.x, c.y + 1);
            }
        }

        // True if (x,z) is the building exterior: out of bounds, or an Empty cell
        // connected to the grid border (not an interior courtyard).
        public bool IsExteriorVoid(int x, int z)
        {
            if (!InBounds(x, z)) return true;
            if (cells[x, z].kind != CellKind.Empty) return false;
            return _exterior != null && _exterior[x, z];
        }

        public bool HasFullWallAtVertex(int vx, int vz)
        {
            if (vx > 0 && EffectiveH(vx - 1, vz).barrier == Barrier.Full) return true;
            if (vx < W && EffectiveH(vx, vz).barrier == Barrier.Full) return true;
            if (vz > 0 && EffectiveV(vx, vz - 1).barrier == Barrier.Full) return true;
            if (vz < D && EffectiveV(vx, vz).barrier == Barrier.Full) return true;
            return false;
        }

        //==================== LAYOUT: MAZE ====================

        // straightness (0-100): chance to keep heading the same direction, for
        // longer straight corridors instead of a twitchy warren.
        private void CarveMaze(int straightness)
        {
            if (!TryFindFloor(out var start)) return;

            var visited = new bool[W, D];
            var stack = new Stack<(Vector2Int cell, Vector2Int dir)>();
            visited[start.x, start.y] = true;
            stack.Push((start, Vector2Int.zero));

            while (stack.Count > 0)
            {
                var (cur, dirIn) = stack.Peek();
                var neighbors = UnvisitedFloorNeighbors(cur, visited);
                if (neighbors.Count == 0) { stack.Pop(); continue; }

                Vector2Int next;
                var straightAhead = cur + dirIn;
                if (dirIn != Vector2Int.zero && Random.Range(0, 100) < straightness &&
                    neighbors.Contains(straightAhead))
                    next = straightAhead;
                else
                    next = neighbors[Random.Range(0, neighbors.Count)];

                OpenBetween(cur, next);
                visited[next.x, next.y] = true;
                stack.Push((next, next - cur));
            }
        }

        private bool TryFindFloor(out Vector2Int cell)
        {
            // Try a random cell first, then scan, so unusual footprints still seed correctly.
            var r = new Vector2Int(Random.Range(0, W), Random.Range(0, D));
            if (IsFloor(r.x, r.y)) { cell = r; return true; }
            for (int x = 0; x < W; x++)
                for (int z = 0; z < D; z++)
                    if (IsFloor(x, z)) { cell = new Vector2Int(x, z); return true; }
            cell = default;
            return false;
        }

        private List<Vector2Int> UnvisitedFloorNeighbors(Vector2Int c, bool[,] visited)
        {
            var list = new List<Vector2Int>(4);
            void Try(int x, int z) { if (IsFloor(x, z) && !visited[x, z]) list.Add(new Vector2Int(x, z)); }
            Try(c.x - 1, c.y); Try(c.x + 1, c.y); Try(c.x, c.y - 1); Try(c.x, c.y + 1);
            return list;
        }

        private void OpenBetween(Vector2Int a, Vector2Int b)
        {
            if (b.x == a.x + 1) vEdges[b.x, a.y].barrier = Barrier.None;
            else if (b.x == a.x - 1) vEdges[a.x, a.y].barrier = Barrier.None;
            else if (b.y == a.y + 1) hEdges[a.x, b.y].barrier = Barrier.None;
            else if (b.y == a.y - 1) hEdges[a.x, a.y].barrier = Barrier.None;
        }

        private void CarveExtraOpenings(int pct)
        {
            for (int x = 0; x < W; x++)
                for (int z = 1; z < D; z++)
                    if (IsFloor(x, z - 1) && IsFloor(x, z) &&
                        hEdges[x, z].barrier == Barrier.Full && hEdges[x, z].opening == Opening.None &&
                        Random.Range(0, 100) < pct)
                        hEdges[x, z].barrier = Barrier.None;

            for (int x = 1; x < W; x++)
                for (int z = 0; z < D; z++)
                    if (IsFloor(x - 1, z) && IsFloor(x, z) &&
                        vEdges[x, z].barrier == Barrier.Full && vEdges[x, z].opening == Opening.None &&
                        Random.Range(0, 100) < pct)
                        vEdges[x, z].barrier = Barrier.None;
        }

        //==================== LAYOUT: OPEN ROOM ====================

        private void OpenAllInternal()
        {
            for (int x = 0; x < W; x++)
                for (int z = 1; z < D; z++)
                    if (IsFloor(x, z - 1) && IsFloor(x, z))
                        hEdges[x, z].barrier = Barrier.None;

            for (int x = 1; x < W; x++)
                for (int z = 0; z < D; z++)
                    if (IsFloor(x - 1, z) && IsFloor(x, z))
                        vEdges[x, z].barrier = Barrier.None;
        }

        //==================== LAYOUT: ROOMS & CORRIDORS ====================

        private void PlaceRooms(int maxRoomSize, int maxCeilingTier)
        {
            var occupied = new bool[W, D];
            int attempts = W * D * 2;
            int maxTier = Mathf.Max(1, maxCeilingTier);

            for (int i = 0; i < attempts; i++)
            {
                int rw = Random.Range(2, Mathf.Min(maxRoomSize, W) + 1);
                int rd = Random.Range(2, Mathf.Min(maxRoomSize, D) + 1);
                int rx = Random.Range(0, W - rw + 1);
                int rz = Random.Range(0, D - rd + 1);
                if (rx < 0 || rz < 0) continue;

                bool blocked = false;
                for (int x = Mathf.Max(0, rx - 1); x < Mathf.Min(W, rx + rw + 1) && !blocked; x++)
                    for (int z = Mathf.Max(0, rz - 1); z < Mathf.Min(D, rz + rd + 1) && !blocked; z++)
                        if (occupied[x, z]) blocked = true;
                if (blocked) continue;

                byte tier = (byte)Random.Range(1, maxTier + 1);
                for (int x = rx; x < rx + rw; x++)
                    for (int z = rz; z < rz + rd; z++)
                    {
                        occupied[x, z] = true;
                        cells[x, z].tier = tier;
                        if (x > rx && IsFloor(x - 1, z) && IsFloor(x, z)) vEdges[x, z].barrier = Barrier.None;
                        if (z > rz && IsFloor(x, z - 1) && IsFloor(x, z)) hEdges[x, z].barrier = Barrier.None;
                    }
            }
        }

        //==================== LAYOUT: HALLS ====================

        // Subtractive: start solid (all Empty), carve a few large open halls and
        // join them with straight corridors. Sparse, monumental — ignores the
        // footprint/column passes (it defines its own shape).
        private void CarveHalls(int hallCount, int corridorWidth, int maxCeilingTier)
        {
            for (int x = 0; x < W; x++)
                for (int z = 0; z < D; z++)
                    cells[x, z].kind = CellKind.Empty;

            int maxTier = Mathf.Max(1, maxCeilingTier);
            int hallMax = Mathf.Clamp(Mathf.Min(W, D) / 2, 3, 8);
            var centers = new List<Vector2Int>();
            int attempts = Mathf.Max(hallCount * 5, 10);

            for (int i = 0; i < attempts && centers.Count < Mathf.Max(1, hallCount); i++)
            {
                int rw = Random.Range(3, hallMax + 1);
                int rd = Random.Range(3, hallMax + 1);
                if (rw > W || rd > D) continue;
                int rx = Random.Range(0, W - rw + 1);
                int rz = Random.Range(0, D - rd + 1);
                CarveRect(rx, rz, rx + rw - 1, rz + rd - 1, (byte)Random.Range(1, maxTier + 1));
                centers.Add(new Vector2Int(rx + rw / 2, rz + rd / 2));
            }

            if (centers.Count == 0)   // grid too small for any hall — open everything
            {
                for (int x = 0; x < W; x++)
                    for (int z = 0; z < D; z++)
                        cells[x, z].kind = CellKind.Floor;
                OpenAllInternal();
                return;
            }

            for (int i = 1; i < centers.Count; i++)
                CarveCorridor(centers[i - 1], centers[i], corridorWidth);

            KeepLargestComponent();
        }

        private void CarveCorridor(Vector2Int a, Vector2Int b, int width)
        {
            int w = Mathf.Clamp(width, 1, 3) - 1;
            CarveRect(a.x, a.y, b.x, a.y + w, 1);   // horizontal leg (corridors stay base height)
            CarveRect(b.x, a.y, b.x + w, b.y, 1);   // vertical leg (overlaps at the corner)
        }

        // Set a rectangle of cells to Floor (with the given ceiling tier) and open
        // all edges interior to the rect.
        private void CarveRect(int x0, int z0, int x1, int z1, byte tier)
        {
            int ax = Mathf.Clamp(Mathf.Min(x0, x1), 0, W - 1);
            int bx = Mathf.Clamp(Mathf.Max(x0, x1), 0, W - 1);
            int az = Mathf.Clamp(Mathf.Min(z0, z1), 0, D - 1);
            int bz = Mathf.Clamp(Mathf.Max(z0, z1), 0, D - 1);

            for (int x = ax; x <= bx; x++)
                for (int z = az; z <= bz; z++)
                {
                    cells[x, z].kind = CellKind.Floor;
                    cells[x, z].tier = tier;
                    if (x > ax) vEdges[x, z].barrier = Barrier.None;
                    if (z > az) hEdges[x, z].barrier = Barrier.None;
                }
        }

        //==================== DECORATION ====================

        // Doors sit on open passages (a walkable doorway). Barrier becomes Full so a
        // door-cutout wall is placed, but the Door opening keeps the passage passable.
        private void AssignDoors(int pct)
        {
            for (int x = 0; x < W; x++)
                for (int z = 1; z < D; z++)
                    if (IsFloor(x, z - 1) && IsFloor(x, z) &&
                        hEdges[x, z].barrier == Barrier.None && Random.Range(0, 100) < pct)
                    { hEdges[x, z].barrier = Barrier.Full; hEdges[x, z].opening = Opening.Door; }

            for (int x = 1; x < W; x++)
                for (int z = 0; z < D; z++)
                    if (IsFloor(x - 1, z) && IsFloor(x, z) &&
                        vEdges[x, z].barrier == Barrier.None && Random.Range(0, 100) < pct)
                    { vEdges[x, z].barrier = Barrier.Full; vEdges[x, z].opening = Opening.Door; }
        }

        // Windows sit on solid internal walls (see-through, not passable).
        private void AssignWindows(int pct)
        {
            for (int x = 0; x < W; x++)
                for (int z = 1; z < D; z++)
                    if (IsFloor(x, z - 1) && IsFloor(x, z) &&
                        hEdges[x, z].barrier == Barrier.Full && hEdges[x, z].opening == Opening.None &&
                        Random.Range(0, 100) < pct)
                        hEdges[x, z].opening = Opening.Window;

            for (int x = 1; x < W; x++)
                for (int z = 0; z < D; z++)
                    if (IsFloor(x - 1, z) && IsFloor(x, z) &&
                        vEdges[x, z].barrier == Barrier.Full && vEdges[x, z].opening == Opening.None &&
                        Random.Range(0, 100) < pct)
                        vEdges[x, z].opening = Opening.Window;
        }

        //==================== FOOTPRINT ====================

        // Break the rectangle: eat bites from the border + carve interior courtyards,
        // then keep only the largest connected floor region (no stranded islands).
        private void CarveFootprint(int irregularity, int courtyards)
        {
            irregularity = Mathf.Clamp(irregularity, 0, 100);
            if (irregularity > 0)
            {
                int maxBite = Mathf.Max(1, Mathf.RoundToInt(irregularity / 100f * Mathf.Min(W, D) * 0.5f));
                int bites = Mathf.RoundToInt(irregularity / 100f * (W + D) / 2f);
                for (int i = 0; i < bites; i++) CarveBite(maxBite);
            }
            for (int i = 0; i < Mathf.Max(0, courtyards); i++) CarveCourtyard();
            KeepLargestComponent();
        }

        private void CarveBite(int maxBite)
        {
            int bw = Random.Range(1, maxBite + 1);
            int bd = Random.Range(1, maxBite + 1);
            int x0, z0;
            switch (Random.Range(0, 4))
            {
                case 0: x0 = 0;      z0 = Random.Range(0, Mathf.Max(1, D - bd + 1)); break; // left
                case 1: x0 = W - bw; z0 = Random.Range(0, Mathf.Max(1, D - bd + 1)); break; // right
                case 2: z0 = 0;      x0 = Random.Range(0, Mathf.Max(1, W - bw + 1)); break; // bottom
                default: z0 = D - bd; x0 = Random.Range(0, Mathf.Max(1, W - bw + 1)); break; // top
            }
            for (int x = x0; x < x0 + bw; x++)
                for (int z = z0; z < z0 + bd; z++)
                    if (InBounds(x, z)) cells[x, z].kind = CellKind.Empty;
        }

        private void CarveCourtyard()
        {
            if (W < 4 || D < 4) return;   // too small for an interior void
            int cw = Random.Range(1, Mathf.Max(2, W / 3));
            int cd = Random.Range(1, Mathf.Max(2, D / 3));
            int x0 = Random.Range(1, Mathf.Max(2, W - cw - 1));
            int z0 = Random.Range(1, Mathf.Max(2, D - cd - 1));
            for (int x = x0; x < x0 + cw; x++)
                for (int z = z0; z < z0 + cd; z++)
                    if (InBounds(x, z)) cells[x, z].kind = CellKind.Empty;
        }

        // Flood-fill floor components; everything outside the largest becomes Empty.
        private void KeepLargestComponent()
        {
            var comp = new int[W, D];
            for (int x = 0; x < W; x++)
                for (int z = 0; z < D; z++)
                    comp[x, z] = -1;

            int best = -1, bestSize = 0, id = 0;
            var stack = new Stack<Vector2Int>();
            for (int sx = 0; sx < W; sx++)
                for (int sz = 0; sz < D; sz++)
                {
                    if (!IsFloor(sx, sz) || comp[sx, sz] != -1) continue;
                    int size = 0;
                    comp[sx, sz] = id;
                    stack.Push(new Vector2Int(sx, sz));
                    while (stack.Count > 0)
                    {
                        var c = stack.Pop();
                        size++;
                        void Try(int x, int z) { if (IsFloor(x, z) && comp[x, z] == -1) { comp[x, z] = id; stack.Push(new Vector2Int(x, z)); } }
                        Try(c.x - 1, c.y); Try(c.x + 1, c.y); Try(c.x, c.y - 1); Try(c.x, c.y + 1);
                    }
                    if (size > bestSize) { bestSize = size; best = id; }
                    id++;
                }

            if (best < 0) return;
            for (int x = 0; x < W; x++)
                for (int z = 0; z < D; z++)
                    if (cells[x, z].kind == CellKind.Floor && comp[x, z] != best)
                        cells[x, z].kind = CellKind.Empty;
        }

        //==================== MASSING (COLUMNS) ====================

        private void PlaceColumns(ColumnPattern pattern, int density, int bigChance)
        {
            if (pattern == ColumnPattern.None || density <= 0) return;
            density = Mathf.Clamp(density, 0, 100);

            if (pattern == ColumnPattern.Lattice)
            {
                int spacing = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(5, 2, density / 100f)), 2, 6);
                for (int x = 1; x < W - 1; x += spacing)
                    for (int z = 1; z < D - 1; z += spacing)
                        TryColumn(x, z, bigChance);
            }
            else // Clusters
            {
                int clusters = Mathf.Max(1, Mathf.RoundToInt(density / 100f * (W * D) / 20f));
                for (int i = 0; i < clusters; i++)
                {
                    int cx = Random.Range(1, Mathf.Max(2, W - 1));
                    int cz = Random.Range(1, Mathf.Max(2, D - 1));
                    int count = Random.Range(1, 4);
                    for (int k = 0; k < count; k++)
                        TryColumn(cx + Random.Range(-1, 2), cz + Random.Range(-1, 2), bigChance);
                }
            }
        }

        // Place a freestanding column only where the block + a 1-cell floor ring are all
        // floor, so columns never merge with a wall or seal off the space (floor stays
        // 4-connected around every column).
        private void TryColumn(int x, int z, int bigChance)
        {
            int s = Random.Range(0, 100) < bigChance ? 2 : 1;
            for (int ix = x - 1; ix <= x + s; ix++)
                for (int iz = z - 1; iz <= z + s; iz++)
                    if (!IsFloor(ix, iz)) return;
            for (int ix = x; ix < x + s; ix++)
                for (int iz = z; iz < z + s; iz++)
                    cells[ix, iz].kind = CellKind.Column;
        }
    }
}
