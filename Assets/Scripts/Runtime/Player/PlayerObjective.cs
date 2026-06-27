using System.Collections.Generic;
using UnityEngine;

namespace Ludocore
{
    /// <summary>
    /// Marks a world position as a player objective (an escape point / goal / waypoint). Self-registers into a
    /// static list on enable so <see cref="PlayerObjectiveCommand"/> can find objectives without scene scans.
    /// Authoring: drop this on an empty GameObject at each goal. Optional <see cref="order"/> sequences them;
    /// toggle <see cref="active"/> to retire one (e.g. a used exit).
    /// </summary>
    public class PlayerObjective : MonoBehaviour
    {
        //==================== REGISTRY =====================
        private static readonly List<PlayerObjective> _all = new();
        /// <summary>Live list of enabled objectives. Snapshot before mutating mid-iteration.</summary>
        public static IReadOnlyList<PlayerObjective> All => _all;

        //==================== CONFIG =====================
        [Tooltip("Optional label (for future 'go to <label>' commands). Not required for nearest/next selection.")]
        [SerializeField] private string label = "";
        [Tooltip("Lower = earlier when a command picks the 'next' objective in order. Ties break by distance.")]
        [SerializeField] private int order = 0;
        [Tooltip("If false, this objective is ignored (e.g. an exit that's been used).")]
        [SerializeField] private bool active = true;

        //==================== QUERIES =====================
        public string Label => label;
        public int Order => order;
        public bool Active => active;
        public Vector3 Position => transform.position;

        //==================== INPUTS =====================
        public void SetActive(bool value) => active = value;

        //==================== LIFECYCLE =====================
        private void OnEnable()
        {
            if (!_all.Contains(this)) _all.Add(this);
        }

        private void OnDisable()
        {
            _all.Remove(this);
        }

        //==================== GIZMOS =====================
        private void OnDrawGizmos()
        {
            Gizmos.color = active ? new Color(1f, 0.85f, 0.2f, 0.9f) : new Color(0.5f, 0.5f, 0.5f, 0.6f);
            Vector3 p = transform.position;
            Gizmos.DrawWireSphere(p, 0.5f);
            Gizmos.DrawLine(p, p + Vector3.up * 2f);
            Gizmos.DrawWireCube(p + Vector3.up * 2f, new Vector3(0.4f, 0.4f, 0.4f));
        }
    }
}
