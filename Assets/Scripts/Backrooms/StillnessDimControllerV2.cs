using UnityEngine;

namespace Ludocore
{
    /// <summary>Detects player stillness and asks the LightManager to Dim or Brighten.
    /// This controller owns only the detection logic — no tweens, no lit/dim values, no curves.
    /// The lighting "feel" (values, timing, shape) lives on the LightManager's profile.
    ///
    /// Manager/controller split: the manager owns WHAT the lights do and WHEN state changes mean
    /// in lighting terms. The controller owns WHY the state should change — here, because the
    /// player stopped moving.</summary>
    public class StillnessDimControllerV2 : MonoBehaviour
    {
        //==================== SCENE REFERENCES =====================
        [Header("Scene References")]
        [Tooltip("The player's CharacterController (reads velocity)")]
        [SerializeField] private CharacterController playerController;

        [Tooltip("Light manager to drive. Falls back to LightManager.Instance if empty.")]
        [SerializeField] private LightManager lightManager;

        //==================== DETECTION =====================
        [Header("Detection")]
        [Tooltip("Speed below which the player counts as still (CharacterController velocity magnitude)")]
        [Min(0f)]
        [SerializeField] private float stillnessThreshold = 0.1f;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private bool isStill;

        private bool _initialized;

        //==================== LIFECYCLE =====================
        private void Update()
        {
            if (!playerController) return;

            bool still = playerController.velocity.magnitude < stillnessThreshold;

            if (!_initialized)
            {
                isStill = still;
                _initialized = true;
                return;
            }

            if (still == isStill) return;
            isStill = still;

            var lm = Manager;
            if (!lm) return;

            if (isStill) lm.Dim();
            else lm.Brighten();
        }

        //==================== PRIVATE =====================
        private LightManager Manager => lightManager ? lightManager : LightManager.Instance;
    }
}
