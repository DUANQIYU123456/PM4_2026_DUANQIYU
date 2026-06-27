// ============================================================================
// FocusView — Focus reaction (highlight)
//
// Listens to a Focusable and makes a set of renderers glow while focused.
// "Glow" = the material's emission is switched on and driven from the object's
// OWN colour (base colour × intensity), so the object keeps its identity and
// just reads as lit-up — no tinting, no outline, no shader to write.
//
// Instant on / off: emission snaps up on focus and snaps back on unfocus.
// Sibling to VideoFocusController / FogFocusController — same idiom: the
// Focusable owns the bit, this owns one reaction.
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace Ludocore
{
    /// <summary>Drives emission glow on a set of renderers from a Focusable's focus state.</summary>
    public class FocusView : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Focusable whose state drives the glow. Defaults to one on this GameObject.")]
        [SerializeField] private Focusable focusable;

        [Tooltip("Renderers that light up while focused. All material slots on each are driven.")]
        [SerializeField] private Renderer[] renderers;

        [Tooltip("How strongly the renderers glow while focused. Multiplies each material's own " +
                 "colour, so the glow keeps the object's colour. >1 blooms if Bloom post-fx is on.")]
        [Min(0f)]
        [SerializeField] private float glowIntensity = 2f;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private bool isHighlighted;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private const string EmissionKeyword = "_EMISSION";

        private struct GlowTarget
        {
            public Material material;
            public Color originalEmission;
            public bool hadEmission;
            public Color glowColor;
        }

        private readonly List<GlowTarget> _targets = new();
        private bool _built;

        //==================== LIFECYCLE =====================
        private void Awake()
        {
            if (!focusable) TryGetComponent(out focusable);
            Build();
        }

        private void OnEnable()
        {
            if (!focusable) return;

            focusable.OnFocused += HandleFocused;
            focusable.OnUnfocused += HandleUnfocused;

            // Reflect whatever the focus state already is.
            Apply(focusable.IsFocused);
        }

        private void OnDisable()
        {
            if (focusable)
            {
                focusable.OnFocused -= HandleFocused;
                focusable.OnUnfocused -= HandleUnfocused;
            }

            // Don't leave anything stuck glowing when we go away.
            Apply(false);
        }

        //==================== HANDLERS =====================
        private void HandleFocused() => Apply(true);
        private void HandleUnfocused() => Apply(false);

        //==================== PRIVATE =====================
        private void Build()
        {
            if (_built) return;
            _built = true;

            if (renderers == null) return;

            foreach (Renderer r in renderers)
            {
                if (!r) continue;

                // .materials returns per-renderer instances — we never touch the shared asset.
                foreach (Material mat in r.materials)
                {
                    if (!mat) continue;

                    Color baseColor = ReadBaseColor(mat);
                    Color original = mat.HasProperty(EmissionColorId) ? mat.GetColor(EmissionColorId) : Color.black;

                    _targets.Add(new GlowTarget
                    {
                        material = mat,
                        originalEmission = original,
                        hadEmission = mat.IsKeywordEnabled(EmissionKeyword),
                        glowColor = baseColor * glowIntensity
                    });
                }
            }
        }

        private void Apply(bool glow)
        {
            isHighlighted = glow;

            foreach (GlowTarget t in _targets)
            {
                if (!t.material) continue;

                if (glow)
                {
                    t.material.EnableKeyword(EmissionKeyword);
                    t.material.SetColor(EmissionColorId, t.glowColor);
                }
                else
                {
                    t.material.SetColor(EmissionColorId, t.originalEmission);
                    if (!t.hadEmission) t.material.DisableKeyword(EmissionKeyword);
                }
            }
        }

        private static Color ReadBaseColor(Material mat)
        {
            if (mat.HasProperty(BaseColorId)) return mat.GetColor(BaseColorId);  // URP/HDRP Lit
            if (mat.HasProperty(ColorId)) return mat.GetColor(ColorId);          // Built-in / Standard
            return Color.white;                                                  // fallback: glow white
        }
    }
}

// ============================================================================
// Setup in a scene
//   1. Add a Focusable to the object the player looks at (PlayerInteractor
//      flips its focus bit automatically).
//   2. Add this FocusView on the same object (or anywhere) and:
//      - leave Focusable empty to auto-grab the one on this GameObject, or
//        assign it explicitly;
//      - drag the Renderer(s) that should glow into Renderers;
//      - tune Glow Intensity (1 = match the object's colour, >1 = brighter /
//        blooms when Bloom post-fx is enabled).
//   3. That's it — emission snaps on while focused, snaps back when not.
//
// Notes
//   - The glow uses each material's own base colour, so colours aren't changed.
//   - Materials are instanced per-renderer at runtime (like Unity's
//     Renderer.materials), so the shared material asset is never modified.
// ============================================================================
