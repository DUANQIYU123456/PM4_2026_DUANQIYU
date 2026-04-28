// ============================================================================
// IFocusable — contract
//
// Anything the player (or any other actor) can target with focus — a tree,
// an NPC, a build site, a door, a sign. Small contract on purpose: just
// "tell me whether I am focused right now." Presentation (prompts, outlines,
// highlights) reacts to the events the implementing component fires.
//
// Paired with IHarvestable and friends. A tree is both focusable and
// harvestable; a nameplate NPC is just focusable; a remote-farmed crop is
// just harvestable. Keeping the contracts small is Interface Segregation.
// ============================================================================

namespace Ludocore
{
    /// <summary>Contract for anything that can be the player's focus target.</summary>
    public interface IFocusable
    {
        bool IsFocused { get; }
        void SetFocused(bool focused);
    }
}
