using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// PlayerHealth es el componente encargado de manejar la vida del jugador, su muerte y efectos de estado relacionados.
/// - Permite ajustar el TTK desde el Inspector gracias a la variable maxHealth.
/// - Sincroniza la vida y el estado de muerte a través de NetworkVariables para que todos los clientes estén actualizados.
/// - Implementa el método TakeDamage para recibir daño, aplicando efectos de estado como Stun o Slow según sea necesario.
/// - Dispara eventos OnHealthChanged y OnPlayerDied para que otros sistemas (como la UI) puedan reaccionar sin acoplarse directamente a PlayerHealth.
/// - Al morir, desactiva el collider y el state machine del jugador para evitar interacciones no deseadas, y al revivir los reactiva.
/// - El sistema de Crowd Control se maneja mediante corrutinas que aplican y remueven los efectos después de su duración,
///   asegurando que solo el cliente afectado ejecute la lógica correspondiente.
/// </summary>
public class PlayerHealth : NetworkBehaviour, IDamageable
{
    [Header("Dependencies")]
    // 1. REFERENCIA ACTUALIZADA AL NUEVO SCRIPT
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Collider playerCollider;

    [Header("Health Settings")]
    [Tooltip("Permite ajustar el TTK de la partida desde el Inspector (Requisito 1)")]
    [SerializeField] private int maxHealth = 5;
    public int MaxHealth => maxHealth;

    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        5,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public event Action<int, int> OnHealthChanged;
    public event Action OnPlayerDied;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
            isDead.Value = false;
        }

        currentHealth.OnValueChanged += HandleHealthChanged;
        isDead.OnValueChanged += HandleDeathState;

        OnHealthChanged?.Invoke(currentHealth.Value, maxHealth);
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= HandleHealthChanged;
        isDead.OnValueChanged -= HandleDeathState;
    }

    public void TakeDamage(float amount, StatusEffect effect, float effectDuration, ulong killerId)
    {
        if (!IsServer || isDead.Value) return;

        currentHealth.Value -= Mathf.RoundToInt(amount);

        if (currentHealth.Value <= 0)
        {
            currentHealth.Value = 0;
            isDead.Value = true;

            if (TryGetComponent(out PlayerNetworkStateMachine stateMachine))
            {
                stateMachine.NotifyDeath();
            }
            else
            {
                Debug.LogError($"[PlayerHealth] ERROR FATAL: El prefab {gameObject.name} no tiene asignado el componente PlayerNetworkStateMachine.");
            }

            if (MatchManager.Instance != null)
            {
                MatchManager.Instance.RecordDeathAndRespawn(OwnerClientId, killerId);
            }
        }
        else if (effect != StatusEffect.None)
        {
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { OwnerClientId } }
            };
            ApplyStatusClientRpc(effect, effectDuration, clientRpcParams);
        }
    }

    [ClientRpc]
    private void ApplyStatusClientRpc(StatusEffect effect, float duration, ClientRpcParams rpcParams = default)
    {
        if (!IsOwner) return;
        StartCoroutine(StatusEffectCoroutine(effect, duration));
    }

    private IEnumerator StatusEffectCoroutine(StatusEffect effect, float duration)
    {
        if (playerMovement == null) yield break;

        // 2. APLICAR CROWD CONTROL AL NUEVO SCRIPT
        if (effect == StatusEffect.Stun) playerMovement.enabled = false;

        yield return new WaitForSeconds(duration);

        // 3. REMOVER CROWD CONTROL
        if (effect == StatusEffect.Stun && !isDead.Value) playerMovement.enabled = true;
    }

    private void HandleHealthChanged(int previousValue, int newValue)
    {
        OnHealthChanged?.Invoke(newValue, maxHealth);
    }

    private void HandleDeathState(bool wasDead, bool isNowDead)
    {
        if (isNowDead)
        {
            if (playerCollider != null) playerCollider.enabled = false;
            // 4. APAGAR MOVIMIENTO AL MORIR
            if (IsOwner && playerMovement != null) playerMovement.enabled = false;
            if (IsOwner) OnPlayerDied?.Invoke();
        }
        else
        {
            if (playerCollider != null) playerCollider.enabled = true;
            // 5. ENCENDER MOVIMIENTO AL REVIVIR
            if (IsOwner && playerMovement != null) playerMovement.enabled = true;

            OnHealthChanged?.Invoke(currentHealth.Value, maxHealth);
        }
    }

    public void Revive()
    {
        if (!IsServer) return;

        currentHealth.Value = maxHealth;
        isDead.Value = false;
    }
}