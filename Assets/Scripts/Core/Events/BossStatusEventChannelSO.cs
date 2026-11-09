using UnityEngine;

namespace Wickfall.Core.Events
{
    /// <summary>What the boss health bar needs to know, sent by the boss whenever it changes.</summary>
    public readonly struct BossStatus
    {
        public readonly string Name;
        public readonly float Health;
        public readonly float MaxHealth;
        /// <summary>True while the fight is on (the boss has noticed the player and is alive).</summary>
        public readonly bool Engaged;

        public BossStatus(string name, float health, float maxHealth, bool engaged)
        {
            Name = name;
            Health = health;
            MaxHealth = maxHealth;
            Engaged = engaged;
        }

        public float Normalized => MaxHealth > 0f ? Health / MaxHealth : 0f;
    }

    [CreateAssetMenu(menuName = "Wickfall/Events/Boss Status Event Channel", fileName = "NewBossStatusEvent")]
    public class BossStatusEventChannelSO : EventChannelSO<BossStatus> { }
}
