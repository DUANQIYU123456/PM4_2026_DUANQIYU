using UnityEngine;

namespace Ludocore
{
    /// <summary>Detects objects via raycast or sphere cast along the forward axis.</summary>
    public class RaycastSensor : Sensor
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("How far the cast extends"), Min(0f)]
        [SerializeField] private float distance = 10f;

        [Tooltip("Sphere radius (0 = thin ray)"), Min(0f)]
        [SerializeField] private float castRadius;

        [Tooltip("Which layers can be detected")]
        [SerializeField] private LayerMask layerMask = ~0;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private GameObject currentTarget;
        private Vector3 _hitPoint;
        private Vector3 _hitNormal;

        public bool IsHitting => currentTarget;
        public Vector3 HitPoint => _hitPoint;
        public Vector3 HitNormal => _hitNormal;

        //==================== LIFECYCLE =====================
        private void Update()
        {
            bool didHit;
            RaycastHit hitInfo;

            if (castRadius > 0f)
                didHit = Physics.SphereCast(transform.position, castRadius, transform.forward, out hitInfo, distance, layerMask);
            else
                didHit = Physics.Raycast(transform.position, transform.forward, out hitInfo, distance, layerMask);

            if (didHit)
            {
                HandleHit(hitInfo);
            }
            else
            {
                HandleNoHit();
            }

            RefreshDistances();
        }

        //==================== PRIVATE =====================
        private void HandleHit(RaycastHit hitInfo)
        {
            GameObject hitObj = hitInfo.collider.gameObject;
            _hitPoint = hitInfo.point;
            _hitNormal = hitInfo.normal;

            if (hitObj == currentTarget) return;

            if (currentTarget) RemoveDetection(currentTarget);

            currentTarget = hitObj;
            AddDetection(new Signal
            {
                Object = hitObj,
                Distance = hitInfo.distance
            });
        }

        private void HandleNoHit()
        {
            _hitPoint = default;
            _hitNormal = default;

            if (!currentTarget) return;

            RemoveDetection(currentTarget);
            currentTarget = null;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position;
            Vector3 end = origin + transform.forward * distance;

            Gizmos.color = IsHitting ? Color.green : Color.red;
            Gizmos.DrawLine(origin, end);

            if (castRadius > 0f)
            {
                Gizmos.DrawWireSphere(origin, castRadius);
                Gizmos.DrawWireSphere(end, castRadius);
            }
            else
            {
                Gizmos.DrawSphere(origin, 0.05f);
            }
        }
    }
}
