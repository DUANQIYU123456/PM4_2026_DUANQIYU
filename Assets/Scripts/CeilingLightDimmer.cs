using System.Collections.Generic;
using UnityEngine;

namespace Ludocore
{
    /// <summary>
    /// Dims ceiling emission on detected panels based on distance.
    /// Reads from any Sensor — closer panels get dimmer (inverse proximity).
    /// Creates material instances per-renderer so panels dim independently.
    /// </summary>
    public class CeilingLightDimmer : MonoBehaviour
    {
        [Header("Source")]
        [Tooltip("Sensor that detects ceiling panels (e.g. ProximitySensor on the player)")]
        [SerializeField] private Sensor sensor;

        [Header("Material")]
        [Tooltip("Which material slot on the renderer is the ceiling (0-based)")]
        [Min(0)]
        [SerializeField] private int ceilingMaterialIndex = 1;

        [Header("Emission")]
        [Tooltip("Original emission color of the ceiling material (HDR)")]
        [SerializeField, ColorUsage(false, true)]
        private Color emissionColor = new Color(5.8f, 8.06f, 9.53f, 1f);

        [Tooltip("Intensity when at full distance (normal brightness)")]
        [Min(0f)]
        [SerializeField] private float maxIntensity = 1f;

        [Tooltip("Intensity when player is right underneath (dimmed)")]
        [Min(0f)]
        [SerializeField] private float minIntensity = 0f;

        [Header("Mapping")]
        [Tooltip("Distance at which dimming starts (closer = dimmer)")]
        [Min(0.01f)]
        [SerializeField] private float effectDistance = 10f;

        [Tooltip("Curve mapping proximity (0=far, 1=close) to dim amount (0=bright, 1=dark)")]
        [SerializeField] private AnimationCurve dimCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Feel")]
        [Tooltip("How fast emission responds (units per second)")]
        [Min(0.1f)]
        [SerializeField] private float smoothSpeed = 5f;

        // --- Runtime ---
        private readonly Dictionary<Renderer, PanelData> _panels = new();
        private readonly HashSet<Renderer> _activeThisFrame = new();

        private struct PanelData
        {
            public Material Instance;
            public float CurrentIntensity;
        }

        private void Update()
        {
            if (!sensor) return;

            _activeThisFrame.Clear();

            // Drive detected panels
            var signals = sensor.Signals;
            for (int i = 0; i < signals.Count; i++)
            {
                if (!signals[i].Object) continue;

                var rend = signals[i].Object.GetComponent<Renderer>();
                if (!rend) continue;

                _activeThisFrame.Add(rend);
                EnsureInstance(rend);

                float proximity = 1f - Mathf.Clamp01(signals[i].Distance / effectDistance);
                float dimAmount = dimCurve.Evaluate(proximity);
                float target = Mathf.Lerp(maxIntensity, minIntensity, dimAmount);

                DrivePanel(rend, target);
            }

            // Restore panels that left sensor range
            RestoreLost();
        }

        private void EnsureInstance(Renderer rend)
        {
            if (_panels.ContainsKey(rend)) return;

            var mats = rend.materials; // creates instances for all slots
            if (ceilingMaterialIndex >= mats.Length) return;

            _panels[rend] = new PanelData
            {
                Instance = mats[ceilingMaterialIndex],
                CurrentIntensity = maxIntensity
            };
        }

        private void DrivePanel(Renderer rend, float target)
        {
            if (!_panels.TryGetValue(rend, out var data)) return;

            data.CurrentIntensity = Mathf.MoveTowards(
                data.CurrentIntensity, target, smoothSpeed * Time.deltaTime);
            data.Instance.SetColor("_EmissionColor", emissionColor * data.CurrentIntensity);

            _panels[rend] = data;
        }

        private readonly List<Renderer> _lostBuffer = new();
        private readonly List<Renderer> _removeBuffer = new();

        private void RestoreLost()
        {
            // Collect lost panels first — can't modify dict during iteration
            _lostBuffer.Clear();
            _removeBuffer.Clear();

            foreach (var kvp in _panels)
            {
                if (!_activeThisFrame.Contains(kvp.Key))
                    _lostBuffer.Add(kvp.Key);
            }

            for (int i = 0; i < _lostBuffer.Count; i++)
            {
                var rend = _lostBuffer[i];
                var data = _panels[rend];

                data.CurrentIntensity = Mathf.MoveTowards(
                    data.CurrentIntensity, maxIntensity, smoothSpeed * Time.deltaTime);
                data.Instance.SetColor("_EmissionColor", emissionColor * data.CurrentIntensity);
                _panels[rend] = data;

                if (Mathf.Approximately(data.CurrentIntensity, maxIntensity))
                    _removeBuffer.Add(rend);
            }

            for (int i = 0; i < _removeBuffer.Count; i++)
                _panels.Remove(_removeBuffer[i]);
        }

        private void OnDestroy()
        {
            // Instances are owned by the renderers — no manual cleanup needed.
            _panels.Clear();
        }
    }
}
