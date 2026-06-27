// ============================================
// Backrooms Generator
// ============================================
// Procedural backrooms level generator (editor window).
// Layout is computed by BackroomsLayout (plain C#); this
// window owns the GUI, the build/instantiation pass, and
// a SceneView debug overlay.
//
// Placement math (verified from actual prefab bounds):
//   Floor:   center pivot      — place at cell CENTER
//   Wall:    bottom-center     — place at edge CENTER (V-edges rotated 90°)
//   Corner:  grid-vertex pivot — place at grid VERTEX
//   Ceiling: floor tiles at wallHeight
//   All walls are full-height 3x3 (Barrier is None or Full).
// ============================================

using UnityEditor;
using UnityEngine;

namespace Ludocore
{
    public class BackroomsGenerator : EditorWindow
    {
        //==================== CONSTANTS ====================

        private const string PrefabBase = "Assets/Asset Packs/BackroomsLite/Prefabs";
        private const string TileSetPrefKey = "BackroomsGenerator_TileSetGUID";

        //==================== CONFIG ====================

        [SerializeField] private BackroomsTileSet tileSet;
        private LayoutMode layoutMode = LayoutMode.Maze;
        private int gridWidth = 4;
        private int gridDepth = 4;
        private int extraOpenings = 30;
        private int doorChance = 20;
        private int windowChance = 10;
        private int maxRoomSize = 4;
        private int straightness = 60;
        private int hallCount = 3;
        private int corridorWidth = 1;
        private int maxCeilingTier = 1;
        private bool generateCeiling = true;
        private int seed;

        // Footprint & massing
        private int irregularity;
        private int courtyards;
        private ColumnPattern columnPattern = ColumnPattern.None;
        private int columnDensity = 40;
        private int columnBigChance = 25;
        private int freePostSpacing;            // 0 = off

        // Openings (build-time)
        private int doorBareChance = 30;        // % of doors that are bare openings (no frame)
        private int doorObjectChance = 70;      // % of framed doors that get a door object
        private int windowBareChance = 10;
        private int windowObjectChance = 90;

        // Surface variety (build-time)
        private int wallHoleChance;
        private int floorHoleChance;
        private int floorDoorChance;

        // Encasing shell
        private bool encasing;
        private int encasingWidth = 3;
        private int encasingHeight = 1;

        //==================== STATE ====================

        private Vector2 scrollPos;
        private BackroomsLayout lastLayout;   // for the debug overlay (not serialized; lost on reload)
        private bool showOverlay = true;

        //==================== WINDOW SETUP ====================

        [MenuItem("Tools/Backrooms Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<BackroomsGenerator>("Backrooms Generator");
            window.minSize = new Vector2(340, 560);
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            if (tileSet == null)   // restore the last-used tile set
            {
                string guid = EditorPrefs.GetString(TileSetPrefKey, "");
                if (!string.IsNullOrEmpty(guid))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(path))
                        tileSet = AssetDatabase.LoadAssetAtPath<BackroomsTileSet>(path);
                }
            }
        }

        private void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

