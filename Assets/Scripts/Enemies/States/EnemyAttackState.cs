namespace Wickfall.Enemies
{
    /// <summary>Wind-up (red tint, the player's cue to dodge), short strike, recovery, then back to chasing.</summary>
    public class EnemyAttackState : EnemyState
    {
        private float elapsed;
        private bool struck;

        public EnemyAttackState(EnemyBrain brain) : base(brain) { }

        public override void Enter()
        {
            elapsed = 0f;
            struck = false;
            brain.Stop();
            brain.AimAt(brain.Perception.PlayerPosition);
            brain.ShowWindupTint();
        }

        public override void Tick(float deltaTime)
        {
            elapsed += deltaTime;
            float windup = brain.Config.attackWindup;
            float strikeEnd = windup + brain.AttackActiveTime;

            if (!struck && elapsed >= windup)
            {
                struck = true;
                brain.ClearTint();
                brain.SetAttackHitbox(true);
                brain.StartAttackCooldown();
            }

            if (struck && elapsed >= strikeEnd) brain.SetAttackHitbox(false);

            if (elapsed >= strikeEnd + brain.AttackRecovery) brain.ChangeState(brain.Chase);
        }

        public override void FixedTick(float fixedDeltaTime) => brain.Stop();

        public override void Exit()
        {
            brain.SetAttackHitbox(false);
            brain.ClearTint();
        }
    }
}
