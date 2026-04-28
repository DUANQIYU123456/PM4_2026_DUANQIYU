// ============================================================================
// FogFocusController — Interaction controller
//
// Listens to a Focusable and tweens RenderSettings.fogDensity up while the
// object is focused, back down when it isn't. Uses DOTween so repeated
// focus/unfocus events interrupt the previous tween cleanly.
// Reads all tuning from a swappable FogFocusProfile asset.
// ============================================================================

using UnityEngine;
using DG.Tweening;

namespace Ludocore
{
    /// <summary>Drives scene fog density based on a Focusable's focus state.</summary>
    public class FogFocusController : MonoBehaviour
    {
        //==================== SCENE REFERENCES =====================
        [Header("Scene References")]
        [Tooltip("Focusable whose state drives the fog density")]
        [SerializeField] private Focusable focusable;

        //==================== PROFILE =====================
        [Header("Profile")]
        [Tooltip("Scriptable object defining densities, durations, and easing")]
        [SerializeField] private FogFocusProfile profile;

        //==================== STATE =====================
        private Tween _tween;

        //==================== LIFECYCLE =====================
        private void OnEnable()
        {
            if (!focusable || !profile) return;

            focusable.OnFocused += HandleFocused;
            focusable.OnUnfocused += HandleUnfocused;

            RenderSettings.fog = true;
            RenderSettings.fogDensity = focusable.IsFocused ? profile.focusedDensity : profile.unfocusedDensity;
        }

        private void OnDisable()
        {
            if (focusable)
            {
                focusable.OnFocused -= HandleFocused;
                focusable.OnUnfocused -= HandleUnfocused;
            }

            _tween?.Kill();
        }

        //==================== HANDLERS =====================
        private void HandleFocused() => TweenTo(profile.focusedDensity, profile.focusDuration);
        private void HandleUnfocused() => TweenTo(profile.unfocusedDensity, profile.unfocusDuration);

        private void TweenTo(float target, float duration)
        {
            if (!profile) return;

            _tween?.Kill();
            _tween = DOTween.To(
                () => RenderSettings.fogDensity,
                v => RenderSettings.fogDensity = v,
                target, duration
            ).SetEase(profile.ease);
        }
    }
}

// ============================================================================
// Setup in a scene
//   1. Add a Focusable to the target object (the one the player looks at).
//   2. Add this component anywhere (e.g. on the same object).
//   3. Assign the Focusable reference.
//   4. Create a FogFocusProfile asset (Create > Ludocore > Fog Focus Profile)
//      and assign it.
//   5. Make sure the scene's fog mode is set to Exponential / Exp Squared in
//      Lighting settings — linear fog uses start/end distance, not density.
// ============================================================================
