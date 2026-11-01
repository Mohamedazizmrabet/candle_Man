using System;
using System.Collections;
using UnityEngine;

namespace Wickfall.Combat
{
    /// <summary>
    /// Health for enemies and breakable things. Raises <see cref="Damaged"/> and <see cref="Died"/>
    /// so the owner (EnemyBrain) reacts with knockback and wax drops. The player uses PlayerHurtbox instead.
    /// </summary>
    public class Damageable : MonoBehaviour, IDamageable
    {
        [SerializeField] private Team team = Team.Enemy;
        [SerializeField, Min(1f)] private float maxHealth = 30f;
        [Tooltip("Short invulnerability after each hit, so one swing never counts twice.")]
        [SerializeField, Min(0f)] private float invulnerabilityAfterHit = 0.1f;

        [Header("Feedback")]
        [Tooltip("Optional. Flashes this color when hit.")]
        [SerializeField] private SpriteRenderer flashRenderer;
        [SerializeField] private Color flashColor = new Color(1f, 0.45f, 0.3f);
        [SerializeField, Min(0f)] private float flashDuration = 0.1f;
        [Tooltip("For test dummies. Enemies leave this off and let EnemyBrain handle death.")]
        [SerializeField] private bool destroyOnDeath;
        [SerializeField] private bool logHits;

        public event Action<DamageInfo> Damaged;
        public event Action<DamageInfo> Died;

        public Team Team => team;
        public float Health { get; private set; }
        public float MaxHealth => maxHealth;
        public bool IsDead { get; private set; }

        private float invulnerableUntil;
        private Color baseColor = Color.white;

        private void Awake()
        {
            Health = maxHealth;
            if (flashRenderer != null) baseColor = flashRenderer.color;
        }

        /// <summary>Lets EnemyBrain apply the value from its EnemyConfig. Also heals to full.</summary>
        public void SetMaxHealth(float value)
        {
            maxHealth = Mathf.Max(1f, value);
            Health = maxHealth;
            IsDead = false;
        }

        public bool TakeHit(DamageInfo hit)
        {
            if (IsDead || hit.SourceTeam == team || Time.time < invulnerableUntil) return false;

            Health = Mathf.Max(0f, Health - hit.Amount);
            invulnerableUntil = Time.time + invulnerabilityAfterHit;
            if (logHits) Debug.Log($"[Wickfall] {name} took {hit.Amount:0.#} ({hit.Flame}), {Health:0.#}/{maxHealth:0.#} left.", this);

            if (flashRenderer != null)
            {
                StopAllCoroutines();
                StartCoroutine(Flash());
            }

            Damaged?.Invoke(hit);

            if (Health <= 0f)
            {
                IsDead = true;
                Died?.Invoke(hit);
                if (destroyOnDeath) Destroy(gameObject);
            }

            return true;
        }

        private IEnumerator Flash()
        {
            flashRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            flashRenderer.color = baseColor;
        }
    }
}
