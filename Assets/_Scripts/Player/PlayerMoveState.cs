using UnityEngine;

public class PlayerMoveState : BaseState
{
    private float moveSpeed = 6f;

    public PlayerMoveState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        //
    }

    public override void Tick()
    {
        CalculateMovement();
    }

    public override void Exit()
    {
        //
    }

    public void CalculateMovement()
    {
        Vector2 input = stateMachine.CurrentMovementInput;
        Vector3 moveDirection = new Vector3(input.x, 0f, input.y);

        // normalizo el movimiento
        if (moveDirection.sqrMagnitude > 0f)
        {
            moveDirection.Normalize();
        }

        // Medida de seguridad absoluta: forzamos Y a 0
        moveDirection.y = 0f;

        // Único llamado de movimiento, exclusivamente en los ejes X y Z
        stateMachine.CharacterController.Move(moveDirection * (moveSpeed * Time.deltaTime));
    }
}