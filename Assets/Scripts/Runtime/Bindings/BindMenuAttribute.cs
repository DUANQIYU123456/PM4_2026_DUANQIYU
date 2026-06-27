using System;

namespace Ludocore
{
    /// <summary>
    /// Places a <see cref="BindTarget"/> subclass in a <see cref="BindingPlayer"/> inspector's
    /// "Add Output" menu under the given slash-separated path, e.g. "Light/Intensity".
    /// Leave a card untagged and it falls back to the class name. Mirror of <see cref="FeedbackMenuAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class BindMenuAttribute : Attribute
    {
        public string Path { get; }
        public BindMenuAttribute(string path) => Path = path;
    }
}
