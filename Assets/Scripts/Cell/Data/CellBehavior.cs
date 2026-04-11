// ============================================
// Cell Behavior
// ============================================
// PURPOSE: Defines how a cell reacts to other cell types.
// USAGE: Create via Assets > Create > Data > CellBehavior.
//        Add InteractionRules mapping each target CellData to an action.
//        Queried by CellAgent and CellFlocking at runtime.
// ============================================

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCellBehavior", menuName = "Data/CellBehavior")]
public class CellBehavior : ScriptableObject
{
    [SerializeField] private List<InteractionRule> interactionRules = new();

    public IReadOnlyList<InteractionRule> InteractionRules => interactionRules;

    public InteractionAction? GetRule(CellData targetType)
    {
        for (int i = 0; i < interactionRules.Count; i++)
        {
            if (interactionRules[i].targetType == targetType)
                return interactionRules[i].action;
        }
        return null;
    }
}

// ============================================
// SETUP
// ============================================
// 1. Create asset: Right-click > Create > Data > CellBehavior
// 2. Add rules: for each species this cell interacts with,
//    set the target CellData and the InteractionAction
// 3. Assign to CellAgent and CellFlocking on the cell prefab
// 4. Unmatched targets return null — the agent ignores them
// ============================================
