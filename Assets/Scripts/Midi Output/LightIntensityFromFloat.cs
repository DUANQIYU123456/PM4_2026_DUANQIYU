// ============================================
// Light Intensity From Float
// ============================================
// PURPOSE: Drives a Light component's intensity from a FloatVariable (typically
//          a 0..1 source like a MIDI knob, but any range works). Maps via
//          AnimationCurve into [minIntensity, maxIntensity] and smooths toward
//          the target so quantized inputs feel continuous.
// USAGE: Assign 'Source' to a FloatVariable an upstream component writes to
//        (e.g. MidiCcEventRaiser). Assign 'Target Light' or leave empty to use
//        the Light on this GameObject. Tune min/max and curve to taste.
// ============================================

using UnityEngine;

namespace Ludocore
{
    public class LightIntensityFromFloat : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Source")]
        [Tooltip("FloatVariable to read from. Typically 0..1 (e.g. MIDI CC value).")]
        [SerializeField] private FloatVariable source;

        [Header("Target")]
        [Tooltip("Light to drive. If empty, uses the Light on this GameObject.")]
        [SerializeField] private Light targetLight;

        [Header("Mapping")]
        [Tooltip("Intensity when the curve evaluates to 0.")]
        [Min(0f)]
        [SerializeField] private float minIntensity = 0f;

        [Tooltip("Intensity when the curve evaluates to 1.")]
        [Min(0f)]
        [SerializeField] private float maxIntensity = 5f;

        [Tooltip("Maps the source value (assumed 0..1) to a 0..1 lerp factor between min and max.")]
        [SerializeField] private AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Feel")]
        [Tooltip("Smoothing speed in intensity units per second. Higher = snappier (use ~50 for near-instant).")]
        [Min(0.1f)]
        [SerializeField] private float smoothSpeed = 20f;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private float currentIntensity;
        [ReadOnly, SerializeField] private float targetIntensity;

        //==================== LIFECYCLE =====================
        private void OnEnable()
        {
            if (!targetLight) targetLight = GetComponent<Light>();
            if (!source)
            {
                Debug.LogWarning($"[LightIntensityFromFloat:{name}] No source FloatVariable assigned.", this);
                return;
            }

            source.OnChanged += HandleSourceChanged;
            ApplyImmediate(source.Value);
        }

        private void OnDisable()
        {
            if (source) source.OnChanged -= HandleSourceChanged;
        }

        private void Update()
        {
            if (!targetLight) return;
            if (Mathf.Approximately(currentIntensity, targetIntensity)) return;

            currentIntensity = Mathf.MoveTowards(currentIntensity, targetIntensity, smoothSpeed * Time.deltaTime);
            targetLight.intensity = currentIntensity;
        }

        //==================== PRIVATE =====================
        private void HandleSourceChanged(float value) => targetIntensity = Evaluate(value);

        private void ApplyImmediate(float value)
        {
            targetIntensity = Evaluate(value);
            currentIntensity = targetIntensity;
            if (targetLight) targetLight.intensity = currentIntensity;
        }

        private float Evaluate(float value)
        {
            float t = curve.Evaluate(Mathf.Clamp01(value));
            return Mathf.LerpUnclamped(minIntensity, maxIntensity, t);
        }
    }
}
