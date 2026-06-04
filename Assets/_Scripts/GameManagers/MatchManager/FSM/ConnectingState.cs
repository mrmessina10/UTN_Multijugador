using Unity.Netcode;

public class ConnectingState : IMatchState
{
    public void Enter(MatchManager manager) { }

    public void Tick(MatchManager manager)
    {
        if (manager.GetSyncedClientsCount() >= manager.minPlayersToStart &&
            manager.GetSyncedClientsCount() == NetworkManager.Singleton.ConnectedClients.Count)
        {
            manager.ChangeState(new MatchStartState());
        }
    }

    public void Exit(MatchManager manager) { }
}