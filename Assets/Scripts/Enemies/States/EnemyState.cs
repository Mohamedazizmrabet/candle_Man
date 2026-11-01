using Wickfall.Core;

namespace Wickfall.Enemies
{
    /// <summary>Base for enemy states. Each state decides its own transitions in Tick.</summary>
    public abstract class EnemyState : IState
    {
        protected readonly EnemyBrain brain;

        protected EnemyState(EnemyBrain brain) => this.brain = brain;

        public virtual void Enter() { }
        public virtual void Tick(float deltaTime) { }
        public virtual void FixedTick(float fixedDeltaTime) { }
        public virtual void Exit() { }
    }
}
