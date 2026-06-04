public class RespawningState : PlayerNetworkState
{
    public RespawningState(PlayerNetworkStateMachine m) : base(m) { }
    public override PlayerNetworkStateType Type => PlayerNetworkStateType.Respawning;
    public override void OnEnterAll() => Machine.SetPhysics(false);
}