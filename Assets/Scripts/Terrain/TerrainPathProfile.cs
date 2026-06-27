using UnityEngine;

namespace Ludocore
{
    /// <summary>Tuning profile for TerrainPathGenerator — wandering pathway splines
    /// laid on a Unity Terrain. Swap profiles per scene to change path style.</summary>
    [CreateAssetMenu(menuName = "Ludocore/Terrain Path Profile")]
    public class TerrainPathProfile : ScriptableObject
    {
        //==================== PATHS =====================
        [Header("Paths")]
        [Tooltip("Number of independent paths produced inside the SplineContainer.")]
        [Range(1, 8)]
        public int pathCount = 1;

        [Tooltip("Knots per path. More = smoother curves, more SampleHeight calls when re-projecting.")]
        [Range(2, 64)]
        public int knotsPerPath = 16;

        //==================== SHAPE =====================
        [Header("Shape")]
        [Tooltip("Distance in metres between consecutive knots along the heading.")]
        [Min(1f)]
        public float stepLength = 25f;

        [Tooltip("Max heading change per step in degrees. 0 = straight line; ~90 = jagged.")]
        [Range(0f, 90f)]
        public float wanderStrength = 35f;

        [Tooltip("Perlin frequency along arc length. Low = lazy curves; high = wiggly.")]
        [Min(0.0001f)]
        public float wanderFrequency = 0.15f;

        //==================== PLACEMENT =====================
        [Header("Placement")]
        [Tooltip("Keep knots this far from the terrain edge (m).")]
        [Min(0f)]
        public float edgePadding = 20f;

        [Tooltip("Deterministic seed — same seed gives the same paths.")]
        public int seed = 0;

        //==================== PROJECTION =====================
        [Header("Projection")]
        [Tooltip("Lift knots above the surface (m). 0 = flush; small offset avoids z-fighting / clipping when path meshes are extruded later.")]
        [Min(0f)]
        public float surfaceOffset = 0f;

        //==================== ENDPOINT =====================
        [Header("Endpoint")]
        [Tooltip("Optional prefab spawned at the END of every path (e.g. a house). Leave empty to disable.")]
        public GameObject endpointPrefab;

        [Tooltip("Lift the endpoint above the spline knot (m). Use a positive value if the prefab pivot sits below its base.")]
        public float endpointGroundOffset = 0f;

        [Tooltip("Rotate the endpoint so its +Z faces along the path's last segment.")]
        public bool endpointAlignToPath = true;
    }
}
