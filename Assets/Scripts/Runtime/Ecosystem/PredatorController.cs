using System;
using UnityEngine;
using Ludocore;
using UnityEngine.Events;

public class PredatorController : MonoBehaviour
{
     //==================== CONFIG =====================
    [Header("Modules")]
    [Tooltip("Lifecycle component managing this entity's energy.")]
    [SerializeField] private Lifecycle lifecycle;
    [Tooltip("Motor used for NavMesh-based movement.")]
    [SerializeField] private NavMeshMotor motor;
    [Tooltip("Wander behavior used when not fleeing or seeking.")]
    [SerializeField] private NavMeshWander wander;
    [Tooltip("Sensor that detects nearby food targets.")]
    [SerializeField] private Sensor foodSensor;
    [Tooltip("Spawner used to create a replica on replication.")]
    [SerializeField] private Spawner replicationSpawner;
    
    [Header("Replication")]
    [Tooltip("Energy ratio above which replication is triggered.")]
    [Range(0f, 1f)]
    [SerializeField] private float replicationThreshold = 0.8f;
    [Tooltip("Fraction of current energy spent on replication.")]
    [Range(0f, 1f)]
    [SerializeField] private float replicationCost = 0.5f;
    
    //==================== OUTPUTS =====================
    public event Action OnReplicated;

    [Header("Events")]
    [Tooltip("Fired when this entity replicates")]
    [SerializeField] private UnityEvent replicatedEvent;
    
    //==================== STATE =====================
    [Header("Debug")]
    [ReadOnly, SerializeField] private string currentBehavior;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        if (!lifecycle.IsAlive) return;

        CheckReplication();

        if (Seek())
        {
            currentBehavior = "Seek";
        }
        else
        {
            currentBehavior = "Wander";
        }

        wander.enabled = currentBehavior == "Wander";
        
    }
    
    private bool Seek()
    {
        if (!foodSensor.TryGetNearest(out var food)) return false;
        if (!food.Object) return false;

        motor.MoveTo(food.Object.transform.position);
        return true;
    }

    private void CheckReplication()
    {
        if (lifecycle.EnergyRatio < replicationThreshold) return;

        replicationSpawner.Spawn();
        lifecycle.RemoveEnergy(lifecycle.CurrentEnergy * replicationCost);
        OnReplicated?.Invoke();
        replicatedEvent?.Invoke();
    }
}
