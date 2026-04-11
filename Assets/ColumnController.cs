using UnityEngine;
using Ludocore;

/// <summary>
/// Proximity feedback component that controls:
/// 1. Emission intensity of target renderer material (URP, HDR)
/// 2. Pitch of assigned Audio Source
/// 3. Intensity of Point Light
/// All properties follow the proximity sensor input.
/// </summary>
public class ColumnController : MonoBehaviour
{
    //==================== CONFIG =====================
    [Header("Proximity Sensor")]
    [Tooltip("Proximity Sensor to read detected objects from")]
    [SerializeField] private ProximitySensor proximitySensor;

    [Header("Emission Control")]
    [Tooltip("Renderer whose emission intensity to control")]
    [SerializeField] private Renderer targetRenderer;
    [Tooltip("Shader property name for emission color")]
    [SerializeField] private string emissionPropertyName = "_EmissionColor";
    [SerializeField, ColorUsage(false, true)] private Color baseEmissionColor = Color.white;
    [Min(0f)]
    [SerializeField] private float maxEmissionIntensity = 5f;

    [Header("Audio Control")]
    [Tooltip("Audio Source to control pitch")]
    [SerializeField] private AudioSource audioSource;
    [Min(0.1f)]
    [SerializeField] private float minPitch = 0.5f;
    [Min(0.1f)]
    [SerializeField] private float maxPitch = 2f;

    [Header("Light Control")]
    [Tooltip("Point Light to control intensity")]
    [SerializeField] private Light pointLight;
    [Min(0f)]
    [SerializeField] private float maxLightIntensity = 2f;

    [Header("Distance Range")]
    [Min(0.1f)]
    [SerializeField] private float minDistance = 0.5f;
    [Min(0.1f)]
    [SerializeField] private float maxDistance = 10f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    //==================== STATE =====================
    private Material _material;
    private float _currentIntensity;

    public float CurrentIntensity => _currentIntensity;

    //==================== LIFECYCLE =====================
    private void Awake()
    {
        InitializeMaterial();
    }

    private void Update()
    {
        if (!proximitySensor) return;

        UpdateProximityFeedback();
    }

    //==================== PRIVATE =====================
    private void InitializeMaterial()
    {
        if (!targetRenderer)
        {
            Debug.LogWarning($"[ColumnController] No target renderer assigned on {gameObject.name}", gameObject);
            return;
        }

        _material = targetRenderer.material;
        if (!_material || !_material.HasProperty(emissionPropertyName))
        {
            Debug.LogWarning($"[ColumnController] Material does not have property '{emissionPropertyName}'", gameObject);
            return;
        }

        _material.EnableKeyword("_EMISSION");
    }

    private void UpdateProximityFeedback()
    {
        float targetIntensity = 0f;

        // Calculate intensity based on proximity
        if (proximitySensor.TryGetNearest(out Signal nearest))
        {
            float distance = nearest.Distance;
            float normalizedDistance = Mathf.Clamp01((distance - minDistance) / (maxDistance - minDistance));
            targetIntensity = Mathf.Lerp(1f, 0f, normalizedDistance);
        }

        // Smooth interpolation
        _currentIntensity = Mathf.Lerp(_currentIntensity, targetIntensity, Time.deltaTime * 5f);

        // Apply to emission
        if (_material)
        {
            ApplyEmissionIntensity(_currentIntensity);
        }

        // Apply to audio pitch
        if (audioSource)
        {
            ApplyAudioPitch(_currentIntensity);
        }

        // Apply to light
        if (pointLight)
        {
            ApplyLightIntensity(_currentIntensity);
        }
    }

    private void ApplyEmissionIntensity(float intensity)
    {
        Color emissionColor = baseEmissionColor * (intensity * maxEmissionIntensity);
        _material.SetColor(emissionPropertyName, emissionColor);
    }

    private void ApplyAudioPitch(float intensity)
    {
        audioSource.pitch = Mathf.Lerp(minPitch, maxPitch, intensity);
    }

    private void ApplyLightIntensity(float intensity)
    {
        pointLight.intensity = intensity * maxLightIntensity;
    }

    private void OnDestroy()
    {
        if (_material && Application.isPlaying)
        {
            Destroy(_material);
        }
    }

    //==================== DEBUG =====================
    private void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;

        if (proximitySensor)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, minDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, maxDistance);
        }
    }

    private void OnGUI()
    {
        if (!showDebugInfo || !proximitySensor) return;

        GUI.Label(new Rect(10, 10, 300, 20), $"[ColumnController] Proximity: {_currentIntensity:F2}");
        
        if (proximitySensor.TryGetNearest(out Signal nearest))
        {
            GUI.Label(new Rect(10, 30, 300, 20), $"Distance: {nearest.Distance:F2}");
        }
        else
        {
            GUI.Label(new Rect(10, 30, 300, 20), "No objects detected");
        }
    }
}
