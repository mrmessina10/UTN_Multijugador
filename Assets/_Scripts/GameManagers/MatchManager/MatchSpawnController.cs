using Unity.Netcode;
using UnityEngine;

public class MatchSpawnController : NetworkBehaviour
{
    [Header("Spawning Settings")]
    [SerializeField] private Transform[] fallbackSpawnPoints;
    [SerializeField] private GameObject redPlayerPrefab;
    [SerializeField] private GameObject bluePlayerPrefab;

    public void SpawnPlayerForClient(ulong clientId)
    {
        if (!IsServer) return;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)
            && client.PlayerObject != null) return;

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

        GameObject prefabToSpawn = isRed ? redPlayerPrefab : bluePlayerPrefab;

        GameObject playerInstance = Instantiate(
            prefabToSpawn,
            targetSpawn.position + (Vector3.up * 1.2f),
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

        var redSpawns = GameObject.FindGameObjectsWithTag("RedTeamSpawn");
        var blueSpawns = GameObject.FindGameObjectsWithTag("BlueTeamSpawn");

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            if (client.PlayerObject == null) return;

            bool isRed = clientId % 2 == 0;
            Transform spawnPoint = transform;

            if (fallbackSpawnPoints != null && fallbackSpawnPoints.Length > 0)
                spawnPoint = fallbackSpawnPoints[0];

            if (isRed && redSpawns != null && redSpawns.Length > 0)
                spawnPoint = redSpawns[clientId % (ulong)redSpawns.Length].transform;
            else if (!isRed && blueSpawns != null && blueSpawns.Length > 0)
                spawnPoint = blueSpawns[clientId % (ulong)blueSpawns.Length].transform;

            if (client.PlayerObject.TryGetComponent(out PlayerNetworkStateMachine stateMachine))
            {
                stateMachine.ServerRespawnPlayer(spawnPoint.position);
            }
        }
    }
}