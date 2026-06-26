// ============================================
// Trigger Zone (3D)
// ============================================
// PURPOSE: Fires events when objects enter/exit a trigger collider
// USAGE: Attach to GameObject with Collider (Is Trigger = true)
// EVENTS:
//   - EnterEvent - fires when object enters
//   - ExitEvent - fires when object exits
// ============================================

using UnityEngine;
using UnityEngine.Events;

public class TriggerZone : MonoBehaviour
{
    // ===== Settings =====
    [Header("Settings")]
    [Tooltip("Only trigger for objects with this tag (leave empty for any)")]
    public string filterTag = "";

    [Tooltip("Delay in seconds before firing events (0 = instant)")]
    public float delay = 0f;

    // ===== Events =====
    [Header("Events")]
    public UnityEvent EnterEvent;
    public UnityEvent ExitEvent;

    // ===== Trigger Logic =====
    private void OnTriggerEnter(Collider other)
    {
        if (string.IsNullOrEmpty(filterTag) || other.CompareTag(filterTag))
        {
            if (delay > 0)
                Invoke(nameof(FireEnter), delay);
            else
                EnterEvent?.Invoke();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (string.IsNullOrEmpty(filterTag) || other.CompareTag(filterTag))
        {
            if (delay > 0)
                Invoke(nameof(FireExit), delay);
            else
                ExitEvent?.Invoke();
        }
    }

    private void FireEnter() => EnterEvent?.Invoke();
    private void FireExit() => ExitEvent?.Invoke();

    // ===== Gizmos =====
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;

        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.DrawWireCube(box.center, box.size);
            return;
        }

        SphereCollider sphere = GetComponent<SphereCollider>();
        if (sphere != null)
        {
            Gizmos.DrawSphere(sphere.center, sphere.radius);
            Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            return;
        }

        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            Gizmos.DrawWireSphere(capsule.center, capsule.radius);
        }
    }
}

// ============================================
// IMPLEMENTATION STEPS
// ============================================
// 1. Attach to GameObject with Collider
// 2. Enable "Is Trigger" on the Collider
// 3. Set filterTag to limit which objects trigger (optional)
// 4. Connect events to Actions in Inspector
// ============================================
