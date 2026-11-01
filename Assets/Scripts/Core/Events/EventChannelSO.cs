using System;
using UnityEngine;

namespace Wickfall.Core.Events
{
    /// <summary>
    /// Base class for ScriptableObject event channels.
    /// A system raises the channel, other systems listen to it, and neither holds a reference to the other.
    /// </summary>
    public abstract class EventChannelSO<T> : ScriptableObject
    {
        [TextArea, SerializeField] private string description;

        public string Description => description;

        public event Action<T> OnRaised;

        public void Raise(T value) => OnRaised?.Invoke(value);
    }
}
