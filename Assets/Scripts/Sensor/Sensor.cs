using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Abstract base for all sensors — maintains a signal list and provides query/event APIs.</summary>
    public abstract class Sensor : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Filter")]
        [Tooltip("Only detect objects with one of these tags (empty = detect all)")]
        [SerializeField] private string[] requiredTags;

        //==================== STATE =====================
        [Header("Debug")]
        [Tooltip("Currently detected signals")]
        [ReadOnly, SerializeField] private List<Signal> signals = new();

        public IReadOnlyList<Signal> Signals => signals;
        public bool HasDetections => signals.Count > 0;

        //==================== OUTPUTS =====================
        public event Action<Signal> OnSignalAdded;
        public event Action<Signal> OnSignalLost;

        [Header("Events")]
        [Tooltip("Fired when a new object is detected")]
        [SerializeField] private UnityEvent<GameObject> detectedEvent;

        [Tooltip("Fired when a previously detected object is lost")]
        [SerializeField] private UnityEvent<GameObject> lostEvent;

        //==================== INPUTS =====================
        /// <summary>Get the nearest signal by distance. Returns false if no detections.</summary>
        public bool TryGetNearest(out Signal nearest)
        {
            nearest = default;
            if (signals.Count == 0) return false;

            nearest = signals[0];
            for (int i = 1; i < signals.Count; i++)
            {
                if (signals[i].Distance < nearest.Distance)
                    nearest = signals[i];
            }
            return true;
        }

        /// <summary>Check whether a specific GameObject is currently detected.</summary>
        public bool IsDetected(GameObject obj)
        {
            for (int i = 0; i < signals.Count; i++)
            {
                if (signals[i].Object == obj) return true;
            }
            return false;
        }

        //==================== PRIVATE =====================
        /// <summary>Returns true if the object passes the tag filter.</summary>
        protected bool PassesFilter(GameObject obj)
        {
            if (requiredTags == null || requiredTags.Length == 0) return true;

            for (int i = 0; i < requiredTags.Length; i++)
            {
                if (obj.CompareTag(requiredTags[i])) return true;
            }
            return false;
        }

        /// <summary>Register a new signal and fire detection events.</summary>
        protected void AddDetection(Signal signal)
        {
            if (!PassesFilter(signal.Object)) return;

            signals.Add(signal);
            OnSignalAdded?.Invoke(signal);
            detectedEvent?.Invoke(signal.Object);
        }

        /// <summary>Remove a detected object and fire lost events.</summary>
        protected void RemoveDetection(GameObject obj)
        {
            for (int i = signals.Count - 1; i >= 0; i--)
            {
                if (signals[i].Object == obj)
                {
                    var lost = signals[i];
                    signals.RemoveAt(i);
                    OnSignalLost?.Invoke(lost);
                    lostEvent?.Invoke(lost.Object);
                    return;
                }
            }
        }

        /// <summary>Update distances for all signals and prune destroyed objects.</summary>
        protected void RefreshDistances()
        {
            for (int i = signals.Count - 1; i >= 0; i--)
            {
                if (!signals[i].Object)
                {
                    var lost = signals[i];
                    signals.RemoveAt(i);
                    OnSignalLost?.Invoke(lost);
                    lostEvent?.Invoke(lost.Object);
                    continue;
                }

                var signal = signals[i];
                signal.Distance = Vector3.Distance(transform.position, signal.Object.transform.position);
                signals[i] = signal;
            }
        }
    }
}
