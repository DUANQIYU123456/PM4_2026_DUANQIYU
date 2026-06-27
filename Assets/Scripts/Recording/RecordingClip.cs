using UnityEngine;

namespace Ludocore
{
    /// <summary>Base ScriptableObject for any recorded clip. Holds the sample rate and total
    /// duration; subclasses store the typed sample data (transforms, floats, events, etc.).
    /// One Clip asset is written by one Recorder and can be read by many Players.</summary>
    public abstract class RecordingClip : ScriptableObject
    {
        //==================== CONFIG =====================
        [Header("Metadata")]
        [Tooltip("Samples per second used during recording. Set by the Recorder on BeginRecording.")]
        [ReadOnly, SerializeField] protected float sampleRate = 30f;

        [Tooltip("Total length of the clip in seconds. Set by the Recorder on EndRecording.")]
        [ReadOnly, SerializeField] protected float duration;

        public float SampleRate => sampleRate;
        public float SampleInterval => sampleRate > 0f ? 1f / sampleRate : 0f;
        public float Duration => duration;
        public bool HasData => duration > 0f;

        //==================== INPUTS =====================
        /// <summary>Called by a Recorder before sampling starts. Resets data and stores the rate.</summary>
        public virtual void BeginRecording(float rate)
        {
            sampleRate = Mathf.Max(1f, rate);
            duration = 0f;
            ClearData();
        }

        /// <summary>Called by a Recorder when sampling stops, with the final clip length.</summary>
        public virtual void EndRecording(float finalDuration)
        {
            duration = Mathf.Max(0f, finalDuration);
        }

        /// <summary>Erase all data and reset metadata to zero.</summary>
        [ContextMenu("Clear")]
        public void Clear()
        {
            duration = 0f;
            ClearData();
        }

        //==================== PRIVATE =====================
        /// <summary>Subclasses clear their typed sample buffers here.</summary>
        protected abstract void ClearData();
    }
}
