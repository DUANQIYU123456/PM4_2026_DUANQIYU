// ============================================
// Cell Agent
// ============================================
// PURPOSE: The cell's brain — reads sensor data, picks the best action,
//          and steers the motor or consumes targets.
// USAGE: Attach to cell root. Wire all references in Inspector.
//        Runs the Sense > Think > Act loop each frame.
//        Priorities: Consume > Repel > Attract > Flock/Ignore.
// ============================================

using UnityEngine;
using Ludocore;

public class CellAgent : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private CellData cellData;
    [SerializeField] private CellBehavior cellBehavior;

    [Header("References")]
    [SerializeField] private CellMotor motor;
    [SerializeField] private CellLifecycle lifecycle;
    [SerializeField] private TriggerSensor sensor;
    [SerializeField] private CellConsume consume;

    public CellData Data => cellData;

    private void Update()
    {
        if (!lifecycle.IsAlive) return;

        Decide();
    }

    private void Decide()
    {
        Signal? bestTarget = null;
        InteractionAction bestAction = default;
        int bestPriority = -1;
        float bestDistance = float.MaxValue;

        var signals = sensor.Signals;
        for (int i = 0; i < signals.Count; i++)
        {
            var signal = signals[i];
            if (!signal.Object) continue;

            var otherAgent = signal.Object.GetComponent<CellAgent>();
            if (!otherAgent) continue;

            var rule = cellBehavior.GetRule(otherAgent.Data);
            if (rule == null) continue;

            int priority = GetPriority(rule.Value);

            if (priority > bestPriority || (priority == bestPriority && signal.Distance < bestDistance))
            {
                bestTarget = signal;
                bestAction = rule.Value;
                bestPriority = priority;
                bestDistance = signal.Distance;
            }
        }

        if (bestTarget == null) return;

        Execute(bestTarget.Value, bestAction);
    }

    private void Execute(Signal target, InteractionAction action)
    {
        if (!target.Object) return;

        Vector3 direction = target.Object.transform.position - transform.position;

        switch (action)
        {
            case InteractionAction.Consume:
                if (consume.IsInRange(target.Object))
                {
                    ConsumeTarget(target.Object);
                    return;
                }
                // Too far — chase it
                motor.SetDirection(direction);
                break;

            case InteractionAction.Attract:
                motor.SetDirection(direction);
                break;

            case InteractionAction.Repel:
                motor.SetDirection(-direction);
                break;
        }
    }

    private void ConsumeTarget(GameObject target)
    {
        var targetLifecycle = target.GetComponent<CellLifecycle>();
        if (!targetLifecycle) return;

        lifecycle.AddEnergy(targetLifecycle.CurrentEnergy);
        Destroy(target);
    }

    private int GetPriority(InteractionAction action)
    {
        return action switch
        {
            InteractionAction.Consume => 2,
            InteractionAction.Repel => 1,
            InteractionAction.Attract => 0,
            InteractionAction.Flock => -1,
            InteractionAction.Ignore => -1,
            _ => -1
        };
    }
}

// ============================================
// SETUP
// ============================================
// 1. Attach to cell root GameObject
// 2. Assign CellData, CellBehavior, CellMotor, CellLifecycle,
//    TriggerSensor, and CellConsume references
// 3. CellBehavior rules define what this cell does to each target type
// 4. The agent picks the highest-priority action from detected signals
// 5. On consume: transfers target's energy, then destroys it
// ============================================
