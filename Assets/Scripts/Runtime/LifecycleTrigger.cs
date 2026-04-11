using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    public class LifecycleTrigger : MonoBehaviour
    {
        public UnityEvent onStart;
        
        void Start()
        {
            onStart.Invoke();
        }
    }
}
