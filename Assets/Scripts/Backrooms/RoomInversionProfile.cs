using UnityEngine;

namespace Ludocore
{
    /// <summary>Tuning profile for RoomInversionController — stagger settings.</summary>
    [CreateAssetMenu(menuName = "Ludocore/Room Inversion Profile")]
    public class RoomInversionProfile : ScriptableObject
    {
        //==================== STAGGER =====================
        [Header("Stagger")]
        [Tooltip("If true, tiles invert sequentially with staggerDelay between them. If false, all tiles invert on the same frame.")]
        public bool staggered = true;

        [Tooltip("Wait between consecutive tile Invert() calls when staggered = true (sec)")]
        [Min(0f)]
        public float staggerDelay = 0.1f;
    }
}
