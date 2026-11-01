using System.Collections.Generic;
using UnityEngine;

namespace Wickfall.Combat
{
    /// <summary>
    /// A trigger zone that deals damage while its GameObject is active. Each target is hit once per activation,
    /// or again every <see cref="rehitInterval"/> seconds for hazards that stay on.
    /// The owner (player attack, enemy attack) switches it on and off and can set <see cref="Damage"/>.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Hitbox : MonoBehaviour
    {
        [SerializeField] private Team team = Team.Player;
        [SerializeField, Min(0f)] private float damage = 10f;
        [SerializeField] private FlameType flame = FlameType.Amber;
        [Tooltip("0 = each target once per activation. Above 0 = hit the same target again after this many seconds.")]
        [SerializeField, Min(0f)] private float rehitInterval;
        [Tooltip("Where the hit comes from, for knockback. Empty = the root object.")]
        [SerializeField] private Transform owner;

        private readonly Dictionary<IDamageable, float> lastHitTime = new Dictionary<IDamageable, float>();

        public float Damage
        {
            get => damage;
            set => damage = value;
        }

        public Team Team => team;

        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void Awake()
        {
            if (owner == null) owner = transform.root;
        }

        private void OnEnable() => lastHitTime.Clear();

        private void OnTriggerEnter2D(Collider2D other) => TryHit(other);

        private void OnTriggerStay2D(Collider2D other)
        {
            if (rehitInterval > 0f) TryHit(other);
        }

        private void TryHit(Collider2D other)
        {
            var target = other.GetComponentInParent<IDamageable>();
            if (target == null || target.Team == team) return;

            if (lastHitTime.TryGetValue(target, out float time))
            {
                if (rehitInterval <= 0f || Time.time - time < rehitInterval) return;
            }

            if (target.TakeHit(new DamageInfo(damage, owner.position, team, flame)))
            {
                lastHitTime[target] = Time.time;
            }
        }
    }
}
