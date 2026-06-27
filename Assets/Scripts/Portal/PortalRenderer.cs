// ============================================================================
// PortalRenderer — a traversable portal. The capture camera is re-emitted through
// PortalIn → PortalOut so the door surface shows a parallax-correct view of the
// destination. An oblique near plane at PortalOut clips geometry between the exit
// and the camera. Pair with a Teleport (Relative mode) to walk through.
//
// All the camera/RT/surface machinery lives in SurfaceRenderer; this subclass
// only answers "where does the capture camera go" + "where is the clip plane".
// ============================================================================

using UnityEngine;

namespace Ludocore
{
    /// <summary>A door surface showing a parallax-correct view of another part of
    /// the scene, via rigid re-emission through an anchor pair.</summary>
    public class PortalRenderer : SurfaceRenderer
    {
        //==================== ANCHORS =====================
        [Header("Portal Anchors")]
        [Tooltip("Anchor on this side (the door). Forward points INTO the door from the player's side. Falls back to this transform.")]
        [SerializeField] private Transform portalIn;

        [Tooltip("Anchor at the destination. Forward points OUT of the destination — the way the player faces after walking through.")]
        [SerializeField] private Transform portalOut;

        //==================== OBLIQUE NEAR PLANE =====================
        [Header("Oblique Near Plane")]
        [Tooltip("Push the oblique near-plane forward into the destination. A small nudge (0.001–0.01) hides z-fighting at the exit seam.")]
        [SerializeField] private float nearPlaneOffset = 0f;

        //==================== HOOKS =====================
        protected override void ComputeCameraPose(Camera playerCam, out Vector3 pos, out Quaternion rot)
        {
            var inAnchor = portalIn ? portalIn : transform;

            // Express the player camera in PortalIn's frame, re-emit through PortalOut:
            // the capture camera ends up offset from PortalOut exactly as the player
            // camera is offset from PortalIn.
            Matrix4x4 m = portalOut.localToWorldMatrix
                          * inAnchor.worldToLocalMatrix
                          * playerCam.transform.localToWorldMatrix;

            pos = m.GetColumn(3);
            rot = m.rotation;
        }

        protected override bool TryGetClipPlane(out Vector3 point, out Vector3 normal)
        {
            // Plane at PortalOut, auto-oriented so its normal points away from wherever
            // the capture camera ended up — the half-space the destination scene is on.
            Vector3 toCam = captureCamera.transform.position - portalOut.position;
            float side = Mathf.Sign(-Vector3.Dot(portalOut.forward, toCam));
            if (side == 0f) side = 1f; // degenerate: camera exactly on the plane

            normal = portalOut.forward * side;
            point = portalOut.position + normal * nearPlaneOffset;
            return true;
        }

        protected override bool Active => base.Active && portalOut;
    }
}

// ============================================================================
// Setup
//   1. PortalIn (forward = into the door) with a child quad surface on a "Portal"
//      layer, using a Ludocore/SurfaceScreenSpace material.
//   2. PortalOut at the destination (forward = the way the player exits, level).
//   3. A disabled Camera: Untagged, no AudioListener, Solid black, Culling Mask
//      EXCLUDING the Portal layer (no recursion).
//   4. Add PortalRenderer and wire Player Camera, Capture Camera, Surface, and the
//      two anchors. Nudge Near Plane Offset by ~0.005 if you see a seam.
//   5. For walk-through, add a Teleport on PortalIn (Relative mode, Use Blackout
//      off) with Entry Anchor = PortalIn, Destination = PortalOut.
// ============================================================================
