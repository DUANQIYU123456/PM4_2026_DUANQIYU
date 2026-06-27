using UnityEngine;

namespace Ludocore
{
    /// <summary>
    /// Drives the HDR intensity of a color/emission property on a renderer's material, pushing it into the
    /// over-1 (bloom-feeding) range. Each frame it writes <c>baseColor × mappedValue</c> to the property,
    /// where the mapped value is the intensity multiplier — so pick the hue you want to glow as
    /// <see cref="baseColor"/> and let the signal scale its brightness.
    ///
    /// Uses a LOCAL material INSTANCE (renderer.materials[index]) so only this object is affected and the
    /// shared asset stays clean. Optionally force-enables the _EMISSION keyword so emission is visible.
    /// Captures and restores the original color and keyword state on play-exit.
    /// </summary>
    [BindMenu("Renderer/Material HDR Intensity")]
    [System.Serializable]
    public class MaterialHDRIntensityOutput : BindTarget
    {
        [Tooltip("Renderer whose LOCAL material instance to drive.")]
        [SerializeField] private Renderer target;
        [Tooltip("Which material slot on the renderer.")]
        [Min(0)]
        [SerializeField] private int materialIndex;
        [Tooltip("HDR color property to drive (e.g. _EmissionColor, _BaseColor).")]
        [SerializeField] private string property = "_EmissionColor";
        [Tooltip("Base (intensity-1) chroma. The driven value multiplies this, so pick the hue you want to glow.")]
        [ColorUsage(true, true)]
        [SerializeField] private Color baseColor = Color.white;
        [Tooltip("Force-enable the _EMISSION keyword on the instance so emission is visible (for emission properties).")]
        [SerializeField] private bool enableEmission = true;

        private Material _mat;
        private Color _original;
        private bool _origEmission;

        public MaterialHDRIntensityOutput() : base(0f, 4f) { }

        public override string DisplayName => "Material HDR Intensity";
        public override Color CardColor => new Color(1f, 0.7f, 0.6f);
        public override bool AltersState => true;

        protected override void OnCapture()
        {
            if (target)
            {
                Material[] mats = target.materials; // local instances — never the shared asset
                if (materialIndex < mats.Length) _mat = mats[materialIndex];
            }
            if (!_mat) return;
            if (_mat.HasProperty(property)) _original = _mat.GetColor(property);
            _origEmission = _mat.IsKeywordEnabled("_EMISSION");
            if (enableEmission) _mat.EnableKeyword("_EMISSION");
        }

        protected override void OnRestore()
        {
            if (!_mat) return;
            if (_mat.HasProperty(property)) _mat.SetColor(property, _original);
            if (_origEmission) _mat.EnableKeyword("_EMISSION");
            else _mat.DisableKeyword("_EMISSION");
        }

        protected override void OnApply(float value)
        {
            if (_mat && _mat.HasProperty(property)) _mat.SetColor(property, baseColor * value);
        }
    }
}
