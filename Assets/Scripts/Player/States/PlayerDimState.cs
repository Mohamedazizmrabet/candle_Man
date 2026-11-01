namespace Wickfall.Player
{
    /// <summary>Wick trimmed: sneaking (standing or moving) at reduced speed. Attacks still work at half damage.</summary>
    public class PlayerDimState : PlayerState
    {
        public PlayerDimState(PlayerController player) : base(player) { }

        public override void Tick(float deltaTime)
        {
            if (player.TryStartAction()) return;
            if (!player.IsDimmed) player.ReturnToLocomotion();
        }

        public override void FixedTick(float fixedDeltaTime) =>
            player.SetVelocity(player.MoveInput * player.Config.moveSpeed * player.DimControl.MoveSpeedMultiplier);
    }
}
