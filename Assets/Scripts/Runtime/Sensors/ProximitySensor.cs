using System.Collections.Generic;
using UnityEngine;

namespace Ludocore
{
    /// <summary>Detects objects within a radius using physics overlap, no collider needed.</summary>
    public class ProximitySensor : Sensor
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Detection radius"), Min(0f)]
        [SerializeField] private float radius = 5f;

        [Tooltip("Which layers can be detected")]
        [SerializeField] private LayerMask layerMask = ~0;

        //==================== STATE =====================
        private readonly Collider[] _overlapBuffer = new Collider[64];
        private readonly HashSet<GameObject> _currentFrame = new();

        //==================== LIFECYCLE =====================
        private void Update()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, radius, _overlapBuffer, layerMask);

            _currentFrame.Clear();
            for (int i = 0; i < count; i++)
            {
                GameObject obj = _overlapBuffer[i].gameObject;
                if (obj == gameObject) continue;
                _currentFrame.Add(obj);
            }

            RemoveLost();
            AddNew();
            RefreshDistances();
        }

        //==================== PRIVATE =====================
        private void RemoveLost()
        {
            for (int i = Signals.Count - 1; i >= 0; i--)
            {
                if (!_currentFrame.Contains(Signals[i].Object))
                    RemoveDetection(Signals[i].Object);
            }
        }

        private void AddNew()
        {
            foreach (var obj in _currentFrame)
            {
                if (IsDetected(obj)) continue;

                AddDetection(new Signal
                {
                    Object = obj,
                    Distance = Vector3.Distance(transform.position, obj.transform.position)
                });
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = HasDetections ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
