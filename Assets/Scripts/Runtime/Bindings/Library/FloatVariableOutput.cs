using UnityEngine;

namespace Ludocore
{
    /// <summary>
    /// Writes the mapped value into a FloatVariable — the bridge card. Lets one BindingPlayer fan out into
    /// other variables that feed existing single Bindings, UI bars, or thresholds, without replacing them.
    ///
    /// WARNING: do NOT point this at the BindingPlayer's own <c>source</c> variable (directly or via a chain
    /// that loops back). Each ramp step re-raises the source's OnChanged, re-targeting the host and fighting
    /// its envelope — an oscillation that never settles.
    /// </summary>
    [BindMenu("Variables/Set Float")]
    [System.Serializable]
    public class FloatVariableOutput : BindTarget
    {
        [Tooltip("FloatVariable asset to write the mapped value to each frame / on each source change. " +
                 "Must NOT be the BindingPlayer's own source (that creates a feedback loop).")]
        [SerializeField] private FloatVariable variable;

        private float _original;

        public FloatVariableOutput() : base(0f, 1f) { }

        public override string DisplayName => "Set Float";
        public override Color CardColor => new Color(0.85f, 0.8f, 0.6f);
        public override bool AltersState => true;

        protected override void OnCapture() { if (variable) _original = variable.Value; }
        protected override void OnRestore() { if (variable) variable.Value = _original; }

        protected override void OnApply(float value)
        {
            if (variable) variable.Value = value;
        }
    }
}
