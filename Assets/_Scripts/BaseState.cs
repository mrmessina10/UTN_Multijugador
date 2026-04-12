public abstract class BaseState
{
    protected PlayerStateMachine stateMachine;

    protected BaseState (PlayerStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }

    public abstract void Enter();
    public abstract void Tick();
    public abstract void Exit();

}
