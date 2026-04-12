using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "InputReader", menuName = "Game/Input/InputReader")]
public class InputReaderSO : ScriptableObject, GameInput.IPlayerActions
{
    public UnityEvent<Vector2> OnMoveEvent;
    public UnityEvent<Vector2> OnAimEvent;
    public UnityEvent<bool> OnShootEvent;
    public UnityEvent OnCrouchEvent;

    private GameInput _gameInput;

    private void OnEnable()
    {
        if (_gameInput == null)
        {
            _gameInput = new GameInput();
            _gameInput.Player.SetCallbacks(this);
        }
        _gameInput.Player.Enable();
    }

    private void OnDisable()
    {
        _gameInput.Player.Disable();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        OnMoveEvent?.Invoke(context.ReadValue<Vector2>());
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        OnAimEvent?.Invoke(context.ReadValue<Vector2>());
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            OnShootEvent?.Invoke(true);
        }
        else if (context.phase == InputActionPhase.Canceled)
        {
            OnShootEvent?.Invoke(false);
        }
    }
    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            OnCrouchEvent?.Invoke();
        }
    }
}