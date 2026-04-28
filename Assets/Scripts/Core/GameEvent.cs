// ============================================
// Game Event
// ============================================
// PURPOSE: Named broadcast channel as a ScriptableObject asset. Any script can
//          Raise() the event; any GameEventListener can react. Raiser and
//          listener never reference each other directly.
// USAGE: Create via Assets > Create > Ludocore/Events/Game Event.
//        Drop a GameEventListener on any GameObject and assign this asset to
//        wire a UnityEvent response in the Inspector.
// ============================================

using System.Collections.Generic;
using UnityEngine;

namespace Ludocore
{
    [CreateAssetMenu(fileName = "NewGameEvent", menuName = "Ludocore/Events/Game Event")]
    public class GameEvent : ScriptableObject
    {
        private readonly List<GameEventListener> _listeners = new();

        public void Raise()
        {
            // Iterate backwards — listeners may unregister themselves during the callback.
            for (int i = _listeners.Count - 1; i >= 0; i--)
                _listeners[i].OnEventRaised();
        }

        public void RegisterListener(GameEventListener listener) => _listeners.Add(listener);
        public void UnregisterListener(GameEventListener listener) => _listeners.Remove(listener);
    }
}
