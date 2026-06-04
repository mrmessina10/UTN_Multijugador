public class UninitializedState : PlayerNetworkState
{
    public UninitializedState(PlayerNetworkStateMachine m) : base(m) { }
    public override PlayerNetworkStateType Type => PlayerNetworkStateType.Uninitialized;
    public override void OnEnterAll() { }
}