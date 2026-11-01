using UnityEngine;

namespace Wickfall.Player
{
    /// <summary>Short burst in the input direction (or facing), invulnerable, cancels trimming the wick.</summary>
    public class PlayerDashState : PlayerState
    {
        private Vector2 direction;
        private float elapsed;

        public PlayerDashState(PlayerController player) : base(player) { }

        public override void Enter()
        {
            elapsed = 0f;
            direction = player.MoveInput != Vector2.zero ? player.MoveInput : player.Facing;
            player.SetCanDim(false);
        }

        public override void Tick(float deltaTime)
        {
            elapsed += deltaTime;
            if (elapsed >= player.Config.dashDuration) player.ReturnToLocomotion();
        }

        public override void FixedTick(float fixedDeltaTime) =>
            player.SetVelocity(direction * player.Config.moveSpeed * player.Config.dashSpeedMultiplier);

        public override void Exit()
        {
            player.Stop();
            player.SetCanDim(true);
            player.StartDashCooldown();
        }
    }
}
