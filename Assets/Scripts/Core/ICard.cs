using UnityEngine;

namespace Ludocore
{
    /// <summary>
    /// Authoring metadata shared by [SerializeReference] "card" types — <see cref="Feedback"/> (one-shot
    /// timeline steps) and <see cref="BindTarget"/> (continuous binding outputs). Implementing this lets a
    /// single inspector renderer (<c>CardListDrawer</c>) draw any card list as colored, titled cards.
    /// </summary>
    public interface ICard
    {
        /// <summary>Name shown on the card header.</summary>
        string DisplayName { get; }

        /// <summary>Header tint — group related cards by color so a list is readable at a glance.</summary>
        Color CardColor { get; }
    }
}
