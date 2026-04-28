using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DG.Tweening;

namespace Ludocore
{
    /// <summary>Desaturates the world and tightens the vignette while the player stares at a target object.
    /// Recovers when the player looks away.
    /// Reads all timing, curves, and target values from a swappable GazeDrainProfile asset.</summary>
    public class GazeDrainController : MonoBehaviour
    {
        //==================== SCENE REFERENCES =====================
        [Header("Scene References")]
        [Tooltip("Sensor that detects the player's gaze (GazeSensor on the target object)")]
        [SerializeField] private Sensor sensor;

        [Tooltip("URP Volume with Color Adjustments and Vignette overrides")]
        [SerializeField] private Volume postProcessVolume;

        //==================== PROFILE =====================
        [Header("Profile")]
        [Tooltip("Scriptable object defining timing, curves, and target values")]
        [SerializeField] private GazeDrainProfile profile;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private bool isGazing;
        [ReadOnly, SerializeField] private float drainProgress;

        private bool _initialized;
        private Tween _drainTween;
        private ColorAdjustments _colorAdjustments;
        private Vignette _vignette;

        //==================== LIFECYCLE =====================
        private void Awake()
        {
            if (postProcessVolume && postProcessVolume.profile)
            {
                postProcessVolume.profile.TryGet(out _colorAdjustments);
                postProcessVolume.profile.TryGet(out _vignette);
            }

            ApplyDrain(0f);
        }

        private void Update()
        {
            if (!sensor || !profile) return;

            bool gazing = sensor.HasDetections;

            // First frame: sync state without triggering a transition
            if (!_initialized)
            {
                isGazing = gazing;
                _initialized = true;
                return;
            }

            if (gazing == isGazing) return;
            isGazing = gazing;

            if (isGazing)
                Drain();
            else
                Release();
        }

        private void OnDestroy()
        {
            _drainTween?.Kill();
        }

        //==================== INPUTS =====================
        /// <summary>Force the drain transition (for testing).</summary>
        [ContextMenu("Drain")]
        public void Drain()
        {
            if (!profile) return;

            _drainTween?.Kill();
            _drainTween = DOTween.To(
                () => drainProgress, ApplyDrain,
                1f, profile.onsetDuration
            ).SetDelay(profile.onsetDelay).SetEase(profile.onsetCurve);
        }

        /// <summary>Force the release transition (for testing).</summary>
        [ContextMenu("Release")]
        public void Release()
        {
            if (!profile) return;

            _drainTween?.Kill();
            _drainTween = DOTween.To(
                () => drainProgress, ApplyDrain,
                0f, profile.releaseDuration
            ).SetEase(profile.releaseCurve);
        }

        //==================== PRIVATE =====================
        private void ApplyDrain(float progress)
        {
            drainProgress = progress;

            if (_colorAdjustments != null)
            {
                _colorAdjustments.saturation.Override(
                    Mathf.Lerp(profile.saturationNormal, profile.saturationGazed, progress));
            }

            if (_vignette != null)
            {
                _vignette.intensity.Override(
                    Mathf.Lerp(profile.vignetteNormal, profile.vignetteGazed, progress));
            }
        }
    }
}
