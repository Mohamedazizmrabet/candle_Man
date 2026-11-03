using UnityEngine;

namespace Wickfall.UI
{
    /// <summary>
    /// The glowing frame on the selected menu entry. It must share a parent with the entries:
    /// it copies the target's anchors, so it takes the entry's exact place and size.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MenuHighlight : MonoBehaviour
    {
        public void MoveTo(RectTransform entry)
        {
            var rect = (RectTransform)transform;
            rect.anchorMin = entry.anchorMin;
            rect.anchorMax = entry.anchorMax;
        }
    }
}
