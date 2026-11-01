using UnityEngine;

namespace Wickfall.Data
{
    /// <summary>Tuning for Candleman: wax, movement, attack, dash, hurt and death (design doc 2.1).</summary>
    [CreateAssetMenu(menuName = "Wickfall/Config/Player Config", fileName = "PlayerConfig")]
    public class PlayerConfigSO : ScriptableObject
    {
        [Header("Wax")]
        [Min(1f)] public float maxWax = 100f;
        [Min(0f)] public float startWax = 100f;
        [Tooltip("Wax lost every second, even standing still.")]
        [Min(0f)] public float passiveLossPerSecond = 0.5f;

        [Header("Movement")]
        [Tooltip("Units (tiles) per second.")]
        [Min(0f)] public float moveSpeed = 4f;

        [Header("Attack (amber flame, x1.0)")]
        [Min(0f)] public float attackDamage = 10f;
        [Tooltip("Wax spent per swing.")]
        [Min(0f)] public float attackWaxCost = 2f;
        [Min(0.01f)] public float attackDuration = 0.3f;
        [Min(0f)] public float attackCooldown = 0.15f;

        [Header("Dash")]
        [Tooltip("Dash speed = moveSpeed x this.")]
        [Min(1f)] public float dashSpeedMultiplier = 3f;
        [Min(0.01f)] public float dashDuration = 0.15f;
        [Min(0f)] public float dashCooldown = 0.6f;

        [Header("Hurt")]
        [Min(0.01f)] public float hurtDuration = 0.25f;
        [Min(0f)] public float knockbackSpeed = 6f;
        [Tooltip("Invulnerable time after being hit (starts with the hurt state).")]
        [Min(0f)] public float invulnerabilityDuration = 0.6f;

        [Header("Extinguished")]
        [Tooltip("Seconds between the flame going out and respawning at the last brazier.")]
        [Min(0f)] public float respawnDelay = 2f;
    }
}
