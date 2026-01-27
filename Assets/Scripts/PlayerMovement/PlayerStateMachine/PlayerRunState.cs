using UnityEngine;

public class PlayerRunState : PlayerBaseState
{
    public PlayerRunState(PlayerStateMachine ctx, PlayerStateFactory factory) : base(ctx, factory) { }

    public override void EnterState()
    {
        ctx.animator.SetFloat("Speed", ctx.Speed * 2f);
    }

    public override void UpdateState()
    {
        // Movement is now handled in PlayerStateMachine.HandleMovement()
        // We just check for transitions here

        if (ctx.isDashing) return;

        // If no input, switch to Idle
        if (ctx.moveInput.magnitude <= 0.1f)
            ctx.SwitchState(factory.Idle());
    }

    public override void ExitState() { }

    public override void HandleJumpInput()
    {
        // Check if we can jump (Grounded OR Coyote Time)
        if (ctx.isGrounded || ctx.coyoteTimeCounter > 0f)
        {
            ctx.SwitchState(factory.Jump());
        }
    }
}
