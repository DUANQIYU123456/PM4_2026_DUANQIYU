// ============================================================================
// IInteractable — contract
//
// The polymorphic dispatch contract for the player's interact key. Anything
// the player can act on — harvestables, build sites, doors, chests — agrees
// to expose a Mode (Press or Hold), a Duration, and Begin / Cancel methods.
//
// PlayerInteractor reads only this interface to drive ALL interactions:
//   Press → key-down completes immediately (Duration = 0).
//   Hold  → key-down begins, releasing or looking away cancels, the
//           implementing component ticks progress to 1.0 and completes.
//
// Verb-specific data (resource yield, build cost, etc.) lives on the
// matching verb interface (IHarvestable, IBuildable). Keeping interaction
// dispatch separate from verb data follows Interface Segregation — the
// interactor stays verb-agnostic, and any future verb plugs in by just
// implementing IInteractable.
// ============================================================================

using System;

namespace Ludocore
{
    /// <summary>How the player input drives this interaction.
    /// Press = single key-down completes immediately.
    /// Hold  = key must be held until Duration elapses; releasing or looking
    /// away cancels and resets progress.</summary>
    public enum InteractionMode { Press, Hold }

    /// <summary>Contract for anything the player can act on with the interact key.
    /// Press-vs-Hold and timing live on the implementing component, so PlayerInteractor
    /// can stay verb-agnostic and dispatch any future verb without modification.</summary>
    public interface IInteractable
    {
        InteractionMode Mode { get; }
        float Duration { get; }
        bool CanInteract { get; }

        /// <summary>Fired each frame the interaction is in progress (0..1).
        /// Press-mode implementations may never invoke this — the radial UI
        /// gracefully ignores them.</summary>
        event Action<float> OnProgress;

        /// <summary>Begin the interaction. For Press, completes immediately.
        /// For Hold, starts the timer that ticks progress to 1.0.</summary>
        void BeginInteract();

        /// <summary>Cancel an in-flight interaction. No-op for Press.</summary>
        void CancelInteract();
    }
}
