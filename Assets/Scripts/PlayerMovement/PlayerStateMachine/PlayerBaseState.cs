public abstract class PlayerBaseState
{
    protected PlayerStateMachine ctx;
    protected PlayerStateFactory factory;

    protected PlayerBaseState currentSubState;
    protected PlayerBaseState superState;

    protected bool isRootState = false;

    public PlayerBaseState(PlayerStateMachine currentContext, PlayerStateFactory stateFactory)
    {
        ctx = currentContext;
        factory = stateFactory;
    }

    public virtual void EnterState() { }
    public virtual void UpdateState() { currentSubState?.UpdateState(); }
    public virtual void ExitState() { currentSubState?.ExitState(); }

    public virtual void HandleJumpInput() { }

    public void SetSubState(PlayerBaseState newSubState)
    {
        currentSubState = newSubState;
        newSubState.superState = this;
        newSubState.EnterState();
    }

    public void SetSuperState(PlayerBaseState newSuperState)
    {
        superState = newSuperState;
    }

    public void SetIsRootState(bool isRoot) => isRootState = isRoot;

    public virtual bool CanRotate()
    {
        return currentSubState != null ? currentSubState.CanRotate() : true;
    }

    public virtual bool CanMove()
    {
        return currentSubState != null ? currentSubState.CanMove() : true;
    }
}
