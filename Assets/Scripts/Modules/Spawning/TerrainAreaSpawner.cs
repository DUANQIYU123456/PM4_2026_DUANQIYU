using UnityEngine;

namespace Ludocore
{
    /// <summary>Spawns at random XZ within an area, snapping Y to the Unity Terrain surface.</summary>
    public class TerrainAreaSpawner : Spawner
    {
        //==================== CONFIG =====================
        [Header("Position")]
        [Tooltip("Origin of the area. Uses this transform if empty.")]
        [SerializeField] private Transform origin;

        [Tooltip("XZ footprint to scatter within. Y comes from the terrain.")]
        [SerializeField] private Vector2 areaSize = new Vector2(10f, 10f);

        [Tooltip("Vertical offset added after snapping to the surface.")]
        [SerializeField] private float yOffset;

        [Header("Terrain")]
        [Tooltip("Terrain to sample. Uses Terrain.activeTerrain if empty.")]
        [SerializeField] private Terrain terrain;

        [Header("Rotation")]
        [Tooltip("If true, the instance's up axis aligns to the terrain surface normal.")]
        [SerializeField] private bool alignToSurface;

        [Tooltip("If true, randomize yaw around the up axis.")]
        [SerializeField] private bool randomYaw;

        //==================== STATE =====================
        private Vector3 _lastNormal = Vector3.up;

        //==================== PRIVATE =====================
        private Terrain ActiveTerrain => terrain ? terrain : Terrain.activeTerrain;
        private Vector3 OriginPos => origin ? origin.position : transform.position;
        private Quaternion OriginRot => origin ? origin.rotation : transform.rotation;

        protected override Vector3 GetPosition()
        {
            var o = OriginPos;
            float x = o.x + Random.Range(-areaSize.x / 2f, areaSize.x / 2f);
            float z = o.z + Random.Range(-areaSize.y / 2f, areaSize.y / 2f);

            var t = ActiveTerrain;
            float y;
            if (t)
            {
                y = t.SampleHeight(new Vector3(x, 0f, z)) + t.transform.position.y;
                _lastNormal = SampleNormal(t, x, z);
            }
            else
            {
                y = o.y;
                _lastNormal = Vector3.up;
            }

            return new Vector3(x, y + yOffset, z);
        }

        protected override Quaternion GetRotation()
        {
            Quaternion baseRot = alignToSurface
                ? Quaternion.FromToRotation(Vector3.up, _lastNormal)
                : OriginRot;
            if (randomYaw) baseRot *= Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            return baseRot;
        }

        private static Vector3 SampleNormal(Terrain t, float worldX, float worldZ)
        {
            var data = t.terrainData;
            var tp = t.transform.position;
            float u = (worldX - tp.x) / data.size.x;
            float v = (worldZ - tp.z) / data.size.z;
            return data.GetInterpolatedNormal(u, v);
        }

        //==================== GIZMOS =====================
        private void OnDrawGizmosSelected()
        {
            var pos = OriginPos;
            var size = new Vector3(areaSize.x, 0.05f, areaSize.y);
            Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
            Gizmos.DrawCube(pos, size);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(pos, size);
        }
    }
}
