using UnityEngine;

public class PlayerJumpFallState : PlayerBaseState
{
    public PlayerJumpFallState(PlayerStateMachine ctx, PlayerStateFactory factory) : base(ctx, factory) { }

    public override void EnterState()
    {
        ctx.animator.Play("JumpFall");
        //Debug.Log("Jump Fall Entered");
    }

    public override void UpdateState()
    {
        if (ctx.isGrounded)
        {
             if(superState != null)
             {
                 superState.SetSubState(factory.JumpLand());
             }
        }
    }

    public override void ExitState() { }

    public override bool CanRotate() => false;
}
