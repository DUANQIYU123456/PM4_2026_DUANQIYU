// ============================================
// Backrooms Tile Set Populator
// ============================================
// Scans the BackroomsLite prefab folder and assigns the curated
// family-A, 3m pieces to the tile-set slots — including the
// shape-matched door/window variants (rect/round/double), each
// paired with the CORRECT insert objects. v1 scope.
// ============================================

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ludocore
{
    public static class BackroomsTileSetPopulator
    {
        public const string DefaultPrefabRoot = "Assets/Asset Packs/BackroomsLite/Prefabs";

        /// Populate the given tile set from prefabRoot. Returns a summary string.
        public static string Populate(BackroomsTileSet so, string prefabRoot)
        {
            // Build a name -> prefab lookup over the whole folder (path-independent).
            var byName = new Dictionary<string, GameObject>();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { prefabRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);
                if (byName.ContainsKey(name)) continue;
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null) byName[name] = go;
            }

            var missing = new List<string>();
            GameObject Get(string n)
            {
                if (byName.TryGetValue(n, out var g)) return g;
                missing.Add(n);
                return null;
            }
            GameObject[] Arr(params string[] names) => names.Select(Get).Where(g => g != null).ToArray();
            WallOpeningSet Set(string label, GameObject[] cutouts, GameObject[] inserts) =>
                new WallOpeningSet { label = label, cutouts = cutouts, inserts = inserts };

            // --- Floor / ceiling ---
            so.floorTiles = Arr("BR_Floor_3x3");
            so.floorHole = Arr("BR_Floor_3x3_Hole");
            so.floorDoor = Arr("BR_Floor_3x3_Door");
            so.ceilingTiles = Arr("BR_Floor_3x3");

            // --- Full walls ---
            so.wallSolid = Arr("BR_Wall_A_3x3");
            so.wallHole = Arr("BR_Wall_A_3x3_Hole");

            // --- Posts (corner piece) + ledges (horizontal level-boundary cap) ---
            so.posts = Arr("BR_Wall_A_Post_3m");
            so.ledges = Arr("BR_Floor_Ledge_3m");

            // --- Doors: cutout shape paired with matching fixtures ---
            so.doorVariants = new[]
            {
                Set("Rectangular Door", Arr("BR_Wall_A_3x3_Door_A"), Arr("Door_A_Grp", "Door_B_Grp")),
                Set("Round Door",       Arr("BR_Wall_A_3x3_Door_B"), Arr("Door_C_Grp", "Door_D_Grp")),
                Set("Double Door",      Arr("BR_Wall_A_3x3_Door_C"),
                    Arr("Door_Dbl_A_Grp", "Door_Dbl_B_Grp", "Door_Dbl_C_Grp", "Door_Dbl_D_Grp")),
            };
            so.doorOpenings = Arr("BR_Wall_A_3x3_Door_A_Alt", "BR_Wall_A_3x3_Door_B_Alt", "BR_Wall_A_3x3_Door_C_Alt");

            // --- Windows: cutout shape paired with matching fixtures ---
            so.windowVariants = new[]
            {
                Set("Rectangular Window", Arr("BR_Wall_A_3x3_Window_A"), Arr("Wndw_A_Grp", "Wndw_B_Grp")),
                Set("Round Window",       Arr("BR_Wall_A_3x3_Window_B"), Arr("Wndw_C_Grp", "Wndw_D_Grp")),
                Set("Double Window",      Arr("BR_Wall_A_3x3_Window_C"), Arr("Wndw_E_Grp", "Wndw_F_Grp", "Wndw_G_Grp")),
            };
            so.windowOpenings = Arr("BR_Wall_A_3x3_Window_A_Alt", "BR_Wall_A_3x3_Window_B_Alt", "BR_Wall_A_3x3_Window_C_Alt");

            EditorUtility.SetDirty(so);
            return BuildSummary(so, missing);
        }

        private static string BuildSummary(BackroomsTileSet so, List<string> missing)
        {
            int Variants(WallOpeningSet[] v) => v == null ? 0 :
                v.Sum(s => (s.cutouts?.Length ?? 0) + (s.inserts?.Length ?? 0));

            int total =
                Len(so.floorTiles) + Len(so.floorHole) + Len(so.floorDoor) + Len(so.ceilingTiles) +
                Len(so.wallSolid) + Len(so.wallHole) + Len(so.posts) + Len(so.ledges) +
                Variants(so.doorVariants) + Variants(so.windowVariants) +
                Len(so.doorOpenings) + Len(so.windowOpenings);

            var sb = new System.Text.StringBuilder();
            sb.Append($"[BackroomsTileSet] Auto-populated {total} prefab references ");
            sb.Append($"(doors: {so.doorVariants?.Length ?? 0} variants, windows: {so.windowVariants?.Length ?? 0} variants).");
            if (missing.Count > 0)
                sb.Append($"\nMissing {missing.Count} expected prefab(s): {string.Join(", ", missing)}");
            return sb.ToString();
        }

        private static int Len(GameObject[] a) => a?.Length ?? 0;

        private const string DefaultAssetPath = "Assets/BackroomsTileSet_Default.asset";

        [MenuItem("Tools/Backrooms/Create Default Tile Set")]
        private static void CreateDefaultTileSet()
        {
            // Refresh the existing default in place if present (no duplicate spam).
            var so = AssetDatabase.LoadAssetAtPath<BackroomsTileSet>(DefaultAssetPath);
            bool created = false;
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<BackroomsTileSet>();
                so.cellSize = 3f;
                so.wallHeight = 3f;
                created = true;
            }

            string summary = Populate(so, DefaultPrefabRoot);

            if (created) AssetDatabase.CreateAsset(so, DefaultAssetPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = so;
            EditorGUIUtility.PingObject(so);
            Debug.Log($"{summary}\n{(created ? "Created" : "Refreshed")} {DefaultAssetPath}", so);
        }

        [MenuItem("Tools/Backrooms/Auto-Populate Selected Tile Set")]
        private static void PopulateSelected()
        {
            if (Selection.activeObject is BackroomsTileSet so)
            {
                string summary = Populate(so, DefaultPrefabRoot);
                AssetDatabase.SaveAssets();
                Debug.Log(summary, so);
            }
            else
            {
                EditorUtility.DisplayDialog("Auto-Populate Tile Set",
                    "Select a Backrooms Tile Set asset in the Project window first.", "OK");
            }
        }

        [MenuItem("Tools/Backrooms/Auto-Populate Selected Tile Set", true)]
        private static bool PopulateSelectedValidate() => Selection.activeObject is BackroomsTileSet;
    }
}
