using UnityEngine;

namespace Ludocore
{
    /// <summary>Maps proximity sensor distance to emission intensity.
    /// Reads all tuning values from a swappable ColumnProfile asset.</summary>
    public class ColumnControllerV2 : MonoBehaviour
    {
        //==================== SCENE REFERENCES =====================
        [Header("Scene References")]
        [Tooltip("Proximity sensor that detects the player")]
        [SerializeField] private ProximitySensor sensor;

        [Tooltip("Renderer whose material emission to drive")]
        [SerializeField] private Renderer targetRenderer;

        [Tooltip("Material index on the renderer")]
        [Min(0)]
        [SerializeField] private int materialIndex;

        //==================== PROFILE =====================
        [Header("Profile")]
        [Tooltip("Scriptable object with emission and smoothing settings")]
        [SerializeField] private ColumnProfile profile;

        //==================== STATE =====================
        private Material _material;
        private float _emissionValue;

        //==================== LIFECYCLE =====================
        private void Start()
        {
            if (!targetRenderer) return;

            Material[] materials = targetRenderer.materials;
            if (materialIndex < 0 || materialIndex >= materials.Length) return;

            _material = materials[materialIndex];
            _material.EnableKeyword("_EMISSION");

            if (profile)
                _emissionValue = profile.minIntensity;
        }

        private void Update()
        {
            if (!sensor || !_material || !profile) return;

            // --- Evaluate raw target from proximity ---
            float emissionTarget = profile.minIntensity;

            if (sensor.TryGetNearest(out Signal nearest))
            {
                float proximity = 1f - Mathf.Clamp01(nearest.Distance / profile.maxDistance);
                float curved = profile.emissionCurve.Evaluate(proximity);
                emissionTarget = Mathf.Lerp(profile.minIntensity, profile.maxIntensity, curved);
            }

            // --- Smooth ---
            _emissionValue = Smooth(_emissionValue, emissionTarget,
                                    profile.emissionAttackSpeed, profile.emissionReleaseSpeed);

            // --- Apply ---
            _material.SetColor("_EmissionColor", profile.emissionColor * _emissionValue);
        }

        private void OnDestroy()
        {
            if (_material && Application.isPlaying)
                Destroy(_material);
        }

        //==================== PRIVATE =====================
        private static float Smooth(float current, float target, float attackSpeed, float releaseSpeed)
        {
            float speed = (target >= current) ? attackSpeed : releaseSpeed;
            if (speed <= 0f) return target;
            return Mathf.MoveTowards(current, target, speed * Time.deltaTime);
        }
    }
}
