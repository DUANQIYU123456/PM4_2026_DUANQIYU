// ============================================================================
// MirrorRenderer — a true mirror. The capture camera renders the scene REFLECTED
// across the surface plane, so the surface shows a real reflection (left/right
// swapped, text reversed) rather than a fake 180° view.
//
// A reflection is orientation-reversing, so it CANNOT be expressed as a camera
// transform alone — we install a reflection view matrix directly (PostConfigure)
// and flip triangle winding (FlipCulling). Reuses Ludocore/SurfaceScreenSpace.
// ============================================================================

using UnityEngine;

namespace Ludocore
{
    /// <summary>A reflective surface: renders the scene mirrored across its plane.</summary>
    public class MirrorRenderer : SurfaceRenderer
    {
        public enum Axis { Forward, Up, Right }

        //==================== MIRROR PLANE =====================
        [Header("Mirror Plane")]
        [Tooltip("Transform whose plane is the mirror. Empty = the surface's own transform. The normal is auto-flipped to face the player.")]
        [SerializeField] private Transform mirrorPlane;

        [Tooltip("Which local axis of the plane transform is its face normal (a Unity Quad faces -Z, so try Forward first).")]
        [SerializeField] private Axis normalAxis = Axis.Forward;

        [Tooltip("Push the clip plane back into the mirror by this much to hide a seam at the surface.")]
        [SerializeField] private float clipOffset = 0f;

        //==================== STATE =====================
        private Vector3 _planePos;
        private Vector3 _planeNormal;

        private Transform Plane => mirrorPlane ? mirrorPlane : (surface ? surface.transform : transform);

        //==================== HOOKS =====================
        protected override bool FlipCulling => true;

        protected override void ComputeCameraPose(Camera playerCam, out Vector3 pos, out Quaternion rot)
        {
            // Cache the plane (normal facing the player) for PostConfigure + the clip step.
            var pl = Plane;
            _planePos = pl.position;
            Vector3 n = AxisDir(pl, normalAxis);
            if (Vector3.Dot(n, playerCam.transform.position - _planePos) < 0f) n = -n;
            _planeNormal = n.normalized;

            // Cosmetic transform so the capture camera sits at the reflected eye in
            // Scene view; the real render uses the reflection matrix in PostConfigure.
            pos = Reflect(playerCam.transform.position, _planePos, _planeNormal);
            Vector3 fwd = ReflectDir(playerCam.transform.forward, _planeNormal);
            Vector3 up  = ReflectDir(playerCam.transform.up,      _planeNormal);
            rot = Quaternion.LookRotation(fwd, up);
        }

        protected override void PostConfigure(Camera playerCam)
        {
            // Bake the plane reflection into the view matrix — the exact, handedness-
            // reversing transform a transform pose can't represent.
            Matrix4x4 reflection = ReflectionMatrix(_planePos, _planeNormal);
            captureCamera.worldToCameraMatrix = playerCam.worldToCameraMatrix * reflection;
        }

        protected override bool TryGetClipPlane(out Vector3 point, out Vector3 normal)
        {
            normal = _planeNormal;
            point = _planePos + _planeNormal * clipOffset;
            return true;
        }

        //==================== MATH =====================
        private static Vector3 AxisDir(Transform t, Axis a) => a switch
        {
            Axis.Up    => t.up,
            Axis.Right => t.right,
            _          => t.forward,
        };

        private static Vector3 Reflect(Vector3 p, Vector3 planePos, Vector3 n)
            => p - 2f * Vector3.Dot(p - planePos, n) * n;

        private static Vector3 ReflectDir(Vector3 d, Vector3 n)
            => d - 2f * Vector3.Dot(d, n) * n;

        // Householder reflection about the plane through planePos with unit normal n.
        private static Matrix4x4 ReflectionMatrix(Vector3 planePos, Vector3 n)
        {
            float d = -Vector3.Dot(n, planePos); // plane: n·x + d = 0
            Matrix4x4 m = Matrix4x4.identity;
            m.m00 = 1f - 2f * n.x * n.x; m.m01 = -2f * n.x * n.y;     m.m02 = -2f * n.x * n.z;     m.m03 = -2f * d * n.x;
            m.m10 = -2f * n.y * n.x;     m.m11 = 1f - 2f * n.y * n.y; m.m12 = -2f * n.y * n.z;     m.m13 = -2f * d * n.y;
            m.m20 = -2f * n.z * n.x;     m.m21 = -2f * n.z * n.y;     m.m22 = 1f - 2f * n.z * n.z; m.m23 = -2f * d * n.z;
            return m;
        }
    }
}

// ============================================================================
// Setup
//   1. A quad on a "Mirror"/"Portal" layer using a Ludocore/SurfaceScreenSpace
//      material (the same screen-space shader portals use).
//   2. A disabled Camera: Untagged, no AudioListener, Solid black, Culling Mask
//      EXCLUDING the mirror's own layer.
//   3. Add MirrorRenderer to the quad; wire Player Camera, Capture Camera, Surface.
//      Leave Mirror Plane empty to use the quad itself. If the reflection renders
//      inside-out or clipped, switch Normal Axis or nudge Clip Offset.
// ============================================================================
