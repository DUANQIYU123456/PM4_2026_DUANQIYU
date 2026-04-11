// ============================================
// Cell Lifecycle
// ============================================
// PURPOSE: Manages a cell's energy — decay over time, death at zero.
// USAGE: Attach to cell root. Assign a CellData asset.
//        Energy starts at CellData.StartingEnergy, decays each frame.
//        Other systems call AddEnergy() to feed the cell.
// ============================================

using UnityEngine;
using Ludocore;

public class CellLifecycle : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private CellData cellData;

    [Header("Debug")]
    [ReadOnly, SerializeField] private float currentEnergy;

    public float CurrentEnergy => currentEnergy;
    public float EnergyRatio => currentEnergy / cellData.StartingEnergy;
    public bool IsAlive => currentEnergy > 0f;

    private void OnEnable()
    {
        currentEnergy = cellData.StartingEnergy;
    }

    private void Update()
    {
        Decay();
        CheckDeath();
    }

    public void AddEnergy(float amount)
    {
        if (amount <= 0f) return;

        currentEnergy += amount;
    }

    private void Decay()
    {
        currentEnergy -= cellData.EnergyDecayRate * Time.deltaTime;
    }

    private void CheckDeath()
    {
        if (currentEnergy > 0f) return;

        Destroy(gameObject);
    }
}

// ============================================
// SETUP
// ============================================
// 1. Attach to cell root GameObject
// 2. Assign CellData asset (provides startingEnergy and energyDecayRate)
// 3. Other systems call AddEnergy() to feed the cell (e.g., after consuming)
// 4. Read IsAlive, CurrentEnergy, or EnergyRatio from other scripts
// ============================================
