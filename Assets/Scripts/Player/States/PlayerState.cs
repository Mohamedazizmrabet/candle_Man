using Wickfall.Core;

namespace Wickfall.Player
{
    /// <summary>Base for Candleman's states. Each state decides its own transitions in Tick.</summary>
    public abstract class PlayerState : IState
    {
        protected readonly PlayerController player;

        protected PlayerState(PlayerController player) => this.player = player;

        public virtual void Enter() { }
        public virtual void Tick(float deltaTime) { }
        public virtual void FixedTick(float fixedDeltaTime) { }
        public virtual void Exit() { }
    }
}
