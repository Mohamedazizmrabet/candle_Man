using UnityEngine;

namespace Wickfall.Core.Events
{
    /// <summary>Snapshot of the wax value, sent by WaxSystem every time it changes.</summary>
    public readonly struct WaxChange
    {
        public readonly float Current;
        public readonly float Max;

        public WaxChange(float current, float max)
        {
            Current = current;
            Max = max;
        }

        /// <summary>Wax between 0 and 1.</summary>
        public float Normalized => Max > 0f ? Current / Max : 0f;
    }

    [CreateAssetMenu(menuName = "Wickfall/Events/Wax Event Channel", fileName = "NewWaxEvent")]
    public class WaxEventChannelSO : EventChannelSO<WaxChange> { }
}
