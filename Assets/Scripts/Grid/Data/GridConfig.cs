// ============================================
// Grid Config
// ============================================
// PURPOSE: ScriptableObject holding all simulation parameters — grid size,
//          tick rate, species list, and inter-species interactions.
// USAGE:   Create via Assets > Create > Grid > Grid Config. Assign to
//          GridSimulation. Tweak values in Inspector during play mode
//          to see behavior changes on the next tick.
// ============================================

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewGridConfig", menuName = "Grid/Grid Config")]
public class GridConfig : ScriptableObject
{
    [Header("Dimensions")]
    [SerializeField] private int width = 32;
    [SerializeField] private int height = 32;
    [SerializeField] private int depth = 32;

    [Header("Simulation")]
    [SerializeField, Range(0.01f, 2f)] private float tickInterval = 0.15f;
    [SerializeField] private bool wrapEdges = true;
    [SerializeField, Range(0f, 0.5f)] private float seedDensity = 0.08f;

    [Header("Species")]
    [SerializeField] private List<SpeciesConfig> species = new List<SpeciesConfig>();

    [Header("Interactions")]
    [SerializeField] private List<SpeciesInteraction> interactions = new List<SpeciesInteraction>();

    public int Width => width;
    public int Height => height;
    public int Depth => depth;
    public float TickInterval => tickInterval;
    public bool WrapEdges => wrapEdges;
    public float SeedDensity => seedDensity;
    public IReadOnlyList<SpeciesConfig> Species => species;
    public IReadOnlyList<SpeciesInteraction> Interactions => interactions;
}

// ============================================
// SETUP
// ============================================
// 1. Assets > Create > Grid > Grid Config
// 2. Set grid dimensions (32x32x32 is a good default)
// 3. Adjust tick interval (lower = faster simulation)
// 4. Add species assets to the species list
// 5. Define interactions between species pairs
// 6. Assign this config to GridSimulation component
// ============================================
