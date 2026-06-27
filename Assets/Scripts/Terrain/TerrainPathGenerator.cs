using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace Ludocore
{
    /// <summary>Generates wandering pathway splines that lie on a Unity Terrain.
    /// Generate() rolls fresh XZ + Y deterministically from the profile seed;
    /// ReprojectHeights() keeps existing XZ but resamples terrain height — call it
    /// after the heightmap changes (e.g. after HillTerrainGenerator.Generate or a
    /// TerrainDrift / TerrainBreathe tick) to keep paths glued to the surface.</summary>
    [RequireComponent(typeof(SplineContainer))]
    public class TerrainPathGenerator : MonoBehaviour
    {
        //==================== SCENE REFERENCES =====================
        [Header("Scene References")]
        [Tooltip("Terrain to project knots onto. Auto-fetched from this GameObject, then Terrain.activeTerrain, if empty.")]
        [SerializeField] private Terrain terrain;

        [Tooltip("Container the generated splines are written into. Auto-fetched from this GameObject if empty.")]
        [SerializeField] private SplineContainer splineContainer;

        //==================== PROFILE =====================
        [Header("Profile")]
        [Tooltip("Tuning data for path count, shape, placement, and projection.")]
        [SerializeField] private TerrainPathProfile profile;

        //==================== BEHAVIOUR =====================
        [Header("Behaviour")]
        [Tooltip("Call Generate() in Start(). Off by default — usually you let HillTerrainGenerator bake first, then trigger paths.")]
        [SerializeField] private bool generateOnStart = false;

        //==================== STATE =====================
        // Tracked so Generate() can clean up previously spawned endpoints, and ReprojectHeights()
        // can re-pose them. Serialized so references survive domain reload / scene save.
        [SerializeField, HideInInspector] private List<GameObject> _spawnedEndpoints = new List<GameObject>();

        //==================== LIFECYCLE =====================
        private void Reset()
        {
            terrain = GetComponent<Terrain>();
            if (!terrain) terrain = Terrain.activeTerrain;
            splineContainer = GetComponent<SplineContainer>();
        }

        private void Start()
        {
            if (generateOnStart) Generate();
        }

        //==================== INPUTS =====================
        /// <summary>Full re-roll: deterministic new XZ from the profile seed, with Y projected onto the terrain.</summary>
        [ContextMenu("Generate")]
        public void Generate()
        {
            if (!terrain) terrain = Terrain.activeTerrain;
            if (!splineContainer) splineContainer = GetComponent<SplineContainer>();
            if (!terrain || !splineContainer || !profile) return;

            // Clear endpoints first so the ReprojectHeights call below sees an empty list and skips the endpoint pass.
            DestroyEndpoints();

            var size = terrain.terrainData.size;
            float minX = profile.edgePadding;
            float maxX = Mathf.Max(profile.edgePadding, size.x - profile.edgePadding);
            float minZ = profile.edgePadding;
            float maxZ = Mathf.Max(profile.edgePadding, size.z - profile.edgePadding);

            var splines = new List<Spline>(profile.pathCount);

            for (int p = 0; p < profile.pathCount; p++)
            {
                // XOR with a large prime — same flavour as HillTerrainGenerator's Perlin offset trick.
                // Each path is independently deterministic from the same profile seed.
                var rng = new System.Random(profile.seed ^ (p * 73856093));
                float ox = (float)(rng.NextDouble() * 200000.0 - 100000.0);
                float oy = (float)(rng.NextDouble() * 200000.0 - 100000.0);

                Vector2 pos = new Vector2(
                    Mathf.Lerp(minX, maxX, (float)rng.NextDouble()),
                    Mathf.Lerp(minZ, maxZ, (float)rng.NextDouble())
                );
                float heading = (float)rng.NextDouble() * Mathf.PI * 2f;

                var knots = new BezierKnot[profile.knotsPerPath];
                knots[0] = new BezierKnot(LocalKnot(pos.x, pos.y));

                float arc = 0f;
                for (int i = 1; i < profile.knotsPerPath; i++)
                {
                    float n = Mathf.PerlinNoise(arc * profile.wanderFrequency + ox, oy) * 2f - 1f;
                    heading += n * profile.wanderStrength * Mathf.Deg2Rad;
                    pos += new Vector2(Mathf.Cos(heading), Mathf.Sin(heading)) * profile.stepLength;

                    Vector2 clamped = new Vector2(
                        Mathf.Clamp(pos.x, minX, maxX),
                        Mathf.Clamp(pos.y, minZ, maxZ)
                    );
                    // If clamping activated, reflect heading so the path turns inward instead of grinding the edge.
                    if (clamped != pos) heading += Mathf.PI;
                    pos = clamped;

                    arc += profile.stepLength;
                    knots[i] = new BezierKnot(LocalKnot(pos.x, pos.y));
                }

                var spline = new Spline { Knots = knots };
                spline.SetTangentMode(new SplineRange(0, spline.Count), TangentMode.AutoSmooth);
                splines.Add(spline);
            }

            splineContainer.Splines = splines;

            // Y is still 0 on every knot — single source of truth for height is ReprojectHeights().
            ReprojectHeights();

            SpawnEndpoints();
        }

        /// <summary>Sticky XZ: walks every knot and replaces only Y by sampling the live terrain.
        /// Cheap enough to call every TerrainDrift / TerrainBreathe tick.</summary>
        [ContextMenu("Reproject Heights")]
        public void ReprojectHeights()
        {
            if (!terrain) terrain = Terrain.activeTerrain;
            if (!splineContainer) splineContainer = GetComponent<SplineContainer>();
            if (!terrain || !splineContainer || !profile) return;

            if (splineContainer.Splines.Count == 0)
            {
                Debug.LogWarning($"{nameof(TerrainPathGenerator)}.ReprojectHeights: container has no splines — call Generate() first.", this);
                return;
            }

            Vector3 terrainPos = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;

            foreach (var spline in splineContainer.Splines)
            {
                for (int i = 0; i < spline.Count; i++)
                {
                    BezierKnot k = spline[i];
                    Vector3 world = splineContainer.transform.TransformPoint((Vector3)k.Position);

                    // SampleHeight returns 0 outside terrain bounds — skip rather than dropping the knot to the floor.
                    float lx = world.x - terrainPos.x;
                    float lz = world.z - terrainPos.z;
                    if (lx < 0f || lz < 0f || lx > size.x || lz > size.z) continue;

                    float surfY = terrain.SampleHeight(world) + terrainPos.y + profile.surfaceOffset;
                    Vector3 localOnSurface = splineContainer.transform.InverseTransformPoint(new Vector3(world.x, surfY, world.z));
                    k.Position = (float3)localOnSurface;
                    spline[i] = k; // Mandatory write-back; BezierKnot is a struct. Indexer setter recomputes AutoSmooth tangents.
                }
            }

            UpdateEndpointPoses();

#if UNITY_EDITOR
            // Don't dirty the scene every tick when driven by TerrainDrift/Breathe at runtime.
            if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(splineContainer);
#endif
        }

        //==================== PRIVATE =====================
        // Terrain-local XZ → SplineContainer-local position (Y left at 0; ReprojectHeights fills it in).
        private float3 LocalKnot(float terrainLocalX, float terrainLocalZ)
        {
            Vector3 world = terrain.transform.position + new Vector3(terrainLocalX, 0f, terrainLocalZ);
            return (float3)splineContainer.transform.InverseTransformPoint(world);
        }

        private void DestroyEndpoints()
        {
            for (int i = 0; i < _spawnedEndpoints.Count; i++)
            {
                var go = _spawnedEndpoints[i];
                if (!go) continue;
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }
            _spawnedEndpoints.Clear();
        }

        private void SpawnEndpoints()
        {
            if (!profile.endpointPrefab) return;

            foreach (var spline in splineContainer.Splines)
            {
                if (spline.Count == 0) continue;

                GameObject go = null;
#if UNITY_EDITOR
                // Preserve the prefab link in editor so the spawned object stays an instance, not a dumb copy.
                // Falls through to plain Instantiate if endpointPrefab isn't a prefab asset (e.g. a scene object).
                if (!Application.isPlaying)
                {
                    go = UnityEditor.PrefabUtility.InstantiatePrefab(profile.endpointPrefab) as GameObject;
                    if (go) go.transform.SetParent(transform, worldPositionStays: false);
                }
#endif
                if (!go) go = Instantiate(profile.endpointPrefab, transform);

                ApplyEndpointPose(go, spline);
                _spawnedEndpoints.Add(go);
            }
        }

        private void UpdateEndpointPoses()
        {
            if (_spawnedEndpoints.Count == 0) return;

            int n = Mathf.Min(_spawnedEndpoints.Count, splineContainer.Splines.Count);
            for (int i = 0; i < n; i++)
            {
                var go = _spawnedEndpoints[i];
                if (!go) continue;
                ApplyEndpointPose(go, splineContainer.Splines[i]);
            }
        }

        private void ApplyEndpointPose(GameObject go, Spline spline)
        {
            int last = spline.Count - 1;
            if (last < 0) return;

            BezierKnot endKnot = spline[last];
            Vector3 endWorld = splineContainer.transform.TransformPoint((Vector3)endKnot.Position);

            // Resample terrain directly so the endpoint sits on the ground regardless of the path's surfaceOffset.
            Vector3 terrainPos = terrain.transform.position;
            Vector3 tSize = terrain.terrainData.size;
            float lx = endWorld.x - terrainPos.x;
            float lz = endWorld.z - terrainPos.z;
            if (lx >= 0f && lz >= 0f && lx <= tSize.x && lz <= tSize.z)
                endWorld.y = terrain.SampleHeight(endWorld) + terrainPos.y + profile.endpointGroundOffset;

            go.transform.position = endWorld;

            if (!profile.endpointAlignToPath || spline.Count < 2)
            {
                go.transform.rotation = Quaternion.identity;
                return;
            }

            Vector3 prevWorld = splineContainer.transform.TransformPoint((Vector3)spline[last - 1].Position);
            Vector3 forward = endWorld - prevWorld;
            forward.y = 0f; // Keep upright on XZ; we only steer the yaw.
            if (forward.sqrMagnitude < 1e-6f)
            {
                go.transform.rotation = Quaternion.identity;
                return;
            }
            go.transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        }
    }
}
