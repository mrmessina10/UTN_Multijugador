using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(MatchScoreController), typeof(MatchSpawnController))]
public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance { get; private set; }

    public enum MatchState { WaitingForPlayers, RoundStarting, RoundActive, RoundEnded }

    [Header("Match State")]
    public NetworkVariable<MatchState> currentState = new NetworkVariable<MatchState>(MatchState.WaitingForPlayers);

    // Patrón timestamp evita el colapso de red
    public NetworkVariable<double> stateEndTime = new NetworkVariable<double>(0);

    [Header("Match Settings")]
    [SerializeField] private int minPlayersToStart = 2;
    [SerializeField] private float startDelay = 3f;
    [SerializeField] private float roundDuration = 60f;
    [SerializeField] private float endDelay = 5f;
    [SerializeField] private int roundsToWin = 3;

    public MatchScoreController Score { get; private set; }
    public MatchSpawnController Spawner { get; private set; }

    private Coroutine _matchLoopCoroutine;
    private HashSet<ulong> _syncedClients = new HashSet<ulong>();

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

        // Forzamos la validación de clientes ya listos al cargar (Ej. el Host)
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
        // Permite al cliente inicializar la memoria interna de Netcode
        yield return new WaitForSeconds(0.5f);

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            if (client.PlayerObject == null)
            {
                Spawner.SpawnPlayerForClient(clientId);
            }

            _syncedClients.Add(clientId);

            if (currentState.Value == MatchState.WaitingForPlayers &&
                _syncedClients.Count >= minPlayersToStart &&
                _syncedClients.Count == NetworkManager.Singleton.ConnectedClients.Count)
            {
                if (_matchLoopCoroutine != null) StopCoroutine(_matchLoopCoroutine);
                _matchLoopCoroutine = StartCoroutine(MatchLoopRoutine());
            }
        }
    }

    private IEnumerator MatchLoopRoutine()
    {
        bool matchIsOver = false;

        while (!matchIsOver)
        {
            currentState.Value = MatchState.RoundStarting;
            stateEndTime.Value = NetworkManager.Singleton.ServerTime.Time + startDelay;

            // Asegura que el PlayerObject existe localmente antes de forzar su teletransporte
            yield return new WaitForSeconds(0.5f);
            Spawner.RespawnAllPlayers();

            while (NetworkManager.Singleton.ServerTime.Time < stateEndTime.Value)
            {
                yield return null;
            }

            currentState.Value = MatchState.RoundActive;
            stateEndTime.Value = NetworkManager.Singleton.ServerTime.Time + roundDuration;
            MatchScoreController.Team roundWinner = MatchScoreController.Team.None;

            while (NetworkManager.Singleton.ServerTime.Time < stateEndTime.Value)
            {
                Score.UpdateAlivePlayersStats();
                roundWinner = Score.CheckRoundWinner();

                if (roundWinner != MatchScoreController.Team.None) break;

                yield return new WaitForSeconds(0.2f);
            }

            Score.AddPointToTeam(roundWinner);
            matchIsOver = Score.CheckMatchWinner(roundsToWin);

            currentState.Value = MatchState.RoundEnded;
            stateEndTime.Value = NetworkManager.Singleton.ServerTime.Time + endDelay;

            while (NetworkManager.Singleton.ServerTime.Time < stateEndTime.Value)
            {
                yield return null;
            }

            if (!matchIsOver) Score.AdvanceRound();
        }

        EndMatch();
    }

    private void EndMatch()
    {
        NetworkManager.Singleton.SceneManager.LoadScene("MainMenu", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}