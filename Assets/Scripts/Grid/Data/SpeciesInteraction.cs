// ============================================
// Species Interaction
// ============================================
// PURPOSE: Defines the relationship between two species — how they
//          affect each other's energy when adjacent in the grid.
// USAGE:   Configured in GridConfig's interaction list. The simulation
//          looks up interactions by species pair each tick.
// ============================================

using UnityEngine;

[System.Serializable]
public struct SpeciesInteraction
{
    [SerializeField] private SpeciesConfig speciesA;
    [SerializeField] private SpeciesConfig speciesB;
    [SerializeField] private InteractionType type;
    [SerializeField, Range(0f, 1f)] private float strength;

    public SpeciesConfig SpeciesA => speciesA;
    public SpeciesConfig SpeciesB => speciesB;
    public InteractionType Type => type;
    public float Strength => strength;
}
