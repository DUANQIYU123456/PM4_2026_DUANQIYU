// ============================================
// Backrooms Generator
// ============================================
// Procedural backrooms level generator.
//
// Placement math (verified from actual prefab bounds):
//   Floor:   center-pivot — place at cell CENTER
//   Wall:    bottom-center pivot — place at edge CENTER
//   Corner:  origin-pivot — place at grid VERTEX
//   Ceiling: same floor tiles at wallHeight
//
// Layout modes:
//   Maze             — DFS maze + extra openings
//   Open Room        — single room, outer walls only
//   Rooms & Corridors — random rooms connected by maze corridors
// ============================================

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ludocore
{
    public class BackroomsGenerator : EditorWindow
    {
        //==================== TYPES ====================

        private enum EdgeState { Wall, Open, Door, Window }

        private enum LayoutMode
        {
            Maze,
            OpenRoom,
            RoomsAndCorridors
        }

        //==================== CONSTANTS ====================

        private const string PrefabBase = "Assets/Asset Packs/BackroomsLite/Prefabs";
        private const string MaterialBase = "Assets/Asset Packs/BackroomsLite/Materials";

        //==================== CONFIG ====================

        private BackroomsTileSet tileSet;
        private LayoutMode layoutMode = LayoutMode.Maze;
        private int gridWidth = 4;
        private int gridDepth = 4;
        private int extraOpenings = 30;
        private int doorChance = 20;
        private int windowChance = 10;
        private int maxRoomSize = 4;
        private bool generateCeiling = true;
        private int seed;

        //==================== STATE ====================

        private Vector2 scrollPos;

        //==================== WINDOW SETUP ====================

        [MenuItem("Tools/Backrooms Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<BackroomsGenerator>("Backrooms Generator");
            window.minSize = new Vector2(340, 520);
        }

        //==================== GUI ====================

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // --- Tile Set ---
            EditorGUILayout.LabelField("Tile Set", EditorStyles.boldLabel);
            tileSet = (BackroomsTileSet)EditorGUILayout.ObjectField(
                "Tile Set", tileSet, typeof(BackroomsTileSet), false);

            if (tileSet == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a Backrooms Tile Set, or click below to auto-create one.",
                    MessageType.Info);
                if (GUILayout.Button("Create Default Tile Set"))
                    CreateDefaultTileSet();
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.Space();

            // --- Layout ---
            EditorGUILayout.LabelField("Layout", EditorStyles.boldLabel);
            layoutMode = (LayoutMode)EditorGUILayout.EnumPopup("Mode", layoutMode);

            gridWidth = EditorGUILayout.IntSlider("Width (cells)", gridWidth, 2, 20);
            gridDepth = EditorGUILayout.IntSlider("Depth (cells)", gridDepth, 2, 20);

            if (layoutMode == LayoutMode.RoomsAndCorridors)
                maxRoomSize = EditorGUILayout.IntSlider("Max Room Size", maxRoomSize, 2, 6);

            float cs = tileSet.cellSize;
            EditorGUILayout.HelpBox(
                $"{gridWidth * cs} x {gridDepth * cs} m  ({gridWidth * gridDepth} cells)",
                MessageType.None);

            EditorGUILayout.Space();

            // --- Generation ---
            EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);

            if (layoutMode != LayoutMode.OpenRoom)
                extraOpenings = EditorGUILayout.IntSlider(
                    new GUIContent("Extra Openings %",
                        "Remove extra internal walls for loops and bigger spaces."),
                    extraOpenings, 0, 100);

            doorChance = EditorGUILayout.IntSlider(
                new GUIContent("Door Chance %", "% of open passages that get a door."),
                doorChance, 0, 100);
            windowChance = EditorGUILayout.IntSlider(
                new GUIContent("Window Chance %", "% of solid internal walls that get a window."),
                windowChance, 0, 100);

            EditorGUILayout.Space();

            // --- Options ---
            EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
            generateCeiling = EditorGUILayout.Toggle("Generate Ceiling", generateCeiling);
            seed = EditorGUILayout.IntField(
                new GUIContent("Seed", "0 = random each time"), seed);

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate", GUILayout.Height(36)))
                Generate();

            EditorGUILayout.EndScrollView();
        }

        //==================== GENERATION ENTRY ====================

        private void Generate()
        {
            if (tileSet == null) return;
            float cs = tileSet.cellSize;

            Random.InitState(seed != 0 ? seed : System.Environment.TickCount);

            // Edge grids — all start as Wall
            var hEdges = new EdgeState[gridWidth, gridDepth + 1];
            var vEdges = new EdgeState[gridWidth + 1, gridDepth];
            Fill(hEdges, EdgeState.Wall);
            Fill(vEdges, EdgeState.Wall);

            // Layout pass
            switch (layoutMode)
            {
                case LayoutMode.Maze:
                    CarveMaze(hEdges, vEdges);
                    CarveExtraOpenings(hEdges, vEdges);
                    break;

                case LayoutMode.OpenRoom:
                    OpenAllInternal(hEdges, vEdges);
                    break;

                case LayoutMode.RoomsAndCorridors:
                    PlaceRooms(hEdges, vEdges);
                    CarveMaze(hEdges, vEdges);
                    CarveExtraOpenings(hEdges, vEdges);
                    break;
            }

            // Decoration pass
            AssignDoors(hEdges, vEdges);
            AssignWindows(hEdges, vEdges);

            // --- Build scene hierarchy ---
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Generate Backrooms");
            int undoGroup = Undo.GetCurrentGroup();

            var root = new GameObject($"Backrooms_{gridWidth}x{gridDepth}");
            Undo.RegisterCreatedObjectUndo(root, "Create Backrooms");

            PlaceFloors(Child("Floor", root.transform), cs);
            PlaceWalls(Child("Walls", root.transform), hEdges, vEdges, cs);
            PlaceCorners(Child("Corners", root.transform), hEdges, vEdges, cs);

            if (generateCeiling)
                PlaceCeiling(Child("Ceiling", root.transform), cs);

            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = root;
        }

        //==================== LAYOUT: MAZE ====================

        private void CarveMaze(EdgeState[,] hEdges, EdgeState[,] vEdges)
        {
            var visited = new bool[gridWidth, gridDepth];
            var stack = new Stack<Vector2Int>();

            var start = new Vector2Int(Random.Range(0, gridWidth), Random.Range(0, gridDepth));
            visited[start.x, start.y] = true;
            stack.Push(start);

            while (stack.Count > 0)
            {
                var cur = stack.Peek();
                var neighbors = UnvisitedNeighbors(cur, visited);
                if (neighbors.Count == 0) { stack.Pop(); continue; }

                var next = neighbors[Random.Range(0, neighbors.Count)];
                OpenEdgeBetween(cur, next, hEdges, vEdges);
                visited[next.x, next.y] = true;
                stack.Push(next);
            }
        }

        private List<Vector2Int> UnvisitedNeighbors(Vector2Int c, bool[,] visited)
        {
            var list = new List<Vector2Int>(4);
            if (c.x > 0 && !visited[c.x - 1, c.y]) list.Add(new Vector2Int(c.x - 1, c.y));
            if (c.x < gridWidth - 1 && !visited[c.x + 1, c.y]) list.Add(new Vector2Int(c.x + 1, c.y));
            if (c.y > 0 && !visited[c.x, c.y - 1]) list.Add(new Vector2Int(c.x, c.y - 1));
            if (c.y < gridDepth - 1 && !visited[c.x, c.y + 1]) list.Add(new Vector2Int(c.x, c.y + 1));
            return list;
        }

        private static void OpenEdgeBetween(Vector2Int a, Vector2Int b,
            EdgeState[,] hEdges, EdgeState[,] vEdges)
        {
            if (b.x == a.x + 1) vEdges[b.x, a.y] = EdgeState.Open;
            else if (b.x == a.x - 1) vEdges[a.x, a.y] = EdgeState.Open;
            else if (b.y == a.y + 1) hEdges[a.x, b.y] = EdgeState.Open;
            else if (b.y == a.y - 1) hEdges[a.x, a.y] = EdgeState.Open;
        }

        private void CarveExtraOpenings(EdgeState[,] hEdges, EdgeState[,] vEdges)
        {
            for (int x = 0; x < gridWidth; x++)
                for (int z = 1; z < gridDepth; z++)
                    if (hEdges[x, z] == EdgeState.Wall && Random.Range(0, 100) < extraOpenings)
                        hEdges[x, z] = EdgeState.Open;

            for (int x = 1; x < gridWidth; x++)
                for (int z = 0; z < gridDepth; z++)
                    if (vEdges[x, z] == EdgeState.Wall && Random.Range(0, 100) < extraOpenings)
                        vEdges[x, z] = EdgeState.Open;
        }

        //==================== LAYOUT: OPEN ROOM ====================

        private void OpenAllInternal(EdgeState[,] hEdges, EdgeState[,] vEdges)
        {
            for (int x = 0; x < gridWidth; x++)
                for (int z = 1; z < gridDepth; z++)
                    hEdges[x, z] = EdgeState.Open;

            for (int x = 1; x < gridWidth; x++)
                for (int z = 0; z < gridDepth; z++)
                    vEdges[x, z] = EdgeState.Open;
        }

        //==================== LAYOUT: ROOMS & CORRIDORS ====================

        private void PlaceRooms(EdgeState[,] hEdges, EdgeState[,] vEdges)
        {
            var occupied = new bool[gridWidth, gridDepth];
            int attempts = gridWidth * gridDepth * 2;

            for (int i = 0; i < attempts; i++)
            {
                int rw = Random.Range(2, maxRoomSize + 1);
                int rd = Random.Range(2, maxRoomSize + 1);
                int rx = Random.Range(0, gridWidth - rw + 1);
                int rz = Random.Range(0, gridDepth - rd + 1);
                if (rx < 0 || rz < 0) continue;

                // Check for overlap (leave 1-cell corridor gap between rooms)
                bool blocked = false;
                for (int x = Mathf.Max(0, rx - 1); x < Mathf.Min(gridWidth, rx + rw + 1) && !blocked; x++)
                    for (int z = Mathf.Max(0, rz - 1); z < Mathf.Min(gridDepth, rz + rd + 1) && !blocked; z++)
                        if (occupied[x, z]) blocked = true;

                if (blocked) continue;

                // Place room — mark cells and open internal edges
                for (int x = rx; x < rx + rw; x++)
                    for (int z = rz; z < rz + rd; z++)
                    {
                        occupied[x, z] = true;
                        if (x > rx) vEdges[x, z] = EdgeState.Open;
                        if (z > rz) hEdges[x, z] = EdgeState.Open;
                    }
            }
        }

        //==================== DECORATION ====================

        private void AssignDoors(EdgeState[,] hEdges, EdgeState[,] vEdges)
        {
            for (int x = 0; x < gridWidth; x++)
                for (int z = 1; z < gridDepth; z++)
                    if (hEdges[x, z] == EdgeState.Open && Random.Range(0, 100) < doorChance)
                        hEdges[x, z] = EdgeState.Door;

            for (int x = 1; x < gridWidth; x++)
                for (int z = 0; z < gridDepth; z++)
                    if (vEdges[x, z] == EdgeState.Open && Random.Range(0, 100) < doorChance)
                        vEdges[x, z] = EdgeState.Door;
        }

        private void AssignWindows(EdgeState[,] hEdges, EdgeState[,] vEdges)
        {
            for (int x = 0; x < gridWidth; x++)
                for (int z = 1; z < gridDepth; z++)
                    if (hEdges[x, z] == EdgeState.Wall && Random.Range(0, 100) < windowChance)
                        hEdges[x, z] = EdgeState.Window;

            for (int x = 1; x < gridWidth; x++)
                for (int z = 0; z < gridDepth; z++)
                    if (vEdges[x, z] == EdgeState.Wall && Random.Range(0, 100) < windowChance)
                        vEdges[x, z] = EdgeState.Window;
        }

        //==================== FLOOR PLACEMENT ====================
        // Floor tiles are CENTER-pivoted.
        // Cell (cx, cz) → world center = ((cx + 0.5) * cs, 0, (cz + 0.5) * cs)

        private void PlaceFloors(Transform parent, float cs)
        {
            if (tileSet.floorTiles == null || tileSet.floorTiles.Length == 0) return;
            float half = cs * 0.5f;

            for (int x = 0; x < gridWidth; x++)
                for (int z = 0; z < gridDepth; z++)
                {
                    var prefab = Pick(tileSet.floorTiles);
                    if (prefab != null)
                        Place(prefab, new Vector3(x * cs + half, 0, z * cs + half),
                            Quaternion.identity, parent);
                }
        }

        //==================== CEILING PLACEMENT ====================
        // Same tiles as floor, placed at wallHeight.
        // The bottom face has Ceiling material — faces downward into room.

        private void PlaceCeiling(Transform parent, float cs)
        {
            var tiles = tileSet.GetCeilingTiles();
            if (tiles == null || tiles.Length == 0) return;
            float half = cs * 0.5f;
            float h = tileSet.wallHeight;

            for (int x = 0; x < gridWidth; x++)
                for (int z = 0; z < gridDepth; z++)
                {
                    var prefab = Pick(tiles);
                    if (prefab != null)
                        Place(prefab, new Vector3(x * cs + half, h, z * cs + half),
                            Quaternion.identity, parent);
                }
        }

        //==================== WALL PLACEMENT ====================
        // Walls are bottom-center-pivoted (center of wall face at Y=0).
        // Horizontal edge hEdges[col, row] at Z = row*cs, center X = (col+0.5)*cs
        // Vertical edge vEdges[col, row] at X = col*cs, center Z = (row+0.5)*cs, rotated 90°

        private void PlaceWalls(Transform parent, EdgeState[,] hEdges, EdgeState[,] vEdges, float cs)
        {
            float half = cs * 0.5f;

            // Horizontal edges
            for (int col = 0; col < gridWidth; col++)
                for (int row = 0; row <= gridDepth; row++)
                {
                    if (hEdges[col, row] == EdgeState.Open) continue;
                    var pos = new Vector3(col * cs + half, 0, row * cs);
                    PlaceEdge(hEdges[col, row], pos, Quaternion.identity, parent);
                }

            // Vertical edges (90° rotation — wall extends along Z)
            var rot90 = Quaternion.Euler(0, 90, 0);
            for (int col = 0; col <= gridWidth; col++)
                for (int row = 0; row < gridDepth; row++)
                {
                    if (vEdges[col, row] == EdgeState.Open) continue;
                    var pos = new Vector3(col * cs, 0, row * cs + half);
                    PlaceEdge(vEdges[col, row], pos, rot90, parent);
                }
        }

        private void PlaceEdge(EdgeState state, Vector3 pos, Quaternion rot, Transform parent)
        {
            // Pick wall variant
            GameObject[] variants = state switch
            {
                EdgeState.Door   => tileSet.wallDoor,
                EdgeState.Window => tileSet.wallWindow,
                _                => null
            };
            if (variants == null || variants.Length == 0)
                variants = tileSet.wallSolid;
            if (variants == null || variants.Length == 0) return;

            var prefab = Pick(variants);
            if (prefab != null)
                Place(prefab, pos, rot, parent);

            // Door insert at wall center
            if (state == EdgeState.Door && tileSet.doorInserts is { Length: > 0 })
            {
                var insert = Pick(tileSet.doorInserts);
                if (insert != null)
                    Place(insert, pos, rot, parent);
            }

            // Window insert at wall center
            if (state == EdgeState.Window && tileSet.windowInserts is { Length: > 0 })
            {
                var insert = Pick(tileSet.windowInserts);
                if (insert != null)
                    Place(insert, pos, rot, parent);
            }
        }

        //==================== CORNER PLACEMENT ====================
        // Corners are origin-pivoted — placed exactly at grid vertices.
        // Place at any vertex where at least one adjacent edge has a wall.

        private void PlaceCorners(Transform parent, EdgeState[,] hEdges, EdgeState[,] vEdges, float cs)
        {
            if (tileSet.corners == null || tileSet.corners.Length == 0) return;

            for (int vx = 0; vx <= gridWidth; vx++)
                for (int vz = 0; vz <= gridDepth; vz++)
                {
                    if (!HasWallAtVertex(vx, vz, hEdges, vEdges)) continue;
                    var prefab = Pick(tileSet.corners);
                    if (prefab != null)
                        Place(prefab, new Vector3(vx * cs, 0, vz * cs),
                            Quaternion.identity, parent);
                }
        }

        private bool HasWallAtVertex(int vx, int vz, EdgeState[,] hEdges, EdgeState[,] vEdges)
        {
            if (vx > 0 && hEdges[vx - 1, vz] != EdgeState.Open) return true;
            if (vx < gridWidth && hEdges[vx, vz] != EdgeState.Open) return true;
            if (vz > 0 && vEdges[vx, vz - 1] != EdgeState.Open) return true;
            if (vz < gridDepth && vEdges[vx, vz] != EdgeState.Open) return true;
            return false;
        }

        //==================== UTILITY ====================

        private static GameObject Pick(GameObject[] arr)
        {
            if (arr == null || arr.Length == 0) return null;
            return arr[Random.Range(0, arr.Length)];
        }

        private GameObject Place(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.SetParent(parent, true);
            Undo.RegisterCreatedObjectUndo(go, "");
            return go;
        }

        private Transform Child(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            Undo.RegisterCreatedObjectUndo(go, "");
            return go.transform;
        }

        private static void Fill(EdgeState[,] arr, EdgeState val)
        {
            for (int a = 0; a < arr.GetLength(0); a++)
                for (int b = 0; b < arr.GetLength(1); b++)
                    arr[a, b] = val;
        }

        //==================== DEFAULT TILE SET ====================

        private void CreateDefaultTileSet()
        {
            var so = CreateInstance<BackroomsTileSet>();
            so.cellSize = 3f;
            so.wallHeight = 3f;

            so.floorTiles = LoadPrefabs(
                "Floor/BR_Floor_3x3",
                "Floor/BR_Floor_3x3_Debris",
                "Floor/BR_Floor_3x3_Hole");

            so.ceilingTiles = LoadPrefabs(
                "Floor/BR_Floor_3x3");

            so.wallSolid = LoadPrefabs(
                "Wall/BR_Wall_A_3x3",
                "Wall/BR_Wall_A_3x3_Debris");

            so.wallDoor = LoadPrefabs(
                "Wall/BR_Wall_A_3x3_Door_A",
                "Wall/BR_Wall_A_3x3_Door_B",
                "Wall/BR_Wall_A_3x3_Door_C");

            so.wallWindow = LoadPrefabs(
                "Wall/BR_Wall_A_3x3_Window_A",
                "Wall/BR_Wall_A_3x3_Window_B",
                "Wall/BR_Wall_A_3x3_Window_C");

            so.corners = LoadPrefabs("Wall/BR_Wall_A_Corner_3m");

            so.doorInserts = LoadPrefabs(
                "DoorWindow/Door_A_Grp",
                "DoorWindow/Door_B_Grp",
                "DoorWindow/Door_C_Grp",
                "DoorWindow/Door_D_Grp");

            so.windowInserts = LoadPrefabs(
                "DoorWindow/Wndw_A_Grp",
                "DoorWindow/Wndw_B_Grp",
                "DoorWindow/Wndw_C_Grp");

            string path = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/BackroomsTileSet_Default.asset");
            AssetDatabase.CreateAsset(so, path);
            AssetDatabase.SaveAssets();
            tileSet = so;
            EditorGUIUtility.PingObject(so);
        }

        private static GameObject[] LoadPrefabs(params string[] paths)
        {
            var list = new List<GameObject>();
            foreach (string p in paths)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabBase}/{p}.prefab");
                if (go != null) list.Add(go);
                else Debug.LogWarning($"[BackroomsGenerator] Missing: {PrefabBase}/{p}.prefab");
            }
            return list.ToArray();
        }
    }
}
