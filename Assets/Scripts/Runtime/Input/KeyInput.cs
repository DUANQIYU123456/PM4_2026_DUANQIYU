using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Fires events when a keyboard or mouse key is pressed or released.</summary>
    public class KeyInput : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("The key to listen for (includes Mouse0-6)")]
        [SerializeField] private KeyCode key = KeyCode.Space;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private bool isHeld;

        public bool IsHeld => isHeld;

        //==================== OUTPUTS =====================
        public event Action OnPressed;
        public event Action OnReleased;

        [Header("Events")]
        [Tooltip("Invoked on the frame the key is pressed down")]
        [SerializeField] private UnityEvent pressedEvent;

        [Tooltip("Invoked on the frame the key is released")]
        [SerializeField] private UnityEvent releasedEvent;

        //==================== LIFECYCLE =====================
        private void Update()
        {
            if (Input.GetKeyDown(key))
            {
                isHeld = true;
                OnPressed?.Invoke();
                pressedEvent?.Invoke();
            }

            if (Input.GetKeyUp(key))
            {
                isHeld = false;
                OnReleased?.Invoke();
                releasedEvent?.Invoke();
            }
        }
    }
}
