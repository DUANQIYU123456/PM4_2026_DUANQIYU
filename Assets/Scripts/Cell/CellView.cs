// ============================================
// Cell View
// ============================================
// PURPOSE: Visual feedback — syncs a fill image to the cell's energy.
// USAGE: Attach to cell root or canvas child.
//        Reads from CellLifecycle, never writes game state.
// ============================================

using UnityEngine;
using UnityEngine.UI;

public class CellView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CellLifecycle lifecycle;
    [SerializeField] private Image energyFill;

    private void Update()
    {
        if (!lifecycle) return;

        energyFill.fillAmount = lifecycle.EnergyRatio;
    }
}

// ============================================
// SETUP
// ============================================
// 1. Create a world-space Canvas as child of the cell
// 2. Add an Image (Image Type: Filled), choose a fill method
// 3. Attach this script, wire CellLifecycle and the fill Image
// 4. The fill amount tracks EnergyRatio (0 = dead, 1 = full)
// ============================================
