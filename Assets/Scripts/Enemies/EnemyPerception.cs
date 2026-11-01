using UnityEngine;
using Wickfall.Core;
using Wickfall.Data;

namespace Wickfall.Enemies
{
    /// <summary>
    /// Sees the player through their light (design doc 2.3): range = 2 + lightRadius x 1.5,
    /// times the player's detection multiplier (0.4 while the wick is trimmed).
    /// Reads PlayerRuntime only, and remembers the last known position for a few seconds.
    /// </summary>
    public class EnemyPerception : MonoBehaviour
    {
        [SerializeField] private EnemyConfigSO config;
        [SerializeField] private PlayerRuntimeSO runtime;
        [Tooltip("Optional. Layers that block sight (walls). Nothing = no line-of-sight check.")]
        [SerializeField] private LayerMask sightBlockers;

        public bool CanSeePlayer { get; private set; }
        public Vector2 LastKnownPosition { get; private set; }
        public float TimeSinceSeen { get; private set; } = float.PositiveInfinity;
        public float CurrentRange { get; private set; }

        public bool RemembersPlayer => TimeSinceSeen <= config.memoryDuration;
        public bool PlayerAlive => runtime != null && runtime.IsRegistered && runtime.IsAlive;
        public Vector2 PlayerPosition => runtime.Position;

        public float DistanceToPlayer => Vector2.Distance(transform.position, runtime.Position);

        /// <summary>Updates what the enemy sees. EnemyBrain calls it every frame before ticking its state.</summary>
        public void Sense(float deltaTime)
        {
            if (config == null || !PlayerAlive)
            {
                // An extinguished player cannot be seen, and is forgotten at once.
                CanSeePlayer = false;
                CurrentRange = 0f;
                TimeSinceSeen = float.PositiveInfinity;
                return;
            }

            CurrentRange = config.DetectionRange(runtime.LightRadius, runtime.DetectionMultiplier);
            CanSeePlayer = DistanceToPlayer <= CurrentRange && HasLineOfSight();

            if (CanSeePlayer) NoticePlayer();
            else TimeSinceSeen += deltaTime;
        }

        /// <summary>Marks the player's current position as known (seen, or just hit us from the dark).</summary>
        public void NoticePlayer()
        {
            if (!PlayerAlive) return;
            LastKnownPosition = runtime.Position;
            TimeSinceSeen = 0f;
        }

        private bool HasLineOfSight()
        {
            if (sightBlockers.value == 0) return true;
            return !Physics2D.Linecast(transform.position, runtime.Position, sightBlockers);
        }

        private void OnDrawGizmosSelected()
        {
            if (config == null) return;

            if (Application.isPlaying)
            {
                Gizmos.color = CanSeePlayer ? Color.red : new Color(1f, 0.6f, 0.2f);
                Gizmos.DrawWireSphere(transform.position, CurrentRange);
                if (RemembersPlayer) Gizmos.DrawLine(transform.position, LastKnownPosition);
                return;
            }

            // In edit mode: range against a full-wax player (outer) and a trimmed full-wax player (inner).
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, config.DetectionRange(6f, 1f));
            Gizmos.color = new Color(0.4f, 0.6f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, config.DetectionRange(6f * 0.35f, 0.4f));
        }
    }
}
