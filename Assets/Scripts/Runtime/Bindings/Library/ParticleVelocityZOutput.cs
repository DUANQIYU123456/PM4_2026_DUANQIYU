using UnityEngine;

namespace Ludocore
{
    /// <summary>Drives a ParticleSystem.velocityOverLifetime.z (positive +Z). POCO card analog of ParticleVelocityZBinding.</summary>
    [BindMenu("Particles/Velocity Z")]
    [System.Serializable]
    public class ParticleVelocityZOutput : BindTarget
    {
        [Tooltip("ParticleSystem to drive. Its Velocity-over-Lifetime module must be enabled to have any visible effect.")]
        [SerializeField] private ParticleSystem targetSystem;

        // Capture the whole MinMaxCurve (mode + curves/constants), not just .constant — otherwise restoring a
        // bare float would force the module into Constant mode and wipe any authored Curve/TwoConstants.
        private ParticleSystem.MinMaxCurve _original;

        public ParticleVelocityZOutput() : base(0f, 5f) { }

        public override string DisplayName => "Particle Velocity Z";
        public override Color CardColor => new Color(0.5f, 0.82f, 0.7f);
        public override bool AltersState => true;

        protected override void OnCapture() { if (targetSystem) _original = targetSystem.velocityOverLifetime.z; }

        protected override void OnRestore()
        {
            if (!targetSystem) return;
            var velocity = targetSystem.velocityOverLifetime;
            velocity.z = _original;
        }

        protected override void OnApply(float value)
        {
            if (!targetSystem) return;
            var velocity = targetSystem.velocityOverLifetime;
            velocity.z = value;
        }
    }
}
