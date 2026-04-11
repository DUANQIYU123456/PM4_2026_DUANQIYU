using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Sets or animates emission intensity on a material color property.</summary>
    public class MaterialEmissionIntensity : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Renderer whose material to modify")]
        [SerializeField] private Renderer targetRenderer;
        [Tooltip("Index of the material on the renderer")]
        [Min(0)]
        [SerializeField] private int materialIndex;
        [Tooltip("Shader color property used for emission")]
        [SerializeField] private string propertyName = "_EmissionColor";
        [Tooltip("Base emission color before intensity is applied")]
        [SerializeField] private Color emissionColor = Color.white;
        [Tooltip("Target intensity used by Play")]
        [Min(0f)]
        [SerializeField] private float targetIntensity = 1f;
        [Tooltip("Animation duration in seconds (0 = instant)")]
        [Min(0f)]
        [SerializeField] private float duration = 0.5f;
        [Tooltip("Tween easing")]
        [SerializeField] private Ease ease = Ease.OutQuad;

        [Tooltip("Play automatically when enabled")]
        [SerializeField] private bool autoPlay;

        //==================== STATE =====================
        private Material _material;
        private bool _initialized;
        private Tween _tween;
        private float _currentIntensity;

        public float CurrentIntensity => _currentIntensity;
        public bool IsAnimating => _tween is { active: true };

        //==================== OUTPUTS =====================
        public event Action OnCompleted;

        [Header("Events")]
        [Tooltip("Fired when the intensity animation completes")]
        [SerializeField] private UnityEvent completedEvent;

        //==================== LIFECYCLE =====================
        private void OnEnable()
        {
            if (autoPlay) Play();
        }

        //==================== INPUTS =====================
        /// <summary>Animate to the configured target intensity.</summary>
        [ContextMenu("Play")]
        public void Play()
        {
            Animate(targetIntensity, duration);
        }

        /// <summary>Set the emission intensity instantly.</summary>
        public void SetIntensity(float intensity)
        {
            if (!EnsureMaterial()) return;

            KillTween();
            ApplyIntensity(intensity);
        }

        /// <summary>Animate the emission intensity over the configured duration.</summary>
        public void Animate(float intensity)
        {
            Animate(intensity, duration);
        }

        /// <summary>Animate the emission intensity over a custom duration.</summary>
        public void Animate(float intensity, float customDuration)
        {
            if (!EnsureMaterial()) return;

            KillTween();

            if (customDuration <= 0f)
            {
                ApplyIntensity(intensity);
                HandleCompleted();
                return;
            }

            _tween = DOTween.To(() => _currentIntensity, ApplyIntensity, intensity, customDuration)
                .SetEase(ease)
                .OnComplete(HandleCompleted);
        }

        /// <summary>Stop the current animation.</summary>
        public void Stop()
        {
            KillTween();
        }

        //==================== PRIVATE =====================
        private bool EnsureMaterial()
        {
            if (_initialized) return _material;
            if (!targetRenderer) return false;

            Material[] materials = targetRenderer.materials;
            if (materialIndex < 0 || materialIndex >= materials.Length) return false;

            _material = materials[materialIndex];
            if (!_material || !_material.HasProperty(propertyName)) return false;

            _material.EnableKeyword("_EMISSION");

            Color currentColor = _material.GetColor(propertyName);
            _currentIntensity = GetIntensity(currentColor);

            _initialized = true;
            return true;
        }

        private void ApplyIntensity(float intensity)
        {
            if (!_material) return;

            _currentIntensity = Mathf.Max(0f, intensity);
            _material.SetColor(propertyName, emissionColor * _currentIntensity);
        }

        private float GetIntensity(Color color)
        {
            float maxChannel = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            float baseChannel = Mathf.Max(emissionColor.r, Mathf.Max(emissionColor.g, emissionColor.b));

            if (baseChannel <= 0f) return maxChannel;

            return maxChannel / baseChannel;
        }

        private void KillTween()
        {
            if (_tween is { active: true }) _tween.Kill();
            _tween = null;
        }

        private void HandleCompleted()
        {
            OnCompleted?.Invoke();
            completedEvent?.Invoke();
        }

        private void OnDestroy()
        {
            KillTween();

            if (_initialized && _material && Application.isPlaying)
                Destroy(_material);
        }
    }
}
