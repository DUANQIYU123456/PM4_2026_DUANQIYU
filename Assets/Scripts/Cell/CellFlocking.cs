// ============================================
// Cell Flocking
// ============================================
// PURPOSE: Boids-style flocking — blends cohesion, alignment, and
//          separation for neighbors marked as Flock in CellBehavior.
// USAGE: Attach to cell root alongside CellAgent.
//        Runs before CellAgent ([DefaultExecutionOrder(-1)]).
//        CellAgent can override flocking if a higher-priority action exists.
// ============================================

using UnityEngine;
using Ludocore;

[DefaultExecutionOrder(-1)]
public class CellFlocking : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private CellBehavior cellBehavior;

    [Header("References")]
    [SerializeField] private CellMotor motor;
    [SerializeField] private TriggerSensor sensor;

    [Header("Separation")]
    [SerializeField] private SphereCollider bodyCollider;

    [Header("Weights")]
    [SerializeField] private float cohesionWeight = 1f;
    [SerializeField] private float alignmentWeight = 1f;
    [SerializeField] private float separationWeight = 1.5f;

    private void Update()
    {
        var signals = sensor.Signals;
        if (signals.Count == 0) return;

        Vector3 cohesion = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 separation = Vector3.zero;
        float separationRadius = bodyCollider.radius * transform.lossyScale.x;
        int flockCount = 0;

        for (int i = 0; i < signals.Count; i++)
        {
            var signal = signals[i];
            if (!signal.Object) continue;

            var otherAgent = signal.Object.GetComponent<CellAgent>();
            if (!otherAgent) continue;

            var rule = cellBehavior.GetRule(otherAgent.Data);
            if (rule != InteractionAction.Flock) continue;

            var otherMotor = signal.Object.GetComponent<CellMotor>();
            if (!otherMotor) continue;

            flockCount++;

            // Cohesion — steer toward flockmate
            cohesion += signal.Object.transform.position - transform.position;

            // Alignment — match flockmate's heading
            alignment += otherMotor.MoveDirection;

            // Separation — push away if too close
            if (signal.Distance > 0f && signal.Distance < separationRadius)
            {
                Vector3 away = transform.position - signal.Object.transform.position;
                separation += away.normalized / signal.Distance;
            }
        }

        if (flockCount == 0) return;

        cohesion = cohesion.normalized * cohesionWeight;
        alignment = alignment.normalized * alignmentWeight;
        separation = separation.normalized * separationWeight;

        Vector3 flockDirection = (cohesion + alignment + separation).normalized;
        motor.SetDirection(flockDirection);
    }
}

// ============================================
// SETUP
// ============================================
// 1. Attach to cell root GameObject
// 2. Assign CellBehavior, CellMotor, TriggerSensor, and body SphereCollider
// 3. In CellBehavior, set target species to Flock action
// 4. Tune weights: cohesion, alignment, separation (separation > 1 recommended)
// 5. Separation radius is derived from the body collider size
// ============================================
