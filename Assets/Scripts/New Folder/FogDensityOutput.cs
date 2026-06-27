using UnityEngine;

namespace Ludocore
{
    /// <summary>
    /// Drives the scene's environment fog density (RenderSettings.fogDensity) from the
    /// host's master signal. Continuous analog of FogFocusController — instead of a
    /// focus event flipping between two densities, the binding signal scrubs the density
    /// across this card's output range. Maps signal 0 -> outputRange.x, 1 -> outputRange.y.
    /// </summary>
    [BindMenu("Environment/Fog Density")]
    [System.Serializable]
    public class FogDensityOutput : BindTarget
    {
        [Tooltip("Force scene fog on while this card drives. Restored on Restore.")]
        [SerializeField] private bool enableFog = true;

        private bool _originalFog;
        private float _originalDensity;

        public FogDensityOutput() : base(0f, 0.1f) { }

        public override string DisplayName => "Fog Density";
        public override Color CardColor => new Color(0.6f, 0.7f, 0.75f);
        public override bool AltersState => true;

        protected override void OnCapture()
        {
            _originalFog = RenderSettings.fog;
            _originalDensity = RenderSettings.fogDensity;
        }

        protected override void OnRestore()
        {
            RenderSettings.fog = _originalFog;
            RenderSettings.fogDensity = _originalDensity;
        }

        protected override void OnApply(float value)
        {
            if (enableFog) RenderSettings.fog = true;
            RenderSettings.fogDensity = value;
        }
    }
}
