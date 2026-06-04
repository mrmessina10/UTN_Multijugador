using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(MatchScoreController), typeof(MatchSpawnController))]
public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance { get; private set; }

    public NetworkVariable<double> stateTimer = new NetworkVariable<double>(0);

    public NetworkVariable<bool> isSettingUp = new NetworkVariable<bool>(true);
    public NetworkVariable<int> warmupCountdown = new NetworkVariable<int>(5);

    [Header("TDM Settings")]
    public int minPlayersToStart = 2;
    public float startDelay = 3f;
    public int scoreToWin = 10;
    public float matchDuration = 300f;
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

            // Verificamos si todos los clientes requeridos ya están sincronizados e instanciados
            if (_currentState is ConnectingState &&
                _syncedClients.Count >= minPlayersToStart &&
                _syncedClients.Count == NetworkManager.Singleton.ConnectedClients.Count)
            {
                StartCoroutine(GlobalWarmupRoutine());
            }
        }
    }

    private IEnumerator GlobalWarmupRoutine()
    {
        isSettingUp.Value = true;

        yield return new WaitForSeconds(0.5f);

        foreach (ulong clientId in _syncedClients)
        {
            Transform spawnPoint = Spawner.GetSpawnPointForClient(clientId);

            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) && client.PlayerObject != null)
            {
                if (client.PlayerObject.TryGetComponent(out PlayerNetworkStateMachine psm))
                {
                    psm.ForceInitialTeleport(spawnPoint.position);
                }
            }
        }

        int timeRemaining = (int)startDelay;
        while (timeRemaining > 0)
        {
            warmupCountdown.Value = timeRemaining;
            yield return new WaitForSeconds(1f);
            timeRemaining--;
        }

        isSettingUp.Value = false;
        stateTimer.Value = NetworkManager.Singleton.ServerTime.Time + matchDuration;
        ChangeState(new MatchStartState());
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
    public bool IsMatchEnded()
    {
        return _currentState is MatchEndedState;
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