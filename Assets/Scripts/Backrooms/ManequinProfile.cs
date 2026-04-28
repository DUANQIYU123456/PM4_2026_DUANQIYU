using UnityEngine;

namespace Ludocore
{
    /// <summary>Tuning profile for ManequinController — teleport timer, ceiling detection,
    /// and emission settings.</summary>
    [CreateAssetMenu(menuName = "Ludocore/Manequin Profile")]
    public class ManequinProfile : ScriptableObject
    {
        //==================== TELEPORT TIMER =====================
        [Header("Teleport Timer")]
        [Tooltip("Master enable for random-interval teleports")]
        public bool useTimer = true;

        [Tooltip("Lower bound of random wait between teleports (sec)")]
        [Min(0f)]
        public float minInterval = 4f;

        [Tooltip("Upper bound of random wait between teleports (sec)")]
        [Min(0f)]
        public float maxInterval = 10f;

        //==================== CEILING DETECTION =====================
        [Header("Ceiling Detection")]
        [Tooltip("Which colliders count as ceiling tiles")]
        public LayerMask layerMask = ~0;

        [Tooltip("XZ size of the overlap box (world units)")]
        public Vector2 footprintSize = new Vector2(1f, 1f);

        [Tooltip("Y offset from mannequin origin where the overlap box starts")]
        public float verticalOffset = 2f;

        [Tooltip("Y size of the overlap box")]
        [Min(0.01f)]
        public float detectionHeight = 1f;

        //==================== EMISSION =====================
        [Header("Emission")]
        [Tooltip("Base emission color applied to lit tiles (HDR)")]
        [ColorUsage(false, true)]
        public Color emissionColor = Color.white;

        [Tooltip("Exponent applied as Mathf.Pow(2, x) × emissionColor")]
        public float brightIntensity = 3f;
    }
}
