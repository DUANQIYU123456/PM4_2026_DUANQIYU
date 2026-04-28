using UnityEngine;

namespace Ludocore
{
    /// <summary>Maps proximity sensor distance to emission intensity — closer means brighter.</summary>
    public class ColumnController : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Source")]
        [Tooltip("Proximity sensor that detects the player")]
        [SerializeField] private ProximitySensor sensor;

        [Header("Renderer")]
        [Tooltip("Renderer whose material emission to drive")]
        [SerializeField] private Renderer targetRenderer;
        [Tooltip("Material index on the renderer")]
        [Min(0)]
        [SerializeField] private int materialIndex;

        [Header("Emission")]
        [Tooltip("Base emission color")]
        [SerializeField, ColorUsage(false, true)] private Color emissionColor = Color.white;
        [Tooltip("Emission intensity when the player is right on top")]
        [Min(0f)]
        [SerializeField] private float maxIntensity = 2f;
        [Tooltip("Emission intensity when nothing is detected or at max distance")]
        [Min(0f)]
        [SerializeField] private float minIntensity;

        [Header("Scale")]
        [Tooltip("Scale when nothing is detected or at max distance")]
        [Min(0f)]
        [SerializeField] private float minScale = 1f;
        [Tooltip("Scale when the player is right on top")]
        [Min(0f)]
        [SerializeField] private float maxScale = 2f;

        [Header("Mapping")]
        [Tooltip("Maximum detection distance (intensity is zero beyond this)")]
        [Min(0.01f)]
        [SerializeField] private float maxDistance = 5f;
        [Tooltip("Remapping curve (left = far, right = close)")]
        [SerializeField] private AnimationCurve responseCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        //==================== STATE =====================
        private Material _material;

        //==================== LIFECYCLE =====================
        private void Start()
        {
            if (!targetRenderer) return;

            Material[] materials = targetRenderer.materials;
            if (materialIndex < 0 || materialIndex >= materials.Length) return;

            _material = materials[materialIndex];
            _material.EnableKeyword("_EMISSION");
        }

        private void Update()
        {
            if (!sensor || !_material) return;

            float intensity = minIntensity;
            float scale = minScale;

            if (sensor.TryGetNearest(out Signal nearest))
            {
                float proximity = 1f - Mathf.Clamp01(nearest.Distance / maxDistance);
                float curved = responseCurve.Evaluate(proximity);
                intensity = Mathf.Lerp(minIntensity, maxIntensity, curved);
                scale = Mathf.Lerp(minScale, maxScale, curved);
            }

            _material.SetColor("_EmissionColor", emissionColor * intensity);
            transform.localScale = Vector3.one * scale;
        }

        private void OnDestroy()
        {
            if (_material && Application.isPlaying)
                Destroy(_material);
        }
    }
}
