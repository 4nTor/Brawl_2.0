using UnityEngine;

public class PlayerJumpRiseState : PlayerBaseState
{
    public PlayerJumpRiseState(PlayerStateMachine ctx, PlayerStateFactory factory) : base(ctx, factory) { }

    public override void EnterState()
    {
        ctx.animator.Play("JumpRise");
        ctx.ApplyJumpVelocity();
        //Debug.Log("Jump Rise Entered");
    }

    public override void UpdateState()
    {
        if (ctx.velocity.y < 0)
        {
             // Transition to Fall Sub-State within the Jump Super-State
             if(superState != null)
             {
                 superState.SetSubState(factory.JumpFall());
             }
        }
    }

    public override void ExitState() { }
    
    public override bool CanRotate() => false;
}
