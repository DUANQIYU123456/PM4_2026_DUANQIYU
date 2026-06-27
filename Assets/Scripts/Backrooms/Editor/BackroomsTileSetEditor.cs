// ============================================
// Backrooms Tile Set — Custom Inspector
// ============================================
// Organizes the tile set's prefab arrays into collapsible category
// sections with count badges and empty-slot warnings. A Grid / Edit
// view switch flips the prefab slots between a compact thumbnail grid
// (click a thumb to ping) and an editable field list with per-row
// add/remove. The shape-paired door/window variants render as their
// own section. "Auto-Populate" rescans BackroomsLite.
// ============================================

using UnityEditor;
using UnityEngine;

namespace Ludocore
{
    [CustomEditor(typeof(BackroomsTileSet))]
    public class BackroomsTileSetEditor : Editor
    {
        // Category -> the serialized GameObject[] field names it contains.
        private static readonly (string header, string[] props)[] Groups =
        {
            ("Floor",              new[] { "floorTiles", "floorHole", "floorDoor" }),
            ("Ceiling",            new[] { "ceilingTiles" }),
            ("Walls — Full",       new[] { "wallSolid", "wallHole" }),
            ("Posts & Ledges",     new[] { "posts", "ledges" }),
        };

        // Empty -> the build falls back / produces nothing useful: warn.
        private static readonly System.Collections.Generic.HashSet<string> Essential =
            new System.Collections.Generic.HashSet<string> { "floorTiles", "wallSolid" };

        private const float GridThumb = 64f;   // scan view
        private const float RowThumb = 48f;    // edit view, beside the field
        private const string EditModeKey = "BR_TileSetEditor_EditMode";

        private static readonly string[] ViewTabs = { "Grid", "Edit" };

        private bool editMode;
        private GUIStyle headerStyle;

        private void OnEnable()
        {
            editMode = EditorPrefs.GetBool(EditModeKey, false);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var so = (BackroomsTileSet)target;

            EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cellSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("wallHeight"));
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Auto-Populate from BackroomsLite", GUILayout.Height(24)))
                {
                    string summary = BackroomsTileSetPopulator.Populate(
                        so, BackroomsTileSetPopulator.DefaultPrefabRoot);
                    AssetDatabase.SaveAssets();
                    serializedObject.Update();
                    Debug.Log(summary, so);
                }

                int sel = GUILayout.Toolbar(editMode ? 1 : 0, ViewTabs, GUILayout.Width(120), GUILayout.Height(24));
                bool newEdit = sel == 1;
                if (newEdit != editMode)
                {
                    editMode = newEdit;
                    EditorPrefs.SetBool(EditModeKey, editMode);
                }
            }

            DrawValidationSummary();
            EditorGUILayout.Space();

            foreach (var g in Groups)
                DrawGroup(g.header, g.props);

            DrawVariantSection("Wall Doors", "doorVariants");
            DrawGroup("Wall Door Openings", new[] { "doorOpenings" });
            DrawVariantSection("Wall Windows", "windowVariants");
            DrawGroup("Wall Window Openings", new[] { "windowOpenings" });

            if (AssetPreview.IsLoadingAssetPreviews())
                Repaint();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawValidationSummary()
        {
            int total = 0, slots = 0, emptyEssential = 0;
            foreach (var g in Groups)
                foreach (var p in g.props)
                {
                    var prop = serializedObject.FindProperty(p);
                    if (prop == null) continue;
                    slots++;
                    total += prop.arraySize;
                    if (prop.arraySize == 0 && Essential.Contains(p)) emptyEssential++;
                }

            string msg = $"{total} prefab references across {slots} slots (+ door/window variants below).";
            if (emptyEssential > 0)
                EditorGUILayout.HelpBox(msg + $"\n{emptyEssential} essential slot(s) empty — generation will be incomplete. Click Auto-Populate.",
                    MessageType.Warning);
            else
                EditorGUILayout.HelpBox(msg, MessageType.None);
        }

        private void DrawGroup(string header, string[] props)
        {
            int count = 0;
            foreach (var p in props)
            {
                var prop = serializedObject.FindProperty(p);
                if (prop != null) count += prop.arraySize;
            }

            string key = "BR_TileSetEditor_Fold_" + header;
            bool open = EditorPrefs.GetBool(key, false);

            bool newOpen = DrawSectionHeader($"{header}   ({count})", open);
            if (newOpen != open) EditorPrefs.SetBool(key, newOpen);

            if (newOpen)
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    foreach (var p in props)
                        DrawSlot(serializedObject.FindProperty(p));

            EditorGUILayout.Space(2);
        }

        // Door/window variants are WallOpeningSet[] (label + cutouts + inserts), drawn
        // with the default property field so the cutout<->insert pairing is clear.
        private void DrawVariantSection(string header, params string[] props)
        {
            int count = 0;
            foreach (var p in props)
            {
                var prop = serializedObject.FindProperty(p);
                if (prop != null) count += prop.arraySize;
            }

            string key = "BR_TileSetEditor_Fold_" + header;
            bool open = EditorPrefs.GetBool(key, false);

            bool newOpen = DrawSectionHeader($"{header}   ({count} variants)", open);
            if (newOpen != open) EditorPrefs.SetBool(key, newOpen);

            if (newOpen)
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    foreach (var p in props)
                    {
                        var prop = serializedObject.FindProperty(p);
                        if (prop != null)
                            EditorGUILayout.PropertyField(prop, true);
                    }

            EditorGUILayout.Space(2);
        }

