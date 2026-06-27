using UnityEngine;

namespace Ludocore
{
    /// <summary>Drives an IScrubbable (a TransformPlayer or PlaybackControl) from a FloatVariable —
    /// e.g. a MIDI potentiometer mapped to a shared float. Turn the knob and the clip scrubs along
    /// its recorded path in isolation, while the rest of the game keeps running in normal runtime.
    ///
    /// Lifecycle: Engage on enable (puts rigidbody / controller targets on rails; a no-op for a plain
    /// transform), Scrub on every value change, and on disable either HOLD in place (default) or hand
    /// control back. The explicit hand-back — resume free play, or drop the recomposed object into
    /// physics — stays a separate action you trigger when you want it (e.g. PlaybackControl.
    /// ResumeFreeFromHere() or TransformPlayer.ReleaseHeldBodies()).</summary>
    public class ClipScrubDriver : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Source")]
        [Tooltip("Shared float to read the scrub position from (e.g. driven by a MIDI knob). Expected range 0..1.")]
        [SerializeField] private FloatVariable scrubValue;

        [Tooltip("Invert the value (use 1 - v) before scrubbing.")]
        [SerializeField] private bool invert;

        [Header("Target")]
        [Tooltip("What to scrub. Must implement IScrubbable — a TransformPlayer or a PlaybackControl.")]
        [SerializeField] private MonoBehaviour target;

        [Header("Lifecycle")]
        [Tooltip("Engage the target (put it on rails) when this driver is enabled.")]
        [SerializeField] private bool engageOnEnable = true;

        [Tooltip("On disable: OFF = stop and hold in place (target stays on rails); " +
                 "ON = hand control back to the live driver (resume free play / drop physics).")]
        [SerializeField] private bool handBackOnDisable;

        //==================== STATE =====================
        private IScrubbable _target;
        private bool _subscribed;

        public float Value => scrubValue ? scrubValue.Value : 0f;

        //==================== LIFECYCLE =====================
        private void Awake()
        {
            _target = target as IScrubbable;
            if (target && _target == null)
                Debug.LogWarning($"{name}: target '{target.GetType().Name}' does not implement IScrubbable.", this);
        }

        private void OnEnable()
        {
            if (_target == null || !scrubValue) return;

            if (engageOnEnable) _target.Engage();

            scrubValue.OnChanged += OnScrubValueChanged;
            _subscribed = true;

            Apply(scrubValue.Value); // snap to the knob's current position
        }

        private void OnDisable()
        {
            if (_subscribed && scrubValue)
            {
                scrubValue.OnChanged -= OnScrubValueChanged;
                _subscribed = false;
            }

            if (_target == null) return;

            if (handBackOnDisable) _target.Release();
            // else: stop and hold — leave the target parked on rails (we simply stop scrubbing it).
        }

        //==================== PRIVATE =====================
        private void OnScrubValueChanged(float v) => Apply(v);

        private void Apply(float v)
        {
            if (_target == null) return;
            float t = invert ? 1f - v : v;
            _target.Scrub(Mathf.Clamp01(t));
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (target && !(target is IScrubbable))
            {
                Debug.LogWarning($"{name}: assigned target '{target.GetType().Name}' is not IScrubbable. " +
                                 "Assign a TransformPlayer or PlaybackControl.", this);
            }
        }
#endif
    }
}
