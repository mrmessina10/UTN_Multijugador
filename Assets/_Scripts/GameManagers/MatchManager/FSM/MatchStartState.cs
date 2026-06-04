using Unity.Netcode;

public class MatchStartState : IMatchState
{
    public void Enter(MatchManager manager)
    {
        manager.stateTimer.Value = NetworkManager.Singleton.ServerTime.Time + manager.startDelay;
    }

    public void Tick(MatchManager manager)
    {
        if (NetworkManager.Singleton.ServerTime.Time >= manager.stateTimer.Value)
        {
            manager.ChangeState(new MatchActiveState());
        }
    }

    public void Exit(MatchManager manager) { }
}