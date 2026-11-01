namespace Wickfall.Core
{
    /// <summary>One state of a state machine (player or enemy).</summary>
    public interface IState
    {
        void Enter();
        void Tick(float deltaTime);
        void FixedTick(float fixedDeltaTime);
        void Exit();
    }
}
