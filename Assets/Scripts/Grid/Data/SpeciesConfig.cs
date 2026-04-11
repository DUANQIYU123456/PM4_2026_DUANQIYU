// ============================================
// Species Config
// ============================================
// PURPOSE: ScriptableObject defining one species' appearance and CA rules.
// USAGE:   Create one asset per species via Assets > Create > Grid > Species Config.
//          Referenced by GridConfig's species list and by the simulation
//          to evaluate birth, survival, and energy dynamics.
// ============================================

using UnityEngine;

[CreateAssetMenu(fileName = "NewSpecies", menuName = "Grid/Species Config")]
public class SpeciesConfig : ScriptableObject
{
    [Header("Appearance")]
    [SerializeField] private Color color = Color.cyan;
    [SerializeField] private Color emissionColor = Color.cyan * 0.5f;

    [Header("Birth")]
    [SerializeField, Tooltip("Exact neighbor count needed to birth this species in an empty cell.")]
    private int birthCount = 4;

    [SerializeField, Range(0f, 1f), Tooltip("Energy a new cell starts with.")]
    private float birthEnergy = 0.5f;

    [Header("Survival")]
    [SerializeField] private int survivalMin = 4;
    [SerializeField] private int survivalMax = 7;

    [Header("Energy Rates")]
    [SerializeField, Range(0f, 1f), Tooltip("Energy gained per tick when in survival range.")]
    private float recoveryRate = 0.08f;

    [SerializeField, Range(0f, 1f), Tooltip("Energy lost per tick when outside survival range.")]
    private float decayRate = 0.15f;

    public Color Color => color;
    public Color EmissionColor => emissionColor;
    public int BirthCount => birthCount;
    public float BirthEnergy => birthEnergy;
    public int SurvivalMin => survivalMin;
    public int SurvivalMax => survivalMax;
    public float RecoveryRate => recoveryRate;
    public float DecayRate => decayRate;
}

// ============================================
// SETUP
// ============================================
// 1. Assets > Create > Grid > Species Config
// 2. Set color and emission color for visual identity
// 3. Tune birth count (how many neighbors spawn a new cell)
// 4. Set survival range (neighbor count for healthy cells)
// 5. Adjust recovery/decay rates for energy dynamics
// 6. Add to GridConfig's species list
// ============================================
