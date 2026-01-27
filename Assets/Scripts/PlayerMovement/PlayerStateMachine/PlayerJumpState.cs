using UnityEngine;

public class PlayerJumpState : PlayerBaseState
{
    public PlayerJumpState(PlayerStateMachine ctx, PlayerStateFactory factory) : base(ctx, factory) { }

    public override void EnterState()
    {
        Debug.Log("[JumpState] Entered Jump SuperState");
        SetSubState(factory.JumpRise());
        ctx.animator.SetBool("isJump", true);
    }
    
    public override void UpdateState()
    {
        // Check if SubState has finished (e.g., JumpLand transition to Idle/Run)
        // This is handled inside Sub-States calling ctx.SwitchState() or similar?
        // OR: Use CheckSwitchStates() if we want centralized logic.
        
        // Ensure substate is updated
        base.UpdateState();
    }

    public override void ExitState()
    {
        Debug.Log("[JumpState] Exiting Jump SuperState");
        ctx.animator.SetBool("isJump", false);
        // Clean up substate
        if(currentSubState != null)
        {
            currentSubState.ExitState();
        }
    }
    
    public override bool CanRotate()
    {
        return currentSubState != null ? currentSubState.CanRotate() : base.CanRotate();
    }
}
