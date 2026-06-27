using System.Collections.Generic;
using UnityEngine;

namespace Ludocore
{
    /// <summary>
    /// One source, many outputs. The continuous-coupling analog of <see cref="FeedbackPlayer"/>: instead of a
    /// MonoBehaviour per output, a single component holds a <see cref="BindTarget"/> list and drives them all
    /// from one signal.
    ///
    /// It IS a <see cref="Binding"/> — it inherits the full pipeline (source / fallback, input range, master
    /// curve, shared attack/release envelope, trigger event, registry). The host's own output range is left at
    /// its 0..1 default so <see cref="Apply"/> hands every card a normalized MASTER SIGNAL; each card then owns
    /// its own curve + output range, so one knob can push a light to 0..2 while pushing a material to 0..1.
    ///
    /// Revert policy (deliberate): stateful cards (AltersState) snapshot their baseline the first time they are
    /// driven and are reverted on PLAY-EXIT by <see cref="BindingResetHook"/> — the same editor "keep the scene
    /// clean" philosophy as <see cref="FeedbackPlayer"/>. Disabling the component at runtime does NOT revert
    /// (an aggregator that snapped every output back on disable would surprise a live rig); call
    /// <see cref="ResetTargets"/> explicitly if you want that.
    /// </summary>
    public class BindingPlayer : Binding
    {
        /// <summary>
        /// Global switch mirroring <see cref="FeedbackPlayer.KeepChangesOnPlayExit"/>. When false (default), the
        /// editor reverts every player's stateful outputs on play-exit. Toggle via
        /// Tools &gt; Ludocore &gt; Bindings &gt; Keep Changes On Play Exit.
        /// </summary>
        public static bool KeepChangesOnPlayExit;

        //==================== CONFIG =====================
        [Header("Outputs")]
        [Tooltip("Outputs driven by the shared 0..1 master signal. Each card remaps it to its own range. " +
                 "Add from the \"+ Add Output\" menu.")]
        [SerializeReference] private List<BindTarget> targets = new();

        //==================== METADATA =====================
        public override string Description => "Fans one shaped source out to a list of output cards.";

        //==================== BINDING =====================
        /// <summary>
        /// Fan the host's shaped + enveloped 0..1 master signal out to every active card. Each card captures its
        /// baseline the first time it is driven (lazy + idempotent), so a card muted at enable and un-muted later
        /// is still snapshotted before it mutates anything — keeping <see cref="ResetTargets"/> able to revert it.
        /// </summary>
        protected override void Apply(float value)
        {
            // Iterate by index — defensive against a card list edited at runtime.
            for (int i = 0; i < targets.Count; i++)
            {
                BindTarget t = targets[i];
                if (t is not { Active: true }) continue;
                t.Capture();
                t.Drive(value);
            }
        }

        //==================== INPUTS =====================
        /// <summary>
        /// Revert every output that captured a "before" value back to that value. No-op for outputs that
        /// never drove or don't alter state. Driven by <see cref="BindingResetHook"/> on play-exit.
        /// </summary>
        [ContextMenu("Reset Outputs")]
        public void ResetTargets()
        {
            foreach (BindTarget t in targets)
                t?.Restore();
        }
    }
}
