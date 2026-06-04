using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InputReaderSO inputReader;

    private CharacterController _cc;
    private Vector2 _currentInput;
    private float _moveSpeed = 6f;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        if (inputReader != null)
        {
            inputReader.OnMoveEvent.AddListener(HandleMoveInput);
        }
        else
        {
            Debug.LogError($"[PlayerMovement] ERROR: Falta asignar el InputReader en el prefab {gameObject.name}");
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        if (inputReader != null)
            inputReader.OnMoveEvent.RemoveListener(HandleMoveInput);
    }

    private void HandleMoveInput(Vector2 input)
    {
        _currentInput = input;
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (_cc == null || !_cc.enabled) return;

        Vector3 moveDirection = new Vector3(_currentInput.x, 0f, _currentInput.y);

        if (moveDirection.sqrMagnitude > 0f)
        {
            moveDirection.Normalize();
        }

        _cc.Move(moveDirection * (_moveSpeed * Time.deltaTime));
    }
}