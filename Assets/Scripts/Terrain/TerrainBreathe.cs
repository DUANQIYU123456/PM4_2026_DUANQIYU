using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

namespace Ludocore
{
    /// <summary>Breathes the terrain vertically: stretches TerrainData.size.y up to a peak
    /// and back to the baseline captured on the first Play. One-shot (a single inhale +
    /// exhale) or looping (continuous breathing). The TerrainCollider follows automatically,
    /// so the player rises and falls with the surface.
    ///
    /// The required TerrainSandbox makes every edit land on a runtime clone, so the on-disk
    /// asset is never written and the terrain resets to its initial state on Stop / Play-exit.</summary>
    [RequireComponent(typeof(Terrain))]
    [RequireComponent(typeof(TerrainSandbox))]
    public class TerrainBreathe : MonoBehaviour
    {
        //==================== SCENE REFERENCES =====================
        [Header("Scene References")]
        [Tooltip("Terrain to breathe. Auto-fetched from this GameObject if empty.")]
        [SerializeField] private Terrain terrain;

        //==================== PROFILE =====================
        [Header("Profile")]
        [Tooltip("Scriptable object defining duration, inhale curve, and peak height.")]
        [SerializeField] private TerrainBreatheProfile profile;

        //==================== BEHAVIOUR =====================
        [Header("Behaviour")]
        [Tooltip("Play automatically when enabled.")]
        [SerializeField] private bool autoPlay;

        [Tooltip("Breathe forever. Off = a single inhale + exhale, then stop at the baseline.")]
        [SerializeField] private bool loop;

        //==================== EVENTS =====================
        [Header("Events")]
        [Tooltip("Fired every time the height changes. Wire to TerrainPathGenerator.ReprojectHeights to keep paths glued to the moving surface.")]
        [SerializeField] private UnityEvent onTerrainChanged;

        [Tooltip("Fired when a non-looping breath completes.")]
        [SerializeField] private UnityEvent completedEvent;

        //==================== STATE =====================
        private Tween _activeTween;
        private float _baseline;
        private bool _baselineCaptured;

        public bool IsAnimating => _activeTween != null && _activeTween.IsActive() && _activeTween.IsPlaying();

        //==================== LIFECYCLE =====================
        private void Reset() => terrain = GetComponent<Terrain>();

        private void OnEnable()
        {
            if (autoPlay) Play();
        }

        private void OnDisable() => _activeTween?.Kill();

        private void OnDestroy() => _activeTween?.Kill();

        //==================== INPUTS =====================
        /// <summary>Start breathing. Restarts cleanly if already running.</summary>
        [ContextMenu("Play")]
        public void Play()
        {
            if (!terrain) terrain = GetComponent<Terrain>();
            if (!terrain || !profile) return;

            // Cache baseline once per lifetime so every breath returns to the same height.
            if (!_baselineCaptured)
            {
                _baseline = terrain.terrainData.size.y;
                _baselineCaptured = true;
            }

            _activeTween?.Kill();

            // One leg = inhale (0->1). Yoyo gives the exhale for free with no velocity kick at
            // the turn (as long as inhaleCurve eases at its ends). 2 legs = one full breath;
            // -1 = breathe forever. Half-duration per leg keeps profile.duration = one breath.
            float p = 0f;
            _activeTween = DOTween.To(() => p, v =>
            {
                p = v;
                float phase = profile.inhaleCurve.Evaluate(p);
                float scale = Mathf.LerpUnclamped(1f, profile.peakHeightMultiplier, phase);
                var size = terrain.terrainData.size;
                size.y = _baseline * scale;
                terrain.terrainData.size = size;
                onTerrainChanged?.Invoke();
            }, 1f, profile.duration * 0.5f)
             .SetLoops(loop ? -1 : 2, LoopType.Yoyo)
             .OnComplete(() =>
             {
                 SnapToBaseline();
                 completedEvent?.Invoke();
             });
        }

        /// <summary>Stop breathing and snap straight back to the baseline height.</summary>
        [ContextMenu("Stop")]
        public void Stop()
        {
            _activeTween?.Kill();
            _activeTween = null;
            SnapToBaseline();
        }

        //==================== PRIVATE =====================
        private void SnapToBaseline()
        {
            if (!_baselineCaptured || !terrain || terrain.terrainData == null) return;
            var size = terrain.terrainData.size;
            size.y = _baseline;
            terrain.terrainData.size = size;
            onTerrainChanged?.Invoke();
        }
    }
}