        private void RememberTileSet()
        {
            string guid = tileSet != null
                ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(tileSet)) : "";
            EditorPrefs.SetString(TileSetPrefKey, guid);
        }

        //==================== GUI ====================

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // --- Tile Set ---
            EditorGUILayout.LabelField("Tile Set", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            tileSet = (BackroomsTileSet)EditorGUILayout.ObjectField(
                "Tile Set", tileSet, typeof(BackroomsTileSet), false);
            if (EditorGUI.EndChangeCheck())
                RememberTileSet();

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

            if (GUILayout.Button(new GUIContent("Auto-Populate Tile Set from BackroomsLite",
                "Scan the BackroomsLite prefab folder and (re)assign all useful family-A pieces.")))
            {
                string summary = BackroomsTileSetPopulator.Populate(tileSet, PrefabBase);
                AssetDatabase.SaveAssets();
                Debug.Log(summary, tileSet);
            }

            EditorGUILayout.Space();

            // --- Layout ---
            EditorGUILayout.LabelField("Layout", EditorStyles.boldLabel);
            layoutMode = (LayoutMode)EditorGUILayout.EnumPopup("Mode", layoutMode);
            gridWidth = EditorGUILayout.IntSlider("Width (cells)", gridWidth, 2, 20);
            gridDepth = EditorGUILayout.IntSlider("Depth (cells)", gridDepth, 2, 20);

            if (layoutMode == LayoutMode.RoomsAndCorridors)
                maxRoomSize = EditorGUILayout.IntSlider("Max Room Size", maxRoomSize, 2, 6);
            if (layoutMode == LayoutMode.Halls)
            {
                hallCount = EditorGUILayout.IntSlider("Hall Count", hallCount, 1, 8);
                corridorWidth = EditorGUILayout.IntSlider("Corridor Width", corridorWidth, 1, 3);
            }
            if (layoutMode == LayoutMode.RoomsAndCorridors || layoutMode == LayoutMode.Halls)
                maxCeilingTier = EditorGUILayout.IntSlider(
                    new GUIContent("Max Ceiling Tier", "1 = uniform 3m. Higher = some rooms/halls get 2-3x taller ceilings."),
                    maxCeilingTier, 1, 3);

            float cs = tileSet.cellSize;
            EditorGUILayout.HelpBox(
                $"{gridWidth * cs} x {gridDepth * cs} m  ({gridWidth * gridDepth} cells)",
                MessageType.None);

            EditorGUILayout.Space();

            // --- Footprint & Massing ---
            EditorGUILayout.LabelField("Footprint & Massing", EditorStyles.boldLabel);
            irregularity = EditorGUILayout.IntSlider(
                new GUIContent("Irregularity %", "Bite chunks out of the rectangular outline."),
                irregularity, 0, 100);
            courtyards = EditorGUILayout.IntSlider(
                new GUIContent("Courtyards", "Interior voids carved into the space."),
                courtyards, 0, 6);
            columnPattern = (ColumnPattern)EditorGUILayout.EnumPopup("Column Pattern", columnPattern);
            if (columnPattern != ColumnPattern.None)
            {
                columnDensity = EditorGUILayout.IntSlider("Column Density %", columnDensity, 0, 100);
                columnBigChance = EditorGUILayout.IntSlider(
                    new GUIContent("2x2 Column %", "Chance a column is a thick 2x2 block."),
                    columnBigChance, 0, 100);
            }
            freePostSpacing = EditorGUILayout.IntSlider(
                new GUIContent("Free Column Spacing", "Free-standing posts in open floor, every N cells (0 = off)."),
                freePostSpacing, 0, 6);

            EditorGUILayout.Space();

            // --- Generation ---
            bool mazeBased = layoutMode == LayoutMode.Maze || layoutMode == LayoutMode.RoomsAndCorridors;
            if (mazeBased)
            {
                EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);
                extraOpenings = EditorGUILayout.IntSlider(
                    new GUIContent("Extra Openings %",
                        "Remove extra internal walls for loops and bigger spaces."),
                    extraOpenings, 0, 100);
                straightness = EditorGUILayout.IntSlider(
                    new GUIContent("Corridor Straightness %",
                        "Higher = longer straight corridors instead of a twisty maze."),
                    straightness, 0, 100);

                EditorGUILayout.Space();
            }

            // --- Openings ---
            EditorGUILayout.LabelField("Openings", EditorStyles.boldLabel);
            doorChance = EditorGUILayout.IntSlider(
                new GUIContent("Door Chance %", "% of open passages that get a door edge."),
                doorChance, 0, 100);
            if (doorChance > 0)
            {
                EditorGUI.indentLevel++;
                doorBareChance = EditorGUILayout.IntSlider(
                    new GUIContent("Bare Opening %", "% of doors that are bare openings (no frame, no object)."),
                    doorBareChance, 0, 100);
                doorObjectChance = EditorGUILayout.IntSlider(
                    new GUIContent("Has Door Object %", "% of framed doors that get a door object."),
                    doorObjectChance, 0, 100);
                EditorGUI.indentLevel--;
            }
            windowChance = EditorGUILayout.IntSlider(
                new GUIContent("Window Chance %", "% of solid internal walls that get a window."),
                windowChance, 0, 100);
            if (windowChance > 0)
            {
                EditorGUI.indentLevel++;
                windowBareChance = EditorGUILayout.IntSlider(
                    new GUIContent("Bare Opening %", "% of windows that are bare openings (no frame)."),
                    windowBareChance, 0, 100);
                windowObjectChance = EditorGUILayout.IntSlider(
                    new GUIContent("Has Glass %", "% of framed windows that get a window object."),
                    windowObjectChance, 0, 100);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // --- Surface Variety ---
            EditorGUILayout.LabelField("Surface Variety", EditorStyles.boldLabel);
            wallHoleChance = EditorGUILayout.IntSlider(
                new GUIContent("Wall Hole %", "% of solid walls replaced with a hole/gap wall."),
                wallHoleChance, 0, 100);
            floorHoleChance = EditorGUILayout.IntSlider(
                new GUIContent("Floor Hole %", "% of floor tiles replaced with a hole/pit tile."),
                floorHoleChance, 0, 100);
            floorDoorChance = EditorGUILayout.IntSlider(
                new GUIContent("Floor Hatch %", "% of floor tiles replaced with a floor-door/hatch tile."),
                floorDoorChance, 0, 100);

            EditorGUILayout.Space();

            // --- Encasing ---
            EditorGUILayout.LabelField("Encasing", EditorStyles.boldLabel);
            encasing = EditorGUILayout.Toggle(
                new GUIContent("Generate Encasing", "Solid shell around the room so holes reveal a bigger space."),
                encasing);
            if (encasing)
            {
                encasingWidth = EditorGUILayout.IntSlider(
                    new GUIContent("Width (cells)", "Horizontal gap around the room on all sides."),
                    encasingWidth, 1, 12);
                encasingHeight = EditorGUILayout.IntSlider(
                    new GUIContent("Height (cells)", "Vertical gap above AND below the room (symmetric)."),
                    encasingHeight, 1, 4);
            }

            EditorGUILayout.Space();

            // --- Options ---
            EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
            generateCeiling = EditorGUILayout.Toggle("Generate Ceiling", generateCeiling);
            showOverlay = EditorGUILayout.Toggle(
                new GUIContent("Show Debug Overlay", "Draw the cell/edge grid in the Scene view."),
                showOverlay);
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

            var settings = new LayoutSettings
            {
                width = gridWidth,
                depth = gridDepth,
                mode = layoutMode,
                extraOpenings = extraOpenings,
                doorChance = doorChance,
                windowChance = windowChance,
                maxRoomSize = maxRoomSize,
                straightness = straightness,
                hallCount = hallCount,
                corridorWidth = corridorWidth,
                maxCeilingTier = maxCeilingTier,
                seed = seed,
                irregularity = irregularity,
                courtyards = courtyards,
                columnPattern = columnPattern,
                columnDensity = columnDensity,
                columnBigChance = columnBigChance,
            };

            var layout = BackroomsLayout.Generate(settings);
            lastLayout = layout;

            // --- Build scene hierarchy ---
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Generate Backrooms");
            int undoGroup = Undo.GetCurrentGroup();

            var root = new GameObject($"Backrooms_{gridWidth}x{gridDepth}");
            Undo.RegisterCreatedObjectUndo(root, "Create Backrooms");

            PlaceFloors(Child("Floor", root.transform), layout, cs);

            // Walls split into Inner / Outer so the exterior shell can be toggled.
            var walls = Child("Walls", root.transform);
            var innerWalls = Child("Inner", walls);
            var outerWalls = Child("Outer", walls);
            PlaceWalls(innerWalls, outerWalls, layout, cs);

            PlacePosts(Child("Posts", root.transform), layout, cs);
            if (freePostSpacing > 0)
                PlaceFreePosts(Child("Columns", root.transform), layout, cs, freePostSpacing);
            if (generateCeiling)
                PlaceCeiling(Child("Ceiling", root.transform), layout, cs);
            if (encasing)
                PlaceEncasing(Child("Encasing", root.transform), layout, cs, encasingWidth, encasingHeight);

            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = root;
            SceneView.RepaintAll();
        }

        //==================== FLOOR / CEILING PLACEMENT ====================

        private void PlaceFloors(Transform parent, BackroomsLayout L, float cs)
        {
            float half = cs * 0.5f;
            for (int x = 0; x < L.W; x++)
                for (int z = 0; z < L.D; z++)
                {
                    if (!L.IsFloor(x, z)) continue;
                    var prefab = Pick(PickFloorSet());
                    if (prefab != null)
                        Place(prefab, new Vector3(x * cs + half, 0, z * cs + half),
                            Quaternion.identity, parent);
                }
        }

        // Per-cell floor variant: occasionally a hole/pit or a floor-hatch, else clean.
        private GameObject[] PickFloorSet()
        {
            if (NonEmpty(tileSet.floorHole) && Random.Range(0, 100) < floorHoleChance)
                return tileSet.floorHole;
            if (NonEmpty(tileSet.floorDoor) && Random.Range(0, 100) < floorDoorChance)
                return tileSet.floorDoor;
            return tileSet.floorTiles;
        }

        //==================== ENCASING ====================
        // A plain solid box centered on the room so holes reveal a bigger space.
        // Symmetric padding: a full floor 'height' levels below and a full ceiling
        // 'height' levels above, with perimeter walls 'width' cells out, stacked to
        // span the whole box. Uses only floor/wall/ceiling tiles.

        private void PlaceEncasing(Transform parent, BackroomsLayout L, float cs, int width, int height)
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
            float half = cs * 0.5f;
            float wh = tileSet.wallHeight;

            var floor = tileSet.floorTiles;
            var ceil = tileSet.GetCeilingTiles();
            var wall = tileSet.WallFull;
            if (!NonEmpty(wall)) return;   // a shell needs at least walls

            int xMin = -width, xMax = L.W + width;     // exclusive
            int zMin = -width, zMax = L.D + width;
            float floorY = -height * wh;               // box floor (below the room)
            float ceilY = wh + height * wh;            // box ceiling (above the room)

            // Box floor + ceiling — full enlarged footprint.
            for (int x = xMin; x < xMax; x++)
                for (int z = zMin; z < zMax; z++)
                {
                    float cx = x * cs + half, cz = z * cs + half;
                    if (NonEmpty(floor)) Place(Pick(floor), new Vector3(cx, floorY, cz), Quaternion.identity, parent);
                    if (NonEmpty(ceil)) Place(Pick(ceil), new Vector3(cx, ceilY, cz), Quaternion.identity, parent);
                }

            // Perimeter walls — stacked from box floor up to box ceiling.
            int tiers = 1 + 2 * height;                // (ceilY - floorY) / wh
            var rot90 = Quaternion.Euler(0, 90, 0);
            for (int t = 0; t < tiers; t++)
            {
                float y = floorY + t * wh;
                for (int x = xMin; x < xMax; x++)
                {
                    Place(Pick(wall), new Vector3(x * cs + half, y, zMin * cs), Quaternion.identity, parent);
                    Place(Pick(wall), new Vector3(x * cs + half, y, zMax * cs), Quaternion.identity, parent);
                }
                for (int z = zMin; z < zMax; z++)
                {
                    Place(Pick(wall), new Vector3(xMin * cs, y, z * cs + half), rot90, parent);
                    Place(Pick(wall), new Vector3(xMax * cs, y, z * cs + half), rot90, parent);
                }
            }
        }

        private void PlaceCeiling(Transform parent, BackroomsLayout L, float cs)
        {
            var tiles = tileSet.GetCeilingTiles();
            if (tiles == null || tiles.Length == 0) return;
            float half = cs * 0.5f;
            float h = tileSet.wallHeight;

            for (int x = 0; x < L.W; x++)
                for (int z = 0; z < L.D; z++)
                {
                    // Ceiling covers floor and solid-column cells (it continues above columns).
                    if (L.cells[x, z].kind == CellKind.Empty) continue;
                    int tier = Mathf.Max(1, L.cells[x, z].tier);
                    var prefab = Pick(tiles);
                    if (prefab != null)
                        Place(prefab, new Vector3(x * cs + half, tier * h, z * cs + half),
                            Quaternion.identity, parent);
                }
        }

        //==================== WALL PLACEMENT ====================

        private void PlaceWalls(Transform inner, Transform outer, BackroomsLayout L, float cs)
        {
            float half = cs * 0.5f;
            float wh = tileSet.wallHeight;
            var rot90 = Quaternion.Euler(0, 90, 0);

            // Horizontal edges (run along X) — between cells (col,row-1) and (col,row).
            for (int col = 0; col < L.W; col++)
                for (int row = 0; row <= L.D; row++)
                {
                    var e = L.EffectiveH(col, row);
                    int tA = TierOf(L, col, row - 1), tB = TierOf(L, col, row);
                    var parent = IsOuter(L, col, row - 1, col, row) ? outer : inner;
                    BuildEdge(e, tA, tB, new Vector3(col * cs + half, 0, row * cs), Quaternion.identity, parent, wh);
                }

            // Vertical edges (run along Z, rotated 90°) — between (col-1,row) and (col,row).
            for (int col = 0; col <= L.W; col++)
                for (int row = 0; row < L.D; row++)
                {
                    var e = L.EffectiveV(col, row);
                    int tA = TierOf(L, col - 1, row), tB = TierOf(L, col, row);
                    var parent = IsOuter(L, col - 1, row, col, row) ? outer : inner;
                    BuildEdge(e, tA, tB, new Vector3(col * cs, 0, row * cs + half), rot90, parent, wh);
                }
        }

        // Ceiling tier of a cell (0 if not floor/column).
        private static int TierOf(BackroomsLayout L, int x, int z)
        {
            if (!L.InBounds(x, z)) return 0;
            var c = L.cells[x, z];
            return (c.kind == CellKind.Floor || c.kind == CellKind.Column) ? Mathf.Max(1, c.tier) : 0;
        }

        // Tallest ceiling tier of the four cells meeting at a grid vertex.
        private static int VertexTier(BackroomsLayout L, int vx, int vz)
        {
            int t = Mathf.Max(
                Mathf.Max(TierOf(L, vx - 1, vz - 1), TierOf(L, vx, vz - 1)),
                Mathf.Max(TierOf(L, vx - 1, vz), TierOf(L, vx, vz)));
            return Mathf.Max(1, t);
        }

        // Does any edge meeting this vertex have a wall/fascia segment at level t?
        // Covers both full walls (stacked) and fascias above an overhang.
        private static bool PostNeededAt(BackroomsLayout L, int vx, int vz, int t)
        {
            if (vx > 0 && EdgeHasWallAt(L.EffectiveH(vx - 1, vz), TierOf(L, vx - 1, vz - 1), TierOf(L, vx - 1, vz), t)) return true;
            if (vx < L.W && EdgeHasWallAt(L.EffectiveH(vx, vz), TierOf(L, vx, vz - 1), TierOf(L, vx, vz), t)) return true;
            if (vz > 0 && EdgeHasWallAt(L.EffectiveV(vx, vz - 1), TierOf(L, vx - 1, vz - 1), TierOf(L, vx, vz - 1), t)) return true;
            if (vz < L.D && EdgeHasWallAt(L.EffectiveV(vx, vz), TierOf(L, vx - 1, vz), TierOf(L, vx, vz), t)) return true;
            return false;
        }

        private static bool EdgeHasWallAt(Edge e, int tierA, int tierB, int t)
        {
            if (e.barrier == Barrier.Full)
                return t < Mathf.Max(1, Mathf.Max(tierA, tierB));     // stacked wall reaches this level
            if (tierA > 0 && tierB > 0 && tierA != tierB)             // fascia on an open height-step edge
                return t >= Mathf.Min(tierA, tierB) && t < Mathf.Max(tierA, tierB);
            return false;
        }

        // A Full edge becomes a wall stacked to the taller neighbor (opening on the
        // bottom tier). An open edge between differing ceiling heights gets a fascia
        // filling the vertical ceiling step above the passage.
        private void BuildEdge(Edge e, int tA, int tB, Vector3 basePos, Quaternion rot, Transform parent, float wh)
        {
            if (e.barrier == Barrier.Full)
            {
                int wallTier = Mathf.Max(1, Mathf.Max(tA, tB));
                PlaceEdge(e, basePos, rot, parent);                 // bottom tier: opening or solid
                for (int t = 1; t < wallTier; t++)
                    Place(Pick(tileSet.WallFull), new Vector3(basePos.x, t * wh, basePos.z), rot, parent);
                // Ledge caps each level boundary from +1 up (seams + ceiling junction).
                if (wallTier >= 2 && NonEmpty(tileSet.ledges))
                    for (int t = 1; t <= wallTier; t++)
                        Place(Pick(tileSet.ledges), new Vector3(basePos.x, t * wh, basePos.z), rot, parent);
            }
            else if (tA > 0 && tB > 0 && tA != tB)                   // open passage, ceiling step
            {
                int lo = Mathf.Min(tA, tB), hi = Mathf.Max(tA, tB);
                for (int t = lo; t < hi; t++)
                    Place(Pick(tileSet.WallFull), new Vector3(basePos.x, t * wh, basePos.z), rot, parent);
                if (NonEmpty(tileSet.ledges))
                    for (int t = lo; t <= hi; t++)
                        Place(Pick(tileSet.ledges), new Vector3(basePos.x, t * wh, basePos.z), rot, parent);
            }
        }

        // A wall is "outer" when its non-floor side is the building exterior
        // (out of bounds or a border-connected void). Interior dividers, column
        // faces, and courtyard (interior-void) walls are "inner".
        private static bool IsOuter(BackroomsLayout L, int ax, int az, int bx, int bz)
        {
            bool aFloor = L.IsFloor(ax, az), bFloor = L.IsFloor(bx, bz);
            if (aFloor && bFloor) return false;
            int nx = aFloor ? bx : ax, nz = aFloor ? bz : az;  // the non-floor side
            return L.IsExteriorVoid(nx, nz);
        }

        // Barrier is always Full when this is called (open edges are skipped earlier).
        private void PlaceEdge(Edge e, Vector3 pos, Quaternion rot, Transform parent)
        {
            switch (e.opening)
            {
                case Opening.Door:
                    PlaceOpening(tileSet.doorVariants, tileSet.doorOpenings,
                        doorBareChance, doorObjectChance, pos, rot, parent);
                    break;
                case Opening.Window:
                    PlaceOpening(tileSet.windowVariants, tileSet.windowOpenings,
                        windowBareChance, windowObjectChance, pos, rot, parent);
                    break;
                default:
                    // Solid wall — occasionally a hole/gap wall instead.
                    var solid = (NonEmpty(tileSet.wallHole) && Random.Range(0, 100) < wallHoleChance)
                        ? tileSet.wallHole : tileSet.WallFull;
                    Place(Pick(solid), pos, rot, parent);
                    break;
            }
        }

        // Place a door/window. With bareChance%, place a bare OPENING (cutout, no
        // frame, no object). Otherwise place a shape-matched FIXTURE cutout, and
        // with objectChance% also place its matching object (not every fixture gets one).
        private void PlaceOpening(WallOpeningSet[] fixtures, GameObject[] openings,
            int bareChance, int objectChance, Vector3 pos, Quaternion rot, Transform parent)
        {
            if (NonEmpty(openings) && Random.Range(0, 100) < bareChance)
            {
                Place(Pick(openings), pos, rot, parent);
                return;
            }

            // Reservoir-pick a fixture shape that has cutouts.
            WallOpeningSet chosen = null;
            int seen = 0;
            if (fixtures != null)
                foreach (var v in fixtures)
                    if (v != null && NonEmpty(v.cutouts) && Random.Range(0, ++seen) == 0)
                        chosen = v;

            if (chosen == null)
            {
                Place(Pick(NonEmpty(openings) ? openings : tileSet.WallFull), pos, rot, parent);
                return;
            }

            Place(Pick(chosen.cutouts), pos, rot, parent);
            if (NonEmpty(chosen.inserts) && Random.Range(0, 100) < objectChance)
                Place(Pick(chosen.inserts), pos, rot, parent);  // same transform as cutout (verified)
        }

        //==================== POST PLACEMENT ====================
        // Posts (round faces, rotation-proof) at every wall vertex — used as the
        // corner piece, since the corner tiles have one flat face that mis-rotates.

        private void PlacePosts(Transform parent, BackroomsLayout L, float cs)
        {
            if (!NonEmpty(tileSet.posts)) return;
            float wh = tileSet.wallHeight;

            for (int vx = 0; vx <= L.W; vx++)
                for (int vz = 0; vz <= L.D; vz++)
                {
                    int top = VertexTier(L, vx, vz);
                    for (int t = 0; t < top; t++)
                    {
                        if (!PostNeededAt(L, vx, vz, t)) continue;
                        var prefab = Pick(tileSet.posts);
                        if (prefab != null)
                            Place(prefab, new Vector3(vx * cs, t * wh, vz * cs), Quaternion.identity, parent);
                    }
                }
        }

        // Free-standing posts on a lattice in open floor (a colonnade) — placed only
        // at interior vertices fully surrounded by floor and clear of walls.
        private void PlaceFreePosts(Transform parent, BackroomsLayout L, float cs, int spacing)
        {
            if (spacing <= 0 || !NonEmpty(tileSet.posts)) return;
            float wh = tileSet.wallHeight;

            for (int vx = spacing; vx < L.W; vx += spacing)
                for (int vz = spacing; vz < L.D; vz += spacing)
                {
                    bool openHere = L.IsFloor(vx - 1, vz - 1) && L.IsFloor(vx, vz - 1) &&
                                    L.IsFloor(vx - 1, vz) && L.IsFloor(vx, vz);
                    if (!openHere || L.HasFullWallAtVertex(vx, vz)) continue;
                    int tier = VertexTier(L, vx, vz);
                    for (int t = 0; t < tier; t++)
                    {
                        var prefab = Pick(tileSet.posts);
                        if (prefab != null)
                            Place(prefab, new Vector3(vx * cs, t * wh, vz * cs), Quaternion.identity, parent);
                    }
                }
        }

        //==================== DEBUG OVERLAY ====================

        private void OnSceneGUI(SceneView sv)
        {
            if (!showOverlay || lastLayout == null || tileSet == null) return;
            var L = lastLayout;
            float cs = tileSet.cellSize;
            float y = 0.03f;

            // Cells
            for (int x = 0; x < L.W; x++)
                for (int z = 0; z < L.D; z++)
                {
                    var kind = L.cells[x, z].kind;
                    if (kind == CellKind.Empty) continue;
                    Color face = kind == CellKind.Column
                        ? new Color(0.9f, 0.2f, 0.2f, 0.18f)
                        : new Color(0.3f, 0.6f, 1f, 0.07f);
                    float x0 = x * cs, z0 = z * cs, x1 = (x + 1) * cs, z1 = (z + 1) * cs;
                    var verts = new[]
                    {
                        new Vector3(x0, y, z0), new Vector3(x1, y, z0),
                        new Vector3(x1, y, z1), new Vector3(x0, y, z1)
                    };
                    Handles.DrawSolidRectangleWithOutline(verts, face, Color.clear);
                }

            // Edges
            for (int col = 0; col < L.W; col++)
                for (int row = 0; row <= L.D; row++)
                    DrawEdgeGizmo(L.EffectiveH(col, row),
                        new Vector3(col * cs, y, row * cs), new Vector3((col + 1) * cs, y, row * cs));

            for (int col = 0; col <= L.W; col++)
                for (int row = 0; row < L.D; row++)
                    DrawEdgeGizmo(L.EffectiveV(col, row),
                        new Vector3(col * cs, y, row * cs), new Vector3(col * cs, y, (row + 1) * cs));
        }

        private static void DrawEdgeGizmo(Edge e, Vector3 a, Vector3 b)
        {
            if (e.barrier == Barrier.None) return;

            Handles.color = Color.white;
            Handles.DrawAAPolyLine(3f, a, b);

            if (e.opening == Opening.None) return;
            Handles.color = e.opening switch
            {
                Opening.Door => Color.green,
                Opening.Window => Color.cyan,
                _ => Color.clear
            };
            var mid = (a + b) * 0.5f;
            Handles.DrawWireDisc(mid, Vector3.up, 0.35f);
        }

        //==================== UTILITY ====================

        private static bool NonEmpty(GameObject[] arr) => arr != null && arr.Length > 0;

        private static GameObject Pick(GameObject[] arr)
        {
            if (!NonEmpty(arr)) return null;
            return arr[Random.Range(0, arr.Length)];
        }

        private GameObject Place(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent)
        {
            if (prefab == null) return null;
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

        //==================== DEFAULT TILE SET ====================

        private void CreateDefaultTileSet()
        {
            var so = CreateInstance<BackroomsTileSet>();
            so.cellSize = 3f;
            so.wallHeight = 3f;

            string summary = BackroomsTileSetPopulator.Populate(so, PrefabBase);

            string path = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/BackroomsTileSet_Default.asset");
            AssetDatabase.CreateAsset(so, path);
            AssetDatabase.SaveAssets();
            tileSet = so;
            RememberTileSet();
            EditorGUIUtility.PingObject(so);
            Debug.Log(summary, so);
        }
    }
}
