using UnityEngine;

namespace Wickfall.Player
{
    public class PlayerIdleState : PlayerState
    {
        public PlayerIdleState(PlayerController player) : base(player) { }

        public override void Enter() => player.Stop();

        public override void Tick(float deltaTime)
        {
            if (player.TryStartAction()) return;
            if (player.IsDimmed) player.ChangeState(player.Dim);
            else if (player.MoveInput != Vector2.zero) player.ChangeState(player.Walk);
        }
    }
}
