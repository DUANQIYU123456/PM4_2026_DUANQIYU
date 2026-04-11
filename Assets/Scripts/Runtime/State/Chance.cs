using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Rolls against a probability and reports success or failure.</summary>
    public class Chance : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Probability of success (0 = never, 1 = always)")]
        [Range(0f, 1f)]
        [SerializeField] private float probability = 0.5f;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private bool lastResult;

        public bool LastResult => lastResult;
        public float Probability => probability;

        //==================== OUTPUTS =====================
        public event Action OnSuccess;
        public event Action OnFail;

        [Header("Events")]
        [Tooltip("Invoked when the roll succeeds")]
        [SerializeField] private UnityEvent successEvent;

        [Tooltip("Invoked when the roll fails")]
        [SerializeField] private UnityEvent failEvent;

        //==================== INPUTS =====================
        /// <summary>Roll against probability. Fires OnSuccess or OnFail.</summary>
        [ContextMenu("Roll")]
        public void Roll()
        {
            lastResult = UnityEngine.Random.value <= probability;

            if (lastResult)
            {
                OnSuccess?.Invoke();
                successEvent?.Invoke();
            }
            else
            {
                OnFail?.Invoke();
                failEvent?.Invoke();
            }
        }

        /// <summary>Set the probability at runtime (0–1).</summary>
        public void SetProbability(float value)
        {
            probability = Mathf.Clamp01(value);
        }
    }
}
