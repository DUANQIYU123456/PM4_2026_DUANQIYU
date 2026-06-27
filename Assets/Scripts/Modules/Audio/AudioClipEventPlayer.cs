using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Watches an AudioSource's playhead and fires events when it crosses markers defined in an AudioClipEvents asset.</summary>
    public class AudioClipEventPlayer : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Source whose playhead is watched.")]
        [SerializeField] private AudioSource source;

        [Tooltip("Marker set to evaluate against playback.")]
        [SerializeField] private AudioClipEvents clipEvents;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private float prevTime = -1f;
        [ReadOnly, SerializeField] private bool wasPlaying;

        //==================== OUTPUTS =====================
        public event Action<AudioMarker> OnMarker;

        [Header("Events")]
        [Tooltip("Invoked whenever a marker is crossed, with the marker's id as payload.")]
        [SerializeField] private UnityEvent<string> markerEvent;

        //==================== LIFECYCLE =====================
        private void Update()
        {
            if (source == null || clipEvents == null || source.clip == null) return;

            if (!source.isPlaying)
            {
                wasPlaying = false;
                return;
            }

            float now = source.time;

            if (!wasPlaying)
            {
                // Fresh start / restart / seek-back resets the playhead, so we want markers from t=0 to fire.
                // A resume-from-pause keeps source.time at the pause point, so we keep prevTime and avoid re-firing.
                if (prevTime < 0f || now < prevTime)
                    prevTime = -1f;
                wasPlaying = true;
            }

            if (now < prevTime)
            {
                // Looped or seeked backwards.
                if (source.loop)
                {
                    FireBetween(prevTime, source.clip.length);
                    FireBetween(-1f, now);
                }
                prevTime = now;
                return;
            }

            FireBetween(prevTime, now);
            prevTime = now;
        }

        //==================== PRIVATE =====================
        private void FireBetween(float fromExclusive, float toInclusive)
        {
            var markers = clipEvents.Markers;
            if (markers == null) return;
            for (int i = 0; i < markers.Count; i++)
            {
                var m = markers[i];
                if (m.time > fromExclusive && m.time <= toInclusive)
                {
                    OnMarker?.Invoke(m);
                    markerEvent?.Invoke(m.id);
                    if (m.channel != null) m.channel.Raise();
                }
            }
        }
    }
}
