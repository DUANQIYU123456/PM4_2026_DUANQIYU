using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ludocore
{
    /// <summary>One pose sample — position, rotation, scale at a single point in time.</summary>
    [Serializable]
    public struct TransformSample
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public static TransformSample Identity => new()
        {
            position = Vector3.zero,
            rotation = Quaternion.identity,
            scale = Vector3.one
        };
    }

    /// <summary>One track = the pose timeline of one Transform. Sample times are implicit:
    /// sample[i] is captured at time i * sampleInterval.</summary>
    [Serializable]
    public class TransformTrack
    {
        [Tooltip("Diagnostic name (target.name at record time).")]
        public string label;

        [Tooltip("Captured samples in order.")]
        public List<TransformSample> samples = new();

        public int Count => samples.Count;
    }

    /// <summary>A recorded clip of transform poses, one track per Transform.
    /// Captured by TransformRecorder, played back by TransformPlayer.</summary>
    [CreateAssetMenu(fileName = "NewTransformClip", menuName = "Ludocore/Recording/Transform Clip")]
    public class TransformClip : RecordingClip
    {
        //==================== CONFIG =====================
        [Header("Space")]
        [Tooltip("Coordinate space the samples were captured in. " +
                 "World survives reparenting (good for detached fragments). " +
                 "Self stores values relative to each Transform's parent (good for parented hierarchies).")]
        [ReadOnly, SerializeField] private Space space = Space.World;

        [Header("Tracks")]
        [SerializeField] private List<TransformTrack> tracks = new();

        public Space Space => space;
        public int TrackCount => tracks.Count;
        public IReadOnlyList<TransformTrack> Tracks => tracks;

        //==================== INPUTS =====================
        /// <summary>Initialize a fresh recording. Allocates the requested number of empty tracks.</summary>
        public void BeginRecording(float rate, int trackCount, Space recordingSpace, IReadOnlyList<string> labels = null)
        {
            base.BeginRecording(rate);
            space = recordingSpace;
            tracks.Clear();
            tracks.Capacity = Mathf.Max(tracks.Capacity, trackCount);
            for (int i = 0; i < trackCount; i++)
            {
                var track = new TransformTrack
                {
                    label = (labels != null && i < labels.Count) ? labels[i] : $"track_{i}"
                };
                tracks.Add(track);
            }
        }

        /// <summary>Append one sample to track i. Caller must call this in track-index order
        /// once per recorded frame so all tracks stay aligned.</summary>
        public void AppendSample(int trackIndex, TransformSample sample)
        {
            if (trackIndex < 0 || trackIndex >= tracks.Count) return;
            tracks[trackIndex].samples.Add(sample);
        }

        /// <summary>Interpolate the sample on track i at time t (seconds, 0..Duration).
        /// Linear for position/scale, Slerp for rotation. Returns Identity if track is empty.</summary>
        public TransformSample Evaluate(int trackIndex, float t)
        {
            if (trackIndex < 0 || trackIndex >= tracks.Count) return TransformSample.Identity;

            var samples = tracks[trackIndex].samples;
            int n = samples.Count;
            if (n == 0) return TransformSample.Identity;
            if (n == 1) return samples[0];

            float interval = SampleInterval;
            if (interval <= 0f) return samples[0];

            float frame = t / interval;
            int i0 = Mathf.Clamp(Mathf.FloorToInt(frame), 0, n - 1);
            int i1 = Mathf.Clamp(i0 + 1, 0, n - 1);

            if (i0 == i1) return samples[i0];

            float u = Mathf.Clamp01(frame - i0);
            var s0 = samples[i0];
            var s1 = samples[i1];

            return new TransformSample
            {
                position = Vector3.LerpUnclamped(s0.position, s1.position, u),
                rotation = Quaternion.SlerpUnclamped(s0.rotation, s1.rotation, u),
                scale    = Vector3.LerpUnclamped(s0.scale, s1.scale, u)
            };
        }

        //==================== PRIVATE =====================
        protected override void ClearData()
        {
            tracks.Clear();
        }
    }
}
