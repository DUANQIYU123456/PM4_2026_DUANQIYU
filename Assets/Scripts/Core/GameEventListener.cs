// ============================================
// Game Event Listener
// ============================================
// PURPOSE: Bridges a GameEvent ScriptableObject to a UnityEvent response in the
//          Inspector. Place on any GameObject that should react to a channel.
// USAGE: Add this component, assign the GameEvent asset, then wire any number
//          of methods to the response UnityEvent in the Inspector.
// ============================================

using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    public class GameEventListener : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("The event channel this listener subscribes to.")]
        [SerializeField] private GameEvent gameEvent;

        //==================== OUTPUTS =====================
        [Header("Events")]
        [Tooltip("Invoked when the assigned GameEvent is raised.")]
        [SerializeField] private UnityEvent response;

        //==================== LIFECYCLE =====================
        private void OnEnable() => gameEvent.RegisterListener(this);
        private void OnDisable() => gameEvent.UnregisterListener(this);

        //==================== INPUTS =====================
        public void OnEventRaised() => response.Invoke();
    }
}
