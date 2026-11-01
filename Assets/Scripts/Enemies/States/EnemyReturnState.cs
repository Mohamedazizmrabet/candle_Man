namespace Wickfall.Enemies
{
    /// <summary>Walks back home. Spotting the player again on the way raises a new alert.</summary>
    public class EnemyReturnState : EnemyState
    {
        public EnemyReturnState(EnemyBrain brain) : base(brain) { }

        public override void Tick(float deltaTime)
        {
            if (brain.Perception.CanSeePlayer && brain.DistanceFromHome <= brain.Config.leashDistance)
            {
                brain.ChangeState(brain.Alert);
            }
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            if (brain.MoveTowards(brain.Home, brain.Config.returnSpeed)) brain.ChangeState(brain.Wander);
        }
    }
}
