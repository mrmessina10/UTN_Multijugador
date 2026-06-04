using UnityEngine;
using Unity.Netcode;
using System.Collections;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerRoll : NetworkBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InputReaderSO inputReader;
    [SerializeField] private PlayerMovement playerMovement; // <- ACTUALIZADO AL NUEVO SCRIPT
    [SerializeField] private Rigidbody rb;
    [SerializeField] private CapsuleCollider playerCollider;

    [Header("Roll Settings")]
    [SerializeField] private float rollDistance = 5f;
    [SerializeField] private float rollSpeed = 15f;
    [SerializeField] private float rollCooldown = 2f;

    public NetworkVariable<bool> isRolling = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private PlayerHealth _playerHealth;
    private CharacterController _cc;
    private Vector2 _currentMoveInput;
    private float _lastRollTime;

    private bool _originalGravityState;
    private int _normalLayer;
    private int _rollingLayer;
    private Coroutine _rollCoroutine;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (playerCollider == null) playerCollider = GetComponent<CapsuleCollider>();

        _playerHealth = GetComponent<PlayerHealth>();
        _cc = GetComponent<CharacterController>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();

        _lastRollTime = -rollCooldown;

        _originalGravityState = rb.useGravity;
        _normalLayer = LayerMask.NameToLayer("Player");
        _rollingLayer = LayerMask.NameToLayer("PlayerRolling");

        if (_normalLayer == -1 || _rollingLayer == -1)
        {
            Debug.LogError("Faltan configurar las capas 'Player' o 'PlayerRolling' en Edit -> Project Settings -> Tags and Layers.");
        }
    }

    public override void OnNetworkSpawn()
    {
        isRolling.OnValueChanged += HandleRollStateChanged;

        if (!IsOwner) return;

        inputReader.OnMoveEvent.AddListener(HandleMoveInput);
        inputReader.OnRollEvent.AddListener(HandleRollInput);
    }

    public override void OnNetworkDespawn()
    {
        isRolling.OnValueChanged -= HandleRollStateChanged;

        if (!IsOwner) return;

        inputReader.OnMoveEvent.RemoveListener(HandleMoveInput);
        inputReader.OnRollEvent.RemoveListener(HandleRollInput);
    }

    private void HandleMoveInput(Vector2 input)
    {
        _currentMoveInput = input;
    }

    private void HandleRollInput(bool isPressed)
    {
        if (!isPressed || isRolling.Value) return;
        if (_playerHealth != null && _playerHealth.isDead.Value) return;
        if (Time.time < _lastRollTime + rollCooldown) return;

        // GUARDIA: Si la red apagó nuestro CC (ej. estamos respawneando), no podemos rodar
        if (_cc != null && !_cc.enabled) return;

        if (_rollCoroutine != null) StopCoroutine(_rollCoroutine);
        _rollCoroutine = StartCoroutine(RollRoutine());
    }

    private IEnumerator RollRoutine()
    {
        _lastRollTime = Time.time;

        if (playerMovement != null) playerMovement.enabled = false;

        TogglePhysics(true);
        SetRollingServerRpc(true);

        Vector3 rollDirection = new Vector3(_currentMoveInput.x, 0f, _currentMoveInput.y).normalized;
        if (rollDirection == Vector3.zero) rollDirection = transform.forward;

        float rollDuration = rollDistance / rollSpeed;
        float startTime = Time.time;

        // TODO: Animator.SetTrigger("Roll");

        while (Time.time < startTime + rollDuration)
        {
            // INTERRUPCIÓN DE EMERGENCIA: Si morimos en pleno dash, cortamos el bucle.
            if (_playerHealth != null && _playerHealth.isDead.Value)
            {
                break;
            }

            rb.linearVelocity = new Vector3(rollDirection.x * rollSpeed, 0f, rollDirection.z * rollSpeed);
            yield return new WaitForFixedUpdate();
        }

        // Freno del Rigidbody
        if (!rb.isKinematic) rb.linearVelocity = Vector3.zero;

        // Verificamos si sobrevivimos al roll para saber si debemos devolverle el control al CC
        bool isStillAlive = _playerHealth == null || !_playerHealth.isDead.Value;

        TogglePhysics(false, isStillAlive);
        SetRollingServerRpc(false);

        if (playerMovement != null && isStillAlive)
        {
            playerMovement.enabled = true;
        }
    }

    [ServerRpc]
    private void SetRollingServerRpc(bool state)
    {
        isRolling.Value = state;
    }

    private void HandleRollStateChanged(bool previous, bool current)
    {
        if (IsOwner) return;
        gameObject.layer = current ? _rollingLayer : _normalLayer;
    }

    private void TogglePhysics(bool isRollingState, bool enableCC = true)
    {
        if (isRollingState)
        {
            if (_cc != null) _cc.enabled = false;
            rb.isKinematic = false;

            gameObject.layer = _rollingLayer;
            rb.useGravity = false;
        }
        else
        {
            rb.isKinematic = true;

            // IMPORTANTE: Solo encendemos el CC si no morimos durante el roll. 
            // Si morimos, respetamos que PlayerNetworkStateMachine lo quiere apagado.
            if (_cc != null && enableCC) _cc.enabled = true;

            gameObject.layer = _normalLayer;
            rb.useGravity = _originalGravityState;
        }
    }
}