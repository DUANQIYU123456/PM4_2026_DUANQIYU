using UnityEngine;
using DG.Tweening;

namespace Ludocore
{
    /// <summary>Dims ceiling emission, ambient lighting, and reflections when the player stops moving.
    /// Restores them when the player moves again.
    /// Reads all timing, curves, and target values from a swappable StillnessDimProfile asset.</summary>
    public class StillnessDimController : MonoBehaviour
    {
        //==================== SCENE REFERENCES =====================
        [Header("Scene References")]
        [Tooltip("The player's CharacterController (reads velocity)")]
        [SerializeField] private CharacterController playerController;

        [Tooltip("Shared ceiling emissive material")]
        [SerializeField] private Material ceilingMaterial;

        [Tooltip("Base emission color before intensity is applied")]
        [SerializeField, ColorUsage(false, true)]
        private Color emissionColor = Color.white;

        //==================== PROFILE =====================
        [Header("Profile")]
        [Tooltip("Scriptable object defining detection, timing, curves, and target values")]
        [SerializeField] private StillnessDimProfile profile;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private bool isStill;

        private bool _initialized;
        private float _emissionIntensity;
        private Tween _emissionTween;
        private Tween _ambientTween;
        private Tween _reflectionTween;

        //==================== LIFECYCLE =====================
        private void Awake()
        {
            if (ceilingMaterial)
                ceilingMaterial.EnableKeyword("_EMISSION");

            SetLitImmediate();
        }

        private void Update()
        {
            if (!playerController || !profile) return;

            bool still = playerController.velocity.magnitude < profile.stillnessThreshold;

            // First frame: sync state without triggering a transition
            if (!_initialized)
            {
                isStill = still;
                _initialized = true;
                return;
            }

            if (still == isStill) return;
            isStill = still;

            if (isStill)
                Dim();
            else
                Brighten();
        }

        private void OnDestroy()
        {
            KillTweens();
        }

        //==================== INPUTS =====================
        /// <summary>Force the dim transition (for testing).</summary>
        [ContextMenu("Dim")]
        public void Dim()
        {
            if (!profile) return;

            KillTweens();

            _emissionTween = DOTween.To(
                () => _emissionIntensity, ApplyEmission,
                profile.emissionDim, profile.dimDuration
            ).SetDelay(profile.dimDelay).SetEase(profile.dimCurve);

            _ambientTween = DOTween.To(
                () => RenderSettings.ambientIntensity,
                x => RenderSettings.ambientIntensity = x,
                profile.ambientDim, profile.dimDuration
            ).SetDelay(profile.dimDelay).SetEase(profile.dimCurve);

            _reflectionTween = DOTween.To(
                () => RenderSettings.reflectionIntensity,
                x => RenderSettings.reflectionIntensity = x,
                profile.reflectionDim, profile.dimDuration
            ).SetDelay(profile.dimDelay).SetEase(profile.dimCurve);
        }

        /// <summary>Force the brighten transition (for testing).</summary>
        [ContextMenu("Brighten")]
        public void Brighten()
        {
            if (!profile) return;

            KillTweens();

            _emissionTween = DOTween.To(
                () => _emissionIntensity, ApplyEmission,
                profile.emissionLit, profile.brightenDuration
            ).SetEase(profile.brightenCurve);

            _ambientTween = DOTween.To(
                () => RenderSettings.ambientIntensity,
                x => RenderSettings.ambientIntensity = x,
                profile.ambientLit, profile.brightenDuration
            ).SetEase(profile.brightenCurve);

            _reflectionTween = DOTween.To(
                () => RenderSettings.reflectionIntensity,
                x => RenderSettings.reflectionIntensity = x,
                profile.reflectionLit, profile.brightenDuration
            ).SetEase(profile.brightenCurve);
        }

        //==================== PRIVATE =====================
        private void SetLitImmediate()
        {
            if (!profile) return;

            ApplyEmission(profile.emissionLit);
            RenderSettings.ambientIntensity = profile.ambientLit;
            RenderSettings.reflectionIntensity = profile.reflectionLit;
        }

        private void ApplyEmission(float intensity)
        {
            _emissionIntensity = intensity;
            if (!ceilingMaterial) return;
            ceilingMaterial.SetColor("_EmissionColor", emissionColor * Mathf.Pow(2f, intensity));
        }

        private void KillTweens()
        {
            _emissionTween?.Kill();
            _ambientTween?.Kill();
            _reflectionTween?.Kill();
        }
    }
}
