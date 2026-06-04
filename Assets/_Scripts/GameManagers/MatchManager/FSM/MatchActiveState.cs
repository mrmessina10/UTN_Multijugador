using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;

public class MatchActiveState : IMatchState
{
    private Dictionary<ulong, double> _respawnQueue = new Dictionary<ulong, double>();

    public void Enter(MatchManager manager)
    {
        _respawnQueue.Clear();
    }

    public void Tick(MatchManager manager)
    {
        if (!manager.IsServer || _respawnQueue.Count == 0) return;

        var clientsToRespawn = _respawnQueue
            .Where(kvp => NetworkManager.Singleton.ServerTime.Time >= kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var clientId in clientsToRespawn)
        {
            manager.Spawner.RespawnPlayer(clientId);
            _respawnQueue.Remove(clientId);
        }
    }

    public void Exit(MatchManager manager)
    {
        // Al terminar la partida, purgamos la cola. Cero reapariciones huérfanas.
        _respawnQueue.Clear();
    }

    public void QueueRespawn(ulong clientId, double delay)
    {
        _respawnQueue[clientId] = NetworkManager.Singleton.ServerTime.Time + delay;
    }
}