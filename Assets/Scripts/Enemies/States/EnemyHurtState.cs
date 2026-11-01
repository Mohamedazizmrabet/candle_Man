using UnityEngine;

namespace Wickfall.Enemies
{
    /// <summary>Knocked back by a hit. Afterwards it knows where the player is (even from the dark) and chases.</summary>
    public class EnemyHurtState : EnemyState
    {
        private Vector2 sourcePosition;
        private Vector2 knockback;
        private float elapsed;

        public EnemyHurtState(EnemyBrain brain) : base(brain) { }

        /// <summary>Set by EnemyBrain before entering.</summary>
        public void SetSource(Vector2 position) => sourcePosition = position;

        public override void Enter()
        {
            elapsed = 0f;
            brain.SetAttackHitbox(false);
            Vector2 away = brain.Position - sourcePosition;
            if (away.sqrMagnitude < 0.0001f) away = Vector2.up;
            knockback = away.normalized * brain.KnockbackSpeed;
        }

        public override void Tick(float deltaTime)
        {
            elapsed += deltaTime;
            if (elapsed < brain.HurtDuration) return;

            brain.Perception.NoticePlayer();
            brain.ChangeState(brain.Chase);
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            float fade = brain.HurtDuration > 0f ? 1f - Mathf.Clamp01(elapsed / brain.HurtDuration) : 0f;
            brain.SetVelocity(knockback * fade);
        }
    }
}
