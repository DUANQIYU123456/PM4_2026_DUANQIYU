using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ludocore
{
    /// <summary>Time-position marker inside an AudioClip.</summary>
    [Serializable]
    public struct AudioMarker
    {
        [Tooltip("Identifier for this marker (passed as the UnityEvent payload and useful for debugging).")]
        public string id;

        [Tooltip("Position in the clip, in seconds, at which to fire.")]
        [Min(0f)] public float time;

        [Tooltip("Optional GameEvent channel raised when this marker is crossed.")]
        public GameEvent channel;
    }

    /// <summary>Data-only list of time-position markers for an AudioClip. Played at runtime by AudioClipEventPlayer.</summary>
    [CreateAssetMenu(fileName = "NewClipEvents", menuName = "Ludocore/Audio/Clip Events")]
    public class AudioClipEvents : ScriptableObject
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Clip these markers belong to. Reference only — the player uses whatever clip is on its AudioSource.")]
        [SerializeField] private AudioClip clip;

        [Tooltip("Markers fired when the playhead crosses them during playback.")]
        [SerializeField] private AudioMarker[] markers;

        public AudioClip Clip => clip;
        public IReadOnlyList<AudioMarker> Markers => markers;
    }
}
