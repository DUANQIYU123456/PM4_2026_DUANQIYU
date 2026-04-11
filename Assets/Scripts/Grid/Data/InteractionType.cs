// ============================================
// Interaction Type
// ============================================
// PURPOSE: Defines how two species affect each other when adjacent.
// USAGE:   Referenced by SpeciesInteraction to classify inter-species
//          relationships. Used by GridSimulation to apply energy effects.
// ============================================

public enum InteractionType
{
    Neutral,
    Predatory,
    Symbiotic,
    Competitive
}
