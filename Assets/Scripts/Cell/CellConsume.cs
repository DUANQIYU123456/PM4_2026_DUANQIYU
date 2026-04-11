// ============================================
// Cell Consume
// ============================================
// PURPOSE: Tracks which objects are within consume range.
// USAGE: Attach to a child object with a trigger SphereCollider.
//        CellAgent queries IsInRange() to decide when to eat.
// ============================================

using System.Collections.Generic;
using UnityEngine;

public class CellConsume : MonoBehaviour
{
    [Header("Gizmos")]
    [SerializeField] private Color gizmoColor = Color.red;

    private readonly HashSet<GameObject> _inRange = new();

    public bool IsInRange(GameObject obj) => _inRange.Contains(obj);

    private void OnTriggerEnter(Collider other)
    {
        _inRange.Add(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        _inRange.Remove(other.gameObject);
    }

    private void LateUpdate()
    {
        // Clean up destroyed objects
        _inRange.RemoveWhere(obj => !obj);
    }

    private void OnDrawGizmosSelected()
    {
        var col = GetComponent<SphereCollider>();
        if (!col) return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, col.radius * transform.lossyScale.x);
    }
}

// ============================================
// SETUP
// ============================================
// 1. Create a child GameObject on the cell (e.g., "Consume")
// 2. Add a SphereCollider (isTrigger: true), radius smaller than sense range
// 3. Attach this script to the child
// 4. CellAgent checks IsInRange() before consuming a target
// ============================================
