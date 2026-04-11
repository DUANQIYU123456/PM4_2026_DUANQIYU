using UnityEngine;

namespace Ludocore
{
    /// <summary>Detects objects entering and exiting a trigger collider.</summary>
    [RequireComponent(typeof(Collider))]
    public class TriggerSensor : Sensor
    {
        //==================== LIFECYCLE =====================
        private void Update()
        {
            RefreshDistances();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsDetected(other.gameObject)) return;

            AddDetection(new Signal
            {
                Object = other.gameObject,
                Distance = Vector3.Distance(transform.position, other.transform.position)
            });
        }

        private void OnTriggerExit(Collider other)
        {
            RemoveDetection(other.gameObject);
        }

        //==================== PRIVATE =====================
        private void OnDrawGizmosSelected()
        {
            var col = GetComponent<SphereCollider>();
            if (!col) return;

            Gizmos.color = HasDetections ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, col.radius * transform.lossyScale.x);
        }
    }
}
