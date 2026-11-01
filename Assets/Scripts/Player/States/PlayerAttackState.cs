namespace Wickfall.Player
{
    /// <summary>
    /// One swing toward the facing direction. The wax cost is paid before entering.
    /// The hitbox is on only during the middle of the swing (wind-up and recovery are safe to read).
    /// </summary>
    public class PlayerAttackState : PlayerState
    {
        private const float ActiveStart = 0.3f;
        private const float ActiveEnd = 0.7f;

        private float elapsed;

        public PlayerAttackState(PlayerController player) : base(player) { }

        public override void Enter()
        {
            elapsed = 0f;
            player.Stop();
            player.AimAttack();
        }

        public override void Tick(float deltaTime)
        {
            elapsed += deltaTime;
            float duration = player.Config.attackDuration;
            float t = elapsed / duration;

            player.SetAttackHitbox(t >= ActiveStart && t <= ActiveEnd);

            if (elapsed >= duration) player.ReturnToLocomotion();
        }

        public override void Exit()
        {
            player.SetAttackHitbox(false);
            player.StartAttackCooldown();
        }
    }
}
