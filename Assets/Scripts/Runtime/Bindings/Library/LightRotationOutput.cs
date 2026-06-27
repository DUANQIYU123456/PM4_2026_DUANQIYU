using UnityEngine;

namespace Ludocore
{
    /// <summary>
    /// Rotates a (directional) Light between two local euler orientations by slerping with the 0..1 master
    /// signal — e.g. sweep the sun across the sky from sunrise to noon. Captures and restores the light's
    /// local rotation on play-exit. The signal (after this card's curve / output range) is the slerp factor,
    /// so leave the output range at 0..1 for a straight sweep.
    /// </summary>
    [BindMenu("Light/Rotation")]
    [System.Serializable]
    public class LightRotationOutput : BindTarget
    {
        [Tooltip("Light to rotate (its transform is driven). Typically a Directional Light acting as the sun.")]
        [SerializeField] private Light targetLight;
        [Tooltip("Local euler rotation at signal 0.")]
        [SerializeField] private Vector3 fromEuler = new Vector3(0f, 0f, 0f);
        [Tooltip("Local euler rotation at signal 1.")]
        [SerializeField] private Vector3 toEuler = new Vector3(90f, 0f, 0f);

        private Quaternion _original;

        public override string DisplayName => "Light Rotation";
        public override Color CardColor => new Color(0.95f, 0.85f, 0.45f);
        public override bool AltersState => true;

        protected override void OnCapture() { if (targetLight) _original = targetLight.transform.localRotation; }
        protected override void OnRestore() { if (targetLight) targetLight.transform.localRotation = _original; }

        protected override void OnApply(float value)
        {
            if (!targetLight) return;
            targetLight.transform.localRotation =
                Quaternion.SlerpUnclamped(Quaternion.Euler(fromEuler), Quaternion.Euler(toEuler), value);
        }
    }
}
