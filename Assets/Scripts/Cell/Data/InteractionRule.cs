// ============================================
// Interaction Rule
// ============================================
// Maps a target CellData type to an InteractionAction.
// Serialized in CellBehavior as a list of rules.
// ============================================

using System;
using UnityEngine;

[Serializable]
public struct InteractionRule
{
    public CellData targetType;
    public InteractionAction action;
}
