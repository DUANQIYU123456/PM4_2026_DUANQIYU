using System;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

namespace Ludocore
{
    /// <summary>Animates position to a destination using DOTween.</summary>
    public class Move : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Where to move to")]
        [SerializeField] private Transform destination;

        [Tooltip("Offset from destination position")]
        [SerializeField] private Vector3 offset;

        [Tooltip("Animation duration in seconds (0 = instant)")]
        [Min(0f)]
        [SerializeField] private float duration = 0.5f;

        [Tooltip("Tween easing")]
        [SerializeField] private Ease ease = Ease.OutQuad;

        [Tooltip("Animate in local space")]
        [SerializeField] private bool useLocalSpace;

        [Tooltip("Play automatically when enabled")]
        [SerializeField] private bool autoPlay;

        //==================== STATE =====================
        private Tween _tween;

        public bool IsAnimating => _tween != null && _tween.IsActive() && _tween.IsPlaying();

        //==================== OUTPUTS =====================
        public event Action OnCompleted;

        [Header("Events")]
        [Tooltip("Fired when the move animation completes")]
        [SerializeField] private UnityEvent completedEvent;

        //==================== LIFECYCLE =====================
        private void OnEnable()
        {
            if (autoPlay) Play();
        }

        //==================== INPUTS =====================
        /// <summary>Move to the configured destination.</summary>
        [ContextMenu("Play")]
        public void Play()
        {
            if (!destination) return;

            Vector3 target = destination.position + offset;
            Animate(target, duration);
        }

        /// <summary>Move to a world position over the configured duration.</summary>
        public void MoveTo(Vector3 position)
        {
            Animate(position, duration);
        }

        /// <summary>Move to a world position over a custom duration.</summary>
        public void MoveTo(Vector3 position, float customDuration)
        {
            Animate(position, customDuration);
        }

        /// <summary>Stop the current animation.</summary>
        public void Stop()
        {
            _tween?.Kill();
            _tween = null;
        }

        //==================== PRIVATE =====================
        private void Animate(Vector3 target, float dur)
        {
            _tween?.Kill();

            if (dur <= 0f)
            {
                if (useLocalSpace)
                    transform.localPosition = transform.parent
                        ? transform.parent.InverseTransformPoint(target)
                        : target;
                else
                    transform.position = target;

                FireCompleted();
                return;
            }

            _tween = useLocalSpace
                ? transform.DOLocalMove(transform.parent
                    ? transform.parent.InverseTransformPoint(target)
                    : target, dur)
                : transform.DOMove(target, dur);

            _tween.SetEase(ease).OnComplete(FireCompleted);
        }

        private void FireCompleted()
        {
            OnCompleted?.Invoke();
            completedEvent?.Invoke();
        }

        private void OnDestroy() => _tween?.Kill();

        private void OnDrawGizmosSelected()
        {
            if (!destination) return;

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(destination.position + offset, 0.1f);
            Gizmos.DrawLine(transform.position, destination.position + offset);
        }
    }
}
