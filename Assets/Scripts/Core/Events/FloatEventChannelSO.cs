using UnityEngine;

namespace Wickfall.Core.Events
{
    /// <summary>Event channel carrying a float (wax damage or wax gain requests).</summary>
    [CreateAssetMenu(menuName = "Wickfall/Events/Float Event Channel", fileName = "NewFloatEvent")]
    public class FloatEventChannelSO : EventChannelSO<float> { }
}
