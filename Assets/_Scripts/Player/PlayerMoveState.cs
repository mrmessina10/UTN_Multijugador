using UnityEngine;

public class PlayerMoveState : BaseState
{
    private float moveSpeed = 6f;
    private float gravity = -9.81f;
    private Vector3 velocity;

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

        //normalizo el movimiento
        if (moveDirection.sqrMagnitude > 0f)
        {
            moveDirection.Normalize();
        }

        stateMachine.CharacterController.Move(moveDirection * (moveSpeed * Time.deltaTime));

        if (stateMachine.CharacterController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Pequeña fuerza para mantener al jugador pegado al suelo
        }

        velocity.y += gravity * Time.deltaTime;

        stateMachine.CharacterController.Move(velocity * Time.deltaTime);
    }
}
