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
    public NetworkVariable<float> stateTimer = new NetworkVariable<float>(0f);

    [Header("Match Settings")]
    [SerializeField] private int minPlayersToStart = 2;
    [SerializeField] private float startDelay = 3f;
    [SerializeField] private float roundDuration = 60f;
    [SerializeField] private float endDelay = 5f;
    [SerializeField] private int roundsToWin = 3;

    public MatchScoreController Score { get; private set; }
    public MatchSpawnController Spawner { get; private set; }

    private Coroutine _matchLoopCoroutine;

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

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Spawner.SpawnPlayerForClient(clientId);
        }

        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += HandleSceneLoaded;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer || NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        if (NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= HandleSceneLoaded;
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (currentState.Value == MatchState.WaitingForPlayers && NetworkManager.Singleton.ConnectedClients.Count >= minPlayersToStart)
        {
            if (_matchLoopCoroutine != null) StopCoroutine(_matchLoopCoroutine);
            _matchLoopCoroutine = StartCoroutine(MatchLoopRoutine());
        }
    }

    private void HandleSceneLoaded(string sceneName, UnityEngine.SceneManagement.LoadSceneMode mode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!IsServer || sceneName != "Mapa1") return;

        foreach (var clientId in clientsCompleted) Spawner.SpawnPlayerForClient(clientId);
    }

    private IEnumerator MatchLoopRoutine()
    {
        bool matchIsOver = false;

        while (!matchIsOver)
        {
            currentState.Value = MatchState.RoundStarting;
            stateTimer.Value = startDelay;

            Spawner.RespawnAllPlayers();

            while (stateTimer.Value > 0) { stateTimer.Value -= Time.deltaTime; yield return null; }

            currentState.Value = MatchState.RoundActive;
            stateTimer.Value = roundDuration;
            MatchScoreController.Team roundWinner = MatchScoreController.Team.None;

            while (stateTimer.Value > 0)
            {
                stateTimer.Value -= Time.deltaTime;
                Score.UpdateAlivePlayersStats();
                roundWinner = Score.CheckRoundWinner();

                if (roundWinner != MatchScoreController.Team.None) break;
                yield return null;
            }

            Score.AddPointToTeam(roundWinner);
            matchIsOver = Score.CheckMatchWinner(roundsToWin);

            currentState.Value = MatchState.RoundEnded;
            stateTimer.Value = endDelay;

            while (stateTimer.Value > 0) { stateTimer.Value -= Time.deltaTime; yield return null; }

            if (!matchIsOver) Score.AdvanceRound();
        }

        EndMatch();
    }

    private void EndMatch()
    {
        NetworkManager.Singleton.SceneManager.LoadScene("MainMenu", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}