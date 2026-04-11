using System;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

namespace Ludocore
{
    /// <summary>Animates scale to a target size using DOTween.</summary>
    public class Scale : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Target scale")]
        [SerializeField] private Vector3 targetScale = Vector3.one;

        [Tooltip("Animation duration in seconds (0 = instant)")]
        [Min(0f)]
        [SerializeField] private float duration = 0.5f;

        [Tooltip("Tween easing")]
        [SerializeField] private Ease ease = Ease.OutQuad;

        [Tooltip("Play automatically when enabled")]
        [SerializeField] private bool autoPlay;

        //==================== STATE =====================
        private Tween _tween;

        public bool IsAnimating => _tween != null && _tween.IsActive() && _tween.IsPlaying();

        //==================== OUTPUTS =====================
        public event Action OnCompleted;

        [Header("Events")]
        [Tooltip("Fired when the scale animation completes")]
        [SerializeField] private UnityEvent completedEvent;

        //==================== LIFECYCLE =====================
        private void OnEnable()
        {
            if (autoPlay) Play();
        }

        //==================== INPUTS =====================
        /// <summary>Scale to the configured target.</summary>
        [ContextMenu("Play")]
        public void Play()
        {
            Animate(targetScale, duration);
        }

        /// <summary>Scale uniformly over the configured duration.</summary>
        public void ScaleTo(float uniform)
        {
            Animate(Vector3.one * uniform, duration);
        }

        /// <summary>Scale to a specific size over the configured duration.</summary>
        public void ScaleTo(Vector3 scale)
        {
            Animate(scale, duration);
        }

        /// <summary>Scale uniformly over a custom duration.</summary>
        public void ScaleTo(float uniform, float customDuration)
        {
            Animate(Vector3.one * uniform, customDuration);
        }

        /// <summary>Stop the current animation.</summary>
        public void Stop()
        {
            _tween?.Kill();
            _tween = null;
        }

        //==================== PRIVATE =====================
        private void Animate(Vector3 scale, float dur)
        {
            _tween?.Kill();

            if (dur <= 0f)
            {
                transform.localScale = scale;
                FireCompleted();
                return;
            }

            _tween = transform.DOScale(scale, dur);
            _tween.SetEase(ease).OnComplete(FireCompleted);
        }

        private void FireCompleted()
        {
            OnCompleted?.Invoke();
            completedEvent?.Invoke();
        }

        private void OnDestroy() => _tween?.Kill();
    }
}