        // A full-width, clickable section header: the background spans the whole
        // inspector and the disclosure arrow sits inside the bar. Drawn by hand
        // rather than with BeginFoldoutHeaderGroup, which clips the highlight and
        // forbids the nested array foldouts the variant/edit views produce.
        private bool DrawSectionHeader(string label, bool open)
        {
            if (headerStyle == null)
                headerStyle = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };

            Rect row = GUILayoutUtility.GetRect(16f, 22f, GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint)
            {
                // Extend past the inspector's left/right margins so the bar
                // reads as a full-width band.
                Rect band = new Rect(row.x - 18f, row.y, row.width + 22f, row.height);
                Color c = EditorGUIUtility.isProSkin
                    ? new Color(1f, 1f, 1f, 0.05f)
                    : new Color(0f, 0f, 0f, 0.06f);
                EditorGUI.DrawRect(band, c);
            }

            return EditorGUI.Foldout(row, open, label, true, headerStyle);
        }

        private void DrawSlot(SerializedProperty arr)
        {
            if (arr == null) return;
            int n = arr.arraySize;
            bool emptyEssential = n == 0 && Essential.Contains(arr.name);

            // Slot title row: name + count on the left; an add (+) button on the
            // right only in Edit mode (Grid mode is read-only browsing).
            using (new EditorGUILayout.HorizontalScope())
            {
                var style = new GUIStyle(EditorStyles.boldLabel);
                if (n == 0) style.normal.textColor = emptyEssential ? new Color(0.9f, 0.5f, 0.2f) : Color.gray;
                EditorGUILayout.LabelField($"{arr.displayName}   ({n})", style);

                if (editMode)
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(new GUIContent("+", "Add slot"), EditorStyles.miniButton, GUILayout.Width(24)))
                    {
                        arr.InsertArrayElementAtIndex(n);
                        arr.GetArrayElementAtIndex(n).objectReferenceValue = null;
                    }
                }
            }

            if (emptyEssential)
                EditorGUILayout.HelpBox("Essential slot is empty.", MessageType.Warning);

            if (editMode)
                DrawEditList(arr);
            else
                DrawGrid(arr);

            EditorGUILayout.Space(4);
        }

        // Grid (scan) mode: compact thumbnails wrapped to the inspector width.
        private void DrawGrid(SerializedProperty arr)
        {
            if (arr.arraySize == 0)
            {
                EditorGUILayout.LabelField("—", EditorStyles.miniLabel);
                return;
            }

            const float pad = 4f;
            int cols = Mathf.Max(1, Mathf.FloorToInt((EditorGUIUtility.currentViewWidth - 48f) / (GridThumb + pad)));

            int i = 0;
            while (i < arr.arraySize)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int c = 0; c < cols && i < arr.arraySize; c++, i++)
                        DrawThumb(arr.GetArrayElementAtIndex(i).objectReferenceValue as GameObject, GridThumb);
                    GUILayout.FlexibleSpace();
                }
            }
        }

        // Edit mode: one editable row per element. Defer removal so we don't
        // mutate the array mid-iteration.
        private void DrawEditList(SerializedProperty arr)
        {
            int removeAt = -1;
            for (int i = 0; i < arr.arraySize; i++)
                if (DrawElementRow(arr.GetArrayElementAtIndex(i)))
                    removeAt = i;

            if (removeAt >= 0)
            {
                var el = arr.GetArrayElementAtIndex(removeAt);
                if (el.objectReferenceValue != null) el.objectReferenceValue = null; // object-ref arrays need the null pass first
                arr.DeleteArrayElementAtIndex(removeAt);
            }
        }

        // One editable element: thumbnail, the object field, remove (✕).
        private bool DrawElementRow(SerializedProperty element)
        {
            bool remove = false;
            var go = element.objectReferenceValue as GameObject;

            using (new EditorGUILayout.HorizontalScope(GUILayout.Height(RowThumb)))
            {
                DrawThumb(go, RowThumb);

                // Vertically centre the field beside the taller thumbnail.
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.FlexibleSpace();
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(element, GUIContent.none);
                        if (GUILayout.Button(new GUIContent("✕", "Remove"), EditorStyles.miniButton, GUILayout.Width(24)))
                            remove = true;
                    }
                    GUILayout.FlexibleSpace();
                }
            }
            return remove;
        }

        private void DrawThumb(GameObject go, float size)
        {
            var content = new GUIContent();
            if (go != null)
            {
                content.image = AssetPreview.GetAssetPreview(go) ?? AssetPreview.GetMiniThumbnail(go);
                content.tooltip = go.name;
            }
            else
            {
                content.text = "—";
                content.tooltip = "Empty / missing reference";
            }

            if (GUILayout.Button(content, GUILayout.Width(size), GUILayout.Height(size)) && go != null)
                EditorGUIUtility.PingObject(go);
        }
    }
}
