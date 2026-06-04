using UnityEngine;

public class AliveState : PlayerNetworkState
{
    public AliveState(PlayerNetworkStateMachine m) : base(m) { }
    public override PlayerNetworkStateType Type => PlayerNetworkStateType.Alive;
    public override void OnEnterAll() => Machine.SetPhysics(true);
}