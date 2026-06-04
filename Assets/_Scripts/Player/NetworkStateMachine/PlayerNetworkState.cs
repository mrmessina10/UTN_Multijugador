public abstract class PlayerNetworkState
{
    protected PlayerNetworkStateMachine Machine;

    protected PlayerNetworkState(PlayerNetworkStateMachine machine)
    {
        Machine = machine;
    }

    public abstract PlayerNetworkStateType Type { get; }

    public virtual void OnEnterAll() { }
    public virtual void OnExitAll() { }
    public virtual void OnTickAll() { }

    public virtual void OnEnterServer() { }
    public virtual void OnTickServer() { }

    public virtual void OnEnterOwner() { }
    public virtual void OnTickOwner() { }
}