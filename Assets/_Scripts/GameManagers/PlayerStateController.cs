using UnityEngine;
using Unity.Netcode;

/// <summary>
/// PlayerStateController es el componente encargado de manejar el estado del jugador, específicamente su posición y física al morir y revivir.
/// - Permite teletransportar al jugador a un punto de spawn específico al revivir, asegurando que el jugador reaparezca en la ubicación correcta.
/// - Al revivir, también se encargará de eliminar cualquier inercia física previa (como derrapes o caídas) para que el jugador no reaparezca con movimientos no deseados.
/// - El método ResetPlayerClientRpc se llama desde el servidor para que cada cliente ejecute la lógica de reseteo localmente, manteniendo la sincronización en red.
/// - Este script se mantiene separado de PlayerHealth para seguir el principio de responsabilidad única, permitiendo que cada componente se enfoque en su función específica.
///   Esto también facilita futuras expansiones, como agregar efectos visuales o sonoros al revivir sin afectar la lógica de salud.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerStateController : NetworkBehaviour
{
    private Rigidbody _rb;
    private PlayerHealth _health;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _health = GetComponent<PlayerHealth>();
    }

    [ClientRpc]
    public void ResetPlayerClientRpc(Vector3 spawnPosition)
    {
        // 1. Teletransportamos al jugador (NGO requiere que el dueño o el server mueva el Transform)
        transform.position = spawnPosition;

        // 2. Matamos inercias físicas (frenamos derrapes o caídas previas)
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        // 3. El servidor revive al jugador (solo el server puede escribir en NetworkVariables)
        if (IsServer && _health != null)
        {
            _health.Revive();
        }
    }
}