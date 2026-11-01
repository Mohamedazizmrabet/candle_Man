using UnityEngine;

namespace Wickfall.Player
{
    public class PlayerWalkState : PlayerState
    {
        public PlayerWalkState(PlayerController player) : base(player) { }

        public override void Tick(float deltaTime)
        {
            if (player.TryStartAction()) return;
            if (player.IsDimmed) player.ChangeState(player.Dim);
            else if (player.MoveInput == Vector2.zero) player.ChangeState(player.Idle);
        }

        public override void FixedTick(float fixedDeltaTime) =>
            player.SetVelocity(player.MoveInput * player.Config.moveSpeed);
    }
}
