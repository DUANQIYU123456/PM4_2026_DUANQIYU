using UnityEngine;

namespace Ludocore
{
    /// <summary>Spawns at the exact position and rotation of a chosen Transform (defaults to this transform).</summary>
    public class PointSpawner : Spawner
    {
        //==================== CONFIG =====================
        [Header("Position")]
        [Tooltip("Where to spawn. Uses this transform if empty.")]
        [SerializeField] private Transform spawnPoint;

        //==================== PRIVATE =====================
        protected override Vector3 GetPosition()
            => spawnPoint ? spawnPoint.position : transform.position;

        protected override Quaternion GetRotation()
            => spawnPoint ? spawnPoint.rotation : transform.rotation;

        //==================== GIZMOS =====================
        private void OnDrawGizmosSelected()
        {
            var origin = spawnPoint ? spawnPoint.position : transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin, 0.15f);
        }
    }
}
