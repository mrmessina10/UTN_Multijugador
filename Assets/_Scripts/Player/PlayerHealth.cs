using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class PlayerHealth : NetworkBehaviour, IDamageable
{
    [Header("Dependencies")]
    [SerializeField] private PlayerStateMachine stateMachine;
    [SerializeField] private Collider playerCollider;

    [Header("Health Settings")]
    private const int MAX_HEALTH = 3;

    // NetworkVariables
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        MAX_HEALTH,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        currentHealth.OnValueChanged += HandleHealthChanged;
        isDead.OnValueChanged += HandleDeathState;
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= HandleHealthChanged;
        isDead.OnValueChanged -= HandleDeathState;
    }

    // Interfaz IDamageable
    public void TakeDamage(float amount, StatusEffect effect, float effectDuration)
    {
        // Solo el servidor procesa el daño para evitar que se reste vida doble
        if (!IsServer || isDead.Value) return;

        currentHealth.Value -= Mathf.RoundToInt(amount);

        if (currentHealth.Value <= 0)
        {
            Die();
        }
        else if (effect != StatusEffect.None)
        {
            // Si hay un efecto, el Servidor le avisa ÚNICAMENTE al dueño de este jugador que se aplique el estado
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

    // --- MANEJO DE ESTADOS (STUN / SLOW) ---

    [ClientRpc]
    private void ApplyStatusClientRpc(StatusEffect effect, float duration, ClientRpcParams rpcParams = default)
    {
        // Verificación de seguridad: solo me afecto a mí mismo
        if (!IsOwner) return;

        StartCoroutine(StatusEffectCoroutine(effect, duration));
    }

    private IEnumerator StatusEffectCoroutine(StatusEffect effect, float duration)
    {
        if (stateMachine == null) yield break;

        //APLICAR EL EFECTO
        if (effect == StatusEffect.Stun)
        {
            Debug.Log("[STATUS] Jugador Stuneado!");
            stateMachine.enabled = false; // Apagamos el input/movimiento
        }
        else if (effect == StatusEffect.Slow)
        {
            Debug.Log("[STATUS] Jugador Ralentizado!");
            // Acá dependerá de tu PlayerStateMachine. Ejemplo genérico:
            // stateMachine.SetSpeedMultiplier(0.5f); 
        }

        // ESPERAR LA DURACIÓN
        yield return new WaitForSeconds(duration);

        // REMOVER EL EFECTO
        if (effect == StatusEffect.Stun && !isDead.Value)
        {
            Debug.Log("[STATUS] Fin del Stun!");
            stateMachine.enabled = true; // Devolvemos el control
        }
        else if (effect == StatusEffect.Slow && !isDead.Value)
        {
            Debug.Log("[STATUS] Fin del Slow!");
            // stateMachine.SetSpeedMultiplier(1f);
        }
    }

    // --- CALLBACKS VISUALES PARA TODOS LOS CLIENTES ---

    private void HandleHealthChanged(int previousValue, int newValue)
    {
        Debug.Log($"Jugador {OwnerClientId} recibió daño. HP Restante: {newValue}");
        // TODO: Actualizar los 3 corazones en la UI
    }

    private void HandleDeathState(bool wasDead, bool isNowDead)
    {
        if (isNowDead)
        {
            Debug.Log($"Jugador {OwnerClientId} ha caído al piso.");

            // Deshabilitar físicas para que los proyectiles pasen de largo
            if (playerCollider != null) playerCollider.enabled = false;

            // Si soy el dueño, me quito el control
            if (IsOwner && stateMachine != null)
            {
                stateMachine.enabled = false;
            }
            // PROXIMAMENTE: Disparar animación de caer al suelo
        }
    }
}
