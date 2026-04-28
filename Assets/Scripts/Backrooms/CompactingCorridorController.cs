using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ludocore
{
    /// <summary>Compacts corridor walls, scrolls wall texture, and drives post-processing
    /// based on player proximity to the corridor exit.
    /// Reads all tuning values from a swappable CompactingCorridorProfile asset.</summary>
    public class CompactingCorridorController : MonoBehaviour
    {
        //==================== SCENE REFERENCES =====================
        [Header("Scene References")]
        [Tooltip("Proximity sensor placed at or near the corridor exit")]
        [SerializeField] private ProximitySensor sensor;

        [SerializeField] private Transform wallA;
        [SerializeField] private Transform wallB;

        [Tooltip("Shared material for both walls (texture scroll applied directly)")]
        [SerializeField] private Material wallMaterial;

        [Tooltip("URP post-processing volume with Vignette and Color Adjustments overrides")]
        [SerializeField] private Volume volume;

        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Local-space direction each wall moves inward (wallA +axis, wallB −axis)")]
        [SerializeField] private Vector3 compactionAxis = Vector3.forward;

        //==================== PROFILE =====================
        [Header("Profile")]
        [Tooltip("Scriptable object with compaction, scroll, and post-processing settings")]
        [SerializeField] private CompactingCorridorProfile profile;

        //==================== STATE =====================
        private Vector3 _restA;
        private Vector3 _restB;
        private float _currentOffset;
        private Vector2 _initialTexOffset;
        private float _scrollAccumulator;
        private Vignette _vignette;
        private ColorAdjustments _colorAdjustments;

        //==================== LIFECYCLE =====================
        private void Start()
        {
            if (wallA) _restA = wallA.localPosition;
            if (wallB) _restB = wallB.localPosition;

            if (wallMaterial)
                _initialTexOffset = wallMaterial.GetTextureOffset("_BaseMap");

            if (volume && volume.profile)
            {
                volume.profile.TryGet(out _vignette);
                volume.profile.TryGet(out _colorAdjustments);
            }
        }

        private void Update()
        {
            if (!sensor || !profile) return;

            // --- 1. Compute normalized proximity ---
            float proximity = 0f;

            if (sensor.TryGetNearest(out Signal nearest))
                proximity = 1f - Mathf.Clamp01(nearest.Distance / profile.maxDistance);

            // --- 2. Wall compaction ---
            if (wallA && wallB)
            {
                float curved = profile.compactionCurve.Evaluate(proximity);
                float offsetTarget = Mathf.Lerp(0f, profile.maxCompaction, curved);

                _currentOffset = Smooth(_currentOffset, offsetTarget,
                                        profile.attackSpeed, profile.releaseSpeed);

                Vector3 axis = compactionAxis.normalized;
                wallA.localPosition = _restA + axis * _currentOffset;
                wallB.localPosition = _restB - axis * _currentOffset;
            }

            // --- 3. Wall texture scroll ---
            if (wallMaterial)
            {
                float scrollFactor = profile.scrollCurve.Evaluate(proximity);
                float scrollSpeed = scrollFactor * profile.maxScrollSpeed;
                _scrollAccumulator += scrollSpeed * Time.deltaTime;

                wallMaterial.SetTextureOffset("_BaseMap",
                    _initialTexOffset + new Vector2(0f, _scrollAccumulator));
            }

            // --- 4. Post-processing ---
            if (_vignette != null)
            {
                float vFactor = profile.vignetteCurve.Evaluate(proximity);
                _vignette.intensity.Override(Mathf.Lerp(0f, profile.maxVignetteIntensity, vFactor));
            }

            if (_colorAdjustments != null)
            {
                float dFactor = profile.desaturationCurve.Evaluate(proximity);
                _colorAdjustments.saturation.Override(Mathf.Lerp(0f, -profile.maxDesaturation * 100f, dFactor));
            }
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
