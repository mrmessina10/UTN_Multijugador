using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class MatchSpawnController : NetworkBehaviour
{
    [Header("Spawning Settings")]
    [SerializeField] private Transform[] fallbackSpawnPoints;
    [SerializeField] private GameObject playerPrefab;

    // Instancia físicamente el cuerpo del jugador por primera vez y le otorga propiedad de red
    public void SpawnPlayerForClient(ulong clientId)
    {
        if (!IsServer) return;

        // Si el cliente ya tiene un PlayerObject, abortamos para evitar duplicados
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) && client.PlayerObject != null) return;

        var spawns = FindObjectsByType<TeamSpawnPoint>(FindObjectsSortMode.None);
        var redSpawns = spawns.Where(s => s.team == TeamSpawnPoint.Team.Red).ToArray();
        var blueSpawns = spawns.Where(s => s.team == TeamSpawnPoint.Team.Blue).ToArray();

        Transform targetSpawn;
        if (clientId % 2 == 0 && redSpawns.Length > 0) targetSpawn = redSpawns[clientId % (ulong)redSpawns.Length].transform;
        else if (blueSpawns.Length > 0) targetSpawn = blueSpawns[clientId % (ulong)blueSpawns.Length].transform;
        else targetSpawn = fallbackSpawnPoints.Length > 0 ? fallbackSpawnPoints[0] : transform;

        GameObject playerInstance = Instantiate(playerPrefab, targetSpawn.position + Vector3.up, targetSpawn.rotation);
        playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
    }

    // Teletransporta a los jugadores vivos o muertos de vuelta a sus bases
    public void RespawnAllPlayers()
    {
        if (!IsServer) return;

        var spawns = FindObjectsByType<TeamSpawnPoint>(FindObjectsSortMode.None);
        var redSpawns = spawns.Where(s => s.team == TeamSpawnPoint.Team.Red).ToArray();
        var blueSpawns = spawns.Where(s => s.team == TeamSpawnPoint.Team.Blue).ToArray();

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            Transform spawnPoint = (client.ClientId % 2 == 0)
                ? redSpawns[client.ClientId % (ulong)redSpawns.Length].transform
                : blueSpawns[client.ClientId % (ulong)blueSpawns.Length].transform;

            if (client.PlayerObject.TryGetComponent(out PlayerStateController playerController))
            {
                // Dispara el RPC hacia los clientes para reestablecer salud y coordinadas físicas
                playerController.ResetPlayerClientRpc(spawnPoint.position);
            }
        }
    }
}