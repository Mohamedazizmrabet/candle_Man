using UnityEngine;

namespace Wickfall.Core
{
    /// <summary>
    /// Smoothly follows the player using PlayerRuntime, so the camera holds no reference to the player.
    /// Put it on the Main Camera next to the Pixel Perfect Camera.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private PlayerRuntimeSO runtime;
        [Tooltip("Offset from the player's feet. Y = 1.5 centers on Candleman's body.")]
        [SerializeField] private Vector2 offset = new Vector2(0f, 1.5f);
        [Tooltip("Seconds to catch up. 0 = locked to the player.")]
        [SerializeField, Min(0f)] private float smoothTime = 0.12f;

        private Vector3 velocity;
        private bool snapped;

        private void LateUpdate()
        {
            if (runtime == null || !runtime.IsRegistered) return;

            var target = new Vector3(runtime.Position.x + offset.x, runtime.Position.y + offset.y, transform.position.z);

            // Snap on the first frame, and after a respawn teleport, instead of sliding across the map.
            if (!snapped || (target - transform.position).sqrMagnitude > 100f)
            {
                transform.position = target;
                velocity = Vector3.zero;
                snapped = true;
                return;
            }

            transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, smoothTime);
        }
    }
}
