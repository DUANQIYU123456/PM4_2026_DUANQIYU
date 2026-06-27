using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Flees from a position by picking a NavMesh point in the opposite direction.</summary>
    [RequireComponent(typeof(NavMeshMotor))]
    public class NavMeshFlee : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Max flee distance")]
        [Min(0f)]
        [SerializeField] private float range = 10f;

        [Tooltip("Min flee distance")]
        [Min(0f)]
        [SerializeField] private float minDistance = 2f;

        [Tooltip("Random spread around the flee direction (degrees)")]
        [Range(0f, 90f)]
        [SerializeField] private float randomAngle = 35f;

        //==================== STATE =====================
        private NavMeshMotor _motor;

        [Header("Debug")]
        [ReadOnly, SerializeField] private bool isFleeing;

        public bool IsFleeing => isFleeing;

        //==================== OUTPUTS =====================
        public event Action<Vector3> OnFled;
        public event Action OnFleeStopped;

        [Header("Events")]
        [Tooltip("Fired when a flee destination is set")]
        [SerializeField] private UnityEvent<Vector3> fledEvent;
        [Tooltip("Fired when flee is explicitly stopped")]
        [SerializeField] private UnityEvent fleeStoppedEvent;

        //==================== LIFECYCLE =====================
        private void Awake()
        {
            _motor = GetComponent<NavMeshMotor>();
        }

        private void OnDisable()
        {
            if (isFleeing) StopFlee();
        }

        //==================== INPUTS =====================
        /// <summary>Flee from a world position.</summary>
        public void FleeFrom(Vector3 threat)
        {
            Vector3 away = transform.position - threat;
            if (away.sqrMagnitude < 0.0001f)
                away = UnityEngine.Random.onUnitSphere;

            away.y = 0f;
            away.Normalize();

            float yaw = UnityEngine.Random.Range(-randomAngle, randomAngle);
            Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * away;
            float dist = UnityEngine.Random.Range(minDistance, range);
            Vector3 desired = transform.position + dir * dist;

            if (TryFleeTo(desired)) return;

            for (int i = 0; i < 4; i++)
            {
                float angle = UnityEngine.Random.Range(-180f, 180f);
                Vector3 fallbackDir = Quaternion.Euler(0f, angle, 0f) * away;
                Vector3 fallbackPos = transform.position + fallbackDir * UnityEngine.Random.Range(minDistance, range);

                if (TryFleeTo(fallbackPos)) return;
            }
        }

        /// <summary>Flee from a Transform's current position.</summary>
        public void FleeFrom(Transform threat)
        {
            if (!threat) return;
            FleeFrom(threat.position);
        }

        /// <summary>Stop fleeing and halt movement.</summary>
        [ContextMenu("Stop Flee")]
        public void StopFlee()
        {
            isFleeing = false;
            _motor.Stop();
            OnFleeStopped?.Invoke();
            fleeStoppedEvent?.Invoke();
        }

        //==================== PRIVATE =====================
        private bool TryFleeTo(Vector3 desired)
        {
            if (!NavMesh.SamplePosition(desired, out var hit, range, NavMesh.AllAreas)) return false;

            _motor.MoveTo(hit.position);
            isFleeing = true;
            OnFled?.Invoke(hit.position);
            fledEvent?.Invoke(hit.position);
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}
