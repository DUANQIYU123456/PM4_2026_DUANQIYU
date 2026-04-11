using UnityEngine;

namespace Ludocore
{
    /// <summary>Detects objects that physically collide with this object.</summary>
    [RequireComponent(typeof(Collider))]
    public class CollisionSensor : Sensor
    {
        //==================== LIFECYCLE =====================
        private void Update()
        {
            RefreshDistances();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (IsDetected(collision.gameObject)) return;

            AddDetection(new Signal
            {
                Object = collision.gameObject,
                Distance = Vector3.Distance(transform.position, collision.transform.position)
            });
        }

        private void OnCollisionExit(Collision collision)
        {
            RemoveDetection(collision.gameObject);
        }
    }
}
