using System;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

namespace Ludocore
{
    /// <summary>Animates rotation to a target euler angle using DOTween.</summary>
    public class Rotate : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Target rotation in euler angles")]
        [SerializeField] private Vector3 targetRotation;

        [Tooltip("Animation duration in seconds (0 = instant)")]
        [Min(0f)]
        [SerializeField] private float duration = 0.5f;

        [Tooltip("Tween easing")]
        [SerializeField] private Ease ease = Ease.OutQuad;

        [Tooltip("Rotate in local space")]
        [SerializeField] private bool useLocalSpace = true;

        [Tooltip("Add to current rotation instead of replacing")]
        [SerializeField] private bool additive;

        [Tooltip("Play automatically when enabled")]
        [SerializeField] private bool autoPlay;

        //==================== STATE =====================
        private Tween _tween;

        public bool IsAnimating => _tween != null && _tween.IsActive() && _tween.IsPlaying();

        //==================== OUTPUTS =====================
        public event Action OnCompleted;

        [Header("Events")]
        [Tooltip("Fired when the rotation animation completes")]
        [SerializeField] private UnityEvent completedEvent;

        //==================== LIFECYCLE =====================
        private void OnEnable()
        {
            if (autoPlay) Play();
        }

        //==================== INPUTS =====================
        /// <summary>Rotate to the configured target.</summary>
        [ContextMenu("Play")]
        public void Play()
        {
            Vector3 finalRotation = additive
                ? GetCurrentRotation() + targetRotation
                : targetRotation;

            Animate(finalRotation, duration);
        }

        /// <summary>Rotate to euler angles over the configured duration.</summary>
        public void RotateTo(Vector3 euler)
        {
            Animate(euler, duration);
        }

        /// <summary>Rotate to euler angles over a custom duration.</summary>
        public void RotateTo(Vector3 euler, float customDuration)
        {
            Animate(euler, customDuration);
        }

        /// <summary>Stop the current animation.</summary>
        public void Stop()
        {
            _tween?.Kill();
            _tween = null;
        }

        //==================== PRIVATE =====================
        private void Animate(Vector3 euler, float dur)
        {
            _tween?.Kill();

            if (dur <= 0f)
            {
                if (useLocalSpace)
                    transform.localEulerAngles = euler;
                else
                    transform.eulerAngles = euler;

                FireCompleted();
                return;
            }

            _tween = useLocalSpace
                ? transform.DOLocalRotate(euler, dur)
                : transform.DORotate(euler, dur);

            _tween.SetEase(ease).OnComplete(FireCompleted);
        }

        private Vector3 GetCurrentRotation()
        {
            return useLocalSpace ? transform.localEulerAngles : transform.eulerAngles;
        }

        private void FireCompleted()
        {
            OnCompleted?.Invoke();
            completedEvent?.Invoke();
        }

        private void OnDestroy() => _tween?.Kill();
    }
}
