using UnityEngine;
using UnityEngine.InputSystem;
using Wickfall.Core.Events;
using Wickfall.Player;

namespace Wickfall.DebugTools
{
    /// <summary>
    /// Test keys for the wax loop (same keys on AZERTY and QWERTY):
    /// K = take a hit, L = gain wax through the WaxGainRequested channel, R = refill and relight.
    /// Remove from the player before a build.
    /// </summary>
    public class WaxDebugKeys : MonoBehaviour
    {
        [SerializeField] private WaxSystem waxSystem;
        [Tooltip("Optional. When set, K is a real hit (knockback, blink, invulnerability) coming from in front of the player.")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private FloatEventChannelSO waxGainRequested;
        [SerializeField, Min(0f)] private float amount = 10f;

        private void Reset()
        {
            waxSystem = GetComponent<WaxSystem>();
            playerController = GetComponent<PlayerController>();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || waxSystem == null) return;

            if (keyboard.kKey.wasPressedThisFrame) Hit();
            if (keyboard.lKey.wasPressedThisFrame && waxGainRequested != null) waxGainRequested.Raise(amount);
            if (keyboard.rKey.wasPressedThisFrame) waxSystem.Refill();
        }

        private void Hit()
        {
            if (playerController != null && playerController.enabled)
            {
                playerController.ApplyHit(amount, (Vector2)transform.position + playerController.Facing);
            }
            else
            {
                waxSystem.TakeDamage(amount);
            }
        }
    }
}
