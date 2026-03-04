using UnityEngine;

public class PlayerIdleState : PlayerBaseState
{
    public PlayerIdleState(PlayerStateMachine ctx, PlayerStateFactory factory) : base(ctx, factory) { }

    public override void EnterState()
    {
        ctx.animator.SetFloat("Speed", 0f);
        
        // Only CrossFade if we are coming from an Attack or Jump (where locomotion isn't playing)
        if (ctx.animator.GetCurrentAnimatorStateInfo(0).IsName("AttackA") || 
            ctx.animator.GetCurrentAnimatorStateInfo(0).IsName("AttackB"))
        {
            ctx.animator.CrossFadeInFixedTime("Idle", 0.15f);
        }
    }

    public override void UpdateState()
    {
        if (ctx.moveInput.magnitude > 0.1f)
            ctx.SwitchState(factory.Run());
    }

    public override void ExitState() { }

    public override void HandleJumpInput()
    {
        if (ctx.isGrounded || ctx.coyoteTimeCounter > 0f)
        {
            Debug.Log("[IdleState] Switching to Jump");
            ctx.SwitchState(factory.Jump());
        }
        else
        {
             Debug.Log("[IdleState] Jump Ignored - Not Grounded");
        }
    }
}
