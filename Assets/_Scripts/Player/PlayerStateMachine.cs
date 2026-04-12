using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;

[RequireComponent(typeof(CharacterController))]
public class PlayerStateMachine : NetworkBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InputReaderSO _inputReader;

    public CharacterController CharacterController { get; private set; }
    public Vector2 CurrentMovementInput { get; private set; }

    private BaseState _currentState;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        CharacterController = GetComponent<CharacterController>();
        _inputReader.OnMoveEvent.AddListener(HandleMoveInput);

        SwitchState(new PlayerMoveState(this));
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        _inputReader.OnMoveEvent.RemoveListener(HandleMoveInput);
    }

    private void Update()
    {
        if (!IsOwner) return;
        _currentState?.Tick();
    }

    public void SwitchState(BaseState newState)
    {
        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();
    }

    private void HandleMoveInput(Vector2 input)
    {
        CurrentMovementInput = input;
    }
}
