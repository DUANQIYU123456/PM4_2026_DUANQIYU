using UnityEngine;

namespace Ludocore
{
    /// <summary>Spawns at a world position set at runtime via SetTarget. Falls back to this transform if no target is set.</summary>
    public class TargetedSpawner : Spawner
    {
        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private Vector3 target;
        [ReadOnly, SerializeField] private bool hasTarget;

        public Vector3 Target => hasTarget ? target : transform.position;
        public bool HasTarget => hasTarget;

        //==================== INPUTS =====================
        /// <summary>Set the spawn target to a world position.</summary>
        public void SetTarget(Vector3 worldPosition)
        {
            target = worldPosition;
            hasTarget = true;
        }

        /// <summary>Set the spawn target to a Transform's current position (snapshot — does not follow).</summary>
        public void SetTarget(Transform t)
        {
            if (!t) { hasTarget = false; return; }
            target = t.position;
            hasTarget = true;
        }

        /// <summary>Forget the current target — next Spawn() falls back to this transform.</summary>
        [ContextMenu("Clear Target")]
        public void ClearTarget() => hasTarget = false;

        //==================== PRIVATE =====================
        protected override Vector3 GetPosition()
            => hasTarget ? target : transform.position;

        //==================== GIZMOS =====================
        private void OnDrawGizmosSelected()
        {
            if (!hasTarget) return;
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(target, 0.25f);
            Gizmos.DrawLine(transform.position, target);
        }
    }
}
