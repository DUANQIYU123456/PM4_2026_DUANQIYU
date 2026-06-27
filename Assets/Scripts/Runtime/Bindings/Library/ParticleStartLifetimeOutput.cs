using UnityEngine;

namespace Ludocore
{
    /// <summary>Drives a ParticleSystem.main.startLifetime. POCO card analog of ParticleStartLifetimeBinding.</summary>
    [BindMenu("Particles/Start Lifetime")]
    [System.Serializable]
    public class ParticleStartLifetimeOutput : BindTarget
    {
        [Tooltip("ParticleSystem to drive. Assigning a value forces startLifetime into Constant mode.")]
        [SerializeField] private ParticleSystem targetSystem;

        // Capture the whole MinMaxCurve (mode + curves/constants), not just .constant — otherwise restoring a
        // bare float would force the module into Constant mode and wipe any authored Curve/TwoConstants.
        private ParticleSystem.MinMaxCurve _original;

        public ParticleStartLifetimeOutput() : base(0.5f, 3f) { }

        public override string DisplayName => "Particle Start Lifetime";
        public override Color CardColor => new Color(0.5f, 0.82f, 0.7f);
        public override bool AltersState => true;

        protected override void OnCapture() { if (targetSystem) _original = targetSystem.main.startLifetime; }

        protected override void OnRestore()
        {
            if (!targetSystem) return;
            var main = targetSystem.main;
            main.startLifetime = _original;
        }

        protected override void OnApply(float value)
        {
            if (!targetSystem) return;
            var main = targetSystem.main;
            main.startLifetime = value;
        }
    }
}
