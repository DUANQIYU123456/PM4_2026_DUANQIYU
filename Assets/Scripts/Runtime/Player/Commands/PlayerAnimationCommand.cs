using UnityEngine;

namespace Ludocore
{
    /// <summary>
    /// Player one-shot animation command. On Execute it fires an Animator trigger (optionally clearing an
    /// opposite trigger first), then completes immediately — the motion lives entirely in the Animator,
    /// mirroring the NPC command pattern (thin orchestrator over a module).
    /// Built for voice dispatch: set <see cref="commandKey"/> to the spoken keyword (e.g. "down", "up")
    /// and assign the PlayerCommandRegistry so <see cref="TranscriptCommandParser"/> can find it by token.
    /// Two instances cover get-down / get-up; more postures/gestures are just more instances, no new class.
    /// <para>NOTE: this is cosmetic only — it fires an Animator trigger and does NOT resize the player's
    /// collider, so it does NOT defeat <see cref="SightSensor"/>'s vertical (loseBelowEyeLevel) detection by
    /// itself. Making "down" actually hide the player needs the deferred PlayerPosture collider-lowering
    /// (lower the CapsuleCollider height/center so bounds.max.y drops below the NPC eye band). See the spec.</para>
    /// </summary>
    public class PlayerAnimationCommand : Command
    {
        //==================== CONFIG =====================
        [Header("Identity")]
        [Tooltip("Spoken keyword this command answers to (lowercased for matching). Must be a SINGLE word the " +
                 "parser will see as a token — e.g. \"down\" matches \"get down\" / \"lie down\", \"up\" matches " +
                 "\"get up\" / \"stand up\". Leave empty to fall back to the class-name default key.")]
        [SerializeField] private string commandKey = "";

        [Header("Animation")]
        [Tooltip("Animator driving the player's posture states. Auto-found in children on Reset.")]
        [SerializeField] private Animator animator;
        [Tooltip("Animator trigger fired on Execute, e.g. \"GetDown\".")]
        [SerializeField] private string setTrigger;
        [Tooltip("Optional opposite trigger cleared before firing setTrigger so get-down and get-up cancel each " +
                 "other cleanly — e.g. get-down resets \"GetUp\". Leave empty if there is no opposite.")]
        [SerializeField] private string resetTrigger;

        //==================== METADATA =====================
        public override string Key =>
            string.IsNullOrWhiteSpace(commandKey) ? base.Key : commandKey.Trim().ToLowerInvariant();

        //==================== LIFECYCLE =====================
        private void Reset()
        {
            if (!animator) animator = GetComponentInChildren<Animator>();
        }

        //==================== COMMAND =====================
        protected override void OnExecute()
        {
            if (animator)
            {
                if (!string.IsNullOrEmpty(resetTrigger)) animator.ResetTrigger(resetTrigger);
                if (!string.IsNullOrEmpty(setTrigger)) animator.SetTrigger(setTrigger);
            }

            // One-shot for now: fire the trigger and finish immediately. When posture state matters, this can
            // instead stay active and call Complete() once the Animator reports the transition is done.
            Complete();
        }
    }
}
