using System;
using UnityEngine;

namespace Wickfall.Core.Events
{
    /// <summary>Event channel with no payload (player extinguished, player respawned...).</summary>
    [CreateAssetMenu(menuName = "Wickfall/Events/Void Event Channel", fileName = "NewVoidEvent")]
    public class VoidEventChannelSO : ScriptableObject
    {
        [TextArea, SerializeField] private string description;

        public string Description => description;

        public event Action OnRaised;

        public void Raise() => OnRaised?.Invoke();
    }
}
