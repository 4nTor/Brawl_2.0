using UnityEngine;

public class PlayerJumpLandState : PlayerBaseState
{
    public PlayerJumpLandState(PlayerStateMachine ctx, PlayerStateFactory factory) : base(ctx, factory) { }

    public override void EnterState()
    {
        ctx.animator.Play("JumpLand");
        //Debug.Log("Jump Land Entered");
    }

    public override void UpdateState()
    {
        AnimatorStateInfo info = ctx.animator.GetCurrentAnimatorStateInfo(0);
        
        // Wait if we are still transitioning INTO the land animation
        if (ctx.animator.IsInTransition(0) && !info.IsName("JumpLand"))
        {
            return;
        }

        // Check if animation finished (or is very close to end)
        // If the animation is named differently, this condition might fail. 
        // User should ensure state name "JumpLand" in Animator.
        if (info.IsName("JumpLand") && info.normalizedTime >= 0.9f)
        {
            if (ctx.moveInput.magnitude > 0.1f)
                ctx.SwitchState(factory.Run());
            else
                ctx.SwitchState(factory.Idle());
        }
    }

    public override void ExitState() { }

    public override bool CanRotate() => false;
    public override bool CanMove() => false;
}
