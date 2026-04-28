using UnityEngine;

namespace Ludocore
{
    /// <summary>Maps an IntVariable's value to a color from a small palette and
    /// writes that color directly to a shared material.</summary>
    public class TeleportStateController : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("IntVariable to observe. Controller subscribes to OnChanged.")]
        [SerializeField] private IntVariable source;

        [Tooltip("Colors applied when source.Value becomes 1, 2, or 3. " +
                 "Values outside [1, palette.Length] are ignored.")]
        [SerializeField] private Color[] palette = new Color[3];

        [Tooltip("Shared material to recolor. Writes affect the asset, so any " +
                 "renderer using it updates instantly.")]
        [SerializeField] private Material sharedMaterial;

        [Tooltip("Shader color property to set on the shared material (e.g. _BaseColor, _EmissionColor).")]
        [SerializeField] private string propertyName = "_BaseColor";

        //==================== LIFECYCLE =====================
        private void OnEnable()
        {
            if (source) source.OnChanged += HandleChanged;
        }

        private void OnDisable()
        {
            if (source) source.OnChanged -= HandleChanged;
        }

        //==================== PRIVATE =====================
        private void HandleChanged(int value)
        {
            if (value < 1 || value > palette.Length) return;
            if (!sharedMaterial || !sharedMaterial.HasProperty(propertyName)) return;

            sharedMaterial.SetColor(propertyName, palette[value - 1]);
        }
    }
}
