using UnityEngine;

public class PlayerWalkState : PlayerBaseState
{
    public PlayerWalkState(PlayerStateMachine ctx, PlayerStateFactory factory) : base(ctx, factory) { }

    public override void EnterState()
    {
        ctx.animator.SetFloat("Speed", ctx.Speed);
        
        // Only CrossFade if we are coming from an Attack or Jump
        if (ctx.animator.GetCurrentAnimatorStateInfo(0).IsName("AttackA") || 
            ctx.animator.GetCurrentAnimatorStateInfo(0).IsName("AttackB"))
        {
            ctx.animator.CrossFadeInFixedTime("Idle", 0.15f);
        }
    }

    public override void UpdateState()
    {
        if (ctx.moveInput.magnitude <= 0.1f)
        {
            ctx.SwitchState(factory.Idle());
            return;
        }
        
        // Movement is handled centrally
    }

    public override void ExitState() { }

    public override void HandleJumpInput()
    {
        if (ctx.isGrounded || ctx.coyoteTimeCounter > 0f)
            ctx.SwitchState(factory.Jump());
    }
}
