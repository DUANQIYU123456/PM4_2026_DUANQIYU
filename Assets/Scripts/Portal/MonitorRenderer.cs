// ============================================================================
// MonitorRenderer — a CCTV screen / TV. A camera placed somewhere in the scene
// films from a FIXED viewpoint (its own transform, its own FOV), and the feed is
// UV-mapped onto the surface like a texture — same picture from any viewing angle,
// no parallax. Use a Ludocore/SurfaceUV material on the surface.
//
// Unlike Portal/Mirror, the capture camera's pose does NOT depend on the player:
// it stays where you placed it (optionally aimed at a target).
// ============================================================================

using UnityEngine;

namespace Ludocore
{
    /// <summary>A surface that displays a fixed remote camera's feed, UV-mapped.</summary>
    public class MonitorRenderer : SurfaceRenderer
    {
        //==================== MONITOR =====================
        [Header("Monitor")]
        [Tooltip("Optional target the capture camera aims at each frame. Empty = keep the camera's placed rotation (static CCTV).")]
        [SerializeField] private Transform lookTarget;

        //==================== HOOKS =====================
        // Keep the capture camera's own lens — a monitor is not parallax-locked to the player.
        protected override bool CopyPlayerProjection => false;

        protected override void ComputeCameraPose(Camera playerCam, out Vector3 pos, out Quaternion rot)
        {
            // The camera is placed deliberately (the CCTV viewpoint) — keep its position.
            var t = captureCamera.transform;
            pos = t.position;
            rot = lookTarget
                ? Quaternion.LookRotation(lookTarget.position - t.position, Vector3.up)
                : t.rotation;
        }

        // No oblique clip plane, no winding flip — both default off.
    }
}

// ============================================================================
// Setup
//   1. A surface (quad/plane) using a Ludocore/SurfaceUV material. Its UVs decide
//      how the feed maps; the feed inherits the screen aspect ratio.
//   2. A disabled Camera placed at the viewpoint you want to film: Untagged, no
//      AudioListener, set its FOV/clip as desired.
//   3. Add MonitorRenderer to the surface; wire Player Camera, Capture Camera,
//      Surface. Optionally set Look Target to make it track something.
//   If the feed is upside down, enable Flip Vertically on the SurfaceUV material.
// ============================================================================
