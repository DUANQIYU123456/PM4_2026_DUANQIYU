using UnityEngine;

namespace Ludocore
{
    /// <summary>
    /// Drives the Deep Water Color (<c>_DeepColor</c>) of the ShaderGraph water material by lerping between
    /// two colors with the 0..1 master signal. Uses a LOCAL material INSTANCE (renderer.materials[index]) so
    /// the shared water asset stays clean, and captures/restores the original color on play-exit. The property
    /// name is configurable for other water graphs that expose deep-water tint under a different reference.
    /// </summary>
    [BindMenu("Water/Deep Water Color")]
    [System.Serializable]
    public class WaterDeepColorOutput : BindTarget
    {
        [Tooltip("Renderer of the water surface whose LOCAL material instance to drive.")]
        [SerializeField] private Renderer target;
        [Tooltip("Which material slot on the renderer.")]
        [Min(0)]
        [SerializeField] private int materialIndex;
        [Tooltip("Deep-water color property. The project's ShaderGraph water shader exposes it as _DeepColor.")]
        [SerializeField] private string property = "_DeepColor";
        [Tooltip("Deep-water color at signal 0.")]
        [ColorUsage(true, true)]
        [SerializeField] private Color colorA = new Color(0f, 0.15f, 0.25f);
        [Tooltip("Deep-water color at signal 1.")]
        [ColorUsage(true, true)]
        [SerializeField] private Color colorB = new Color(0f, 0.4f, 0.6f);

        private Material _mat;
        private Color _original;

        public override string DisplayName => "Water Deep Color";
        public override Color CardColor => new Color(0.35f, 0.7f, 0.85f);
        public override bool AltersState => true;

        protected override void OnCapture()
        {
            if (target)
            {
                Material[] mats = target.materials; // local instances — never the shared asset
                if (materialIndex < mats.Length) _mat = mats[materialIndex];
            }
            if (_mat && _mat.HasProperty(property)) _original = _mat.GetColor(property);
        }
        protected override void OnRestore() { if (_mat && _mat.HasProperty(property)) _mat.SetColor(property, _original); }

        protected override void OnApply(float value)
        {
            if (_mat && _mat.HasProperty(property)) _mat.SetColor(property, Color.LerpUnclamped(colorA, colorB, value));
        }
    }
}
