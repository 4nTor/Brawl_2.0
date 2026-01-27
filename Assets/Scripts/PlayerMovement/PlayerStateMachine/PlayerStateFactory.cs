using UnityEngine;

public class PlayerStateFactory
{
    private PlayerStateMachine ctx;

    public PlayerStateFactory(PlayerStateMachine context)
    {
        ctx = context;
    }

    public PlayerBaseState Idle() => new PlayerIdleState(ctx, this);
    public PlayerBaseState Run() => new PlayerRunState(ctx, this);
    public PlayerBaseState Jump() => new PlayerJumpState(ctx, this);
    public PlayerBaseState JumpRise() => new PlayerJumpRiseState(ctx, this);
    public PlayerBaseState JumpFall() => new PlayerJumpFallState(ctx, this);
    public PlayerBaseState JumpLand() => new PlayerJumpLandState(ctx, this);
    public PlayerBaseState Attack(AttackData attackData) => new PlayerAttackState(ctx, this, attackData);
    public PlayerBaseState Grab() => new PlayerGrabState(ctx, this);
    public PlayerPowerUpState PowerUp(PowerUpType type, float duration)
    {
        return new PlayerPowerUpState(ctx, this, type, duration);
    }
    /*--------------------------------------------------------------------------*/
    public PlayerBaseState Defense() => new PlayerDefenseState(ctx, this);
    public PlayerBaseState ParrySub() => new PlayerParrySubState(ctx, this);
    public PlayerBaseState BlockSub() => new PlayerBlockSubState(ctx, this);
    /*--------------------------------------------------------------------------*/

}
