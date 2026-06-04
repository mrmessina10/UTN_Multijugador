using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CharacterController), typeof(PlayerHealth))]
public class PlayerNetworkStateMachine : NetworkBehaviour
{
    public NetworkVariable<PlayerNetworkStateType> StateType = new NetworkVariable<PlayerNetworkStateType>(PlayerNetworkStateType.Alive);

    private PlayerNetworkState _currentState;
    private Dictionary<PlayerNetworkStateType, PlayerNetworkState> _states;

    public Rigidbody Rb { get; private set; }
    public CharacterController Cc { get; private set; }
    public PlayerHealth Health { get; private set; }

    private void Awake()
    {
        Rb = GetComponent<Rigidbody>();
        Cc = GetComponent<CharacterController>();
        Health = GetComponent<PlayerHealth>();

        _states = new Dictionary<PlayerNetworkStateType, PlayerNetworkState>
        {
            { PlayerNetworkStateType.Uninitialized, new UninitializedState(this) },
            { PlayerNetworkStateType.Spawning, new SpawningState(this) },
            { PlayerNetworkStateType.Alive, new AliveState(this) },
            { PlayerNetworkStateType.Dead, new DeadState(this) },
            { PlayerNetworkStateType.Respawning, new RespawningState(this) }
        };

        _currentState = _states[PlayerNetworkStateType.Alive];
    }

    public override void OnNetworkSpawn()
    {
        StateType.OnValueChanged += HandleStateChanged;

        if (StateType.Value != PlayerNetworkStateType.Alive)
        {
            ApplyState(StateType.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        StateType.OnValueChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(PlayerNetworkStateType prev, PlayerNetworkStateType current)
    {
        ApplyState(current);
    }

    private void ApplyState(PlayerNetworkStateType newType)
    {
        if (_currentState != null && _currentState.Type == newType) return;

        _currentState?.OnExitAll();
        _currentState = _states[newType];

        _currentState.OnEnterAll();
        if (IsServer) _currentState.OnEnterServer();
        if (IsOwner) _currentState.OnEnterOwner();
    }

    private void Update()
    {
        if (!IsSpawned || _currentState == null) return;

        _currentState.OnTickAll();
        if (IsServer) _currentState.OnTickServer();
        if (IsOwner) _currentState.OnTickOwner();
    }

    public void ChangeStateServer(PlayerNetworkStateType newType)
    {
        if (!IsServer) return;
        StateType.Value = newType;

        if (IsHost)
        {
            ApplyState(newType);
        }
    }

    public void ServerRespawnPlayer(Vector3 spawnPosition)
    {
        if (!IsServer) return;
        if (Health != null) Health.Revive();

        transform.position = spawnPosition + (Vector3.up * 1.2f);

        TeleportOwnerClientRpc(spawnPosition);

        ChangeStateServer(PlayerNetworkStateType.Alive);
    }

    [ClientRpc]
    private void TeleportOwnerClientRpc(Vector3 spawnPosition)
    {
        if (IsOwner && !IsServer)
        {
            transform.position = spawnPosition + (Vector3.up * 1.2f);
        }
    }

    private IEnumerator WaitAndSetAlive()
    {
        yield return new WaitForFixedUpdate();
        ChangeStateServer(PlayerNetworkStateType.Alive);
    }

    public void NotifyDeath()
    {
        if (!IsServer) return;
        ChangeStateServer(PlayerNetworkStateType.Dead);
    }

    public void SetPhysics(bool enabled)
    {
        Debug.Log($"[FSM] SetPhysics({enabled}) — State:{StateType.Value} " +
              $"Server:{IsServer} Owner:{IsOwner}");

        if (!IsOwner) return;

        if (Cc != null) Cc.enabled = enabled;
        if (Rb != null && !enabled)
        {
            if (!Rb.isKinematic)
            {
                Rb.linearVelocity = Vector3.zero;
                Rb.angularVelocity = Vector3.zero;
            }
        }
    }
}