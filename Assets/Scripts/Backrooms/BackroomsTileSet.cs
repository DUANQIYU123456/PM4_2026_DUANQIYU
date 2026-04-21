// ============================================
// Backrooms Tile Set
// ============================================
// ScriptableObject holding prefab references
// for the backrooms procedural generator.
// All tiles are center-pivoted, 3m grid.
// Floor tiles have two materials: floor (top)
// and ceiling (bottom) — reused for ceilings.
// ============================================

using UnityEngine;

namespace Ludocore
{
    [CreateAssetMenu(fileName = "NewBackroomsTileSet", menuName = "Ludocore/Backrooms Tile Set")]
    public class BackroomsTileSet : ScriptableObject
    {
        [Header("Grid Settings")]
        [Tooltip("Cell size in meters. Must match tile width/depth.")]
        public float cellSize = 3f;

        [Tooltip("Wall height in meters. Must match wall tile height.")]
        public float wallHeight = 3f;

        [Header("Floor Tiles")]
        [Tooltip("Floor tile variants (center-pivoted). Randomly picked per cell.")]
        public GameObject[] floorTiles;

        [Header("Ceiling Tiles")]
        [Tooltip("Tiles placed at wall height for ceiling. If empty, uses floorTiles.")]
        public GameObject[] ceilingTiles;

        [Header("Walls — Solid")]
        [Tooltip("Solid wall variants (center-pivoted, bottom-aligned).")]
        public GameObject[] wallSolid;

        [Header("Walls — Door Opening")]
        [Tooltip("Wall variants with a door cutout.")]
        public GameObject[] wallDoor;

        [Header("Walls — Window")]
        [Tooltip("Wall variants with a window cutout.")]
        public GameObject[] wallWindow;

        [Header("Corners")]
        [Tooltip("Corner columns placed at grid vertices.")]
        public GameObject[] corners;

        [Header("Door Inserts")]
        [Tooltip("Standalone door prefabs for door openings.")]
        public GameObject[] doorInserts;

        [Header("Window Inserts")]
        [Tooltip("Standalone window prefabs for window openings.")]
        public GameObject[] windowInserts;

        public GameObject[] GetCeilingTiles()
        {
            return (ceilingTiles != null && ceilingTiles.Length > 0) ? ceilingTiles : floorTiles;
        }
    }
}
