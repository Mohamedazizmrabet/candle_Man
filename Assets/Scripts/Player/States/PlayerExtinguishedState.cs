using UnityEngine;

namespace Wickfall.Player
{
    /// <summary>Wax reached 0: the flame is out. Waits, then respawns at the last brazier.</summary>
    public class PlayerExtinguishedState : PlayerState
    {
        private static readonly Color DeadTint = new Color(0.35f, 0.35f, 0.4f);

        private float elapsed;

        public PlayerExtinguishedState(PlayerController player) : base(player) { }

        public override void Enter()
        {
            elapsed = 0f;
            player.Stop();
            player.SetAttackHitbox(false);
            player.SetCanDim(false);
            player.SetAlive(false);
            player.SetBodyTint(DeadTint);
        }

        public override void Tick(float deltaTime)
        {
            elapsed += deltaTime;
            if (elapsed >= player.Config.respawnDelay) player.Respawn();
        }

        public override void FixedTick(float fixedDeltaTime) => player.Stop();

        public override void Exit()
        {
            player.SetCanDim(true);
            player.SetAlive(true);
            player.SetBodyTint(Color.white);
        }
    }
}
