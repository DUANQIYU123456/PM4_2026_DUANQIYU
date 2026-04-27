using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

namespace Ludocore
{
    /// <summary>Plays layered feedback when the player enters Room 1.
    /// Reads all timing and curve values from a swappable Room1EntryProfile asset.</summary>
    public class Room1EntryControllerV2 : MonoBehaviour
    {
        //==================== SCENE REFERENCES =====================
        [Header("Scene References")]
        [Tooltip("Sensor that detects the player")]
        [SerializeField] private Sensor sensor;

        [Tooltip("Audio source whose pitch is animated")]
        [SerializeField] private AudioSource audioSource;

        [Tooltip("Material with emission to animate")]
        [SerializeField] private Material ceilingEmissiveMaterial;

        //==================== PROFILE =====================
        [Header("Profile")]
        [Tooltip("Scriptable object with timing, curve, and color settings for all four response interactions")]
        [SerializeField] private Room1EntryProfile profile;

        //==================== EVENTS =====================
        [Header("Events")]
        [Tooltip("Fired when all four response interactions have finished playing")]
        [SerializeField] private UnityEvent onComplete;

        //==================== STATE =====================
        private Tween _pitchTween;
        private Tween _ambientTween;
        private Tween _reflectionTween;
        private Tween _emissionTween;
        private float _emissionIntensity;
        private int _activeTweens;

        //==================== LIFECYCLE =====================
        private void OnEnable()
        {
            if (sensor) sensor.OnSignalAdded += HandleDetected;
            InitializeValues();
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
            if (!profile) return;

            _activeTweens = 0;

            // --- Pitch: 0 → 1 ---
            _pitchTween?.Kill();
            if (audioSource)
            {
                _activeTweens++;
                audioSource.pitch = 0f;
                _pitchTween = DOTween.To(
                    () => audioSource.pitch,
                    x => audioSource.pitch = x,
                    1f,
                    profile.pitchDuration
                ).SetDelay(profile.pitchDelay).SetEase(profile.pitchCurve)
                 .OnComplete(HandleTweenComplete);
            }

            // --- Ambient Lighting: 0 → 1 ---
            _ambientTween?.Kill();
            _activeTweens++;
            RenderSettings.ambientIntensity = 0f;
            _ambientTween = DOTween.To(
                () => RenderSettings.ambientIntensity,
                x => RenderSettings.ambientIntensity = x,
                1f,
                profile.ambientDuration
            ).SetDelay(profile.ambientDelay).SetEase(profile.ambientCurve)
             .OnComplete(HandleTweenComplete);

            // --- Reflections: 0 → 1 ---
            _reflectionTween?.Kill();
            _activeTweens++;
            RenderSettings.reflectionIntensity = 0f;
            _reflectionTween = DOTween.To(
                () => RenderSettings.reflectionIntensity,
                x => RenderSettings.reflectionIntensity = x,
                1f,
                profile.reflectionDuration
            ).SetDelay(profile.reflectionDelay).SetEase(profile.reflectionCurve)
             .OnComplete(HandleTweenComplete);

            // --- Ceiling Emission: -10 → 5 ---
            _emissionTween?.Kill();
            if (ceilingEmissiveMaterial)
            {
                _activeTweens++;
                ceilingEmissiveMaterial.EnableKeyword("_EMISSION");
                _emissionIntensity = -10f;
                ApplyEmission(-10f);
                _emissionTween = DOTween.To(
                    () => _emissionIntensity,
                    ApplyEmission,
                    5f,
                    profile.emissionDuration
                ).SetDelay(profile.emissionDelay).SetEase(profile.emissionCurve)
                 .OnComplete(HandleTweenComplete);
            }
        }

        //==================== PRIVATE =====================
        private void InitializeValues()
        {
            if (audioSource) audioSource.pitch = 0f;
            RenderSettings.ambientIntensity = 0f;
            RenderSettings.reflectionIntensity = 0f;

            if (ceilingEmissiveMaterial)
            {
                ceilingEmissiveMaterial.EnableKeyword("_EMISSION");
                _emissionIntensity = -10f;
                ApplyEmission(-10f);
            }
        }

        private void HandleTweenComplete()
        {
            _activeTweens--;
            if (_activeTweens <= 0)
                onComplete?.Invoke();
        }

        private void HandleDetected(Signal signal)
        {
            OnPlay();
        }

        private void ApplyEmission(float intensity)
        {
            _emissionIntensity = intensity;
            if (!ceilingEmissiveMaterial || !profile) return;
            ceilingEmissiveMaterial.SetColor("_EmissionColor", profile.emissionColor * Mathf.Pow(2f, intensity));
        }
    }
}
