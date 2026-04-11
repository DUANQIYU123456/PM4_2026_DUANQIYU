using DG.Tweening;
using UnityEngine;

namespace Ludocore
{
    /// <summary>Visual feedback for energy — emission, light, pulse on gain, and rotation based on energy.</summary>
    public class LifecycleView : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("References")]
        [Tooltip("Lifecycle component to read energy from.")]
        [SerializeField] private Lifecycle lifecycle;
        [Tooltip("Renderer whose emission color is driven by energy.")]
        [SerializeField] private Renderer targetRenderer;
        [Tooltip("Optional point light whose range scales with energy.")]
        [SerializeField] private Light pointLight;

        [Header("Emission")]
        [Tooltip("HDR color used for emission at full energy.")]
        [SerializeField, ColorUsage(false, true)] private Color emissionColor = Color.white;
        [Tooltip("Emission intensity multiplier at full energy.")]
        [Min(0f)]
        [SerializeField] private float maxEmissionIntensity = 5f;

        [Header("Light")]
        [Tooltip("Point light range at full energy.")]
        [Min(0f)]
        [SerializeField] private float maxLightRange = 10f;

        [Header("Pulse")]
        [Tooltip("Scale multiplier applied during a pulse on energy gain.")]
        [Min(0f)]
        [SerializeField] private float pulseScale = 1.2f;
        [Tooltip("Duration of the pulse animation in seconds.")]
        [Min(0f)]
        [SerializeField] private float pulseDuration = 0.2f;

        [Header("Rotation")]
        [Tooltip("Enable rotation based on energy level.")]
        [SerializeField] private bool enableRotation = true;
        [Tooltip("Maximum rotation speed (degrees per second) at full energy.")]
        [Min(0f)]
        [SerializeField] private float maxRotationSpeed = 180f;
        [Tooltip("Rotation axis (typically Y for vertical spin).")]
        [SerializeField] private Vector3 rotationAxis = Vector3.up;

        //==================== STATE =====================
        private Material _material;
        private Vector3 _baseScale;
        private float _previousEnergy;

        //==================== LIFECYCLE =====================
        private void Awake()
        {
            _material = targetRenderer.material;
            _baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            _previousEnergy = lifecycle.CurrentEnergy;
        }

        private void Update()
        {
            float ratio = lifecycle.EnergyRatio;

            _material.SetColor("_EmissionColor", emissionColor * (ratio * maxEmissionIntensity));

            if (pointLight) pointLight.range = ratio * maxLightRange;

            if (enableRotation)
            {
                ApplyRotation(ratio);
            }

            float energy = lifecycle.CurrentEnergy;
            if (energy > _previousEnergy) Pulse();
            _previousEnergy = energy;
        }

        //==================== PRIVATE =====================
        private void ApplyRotation(float energyRatio)
        {
            float currentRotationSpeed = energyRatio * maxRotationSpeed;
            Vector3 rotation = rotationAxis.normalized * currentRotationSpeed;
            transform.Rotate(rotation * Time.deltaTime);
        }

        private void Pulse()
        {
            transform.DOKill();
            transform.localScale = _baseScale;
            transform.DOPunchScale(_baseScale * (pulseScale - 1f), pulseDuration, 1, 0f);
        }

        private void OnDestroy()
        {
            transform.DOKill();
            if (_material) Destroy(_material);
        }
    }
}
