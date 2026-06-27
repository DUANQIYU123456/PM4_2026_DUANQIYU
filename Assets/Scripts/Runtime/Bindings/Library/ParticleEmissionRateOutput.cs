using UnityEngine;

namespace Ludocore
{
    /// <summary>Drives a ParticleSystem.emission.rateOverTime. POCO card analog of ParticleEmissionRateBinding.</summary>
    [BindMenu("Particles/Emission Rate")]
    [System.Serializable]
    public class ParticleEmissionRateOutput : BindTarget
    {
        [Tooltip("ParticleSystem to drive. Its Emission module must be enabled to have any visible effect.")]
        [SerializeField] private ParticleSystem targetSystem;

        // Capture the whole MinMaxCurve (mode + curves/constants), not just .constant — otherwise restoring a
        // bare float would force the module into Constant mode and wipe any authored Curve/TwoConstants.
        private ParticleSystem.MinMaxCurve _original;

        public ParticleEmissionRateOutput() : base(0f, 100f) { }

        public override string DisplayName => "Particle Emission Rate";
        public override Color CardColor => new Color(0.5f, 0.82f, 0.7f);
        public override bool AltersState => true;

        protected override void OnCapture() { if (targetSystem) _original = targetSystem.emission.rateOverTime; }

        protected override void OnRestore()
        {
            if (!targetSystem) return;
            var emission = targetSystem.emission;
            emission.rateOverTime = _original;
        }

        protected override void OnApply(float value)
        {
            if (!targetSystem) return;
            var emission = targetSystem.emission;
            emission.rateOverTime = value;
        }
    }
}
