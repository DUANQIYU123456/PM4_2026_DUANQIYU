using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Counts increments and notifies when a target is reached.</summary>
    public class Counter : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Number of increments needed to trigger the target-reached event")]
        [Min(0)]
        [SerializeField] private int targetCount = 5;

        [Tooltip("Automatically reset the count to zero when the target is reached")]
        [SerializeField] private bool autoReset;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private int count;

        public int Count => count;
        public int TargetCount => targetCount;
        public float Ratio => targetCount > 0 ? (float)count / targetCount : 0f;
        public bool IsComplete => count >= targetCount;

        //==================== OUTPUTS =====================
        public event Action<int> OnChanged;
        public event Action OnTargetReached;

        [Header("Events")]
        [Tooltip("Invoked each time the count changes, passes the new count")]
        [SerializeField] private UnityEvent<int> changedEvent;

        [Tooltip("Invoked when the count reaches the target")]
        [SerializeField] private UnityEvent targetReachedEvent;

        //==================== INPUTS =====================
        /// <summary>Add one to the count. Fires OnTargetReached when target is hit.</summary>
        [ContextMenu("Increment")]
        public void Increment()
        {
            count++;
            OnChanged?.Invoke(count);
            changedEvent?.Invoke(count);

            if (count < targetCount) return;

            OnTargetReached?.Invoke();
            targetReachedEvent?.Invoke();

            if (autoReset) count = 0;
        }

        /// <summary>Reset the count to zero.</summary>
        [ContextMenu("Reset")]
        public void Reset()
        {
            count = 0;
            OnChanged?.Invoke(count);
            changedEvent?.Invoke(count);
        }
    }
}
