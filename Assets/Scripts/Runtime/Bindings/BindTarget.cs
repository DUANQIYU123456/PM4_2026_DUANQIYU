using UnityEngine;

namespace Ludocore
{
    /// <summary>
    /// One output inside a <see cref="BindingPlayer"/>. A lightweight [SerializeReference] data object
    /// (NOT a MonoBehaviour) so one source can drive many outputs from a single component and inspector —
    /// the continuous-coupling analog of <see cref="Feedback"/> in a <see cref="FeedbackPlayer"/>.
    ///
    /// The host shapes its source into a normalized 0..1 "master signal" (input range, master curve,
    /// shared envelope) and pushes that to every active card via <see cref="Drive"/>. Each card owns the
    /// OUTPUT side: its own <see cref="curve"/> response and <see cref="outputRange"/> remap — so one knob
    /// can push a light to 0..2 intensity while pushing a material to 0..1 at the same time.
    ///
    /// To add a new output: inherit this, tag it with <see cref="BindMenuAttribute"/>, give it a
    /// <see cref="DisplayName"/>, and override <see cref="OnApply"/> to write the mapped value to your
    /// target. It then appears automatically in the "+ Add Output" menu.
    ///
    /// If the output leaves a LASTING change (a light's intensity, a material property, an SO variable),
    /// also override <see cref="AltersState"/> =&gt; true and <see cref="OnCapture"/>/<see cref="OnRestore"/>
    /// so the host can revert it on play-exit — see <see cref="BindingPlayer.ResetTargets"/>.
    /// </summary>
    [System.Serializable]
    public abstract class BindTarget : ICard
    {
        //==================== CONFIG =====================
        [Tooltip("Uncheck to mute this output without removing it.")]
        [SerializeField] private bool active = true;
        [Tooltip("Optional name shown on the card. Leave empty to use the output's default name.")]
        [SerializeField] private string label = "";
        [Header("Mapping")]
        [Tooltip("Output value range. This card's curve shapes the host's 0..1 master signal, then it is " +
                 "remapped (unclamped) between these endpoints and written to the target.")]
        [SerializeField] private Vector2 outputRange = new Vector2(0f, 1f);
        [Tooltip("Per-output response curve applied to the host's 0..1 master signal before it is remapped to outputRange.")]
        [SerializeField] private AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public bool Active => active;

        /// <summary>Optional author-supplied name; empty falls back to <see cref="DisplayName"/>.</summary>
        public string Label => label;

        //==================== STATE =====================
        [System.NonSerialized] private bool _captured;

        //==================== INSPECTOR METADATA =====================
        /// <summary>Name shown on the card header in the inspector.</summary>
        public abstract string DisplayName { get; }

        /// <summary>Header tint — group related outputs by color so a list is readable at a glance.</summary>
        public virtual Color CardColor => new Color(0.45f, 0.45f, 0.5f);

        /// <summary>True if this output leaves a lasting change that <see cref="OnRestore"/> can revert.</summary>
        public virtual bool AltersState => false;

        //==================== CONSTRUCTION =====================
        protected BindTarget() { }

        /// <summary>Seed a sensible default output range for newly added cards (the native range of the target).</summary>
        protected BindTarget(float outMin, float outMax) => outputRange = new Vector2(outMin, outMax);

        //==================== HOST API =====================
        /// <summary>
        /// Push the host's normalized 0..1 master signal through this card's curve + range and write it.
        /// Called every frame / on every source change by the owning <see cref="BindingPlayer"/>.
        /// </summary>
        public void Drive(float normalized)
        {
            float shaped = curve.Evaluate(normalized);
            OnApply(Mathf.LerpUnclamped(outputRange.x, outputRange.y, shaped));
        }

        /// <summary>Snapshot the "before" value the first time the card is driven (no-op for stateless outputs).</summary>
        public void Capture()
        {
            if (_captured || !AltersState) return;
            _captured = true;
            OnCapture();
        }

        /// <summary>Restore the snapshot taken at first enable. No-op if this output never captured.</summary>
        public void Restore()
        {
            if (!_captured) return;
            _captured = false;
            OnRestore();
        }

        //==================== SUBCLASS API =====================
        /// <summary>Write the shaped, remapped output value to the target (light, material, particle module, etc.).</summary>
        protected abstract void OnApply(float value);

        /// <summary>
        /// Snapshot original values. Called once, immediately before this output is first driven. Stateful
        /// outputs override. Also the right place to force-enable a target effect (capture the "before" flag
        /// here first, then enable) so <see cref="OnRestore"/> can revert it.
        /// </summary>
        protected virtual void OnCapture() { }

        /// <summary>Revert to the snapshot taken in <see cref="OnCapture"/>. Stateful outputs override.</summary>
        protected virtual void OnRestore() { }
    }
}
