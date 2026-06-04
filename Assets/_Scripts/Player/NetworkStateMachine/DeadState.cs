public class DeadState : PlayerNetworkState
{
    public DeadState(PlayerNetworkStateMachine m) : base(m) { }
    public override PlayerNetworkStateType Type => PlayerNetworkStateType.Dead;
    public override void OnEnterAll() => Machine.SetPhysics(false);
}