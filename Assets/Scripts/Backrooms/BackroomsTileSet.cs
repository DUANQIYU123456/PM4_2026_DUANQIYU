// ============================================
// Backrooms Tile Set
// ============================================
// ScriptableObject holding prefab references for the
// backrooms procedural generator. 3m grid, Wall family A.
//
// Openings model: cutout walls are organized as WallOpeningSet
// "variants" by shape, each pairing the cutout wall(s) with the
// CORRECT insert object(s). A variant with no inserts is a bare
// opening (e.g. the "_Alt" cutouts that have no door fixture).
// ============================================

using System;
using UnityEngine;

namespace Ludocore
{
    // A door/window kind: the cutout wall(s) + the matching insert object(s).
    // Empty inserts = a bare opening (no fixture placed).
    [Serializable]
    public class WallOpeningSet
    {
        public string label;
        [Tooltip("Wall tiles with this opening cut into them.")]
        public GameObject[] cutouts;
        [Tooltip("Matching fixture objects placed in the cutout. Empty = bare opening.")]
        public GameObject[] inserts;
    }

    [CreateAssetMenu(fileName = "NewBackroomsTileSet", menuName = "Ludocore/Backrooms Tile Set")]
    public class BackroomsTileSet : ScriptableObject
    {
        [Header("Grid Settings")]
        [Tooltip("Cell size in meters. Must match tile width/depth.")]
        public float cellSize = 3f;

        [Tooltip("Wall height in meters. Must match wall tile height.")]
        public float wallHeight = 3f;

        [Header("Floor Tiles")]
        [Tooltip("Clean floor tile variants (center-pivoted). Randomly picked per cell.")]
        public GameObject[] floorTiles;

        [Tooltip("Hole / pit floor variants.")]
        public GameObject[] floorHole;

        [Tooltip("Floor tile with a hatch / floor-door.")]
        public GameObject[] floorDoor;

        [Header("Ceiling Tiles")]
        [Tooltip("Tiles placed at wall height for ceiling. If empty, uses floorTiles.")]
        public GameObject[] ceilingTiles;

        [Header("Walls — Full Height (3x3)")]
        [Tooltip("Solid full-height wall variants (bottom-aligned).")]
        public GameObject[] wallSolid;

        [Tooltip("Full-height wall variants with an informal hole / gap.")]
        public GameObject[] wallHole;

        [Header("Wall Doors (fixtures)")]
        [Tooltip("Door kinds by shape: cutout wall paired with its matching door objects.")]
        public WallOpeningSet[] doorVariants;

        [Tooltip("Bare door openings — cutout walls with no door object.")]
        public GameObject[] doorOpenings;

        [Header("Wall Windows (fixtures)")]
        [Tooltip("Window kinds by shape: cutout wall paired with its matching window objects.")]
        public WallOpeningSet[] windowVariants;

        [Tooltip("Bare window openings — cutout walls with no window object.")]
        public GameObject[] windowOpenings;

        [Header("Posts & Ledges")]
        [Tooltip("Posts at wall vertices (round faces — rotation-proof). Used as the corner piece, stacked per level.")]
        public GameObject[] posts;

        [Tooltip("Horizontal ledge/cornice (3m) capping wall/ceiling seams at level boundaries in tall rooms.")]
        public GameObject[] ledges;

        // --- Convenience alias ---
        public GameObject[] WallFull => wallSolid;

        public GameObject[] GetCeilingTiles()
        {
            return (ceilingTiles != null && ceilingTiles.Length > 0) ? ceilingTiles : floorTiles;
        }
    }
}
