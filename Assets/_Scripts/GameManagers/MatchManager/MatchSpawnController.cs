using Unity.Netcode;
using UnityEngine;

public class MatchSpawnController : NetworkBehaviour
{
    [Header("Spawn Points — asignar en el Inspector")]
    [SerializeField] private Transform[] redTeamSpawnPoints;   // arrastrá los objetos acá
    [SerializeField] private Transform[] blueTeamSpawnPoints;  // arrastrá los objetos acá
    [SerializeField] private Transform[] fallbackSpawnPoints;
    [SerializeField] private GameObject playerPrefab;

    public void SpawnPlayerForClient(ulong clientId)
    {
        if (!IsServer) return;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)
            && client.PlayerObject != null) return;

        // Log de diagnóstico directo
        Debug.Log($"[SpawnController] Arrays — Red: {redTeamSpawnPoints.Length} | Blue: {blueTeamSpawnPoints.Length}");

        Transform targetSpawn = GetSpawnForClient(clientId);

        Debug.Log($"[SpawnController] ClientId {clientId} → {targetSpawn.name} @ {targetSpawn.position}");

        float halfHeight = playerPrefab.GetComponent<CharacterController>().height / 2f;
        var instance = Instantiate(playerPrefab, targetSpawn.position + Vector3.up * halfHeight, targetSpawn.rotation);
        instance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
    }

    private Transform GetSpawnForClient(ulong clientId)
    {
        bool isRed = clientId % 2 == 0;
        var pool = isRed ? redTeamSpawnPoints : blueTeamSpawnPoints;

        if (pool != null && pool.Length > 0)
            return pool[clientId % (ulong)pool.Length];

        Debug.LogError($"[SpawnController] ¡Pool de spawns VACÍO para clientId {clientId}! isRed={isRed}");

        if (fallbackSpawnPoints != null && fallbackSpawnPoints.Length > 0)
            return fallbackSpawnPoints[0];

        Debug.LogError("[SpawnController] Fallback también vacío. Usando transform del MatchManager.");
        return transform;
    }

    public void RespawnAllPlayers()
    {
        if (!IsServer) return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            Transform spawnPoint = GetSpawnForClient(client.ClientId);

            if (client.PlayerObject.TryGetComponent(out PlayerStateController psc))
                psc.ServerRespawnPlayer(spawnPoint.position);
        }
    }
}