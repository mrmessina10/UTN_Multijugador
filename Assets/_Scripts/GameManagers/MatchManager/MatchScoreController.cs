using Unity.Netcode;
using UnityEngine;

public class MatchScoreController : NetworkBehaviour
{
    public enum Team { None, Red, Blue }

    [Header("Match Stats")]
    public NetworkVariable<int> currentRound = new NetworkVariable<int>(1);
    public NetworkVariable<int> redTeamScore = new NetworkVariable<int>(0);
    public NetworkVariable<int> blueTeamScore = new NetworkVariable<int>(0);

    [Header("Tactical Stats")]
    public NetworkVariable<int> redPlayersAlive = new NetworkVariable<int>(0);
    public NetworkVariable<int> bluePlayersAlive = new NetworkVariable<int>(0);

    // Método llamado por el MatchManager en cada frame durante la ronda activa
    public void UpdateAlivePlayersStats()
    {
        if (!IsServer) return;

        int redAlive = 0;
        int blueAlive = 0;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null && client.PlayerObject.TryGetComponent(out PlayerHealth health))
            {
                if (!health.isDead.Value)
                {
                    if (client.ClientId % 2 == 0) redAlive++;
                    else blueAlive++;
                }
            }
        }

        redPlayersAlive.Value = redAlive;
        bluePlayersAlive.Value = blueAlive;
    }

    // Evaluación matemática para determinar si un equipo fue eliminado
    public Team CheckRoundWinner()
    {
        if (!IsServer) return Team.None;

        if (redPlayersAlive.Value == 0 && bluePlayersAlive.Value > 0) return Team.Blue;
        if (bluePlayersAlive.Value == 0 && redPlayersAlive.Value > 0) return Team.Red;

        return Team.None;
    }

    public void AddPointToTeam(Team winningTeam)
    {
        if (!IsServer) return;

        if (winningTeam == Team.Red) redTeamScore.Value++;
        else if (winningTeam == Team.Blue) blueTeamScore.Value++;
    }

    public bool CheckMatchWinner(int roundsToWin)
    {
        return redTeamScore.Value >= roundsToWin || blueTeamScore.Value >= roundsToWin;
    }

    public void AdvanceRound()
    {
        if (!IsServer) return;
        currentRound.Value++;
    }
}