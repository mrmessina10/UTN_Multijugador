using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(MatchScoreController), typeof(MatchSpawnController))]
public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance { get; private set; }

    public NetworkVariable<double> stateTimer = new NetworkVariable<double>(0);

    [Header("TDM Settings")]
    public int minPlayersToStart = 2;
    public float startDelay = 3f;
    public int scoreToWin = 10;
    public float respawnDelay = 3f;
    public float endDelay = 5f;
    public string mainMenuSceneName = "MainMenu";

    public MatchScoreController Score { get; private set; }
    public MatchSpawnController Spawner { get; private set; }

    private HashSet<ulong> _syncedClients = new HashSet<ulong>();
    private IMatchState _currentState;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        Score = GetComponent<MatchScoreController>();
        Spawner = GetComponent<MatchSpawnController>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.SceneManager.OnSceneEvent += HandleSceneEvent;

        ChangeState(new ConnectingState());

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            StartCoroutine(SpawnAndCheckReady(clientId));
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer || NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= HandleSceneEvent;
    }

    private void HandleSceneEvent(SceneEvent sceneEvent)
    {
        if (sceneEvent.SceneEventType == SceneEventType.SynchronizeComplete)
        {
            StartCoroutine(SpawnAndCheckReady(sceneEvent.ClientId));
        }
    }

    private IEnumerator SpawnAndCheckReady(ulong clientId)
    {
        yield return new WaitForSeconds(1f);

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            if (client.PlayerObject == null) Spawner.SpawnPlayerForClient(clientId);
            _syncedClients.Add(clientId);

            if (_currentState is ConnectingState &&
                _syncedClients.Count >= minPlayersToStart &&
                _syncedClients.Count == NetworkManager.Singleton.ConnectedClients.Count)
            {
                ChangeState(new MatchStartState());
            }
        }
    }

    private void Update()
    {
        if (!IsServer || _currentState == null) return;
        _currentState.Tick(this);
    }

    public void ChangeState(IMatchState newState)
    {
        if (!IsServer) return;

        _currentState?.Exit(this);
        _currentState = newState;
        _currentState?.Enter(this);
    }

    public int GetSyncedClientsCount()
    {
        return _syncedClients.Count;
    }

    public bool IsMatchActive()
    {
        return _currentState is MatchActiveState;
    }

    public void RecordDeathAndRespawn(ulong deadClientId, ulong killerClientId)
    {
        if (!IsServer || !IsMatchActive()) return;

        Score.AddPointForKill(deadClientId, killerClientId);

        if (Score.CheckMatchWinner(scoreToWin))
        {
            ChangeState(new MatchEndedState());
        }
        else
        {
            if (_currentState is MatchActiveState activeState)
            {
                activeState.QueueRespawn(deadClientId, respawnDelay);
            }
        }
    }
}