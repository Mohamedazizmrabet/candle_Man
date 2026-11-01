namespace Wickfall.Enemies
{
    /// <summary>Freezes for a moment after spotting the player (a readable "it saw me"), then chases.</summary>
    public class EnemyAlertState : EnemyState
    {
        private float elapsed;

        public EnemyAlertState(EnemyBrain brain) : base(brain) { }

        public override void Enter()
        {
            elapsed = 0f;
            brain.Stop();
            brain.ShowAlertTint();
        }

        public override void Tick(float deltaTime)
        {
            elapsed += deltaTime;
            if (elapsed < brain.Config.alertDuration) return;

            // Lost the player during the pause (they trimmed the wick or walked away): go home.
            brain.ChangeState(brain.Perception.RemembersPlayer ? (EnemyState)brain.Chase : brain.Return);
        }

        public override void FixedTick(float fixedDeltaTime) => brain.Stop();

        public override void Exit() => brain.ClearTint();
    }
}
