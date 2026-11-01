using UnityEngine;

namespace Wickfall.Core.Events
{
    /// <summary>Event channel carrying a position (a brazier rested at, carrying the respawn point).</summary>
    [CreateAssetMenu(menuName = "Wickfall/Events/Vector3 Event Channel", fileName = "NewVector3Event")]
    public class Vector3EventChannelSO : EventChannelSO<Vector3> { }
}
