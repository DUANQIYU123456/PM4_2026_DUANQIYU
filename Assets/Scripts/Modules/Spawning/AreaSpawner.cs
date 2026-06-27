using UnityEngine;

namespace Ludocore
{
    /// <summary>Spawns at a random position within a box around the chosen origin.</summary>
    public class AreaSpawner : Spawner
    {
        //==================== CONFIG =====================
        [Header("Position")]
        [Tooltip("Origin of the area. Uses this transform if empty.")]
        [SerializeField] private Transform origin;

        [Tooltip("Box dimensions for randomization. Zero on an axis = no jitter on that axis.")]
        [SerializeField] private Vector3 areaSize = new Vector3(2f, 0f, 2f);

        [Header("Rotation")]
        [Tooltip("If true, each instance gets a fully random rotation. If false, uses the origin's rotation.")]
        [SerializeField] private bool randomRotation;

        //==================== PRIVATE =====================
        private Vector3 OriginPos => origin ? origin.position : transform.position;
        private Quaternion OriginRot => origin ? origin.rotation : transform.rotation;

        protected override Vector3 GetPosition()
            => OriginPos + new Vector3(
                Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
                Random.Range(-areaSize.y / 2f, areaSize.y / 2f),
                Random.Range(-areaSize.z / 2f, areaSize.z / 2f));

        protected override Quaternion GetRotation()
            => randomRotation ? Random.rotation : OriginRot;

        //==================== GIZMOS =====================
        private void OnDrawGizmosSelected()
        {
            var pos = OriginPos;
            Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
            Gizmos.DrawCube(pos, areaSize);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(pos, areaSize);
        }
    }
}
