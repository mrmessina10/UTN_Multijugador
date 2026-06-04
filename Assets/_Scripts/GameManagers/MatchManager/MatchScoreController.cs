using Unity.Netcode;

public class MatchScoreController : NetworkBehaviour
{
    public NetworkVariable<int> redTeamScore = new NetworkVariable<int>(0);
    public NetworkVariable<int> blueTeamScore = new NetworkVariable<int>(0);

    public void AddPointForKill(ulong deadClientId, ulong killerClientId)
    {
        if (!IsServer) return;

        if (deadClientId % 2 == 0) blueTeamScore.Value++;
        else redTeamScore.Value++;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(killerClientId, out var killer))
        {
            if (killer.PlayerObject != null && killer.PlayerObject.TryGetComponent(out PlayerScore killerScore))
            {
                killerScore.AddKill();
            }
        }

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(deadClientId, out var dead))
        {
            if (dead.PlayerObject != null && dead.PlayerObject.TryGetComponent(out PlayerScore deadScore))
            {
                deadScore.AddDeath();
            }
        }
    }

    public bool CheckMatchWinner(int targetScore)
    {
        if (!IsServer) return false;
        return redTeamScore.Value >= targetScore || blueTeamScore.Value >= targetScore;
    }
}