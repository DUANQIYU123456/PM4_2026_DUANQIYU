using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

namespace Ludocore
{
    /// <summary>Slides the hill pattern across the terrain by feeding an animated additive
    /// noise offset into HillTerrainGenerator each throttled tick. One-shot (stops at full
    /// distance) or looping (the offset increments each loop for an endless slide).
    ///
    /// The terrain's TerrainSandbox makes every rebuild land on a runtime clone, so the
    /// on-disk asset is never written and the terrain resets to its initial state on Play-exit.</summary>
    public class TerrainDrift : MonoBehaviour
    {
        //==================== SCENE REFERENCES =====================
        [Header("Scene References")]
        [Tooltip("Generator to drive — must be on a Unity Terrain (which carries a TerrainSandbox).")]
        [SerializeField] private HillTerrainGenerator generator;

        //==================== PROFILE =====================
        [Header("Profile")]
        [Tooltip("Scriptable object defining duration, curve, drift direction, and throttle.")]
        [SerializeField] private TerrainDriftProfile profile;

        //==================== BEHAVIOUR =====================
        [Header("Behaviour")]
        [Tooltip("Play automatically when enabled.")]
        [SerializeField] private bool autoPlay;

        [Tooltip("Slide forever (the offset keeps incrementing). Off = a single drift that stops at full distance.")]
        [SerializeField] private bool loop;

        [Tooltip("Drive the tween AND its throttle on unscaled time, so a custom Time.timeScale doesn't desync the drift cadence.")]
        [SerializeField] private bool useUnscaledTime;

        //==================== EVENTS =====================
        [Header("Events")]
        [Tooltip("Fired after each heightmap rebuild. Wire to TerrainPathGenerator.ReprojectHeights to keep paths glued to the moving surface.")]
        [SerializeField] private UnityEvent onTerrainChanged;

        [Tooltip("Fired when a non-looping drift completes.")]
        [SerializeField] private UnityEvent completedEvent;

        //==================== STATE =====================
        private Tween _activeTween;
        private float _lastGenTime;
        private Vector2 _currentDelta;

        public bool IsAnimating => _activeTween != null && _activeTween.IsActive() && _activeTween.IsPlaying();

        //==================== LIFECYCLE =====================
        private void OnEnable()
        {
            if (autoPlay) Play();
        }

        private void OnDisable() => _activeTween?.Kill();

        private void OnDestroy() => _activeTween?.Kill();

        //==================== INPUTS =====================
        /// <summary>Start the drift. Re-Play continues from the current offset rather than snapping back.</summary>
        [ContextMenu("Play")]
        public void Play()
        {
            if (!generator || !profile) return;

            _activeTween?.Kill();

            // Continue from current visual state — re-Play adds another drift on top of where
            // we already are. LerpUnclamped + Incremental loops let progress run past 1 for the
            // endless-slide case, extrapolating the offset further in the same direction.
            Vector2 startDelta = _currentDelta;
            Vector2 endDelta = startDelta + profile.direction.normalized * profile.distance;
            _lastGenTime = float.NegativeInfinity;
            float progress = 0f;

            _activeTween = DOTween.To(() => progress, v =>
            {
                progress = v;
                _currentDelta = Vector2.LerpUnclamped(startDelta, endDelta, progress);

                // Throttle to avoid SetHeights spam at high res. Gate and tween share a clock.
                float now = useUnscaledTime ? Time.unscaledTime : Time.time;
                if (now - _lastGenTime >= profile.updateInterval)
                {
                    Rebuild();
                    _lastGenTime = now;
                }
            }, 1f, profile.duration)
             .SetEase(profile.curve)
             .SetUpdate(useUnscaledTime)
             .SetLoops(loop ? -1 : 1, LoopType.Incremental)
             .OnComplete(() =>
             {
                 // Always bake the exact final frame.
                 _currentDelta = Vector2.LerpUnclamped(startDelta, endDelta, progress);
                 Rebuild();
                 completedEvent?.Invoke();
             });
        }

        /// <summary>Stop the drift where it is. The terrain keeps its current (drifted) shape.</summary>
        [ContextMenu("Stop")]
        public void Stop()
        {
            _activeTween?.Kill();
            _activeTween = null;
        }

        //==================== PRIVATE =====================
        private void Rebuild()
        {
            generator.Generate(_currentDelta);
            onTerrainChanged?.Invoke();
        }
    }
}
