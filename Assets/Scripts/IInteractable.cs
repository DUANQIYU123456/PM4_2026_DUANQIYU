// ============================================================================
// IInteractable — contract
//
// The polymorphic dispatch contract for the player's interact key. Anything
// the player can act on — grabbables, doors, buttons, switches — exposes this
// pair: a CanInteract gate and a BeginInteract action that fires once per
// key-down.
//
// PlayerInteractor reads only this interface to drive ALL interactions: on
// key-down, if the focused target reports CanInteract, BeginInteract runs.
// Verb-specific data (carry physics, toggle state, activation events) lives
// on the implementing component, so the interactor stays verb-agnostic and
// any future verb plugs in by just implementing IInteractable.
// ============================================================================
namespace Ludocore
{
    /// <summary>Contract for anything the player can act on with the interact key.
    /// Single key-down dispatches BeginInteract; the implementing component owns
    /// any state (toggle, one-shot, carry).</summary>
    public interface IInteractable
    {
        bool CanInteract { get; }

        /// <summary>Fired once per key-down while CanInteract is true.</summary>
        void BeginInteract();
    }
}
