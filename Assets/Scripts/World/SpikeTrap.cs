using UnityEngine;
using Wickfall.Combat;

namespace Wickfall.World
{
    /// <summary>
    /// Floor spikes on a loop: hidden, a short warning (tips showing), up and dangerous, sinking back down.
    /// While up, its Hitbox hurts the player once per cycle. Watch the warning frame and time your crossing.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpikeTrap : MonoBehaviour
    {
        [Tooltip("4 frames in 0x72's order: holes only, tips showing, half up, fully up.")]
        [SerializeField] private Sprite[] frames;
        [SerializeField] private Hitbox hitbox;

        [Header("Timing (seconds)")]
        [SerializeField, Min(0f)] private float hiddenTime = 1.8f;
        [SerializeField, Min(0f)] private float warningTime = 0.45f;
        [SerializeField, Min(0f)] private float upTime = 0.9f;
        [SerializeField, Min(0f)] private float sinkTime = 0.15f;
        [Tooltip("Shifts the cycle, so a row of traps can ripple instead of firing together.")]
        [SerializeField, Min(0f)] private float phaseOffset;

        private SpriteRenderer spriteRenderer;

        private float Cycle => hiddenTime + warningTime + upTime + sinkTime;

        private void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

        private void Update()
        {
            if (frames == null || frames.Length < 4) return;

            float t = (Time.time + phaseOffset) % Cycle;
            int frame;
            if (t < hiddenTime) frame = 0;                                 // holes only
            else if (t < hiddenTime + warningTime) frame = 1;              // tips: the warning
            else if (t < hiddenTime + warningTime + upTime) frame = 3;     // fully up
            else frame = 2;                                                // half, sinking back

            spriteRenderer.sprite = frames[frame];

            bool dangerous = frame == 3;
            if (hitbox != null && hitbox.gameObject.activeSelf != dangerous) hitbox.gameObject.SetActive(dangerous);
        }

        /// <summary>Used by the level builder to make neighbouring traps fire one after another.</summary>
        public void SetPhase(float seconds) => phaseOffset = seconds;
    }
}
