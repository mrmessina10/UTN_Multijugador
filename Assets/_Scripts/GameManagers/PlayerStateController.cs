using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CharacterController))]
public class PlayerStateController : NetworkBehaviour
{
    private Rigidbody _rb;
    private PlayerHealth _health;
    private CharacterController _cc;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _health = GetComponent<PlayerHealth>();
        _cc = GetComponent<CharacterController>();

        if (_rb != null) _rb.isKinematic = true;
        if (_cc != null) _cc.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            if (_cc != null) _cc.enabled = true;
        }
        else
        {
            StartCoroutine(ClientWaitNetworkSync());
        }
    }

    public void ServerRespawnPlayer(Vector3 spawnPosition)
    {
        if (!IsServer) return;

        if (_health != null) _health.Revive();

        StartCoroutine(ServerTeleportRoutine(spawnPosition));
        PrepareClientTeleportClientRpc(); // <- Llamada corregida
    }

    private IEnumerator ServerTeleportRoutine(Vector3 spawnPosition)
    {
        if (_cc != null) _cc.enabled = false;

        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
        }

        transform.position = spawnPosition + (Vector3.up * 2f);

        yield return new WaitForFixedUpdate();

        if (_cc != null) _cc.enabled = true;
    }

    [ClientRpc]
    private void PrepareClientTeleportClientRpc()
    {
        if (IsServer) return;

        StartCoroutine(ClientWaitNetworkSync());
    }

    private IEnumerator ClientWaitNetworkSync()
    {
        if (_cc != null) _cc.enabled = false;

        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
        }

        yield return new WaitForSeconds(0.3f);

        if (_cc != null) _cc.enabled = true;
    }
}