using UnityEngine;

namespace Wickfall.Data
{
    /// <summary>Tuning for one enemy type. Phase 1 has one asset: DriplingConfig.</summary>
    [CreateAssetMenu(menuName = "Wickfall/Config/Enemy Config", fileName = "EnemyConfig")]
    public class EnemyConfigSO : ScriptableObject
    {
        [Header("Health and damage")]
        [Min(1f)] public float maxHealth = 30f;
        [Tooltip("Wax the player loses per hit.")]
        [Min(0f)] public float attackDamage = 10f;

        [Header("Perception (design doc 2.3)")]
        [Tooltip("Detection range = base + player light radius x perRadius, then x the player's detection multiplier.")]
        [Min(0f)] public float detectionBase = 2f;
        [Min(0f)] public float detectionPerRadius = 1.5f;
        [Tooltip("How long the enemy keeps chasing the last known position after losing the player.")]
        [Min(0f)] public float memoryDuration = 3f;

        [Header("Movement")]
        [Min(0f)] public float wanderSpeed = 1f;
        [Min(0f)] public float chaseSpeed = 2.2f;
        [Min(0f)] public float returnSpeed = 1.5f;
        [Tooltip("How far from its spawn point the enemy wanders.")]
        [Min(0f)] public float wanderRadius = 3f;
        public Vector2 wanderPause = new Vector2(1f, 2.5f);

        [Header("States")]
        [Tooltip("Pause after spotting the player, before chasing.")]
        [Min(0f)] public float alertDuration = 0.6f;
        [Tooltip("Distance between the enemy's and the player's positions (feet) to start an attack. Must be larger than the two colliders touching.")]
        [Min(0f)] public float attackRange = 1.2f;
        [Tooltip("Telegraph time before the hit lands.")]
        [Min(0f)] public float attackWindup = 0.4f;
        [Min(0f)] public float attackCooldown = 1.2f;
        [Tooltip("Gives up and returns home past this distance from its spawn.")]
        [Min(0f)] public float leashDistance = 10f;

        [Header("Reward (design doc 2.3: 4 to 15 wax)")]
        [Min(0)] public int waxDropMin = 4;
        [Min(0)] public int waxDropMax = 15;

        /// <summary>Detection range for the player's current light radius and detection multiplier.</summary>
        public float DetectionRange(float lightRadius, float detectionMultiplier) =>
            (detectionBase + lightRadius * detectionPerRadius) * detectionMultiplier;
    }
}
