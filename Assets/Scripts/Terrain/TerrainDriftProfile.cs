using UnityEngine;

namespace Ludocore
{
    /// <summary>Tuning profile for TerrainDrift — slides the hill pattern over a
    /// duration via an animated additive noise offset.</summary>
    [CreateAssetMenu(menuName = "Ludocore/Terrain Drift Profile")]
    public class TerrainDriftProfile : ScriptableObject
    {
        //==================== TWEEN =====================
        [Header("Tween")]
        [Tooltip("How long the drift takes (seconds)")]
        [Min(0.01f)]
        public float duration = 2f;

        [Tooltip("Drives the tween parameter (X = normalised time 0..1, Y = drift progress 0..1). Output 0 = no drift, output 1 = full drift.")]
        public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        //==================== DRIFT =====================
        [Header("Drift")]
        [Tooltip("Direction in noise-space to slide the hill pattern. Normalised at runtime.")]
        public Vector2 direction = new Vector2(1f, 0f);

        [Tooltip("Total noise-space distance travelled at curve = 1.")]
        public float distance = 100f;

        //==================== THROTTLING =====================
        [Header("Throttling")]
        [Tooltip("Minimum gap between heightmap rebuilds (seconds). 0 = rebuild every frame.")]
        [Min(0f)]
        public float updateInterval = 0.1f;
    }
}
