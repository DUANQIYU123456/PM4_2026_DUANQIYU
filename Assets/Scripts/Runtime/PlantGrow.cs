using DG.Tweening;
using UnityEngine;

namespace Ludocore
{
    /// <summary>Animates a plant from nothing to full size and emission intensity.</summary>
    public class PlantGrow : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("References")]
        [Tooltip("Renderer whose emission is animated during growth.")]
        [SerializeField] private Renderer targetRenderer;

        [Header("Scale")]
        [Tooltip("Final uniform scale the plant grows to.")]
        [Min(0f)]
        [SerializeField] private float targetScale = 1f;
        [Tooltip("Easing curve for the scale animation.")]
        [SerializeField] private Ease scaleEase = Ease.OutBack;

        [Header("Emission")]
        [Tooltip("HDR color used for emission at full growth.")]
        [SerializeField, ColorUsage(false, true)] private Color emissionColor = Color.white;
        [Tooltip("Emission intensity multiplier at full growth.")]
        [Min(0f)]
        [SerializeField] private float maxIntensity = 2f;
        [Tooltip("Easing curve for the emission animation.")]
        [SerializeField] private Ease emissionEase = Ease.OutQuad;

        [Header("Timing")]
        [Tooltip("How long the grow animation takes in seconds.")]
        [Min(0f)]
        [SerializeField] private float duration = 1f;
        [Tooltip("Start growing automatically when the object is enabled.")]
        [SerializeField] private bool autoStart = true;

        //==================== STATE =====================
        private Material _material;

        //==================== LIFECYCLE =====================
        private void Awake()
        {
            _material = targetRenderer.material;
        }

        private void OnEnable()
        {
            if (autoStart) Grow();
        }

        //==================== PRIVATE =====================
        private void Grow()
        {
            transform.localScale = Vector3.zero;
            _material.SetColor("_EmissionColor", Color.black);

            transform.DOScale(Vector3.one * targetScale, duration).SetEase(scaleEase);

            _material.DOColor(emissionColor * maxIntensity, "_EmissionColor", duration).SetEase(emissionEase);
        }

        private void OnDestroy()
        {
            transform.DOKill();
            if (_material)
            {
                _material.DOKill();
                Destroy(_material);
            }
        }
    }
}
