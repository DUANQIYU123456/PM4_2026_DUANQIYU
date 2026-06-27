using UnityEngine;

namespace Ludocore
{
    /// <summary>Spawns at a local-space offset from this transform — position moves and rotation matches the caster.</summary>
    public class LocalSpawner : Spawner
    {
        //==================== CONFIG =====================
        [Header("Position")]
        [Tooltip("Offset in this transform's local space. (0,0,1) = 1m in front. The offset rotates with the entity.")]
        [SerializeField] private Vector3 localOffset = new Vector3(0f, 0f, 1f);

        //==================== PRIVATE =====================
        protected override Vector3 GetPosition()
            => transform.TransformPoint(localOffset);

        protected override Quaternion GetRotation()
            => transform.rotation;

        //==================== GIZMOS =====================
        private void OnDrawGizmosSelected()
        {
            var pos = transform.TransformPoint(localOffset);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(pos, 0.15f);
            Gizmos.DrawLine(transform.position, pos);
        }
    }
}
