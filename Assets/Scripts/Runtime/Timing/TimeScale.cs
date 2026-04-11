using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Controls the game's time scale for slow motion and pause.</summary>
    public class TimeScale : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Time scale to apply when SlowDown is called")]
        [Range(0f, 2f)]
        [SerializeField] private float targetScale = 0.2f;

        [Tooltip("Transition duration in seconds (0 = instant)")]
        [Min(0f)]
        [SerializeField] private float transitionDuration;

        [Header("Auto Reset")]
        [Tooltip("Automatically reset to normal speed after slowing down")]
        [SerializeField] private bool autoReset;
        [Tooltip("Seconds to wait (in real time) before resetting")]
        [Min(0f)]
        [SerializeField] private float autoResetDelay = 1f;

        //==================== STATE =====================
        private float _originalFixedDelta;
        private Tween _tween;

        public float CurrentScale => Time.timeScale;
        public bool IsSlowed => Time.timeScale < 1f;
        public bool IsPaused => Time.timeScale == 0f;

        //==================== OUTPUTS =====================
        public event Action<float> OnScaleChanged;

        [Header("Events")]
        [Tooltip("Fired when the time scale changes, passes the new scale")]
        [SerializeField] private UnityEvent<float> scaleChangedEvent;

        //==================== LIFECYCLE =====================
        private void Awake()
        {
            _originalFixedDelta = Time.fixedDeltaTime;
        }

        //==================== INPUTS =====================
        /// <summary>Transition to the configured target time scale.</summary>
        [ContextMenu("Slow Down")]
        public void SlowDown() => TransitionTo(targetScale);

        /// <summary>Transition to a specific time scale.</summary>
        public void SlowDown(float scale) => TransitionTo(scale);

        /// <summary>Return to normal speed.</summary>
        [ContextMenu("Reset")]
        public void ResetTime() => TransitionTo(1f);

        /// <summary>Freeze time immediately.</summary>
        [ContextMenu("Pause")]
        public void Pause() => Apply(0f);

        /// <summary>Restore normal speed immediately.</summary>
        [ContextMenu("Unpause")]
        public void Unpause() => Apply(1f);

        //==================== PRIVATE =====================
        private void TransitionTo(float scale)
        {
            KillTween();

            if (transitionDuration > 0f)
            {
                _tween = DOTween.To(() => Time.timeScale, Apply, scale, transitionDuration)
                    .SetUpdate(true);
            }
            else
            {
                Apply(scale);
            }

            if (autoReset && scale < 1f)
            {
                CancelInvoke(nameof(ResetTime));
                Invoke(nameof(ResetTime), autoResetDelay);
            }
        }

        private void Apply(float scale)
        {
            Time.timeScale = scale;
            Time.fixedDeltaTime = _originalFixedDelta * Mathf.Max(scale, 0.0001f);
            OnScaleChanged?.Invoke(scale);
            scaleChangedEvent?.Invoke(scale);
        }

        private void KillTween()
        {
            if (_tween is { active: true }) _tween.Kill();
        }

        private void OnDestroy()
        {
            KillTween();
            // Restore normal time if this object is destroyed while slowed
            Time.timeScale = 1f;
            Time.fixedDeltaTime = _originalFixedDelta;
        }
    }
}
