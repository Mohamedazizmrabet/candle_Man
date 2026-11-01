using UnityEngine;

namespace Wickfall.Player
{
    /// <summary>Knocked back away from the hit source. No control until it ends.</summary>
    public class PlayerHurtState : PlayerState
    {
        private Vector2 sourcePosition;
        private Vector2 knockback;
        private float elapsed;

        public PlayerHurtState(PlayerController player) : base(player) { }

        /// <summary>Set by PlayerController.ApplyHit before entering.</summary>
        public void SetSource(Vector2 position) => sourcePosition = position;

        public override void Enter()
        {
            elapsed = 0f;
            Vector2 away = (Vector2)player.transform.position - sourcePosition;
            if (away.sqrMagnitude < 0.0001f) away = -player.Facing;
            knockback = away.normalized * player.Config.knockbackSpeed;
        }

        public override void Tick(float deltaTime)
        {
            elapsed += deltaTime;
            if (elapsed >= player.Config.hurtDuration) player.ReturnToLocomotion();
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            // Knockback fades out over the hurt duration.
            float fade = 1f - Mathf.Clamp01(elapsed / player.Config.hurtDuration);
            player.SetVelocity(knockback * fade);
        }

        public override void Exit() => player.Stop();
    }
}
