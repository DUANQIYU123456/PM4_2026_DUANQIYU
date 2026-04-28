using UnityEngine;
using DG.Tweening;

namespace Ludocore
{
    /// <summary>Plays a one-shot grow animation on a tree — uniformly scales up while
    /// driving bark and leaves shader properties from sapling to fully grown state.
    /// Reads all timing, curves, and target values from a swappable TreeGrowProfile asset.</summary>
    public class TreeGrower : MonoBehaviour
    {
        //==================== SCENE REFERENCES =====================
        [Header("Scene References")]
        [Tooltip("Renderer with bark at material slot 0 and leaves at material slot 1")]
        [SerializeField] private Renderer treeRenderer;

        //==================== PROFILE =====================
        [Header("Profile")]
        [Tooltip("Scriptable object defining timing, curve, and target values")]
        [SerializeField] private TreeGrowProfile profile;

        [Header("Behaviour")]
        [Tooltip("Start growing automatically when the object is enabled")]
        [SerializeField] private bool autoPlay;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private float growProgress;

        private Material _barkMaterial;
        private Material _leavesMaterial;
        private Tween _growTween;

        //==================== LIFECYCLE =====================
        private void Awake()
        {
            // .materials returns instances unique to this renderer
            var instances = treeRenderer.materials;
            _barkMaterial = instances[0];
            _leavesMaterial = instances[1];

            ApplyProgress(0f);
        }

        private void OnEnable()
        {
            if (autoPlay) Grow();
        }

        private void OnDestroy()
        {
            _growTween?.Kill();
            if (_barkMaterial) Destroy(_barkMaterial);
            if (_leavesMaterial) Destroy(_leavesMaterial);
        }

        //==================== INPUTS =====================
        /// <summary>Play the grow animation from the start. Restarts if already running.</summary>
        [ContextMenu("Grow")]
        public void Grow()
        {
            if (!profile) return;

            _growTween?.Kill();
            ApplyProgress(0f);

            _growTween = DOTween.To(
                () => growProgress, ApplyProgress,
                1f, profile.growDuration
            ).SetEase(profile.growCurve);
        }

        //==================== PRIVATE =====================
        private void ApplyProgress(float progress)
        {
            growProgress = progress;
            if (!profile) return;

            transform.localScale = Vector3.one * Mathf.Lerp(profile.startScale, profile.endScale, progress);

            _barkMaterial.SetFloat("_Base_Map_Scale",
                Mathf.Lerp(profile.barkScaleStart, profile.barkScaleEnd, progress));

            _leavesMaterial.SetFloat("_Blend_Height",
                Mathf.Lerp(profile.leavesBlendHeightStart, profile.leavesBlendHeightEnd, progress));
            _leavesMaterial.SetFloat("_Alpha_Cutoff",
                Mathf.Lerp(profile.leavesAlphaCutoffStart, profile.leavesAlphaCutoffEnd, progress));
        }
    }
}
