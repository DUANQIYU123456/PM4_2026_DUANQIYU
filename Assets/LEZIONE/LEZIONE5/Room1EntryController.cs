using UnityEngine;
using DG.Tweening;

namespace Ludocore
{
    /// <summary>Plays layered feedback when the player enters Room 1.</summary>
    public class Room1EntryController : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Source")]
        [Tooltip("Sensor that detects the player")]
        [SerializeField] private Sensor sensor;

        [Header("Audio")]
        [Tooltip("Audio source whose pitch is animated")]
        [SerializeField] private AudioSource audioSource;

        [Min(0f)]
        [SerializeField] private float pitchEntryDelay;

        [Min(0f)]
        [SerializeField] private float pitchEntryDuration = 1f;

        [SerializeField] private AnimationCurve pitchEntryCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Environment Lighting")]
        [Min(0f)]
        [SerializeField] private float ambientIntensityDelay;

        [Min(0f)]
        [SerializeField] private float ambientIntensityDuration = 1f;

        [SerializeField] private AnimationCurve ambientIntensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Environment Reflections")]
        [Min(0f)]
        [SerializeField] private float reflectionIntensityDelay;

        [Min(0f)]
        [SerializeField] private float reflectionIntensityDuration = 1f;

        [SerializeField] private AnimationCurve reflectionIntensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Ceiling Emission")]
        [Tooltip("Material with emission to animate")]
        [SerializeField] private Material ceilingEmissiveMaterial;

        [Tooltip("Base emission color before intensity is applied")]
        [SerializeField] private Color emissionColor = Color.white;

        [Min(0f)]
        [SerializeField] private float emissionIntensityDelay;

        [Min(0f)]
        [SerializeField] private float emissionIntensityDuration = 1f;

        [SerializeField] private AnimationCurve emissionIntensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        //==================== STATE =====================
        private Tween _pitchTween;
        private Tween _ambientTween;
        private Tween _reflectionTween;
        private Tween _emissionTween;
        private float _emissionIntensity;

        //==================== LIFECYCLE =====================
        private void OnEnable()
        {
            if (sensor) sensor.OnSignalAdded += HandleDetected;
        }

        private void OnDisable()
        {
            if (sensor) sensor.OnSignalAdded -= HandleDetected;
        }

        private void OnDestroy()
        {
            _pitchTween?.Kill();
            _ambientTween?.Kill();
            _reflectionTween?.Kill();
            _emissionTween?.Kill();
        }

        //==================== INPUTS =====================
        /// <summary>Play the entry interaction.</summary>
        [ContextMenu("On Play")]
        public void OnPlay()
        {
            // Audio pitch: 0 → 1
            _pitchTween?.Kill();
            audioSource.pitch = 0f;
            _pitchTween = DOTween.To(
                () => audioSource.pitch,
                x => audioSource.pitch = x,
                1f,
                pitchEntryDuration
            ).SetDelay(pitchEntryDelay).SetEase(pitchEntryCurve);

            // Environment Lighting intensity: 0 → 1
            _ambientTween?.Kill();
            RenderSettings.ambientIntensity = 0f;
            _ambientTween = DOTween.To(
                () => RenderSettings.ambientIntensity,
                x => RenderSettings.ambientIntensity = x,
                1f,
                ambientIntensityDuration
            ).SetDelay(ambientIntensityDelay).SetEase(ambientIntensityCurve);

            // Environment Reflections intensity: 0 → 1
            _reflectionTween?.Kill();
            RenderSettings.reflectionIntensity = 0f;
            _reflectionTween = DOTween.To(
                () => RenderSettings.reflectionIntensity,
                x => RenderSettings.reflectionIntensity = x,
                1f,
                reflectionIntensityDuration
            ).SetDelay(reflectionIntensityDelay).SetEase(reflectionIntensityCurve);

            // Ceiling emission intensity: -10 → 5
            _emissionTween?.Kill();
            if (ceilingEmissiveMaterial)
            {
                ceilingEmissiveMaterial.EnableKeyword("_EMISSION");
                _emissionIntensity = -10f;
                ApplyEmission(-10f);
                _emissionTween = DOTween.To(
                    () => _emissionIntensity,
                    ApplyEmission,
                    5f,
                    emissionIntensityDuration
                ).SetDelay(emissionIntensityDelay).SetEase(emissionIntensityCurve);
            }
        }

        //==================== PRIVATE =====================
        private void HandleDetected(Signal signal)
        {
            OnPlay();
        }

        private void ApplyEmission(float intensity)
        {
            _emissionIntensity = intensity;
            if (!ceilingEmissiveMaterial) return;
            ceilingEmissiveMaterial.SetColor("_EmissionColor", emissionColor * Mathf.Pow(2f, intensity));
        }
    }
}
