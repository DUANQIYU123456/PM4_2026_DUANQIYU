using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>
    /// Moves and rotates an object between its rest pose and a pose offset as a sensed target
    /// (e.g. the player) gets closer. All tuning lives in a swappable <see cref="ProximityMoverProfile"/>,
    /// so one floating-house prefab can be reposed (rise, sink, slide, tumble, latch) by swapping profiles.
    /// Fires threshold events you wire to anything (start a Float, release a Rigidbody, play a sound, Stop this mover).
    ///
    /// Note: if the sensor rides this moving object, motion feeds back into proximity — it self-limits
    /// at low offsets and oscillates at high ones. Put the sensor on a STATIONARY anchor to avoid both.
    /// </summary>
    public class ProximityMover : MonoBehaviour
    {
        //==================== SCENE REFERENCES =====================
        [Header("Scene References")]
        [Tooltip("Proximity sensor that detects the target. Put it on a stationary anchor (not this moving rig) for stable, full-range motion.")]
        [SerializeField] private ProximitySensor sensor;

        //==================== PROFILE =====================
        [Header("Profile")]
        [Tooltip("Scriptable object with all motion, smoothing, and threshold tuning. Swap to repose the house.")]
        [SerializeField] private ProximityMoverProfile profile;

        //==================== OUTPUTS =====================
        public event Action OnThresholdEntered;
        public event Action OnThresholdExited;

        [Header("Events")]
        [Tooltip("Fired when progress rises past the threshold. Wire e.g. Float.Play, Rigidbody.isKinematic=false, a sound, or ProximityMover.Stop to latch.")]
        [SerializeField] private UnityEvent thresholdEnteredEvent;
        [Tooltip("Fired when progress falls back below the threshold.")]
        [SerializeField] private UnityEvent thresholdExitedEvent;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private float progress;
        [ReadOnly, SerializeField] private bool aboveThreshold;

        private Vector3 _restPos;
        private Quaternion _restRot;
        private bool _driving = true;

        public bool IsDriving => _driving;

        //==================== LIFECYCLE =====================
        private void Reset()
        {
            if (!sensor) sensor = GetComponent<ProximitySensor>();
        }

        private void Start()
        {
            if (!sensor) sensor = GetComponent<ProximitySensor>();

            bool local = !profile || profile.useLocalSpace;
            _restPos = local ? transform.localPosition : transform.position;
            _restRot = local ? transform.localRotation : transform.rotation;
            _driving = true;
        }

        private void Update()
        {
            if (!sensor || !profile || !_driving) return;

            // --- 1. Raw proximity from the nearest detection (0 = far / none, 1 = at the sensor) ---
            float proximity = 0f;
            if (sensor.TryGetNearest(out Signal nearest))
            {
                float range = profile.maxDistance > 0f ? profile.maxDistance : sensor.Radius;
                proximity = 1f - Mathf.Clamp01(nearest.Distance / Mathf.Max(0.0001f, range));
            }

            // --- 2. Shape and smooth into movement progress ---
            float target = profile.response.Evaluate(proximity);
            float speed = target >= progress ? profile.riseSpeed : profile.fallSpeed;
            progress = speed <= 0f
                ? target
                : Mathf.MoveTowards(progress, target, speed * Time.deltaTime);

            // --- 3. Threshold crossing: fire events (wire Float / Rigidbody / Stop externally) ---
            UpdateThreshold();

            // --- 4. Drive position and rotation ---
            Vector3 drivenPos = _restPos + profile.moveOffset * progress;
            Quaternion drivenRot = _restRot * Quaternion.Euler(profile.rotationOffset * progress);
            if (profile.useLocalSpace)
            {
                transform.localPosition = drivenPos;
                transform.localRotation = drivenRot;
            }
            else
            {
                transform.position = drivenPos;
                transform.rotation = drivenRot;
            }
        }

        //==================== INPUTS =====================
        /// <summary>Resume driving from the current pose.</summary>
        [ContextMenu("Play")]
        public void Play() => _driving = true;

        /// <summary>Stop driving, freezing the object at its current pose (e.g. to latch it in place).</summary>
        [ContextMenu("Stop")]
        public void Stop() => _driving = false;

        //==================== PRIVATE =====================
        private void UpdateThreshold()
        {
            bool over = progress >= profile.threshold;
            if (over == aboveThreshold) return;

            aboveThreshold = over;
            if (over)
            {
                OnThresholdEntered?.Invoke();
                thresholdEnteredEvent?.Invoke();
            }
            else
            {
                OnThresholdExited?.Invoke();
                thresholdExitedEvent?.Invoke();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!profile) return;

            Vector3 worldOffset = profile.useLocalSpace
                ? (transform.parent ? transform.parent.TransformVector(profile.moveOffset) : profile.moveOffset)
                : profile.moveOffset;

            Vector3 start = transform.position;
            Vector3 end = start + worldOffset;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(start, 0.15f);
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(end, 0.15f);
        }
    }
}
