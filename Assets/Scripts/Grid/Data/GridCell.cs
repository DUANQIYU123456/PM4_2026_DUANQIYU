// ============================================
// Grid Cell
// ============================================
// PURPOSE: Lightweight value type representing one cell in the 3D grid.
// USAGE:   Stored in flat arrays inside GridSimulation. speciesIndex 0
//          means empty; energy ranges from 0 to 1.
// ============================================

[System.Serializable]
public struct GridCell
{
    public int speciesIndex;
    public float energy;

    public bool IsAlive => speciesIndex > 0;

    public static GridCell Empty => new GridCell { speciesIndex = 0, energy = 0f };

    public GridCell(int speciesIndex, float energy)
    {
        this.speciesIndex = speciesIndex;
        this.energy = energy;
    }
}
