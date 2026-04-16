using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections;

public class PlayerHealth : NetworkBehaviour, IDamageable
{
    [Header("Dependencies")]
    [SerializeField] private PlayerStateMachine stateMachine;
    [SerializeField] private Collider playerCollider;
    // [SerializeField] private Animator animator; // TODO: Para futuras animaciones

    [Header("Health Settings")]
    [Tooltip("Permite ajustar el TTK de la partida desde el Inspector (Requisito 1)")]
    [SerializeField] private int maxHealth = 5;
    public int MaxHealth => maxHealth; // Propiedad para que la UI lea el valor

    // --- VARIABLES DE RED ---
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

    // Cualquier script de UI puede suscribirse a estos eventos sin que PlayerHealth sepa que existen
    public event Action<int, int> OnHealthChanged; // Pasa: (vida actual, vida máxima)
    public event Action OnPlayerDied;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
            isDead.Value = false;
        }

        // Suscripción a cambios de estado en red
        currentHealth.OnValueChanged += HandleHealthChanged;
        isDead.OnValueChanged += HandleDeathState;

        OnHealthChanged?.Invoke(currentHealth.Value, maxHealth);
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= HandleHealthChanged;
        isDead.OnValueChanged -= HandleDeathState;
    }

    // --- LÓGICA DE DAÑO ---
    public void TakeDamage(float amount, StatusEffect effect, float effectDuration)
    {
        if (!IsServer || isDead.Value) return;

        currentHealth.Value -= Mathf.RoundToInt(amount);

        if (currentHealth.Value <= 0)
        {
            Die();
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

    private void Die()
    {
        currentHealth.Value = 0;
        isDead.Value = true;
    }

    // --- SISTEMA DE CROWD CONTROL ---
    [ClientRpc]
    private void ApplyStatusClientRpc(StatusEffect effect, float duration, ClientRpcParams rpcParams = default)
    {
        if (!IsOwner) return; // Solo el cliente afectado ejecuta la corrutina
        StartCoroutine(StatusEffectCoroutine(effect, duration));
    }

    private IEnumerator StatusEffectCoroutine(StatusEffect effect, float duration)
    {
        if (stateMachine == null) yield break;

        // APLICAR
        if (effect == StatusEffect.Stun) stateMachine.enabled = false;
        else if (effect == StatusEffect.Slow)
        {
            // stateMachine.SetSpeedMultiplier(0.5f); 
        }

        yield return new WaitForSeconds(duration);

        // REMOVER
        if (effect == StatusEffect.Stun && !isDead.Value) stateMachine.enabled = true;
        else if (effect == StatusEffect.Slow && !isDead.Value)
        {
            // stateMachine.SetSpeedMultiplier(1f);
        }
    }

    // --- CALLBACKS VISUALES / EVENTOS ---
    private void HandleHealthChanged(int previousValue, int newValue)
    {
        OnHealthChanged?.Invoke(newValue, maxHealth);
    }

    private void HandleDeathState(bool wasDead, bool isNowDead)
    {
        if (isNowDead)
        {
            if (playerCollider != null) playerCollider.enabled = false;

            if (IsOwner && stateMachine != null)
            {
                stateMachine.enabled = false;
            }

            if (IsOwner) OnPlayerDied?.Invoke();

            // TODO: animator.SetTrigger("Die");
        }
    }
}