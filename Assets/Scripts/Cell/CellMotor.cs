// ============================================
// Cell Motor
// ============================================
// PURPOSE: Moves a cell each frame — wander by default, steered by agents.
// USAGE: Attach to cell root. Assign CellData (provides speed).
//        Other scripts call SetDirection() each frame to override wander.
//        Direction resets every frame — no persistent steering.
// ============================================

using UnityEngine;

public class CellMotor : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private CellData cellData;

    [Header("Wander")]
    [SerializeField] private float wanderDrift = 2f;

    [Header("Boundary")]
    [SerializeField] private Vector3 boundaryCenter;
    [SerializeField] private float boundaryRadius = 20f;

    private Vector3 _wanderDirection;
    private Vector3 _moveDirection;
    private bool _hasDirection;

    public Vector3 MoveDirection => _moveDirection;

    private void OnEnable()
    {
        _wanderDirection = Random.onUnitSphere;
    }

    private void Update()
    {
        if (!_hasDirection)
            Wander();

        _moveDirection = ApplyBoundary(_moveDirection);
        transform.position += _moveDirection * cellData.Speed * Time.deltaTime;

        // Reset each frame — agent must call SetDirection every Update to steer
        _hasDirection = false;
    }

    /// <summary>Set movement direction for this frame. Overrides wander.</summary>
    public void SetDirection(Vector3 direction)
    {
        _moveDirection = direction.normalized;
        _hasDirection = true;
    }

    private Vector3 ApplyBoundary(Vector3 direction)
    {
        Vector3 offset = transform.position - boundaryCenter;
        float distance = offset.magnitude;
        if (distance < boundaryRadius) return direction;

        // Reflect off the boundary — only if heading outward
        Vector3 outward = offset.normalized;
        if (Vector3.Dot(direction, outward) > 0f)
            return Vector3.Reflect(direction, -outward);

        return direction;
    }

    private void Wander()
    {
        // Drift the direction by a small random offset each frame
        _wanderDirection += Random.insideUnitSphere * wanderDrift * Time.deltaTime;
        _wanderDirection = _wanderDirection.normalized;
        _moveDirection = _wanderDirection;
    }
}

// ============================================
// SETUP
// ============================================
// 1. Attach to cell root GameObject
// 2. Assign CellData asset (provides speed)
// 3. Set boundaryCenter and boundaryRadius to define the play area
// 4. Other scripts (CellAgent, CellFlocking) call SetDirection() to steer
// 5. If no direction is set in a frame, the cell wanders randomly
// ============================================
