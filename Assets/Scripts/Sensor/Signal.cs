using System;
using UnityEngine;

namespace Ludocore
{
    /// <summary>Universal detection result — what was detected and how far away.</summary>
    [Serializable]
    public struct Signal
    {
        public GameObject Object;
        public float Distance;
    }
}
