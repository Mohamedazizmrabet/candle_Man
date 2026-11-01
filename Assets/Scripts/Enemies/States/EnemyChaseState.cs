namespace Wickfall.Enemies
{
    /// <summary>
    /// Runs at the player while it can see them, then to their last known position while it remembers.
    /// Gives up (returns home) when memory runs out or it is pulled too far from home.
    /// </summary>
    public class EnemyChaseState : EnemyState
    {
        public EnemyChaseState(EnemyBrain brain) : base(brain) { }

        public override void Tick(float deltaTime)
        {
            var perception = brain.Perception;

            if (!perception.RemembersPlayer || brain.DistanceFromHome > brain.Config.leashDistance)
            {
                brain.ChangeState(brain.Return);
                return;
            }

            if (perception.CanSeePlayer
                && brain.AttackReady
                && perception.DistanceToPlayer <= brain.Config.attackRange)
            {
                brain.ChangeState(brain.Attack);
            }
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            var perception = brain.Perception;
            var target = perception.CanSeePlayer ? perception.PlayerPosition : perception.LastKnownPosition;

            // Close enough and waiting for the attack cooldown: hold position instead of pushing into the player.
            if (perception.CanSeePlayer && perception.DistanceToPlayer <= brain.Config.attackRange)
            {
                brain.Stop();
                return;
            }

            brain.MoveTowards(target, brain.Config.chaseSpeed);
        }
    }
}
