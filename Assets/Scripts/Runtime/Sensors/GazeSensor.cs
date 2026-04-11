using System.Collections.Generic;
using UnityEngine;

namespace Ludocore
{
    /// <summary>Detects objects the viewer is looking at using dot product and distance.</summary>
    public class GazeSensor : Sensor
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("The viewer transform (defaults to main camera)")]
        [SerializeField] private Transform viewer;

        [Tooltip("How directly the viewer must look at a target (0 = 90°, 1 = exact)")]
        [Range(0f, 1f)]
        [SerializeField] private float threshold = 0.9f;

        [Tooltip("Maximum detection distance"), Min(0f)]
        [SerializeField] private float maxDistance = 50f;

        [Tooltip("Which layers can be detected")]
        [SerializeField] private LayerMask layerMask = ~0;

        //==================== STATE =====================
        private Transform _eye;

        public Transform Viewer => _eye;

        private readonly Collider[] _overlapBuffer = new Collider[64];
        private readonly HashSet<GameObject> _currentFrame = new();

        //==================== LIFECYCLE =====================
        private void Awake()
        {
            _eye = viewer;
        }

        private void Update()
        {
            if (!_eye)
            {
                if (Camera.main) _eye = Camera.main.transform;
                else return;
            }

            int count = Physics.OverlapSphereNonAlloc(
                _eye.position, maxDistance, _overlapBuffer, layerMask);

            _currentFrame.Clear();
            for (int i = 0; i < count; i++)
            {
                GameObject obj = _overlapBuffer[i].gameObject;
                if (obj == _eye.gameObject) continue;
                if (!IsGazedAt(obj)) continue;
                _currentFrame.Add(obj);
            }

            RemoveLost();
            AddNew();
            RefreshDistances();
        }

        //==================== PRIVATE =====================
        private bool IsGazedAt(GameObject obj)
        {
            Vector3 dir = (obj.transform.position - _eye.position).normalized;
            float dot = Vector3.Dot(_eye.forward, dir);
            return dot >= threshold;
        }

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
                    Distance = Vector3.Distance(_eye.position, obj.transform.position)
                });
            }
        }

        private void OnDrawGizmosSelected()
        {
            Transform eye = _eye ? _eye : viewer;
            if (!eye) return;

            Gizmos.color = HasDetections ? Color.green : Color.cyan;
            Gizmos.DrawWireSphere(eye.position, maxDistance);

            // Draw cone edges to visualize gaze angle
            float angle = Mathf.Acos(threshold) * Mathf.Rad2Deg;
            Vector3 origin = eye.position;

            Gizmos.color = Color.cyan;
            DrawGizmoRay(origin, eye, angle, Vector3.up);
            DrawGizmoRay(origin, eye, -angle, Vector3.up);
            DrawGizmoRay(origin, eye, angle, Vector3.right);
            DrawGizmoRay(origin, eye, -angle, Vector3.right);
        }

        private static void DrawGizmoRay(Vector3 origin, Transform eye, float angle, Vector3 axis)
        {
            Vector3 dir = Quaternion.AngleAxis(angle, eye.TransformDirection(axis)) * eye.forward;
            Gizmos.DrawRay(origin, dir * 5f);
        }
    }
}
