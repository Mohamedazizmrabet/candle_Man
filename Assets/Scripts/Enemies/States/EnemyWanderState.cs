using UnityEngine;

namespace Wickfall.Enemies
{
    /// <summary>Drifts between random points around home, pausing at each. Spotting the player raises the alert.</summary>
    public class EnemyWanderState : EnemyState
    {
        private Vector2 target;
        private float pause;

        public EnemyWanderState(EnemyBrain brain) : base(brain) { }

        public override void Enter() => StartPause();

        public override void Tick(float deltaTime)
        {
            if (brain.Perception.CanSeePlayer)
            {
                brain.ChangeState(brain.Alert);
                return;
            }

            if (pause > 0f)
            {
                pause -= deltaTime;
                if (pause <= 0f) target = brain.Home + Random.insideUnitCircle * brain.Config.wanderRadius;
            }
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            if (pause > 0f)
            {
                brain.Stop();
                return;
            }

            if (brain.MoveTowards(target, brain.Config.wanderSpeed)) StartPause();
        }

        private void StartPause() => pause = Random.Range(brain.Config.wanderPause.x, brain.Config.wanderPause.y);
    }
}
