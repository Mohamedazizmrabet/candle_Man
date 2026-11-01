namespace Wickfall.Core
{
    /// <summary>Minimal state machine: one current state, Exit then Enter on every change.</summary>
    public class StateMachine
    {
        public IState Current { get; private set; }

        public void ChangeState(IState next)
        {
            if (next == Current) return;

            Current?.Exit();
            Current = next;
            Current?.Enter();
        }

        public void Tick(float deltaTime) => Current?.Tick(deltaTime);

        public void FixedTick(float fixedDeltaTime) => Current?.FixedTick(fixedDeltaTime);

        /// <summary>Name of the current state, for debug display.</summary>
        public string CurrentName => Current != null ? Current.GetType().Name : "None";
    }
}
