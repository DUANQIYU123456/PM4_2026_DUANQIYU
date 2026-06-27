// ============================================
// Audio Distortion Binding
// ============================================
// PURPOSE: Drives a Unity AudioDistortionFilter's distortionLevel from a
//          FloatVariable source (typically a 0..1 MIDI knob, remapped via
//          inputRange / outputRange).
// USAGE: Assign 'source' to a FloatVariable an upstream MIDI component writes to.
//        Assign 'target' or leave empty to use the AudioDistortionFilter on this
//        GameObject (auto-added via RequireComponent). distortionLevel is native
//        [0, 1], so the default outputRange already covers the full sweep.
// ============================================

using UnityEngine;

namespace Ludocore
{
    [RequireComponent(typeof(AudioDistortionFilter))]
    public class AudioDistortionBinding : Binding
    {
        //==================== CONFIG =====================
        [Header("Target")]
        [Tooltip("AudioDistortionFilter to drive. If empty, the one on this GameObject is used.")]
        [SerializeField] private AudioDistortionFilter target;

        //==================== METADATA =====================
        public override string Description => "Drives an AudioDistortionFilter's distortionLevel from a source. Native range [0, 1].";

        //==================== LIFECYCLE =====================
        private void Reset()
        {
            if (!target) target = GetComponent<AudioDistortionFilter>();
        }

        protected override void Awake()
        {
            base.Awake();
            if (!target) target = GetComponent<AudioDistortionFilter>();
        }

        //==================== BINDING =====================
        protected override void Apply(float value)
        {
            if (target) target.distortionLevel = value;
        }
    }
}
