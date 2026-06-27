namespace Ludocore
{
    /// <summary>Something that can be driven along a normalized [0,1] timeline in isolation — a
    /// recorded-clip Player, or a coordinator (e.g. PlaybackControl) that wraps several. Lets one
    /// driver — a UI slider, a FloatVariable, a MIDI potentiometer — scrub any target uniformly.
    ///
    /// Scrub() moves the target along its recorded path WITHOUT touching the rest of the game; the
    /// world keeps simulating in normal runtime, exactly like scrubbing a Cinemachine dolly cart.
    ///
    /// Engage()/Release() exist ONLY for targets that have a competing live driver:
    ///   - a dynamic Rigidbody  -> set kinematic while on rails, restored after,
    ///   - a player controller  -> disabled while the clip drives, re-enabled after.
    /// For a plain transform with nothing else moving it, Engage/Release do nothing and Scrub alone
    /// is enough — that is the dolly-cart case.</summary>
    public interface IScrubbable
    {
        /// <summary>Take the target off any competing live driver so it can be scrubbed in isolation
        /// (e.g. set its Rigidbody kinematic). No-op when nothing competes. Idempotent.</summary>
        void Engage();

        /// <summary>Move to a normalized position [0,1] along the recorded path. Pure: affects only
        /// this target, never the rest of the game.</summary>
        void Scrub(float normalized);

        /// <summary>Hand the target back to its live driver (restore dynamic physics, resume free
        /// control). Pairs with Engage. Safe to call when not engaged.</summary>
        void Release();
    }
}
