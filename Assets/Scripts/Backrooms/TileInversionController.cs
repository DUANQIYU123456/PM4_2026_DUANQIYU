using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

namespace Ludocore
{
    /// <summary>Rotates a ceiling/floor tile on a random interval or via external trigger.
    /// Reads all timing and rotation values from a swappable TileInversionProfile asset.</summary>
    public class TileInversionController : MonoBehaviour
    {
        //==================== SCENE REFERENCES =====================
        [Header("Scene References")]
        [Tooltip("The ceiling/floor tile to rotate")]
        [SerializeField] private Transform tile;

        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Local-space rotation axis (e.g. 1,0,0 for a forward flip)")]
        [SerializeField] private Vector3 axis = Vector3.right;

        //==================== PROFILE =====================
        [Header("Profile")]
        [Tooltip("Scriptable object with timer and rotation settings")]
        [SerializeField] private TileInversionProfile profile;

        //==================== EVENTS =====================
        [Header("Events")]
        [SerializeField] private UnityEvent onInvertStart;
        [SerializeField] private UnityEvent onInvertComplete;

        //==================== STATE =====================
        private Coroutine _timerRoutine;
        private Tween _rotationTween;
        private bool _isInverting;

        /// <summary>Duration of a single flip (0 if instant or not configured).</summary>
        public float InvertDuration =>
            (profile && profile.animate) ? Mathf.Max(0f, profile.duration) : 0f;

        //==================== LIFECYCLE =====================
        private void OnEnable()
        {
            if (profile && profile.useTimer)
                _timerRoutine = StartCoroutine(TimerLoop());
        }

        private void OnDisable()
        {
            StopTimer();
            _rotationTween?.Kill();
            _rotationTween = null;
            _isInverting = false;
        }

        private void OnDestroy()
        {
            _rotationTween?.Kill();
        }

        //==================== INPUTS =====================
        /// <summary>Rotate the tile by profile.rotationAmount around axis.
        /// Ignored if a rotation is already in flight.</summary>
        [ContextMenu("Invert")]
        public void Invert()
        {
            if (!tile || !profile || _isInverting) return;

            _isInverting = true;
            onInvertStart?.Invoke();

            Vector3 localAxis = axis.sqrMagnitude > 0f ? axis.normalized : Vector3.right;
            Vector3 delta = localAxis * profile.rotationAmount;

            if (!profile.animate || profile.duration <= 0f)
            {
                tile.localRotation = tile.localRotation * Quaternion.Euler(delta);
                Finish();
                return;
            }

            _rotationTween?.Kill();
            _rotationTween = tile.DOLocalRotate(delta, profile.duration, RotateMode.LocalAxisAdd)
                .SetEase(profile.ease)
                .OnComplete(Finish);
        }

        //==================== PRIVATE =====================
        private void Finish()
        {
            _isInverting = false;
            onInvertComplete?.Invoke();
            RestartTimer();
        }

        private IEnumerator TimerLoop()
        {
            while (true)
            {
                float min = Mathf.Min(profile.minInterval, profile.maxInterval);
                float max = Mathf.Max(profile.minInterval, profile.maxInterval);
                yield return new WaitForSeconds(Random.Range(min, max));
                Invert();
            }
        }

        private void StopTimer()
        {
            if (_timerRoutine != null)
            {
                StopCoroutine(_timerRoutine);
                _timerRoutine = null;
            }
        }

        private void RestartTimer()
        {
            if (!isActiveAndEnabled || profile == null || !profile.useTimer) return;
            StopTimer();
            _timerRoutine = StartCoroutine(TimerLoop());
        }
    }
}
