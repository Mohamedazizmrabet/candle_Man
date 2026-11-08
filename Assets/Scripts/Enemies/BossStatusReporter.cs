using UnityEngine;
using Wickfall.Combat;
using Wickfall.Core.Events;

namespace Wickfall.Enemies
{
    /// <summary>
    /// Put on a boss: tells the boss health bar (through the BossStatusChanged channel) its name, health,
    /// and whether the fight is on. The fight starts when the boss notices the player and ends when the boss
    /// dies or loses track of him.
    /// </summary>
    public class BossStatusReporter : MonoBehaviour
    {
        [SerializeField] private string bossName = "Demon Lord";
        [SerializeField] private Damageable damageable;
        [SerializeField] private EnemyPerception perception;

        [Header("Broadcasts")]
        [SerializeField] private BossStatusEventChannelSO bossStatusChanged;

        private bool engaged;
        private float lastHealth = -1f;

        private void Reset()
        {
            damageable = GetComponent<Damageable>();
            perception = GetComponent<EnemyPerception>();
        }

        private void OnEnable()
        {
            if (damageable != null) damageable.Died += OnDied;
        }

        private void OnDisable()
        {
            if (damageable != null) damageable.Died -= OnDied;
            // The boss is gone (or the scene is unloading): hide the bar.
            if (engaged) Report(false);
        }

        private void Update()
        {
            if (damageable == null || damageable.IsDead) return;

            bool nowEngaged = perception != null && perception.RemembersPlayer;
            if (nowEngaged != engaged || !Mathf.Approximately(lastHealth, damageable.Health)) Report(nowEngaged);
        }

        private void OnDied(DamageInfo hit) => Report(false);

        private void Report(bool isEngaged)
        {
            engaged = isEngaged;
            lastHealth = damageable != null ? damageable.Health : 0f;
            if (bossStatusChanged != null)
            {
                bossStatusChanged.Raise(new BossStatus(bossName, lastHealth, damageable != null ? damageable.MaxHealth : 1f, isEngaged));
            }
        }
    }
}
