public class SpawningState : PlayerNetworkState
{
    public SpawningState(PlayerNetworkStateMachine m) : base(m) { }
    public override PlayerNetworkStateType Type => PlayerNetworkStateType.Spawning;
    public override void OnEnterAll() { }
}