using Unity.Netcode;
using UnityEngine;

public class MatchSpawnController : NetworkBehaviour
{
    [Header("Spawning Settings")]
    [SerializeField] private Transform[] fallbackSpawnPoints;
    [SerializeField] private GameObject redPlayerPrefab;
    [SerializeField] private GameObject bluePlayerPrefab;

    public Transform GetSpawnPointForClient(ulong clientId)
    {
        var redSpawns = GameObject.FindGameObjectsWithTag("RedTeamSpawn");
        var blueSpawns = GameObject.FindGameObjectsWithTag("BlueTeamSpawn");

        bool isRed = clientId % 2 == 0;
        Transform targetSpawn = transform;

        if (fallbackSpawnPoints != null && fallbackSpawnPoints.Length > 0)
            targetSpawn = fallbackSpawnPoints[0];

        if (isRed && redSpawns != null && redSpawns.Length > 0)
            targetSpawn = redSpawns[clientId % (ulong)redSpawns.Length].transform;
        else if (!isRed && blueSpawns != null && blueSpawns.Length > 0)
            targetSpawn = blueSpawns[clientId % (ulong)blueSpawns.Length].transform;

        return targetSpawn;
    }

    public void SpawnPlayerForClient(ulong clientId)
    {
        if (!IsServer) return;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)
            && client.PlayerObject != null) return;

        Transform targetSpawn = GetSpawnPointForClient(clientId);
        bool isRed = clientId % 2 == 0;

        GameObject prefabToSpawn = isRed ? redPlayerPrefab : bluePlayerPrefab;

        GameObject playerInstance = Instantiate(
            prefabToSpawn,
            targetSpawn.position,
            targetSpawn.rotation
        );

        playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
    }

    public void RespawnAllPlayers()
    {
        if (!IsServer) return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            RespawnPlayer(client.ClientId);
        }
    }

    public void RespawnPlayer(ulong clientId)
    {
        if (!IsServer) return;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            if (client.PlayerObject == null) return;

            Transform spawnPoint = GetSpawnPointForClient(clientId);

            if (client.PlayerObject.TryGetComponent(out PlayerNetworkStateMachine stateMachine))
            {
                stateMachine.ServerRespawnPlayer(spawnPoint.position);
            }
        }
    }
}