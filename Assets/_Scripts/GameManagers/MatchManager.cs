using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// MatchManager es el componente central encargado de gestionar el flujo de la partida, incluyendo:
/// - Controlar el estado actual de la partida 
///   (esperando jugadores, ronda iniciando, ronda activa, ronda terminada) 
///   mediante NetworkVariables para mantener a todos los clientes sincronizados.
/// - Implementar un sistema de temporizadores para cada fase del juego, permitiendo transiciones automáticas entre estados.
/// - Gestionar el respawn de jugadores al inicio de cada ronda, asegurando que todos los jugadores reaparezcan en puntos de spawn designados.
/// - El MatchManager se asegura de que solo el servidor tome decisiones sobre el flujo del juego, mientras que los clientes simplemente reaccionan 
///   a los cambios de estado sincronizados, manteniendo una arquitectura clara y escalable para futuras expansiones (como agregar más fases o condiciones de victoria).
/// </summary>
public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance { get; private set; }

    public enum MatchState { WaitingForPlayers, RoundStarting, RoundActive, RoundEnded }

    [Header("Match State")]
    // Sincronizado automáticamente a todos los clientes
    public NetworkVariable<MatchState> currentState = new NetworkVariable<MatchState>(MatchState.WaitingForPlayers);
    public NetworkVariable<float> stateTimer = new NetworkVariable<float>(0f);

    [Header("Settings")]
    [SerializeField] private int minPlayersToStart = 2;
    [SerializeField] private float startDelay = 3f;
    [SerializeField] private float roundDuration = 60f;
    [SerializeField] private float endDelay = 5f;

    [Header("Spawning")]
    [SerializeField] private Transform[] spawnPoints;

    private Coroutine _matchLoopCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        // Solo el servidor toma decisiones
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (currentState.Value == MatchState.WaitingForPlayers)
        {
            if (NetworkManager.Singleton.ConnectedClients.Count >= minPlayersToStart)
            {
                if (_matchLoopCoroutine != null) StopCoroutine(_matchLoopCoroutine);
                _matchLoopCoroutine = StartCoroutine(MatchLoopRoutine());
            }
        }
    }

    private IEnumerator MatchLoopRoutine()
    {
        while (true)
        {
            // --- FASE 1: PREPARACIÓN ---
            currentState.Value = MatchState.RoundStarting;
            stateTimer.Value = startDelay;

            RespawnAllPlayers();

            while (stateTimer.Value > 0)
            {
                stateTimer.Value -= Time.deltaTime;
                yield return null;
            }

            // --- FASE 2: RONDA ACTIVA ---
            currentState.Value = MatchState.RoundActive;
            stateTimer.Value = roundDuration;

            // La ronda termina por tiempo (o podrías agregar condiciones de muerte aquí)
            while (stateTimer.Value > 0)
            {
                stateTimer.Value -= Time.deltaTime;
                yield return null;
            }

            // --- FASE 3: FIN DE RONDA ---
            currentState.Value = MatchState.RoundEnded;
            stateTimer.Value = endDelay;

            while (stateTimer.Value > 0)
            {
                stateTimer.Value -= Time.deltaTime;
                yield return null;
            }

            // El loop se reinicia para la siguiente ronda
        }
    }

    private void RespawnAllPlayers()
    {
        int spawnIndex = 0;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (spawnPoints.Length == 0) break;

            Transform spawnPoint = spawnPoints[spawnIndex % spawnPoints.Length];

            // Llamamos al script del jugador para que gestione su reseteo local
            if (client.PlayerObject.TryGetComponent(out PlayerStateController playerController))
            {
                playerController.ResetPlayerClientRpc(spawnPoint.position);
            }

            spawnIndex++;
        }
    }
}