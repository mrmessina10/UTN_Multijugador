using UnityEngine;
using Unity.Netcode;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerRoll : NetworkBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InputReaderSO inputReader;
    [SerializeField] private PlayerStateMachine stateMachine;
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

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (playerCollider == null) playerCollider = GetComponent<CapsuleCollider>();

        _playerHealth = GetComponent<PlayerHealth>();
        _cc = GetComponent<CharacterController>();

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

        StartCoroutine(RollRoutine());
    }

    private IEnumerator RollRoutine()
    {
        _lastRollTime = Time.time;

        if (stateMachine != null) stateMachine.enabled = false;

        // Delegamos las físicas temporales al Rigidbody
        TogglePhysics(true);
        SetRollingServerRpc(true);

        Vector3 rollDirection = new Vector3(_currentMoveInput.x, 0f, _currentMoveInput.y).normalized;
        if (rollDirection == Vector3.zero) rollDirection = transform.forward;

        float rollDuration = rollDistance / rollSpeed;
        float startTime = Time.time;

        // TODO: Animator.SetTrigger("Roll");

        while (Time.time < startTime + rollDuration)
        {
            rb.linearVelocity = new Vector3(rollDirection.x * rollSpeed, 0f, rollDirection.z * rollSpeed);
            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = Vector3.zero;

        if (stateMachine != null && !_playerHealth.isDead.Value)
        {
            stateMachine.enabled = true;
        }

        // Devolvemos el control al CharacterController
        TogglePhysics(false);
        SetRollingServerRpc(false);
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

    private void TogglePhysics(bool isRollingState)
    {
        if (isRollingState)
        {
            if (_cc != null) _cc.enabled = false; // Apagamos CC
            rb.isKinematic = false;               // Activamos Rigidbody dinámico

            gameObject.layer = _rollingLayer;
            rb.useGravity = false;
        }
        else
        {
            rb.isKinematic = true;                // Apagamos Rigidbody dinámico
            if (_cc != null) _cc.enabled = true;  // Encendemos CC

            gameObject.layer = _normalLayer;
            rb.useGravity = _originalGravityState;
        }
    }
}