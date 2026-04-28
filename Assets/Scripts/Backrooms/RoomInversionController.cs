using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Scene-level orchestrator that triggers Invert() on every
    /// TileInversionController in the scene, optionally with a stagger.</summary>
    public class RoomInversionController : MonoBehaviour
    {
        //==================== PROFILE =====================
        [Header("Profile")]
        [Tooltip("Scriptable object with stagger settings")]
        [SerializeField] private RoomInversionProfile profile;

        //==================== EVENTS =====================
        [Header("Events")]
        [Tooltip("Fired when Play() begins. Wire to StillnessDimController.Dim() to darken the room.")]
        [SerializeField] private UnityEvent onPlayStart;

        [Tooltip("Fired after the last tile's flip animation finishes. Wire to StillnessDimController.Brighten() to restore lighting.")]
        [SerializeField] private UnityEvent onPlayComplete;

        //==================== STATE =====================
        private readonly List<TileInversionController> _tiles = new();
        private Coroutine _staggerRoutine;
        private Coroutine _completeRoutine;

        //==================== LIFECYCLE =====================
        private void Start()
        {
            Refresh();
        }

        private void OnDisable()
        {
            StopStagger();
            StopComplete();
        }

        //==================== INPUTS =====================
        /// <summary>Dispatch Invert() to every cached tile. Staggered or
        /// instantaneous per profile. Fires onPlayStart immediately and
        /// onPlayComplete after the last tile's animation finishes.</summary>
        [ContextMenu("Play")]
        public void Play()
        {
            if (!profile || _tiles.Count == 0) return;

            onPlayStart?.Invoke();

            if (!profile.staggered || profile.staggerDelay <= 0f)
            {
                for (int i = 0; i < _tiles.Count; i++)
                    if (_tiles[i]) _tiles[i].Invert();
            }
            else
            {
                _staggerRoutine = StartCoroutine(StaggerLoop());
            }

            ScheduleComplete();
        }

        /// <summary>Re-scan the scene for TileInversionController instances.
        /// Call this after spawning tiles at runtime.</summary>
        [ContextMenu("Refresh")]
        public void Refresh()
        {
            _tiles.Clear();
            var found = FindObjectsByType<TileInversionController>(FindObjectsSortMode.None);
            _tiles.AddRange(found);
            _tiles.Sort(CompareByHierarchy);
        }

        //==================== PRIVATE =====================
        private IEnumerator StaggerLoop()
        {
            var wait = new WaitForSeconds(profile.staggerDelay);
            for (int i = 0; i < _tiles.Count; i++)
            {
                if (_tiles[i]) _tiles[i].Invert();
                if (i < _tiles.Count - 1) yield return wait;
            }
            _staggerRoutine = null;
        }

        private void ScheduleComplete()
        {
            StopComplete();

            float staggerTotal = (profile.staggered && profile.staggerDelay > 0f)
                ? Mathf.Max(0, _tiles.Count - 1) * profile.staggerDelay
                : 0f;

            float maxFlip = 0f;
            for (int i = 0; i < _tiles.Count; i++)
                if (_tiles[i]) maxFlip = Mathf.Max(maxFlip, _tiles[i].InvertDuration);

            float total = staggerTotal + maxFlip;
            _completeRoutine = StartCoroutine(CompleteAfter(total));
        }

        private IEnumerator CompleteAfter(float seconds)
        {
            if (seconds > 0f) yield return new WaitForSeconds(seconds);
            onPlayComplete?.Invoke();
            _completeRoutine = null;
        }

        private void StopStagger()
        {
            if (_staggerRoutine != null)
            {
                StopCoroutine(_staggerRoutine);
                _staggerRoutine = null;
            }
        }

        private void StopComplete()
        {
            if (_completeRoutine != null)
            {
                StopCoroutine(_completeRoutine);
                _completeRoutine = null;
            }
        }

        private static int CompareByHierarchy(TileInversionController a, TileInversionController b)
        {
            if (!a && !b) return 0;
            if (!a) return 1;
            if (!b) return -1;

            var ta = a.transform;
            var tb = b.transform;

            var pathA = BuildHierarchyPath(ta);
            var pathB = BuildHierarchyPath(tb);

            int len = Mathf.Min(pathA.Count, pathB.Count);
            for (int i = 0; i < len; i++)
            {
                int cmp = pathA[i].CompareTo(pathB[i]);
                if (cmp != 0) return cmp;
            }
            return pathA.Count.CompareTo(pathB.Count);
        }

        private static List<int> BuildHierarchyPath(Transform t)
        {
            var path = new List<int>();
            while (t != null)
            {
                path.Add(t.GetSiblingIndex());
                t = t.parent;
            }
            path.Reverse();
            return path;
        }
    }
}
