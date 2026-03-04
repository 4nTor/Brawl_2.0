using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;

public class PlayerAttackState : PlayerBaseState
{
    private AttackData data;
    private float timer;
    private enum AttackPhase { Startup, Active, Recovery, Finished }
    private AttackPhase currentPhase;
    private bool hasAppliedDamage;
    
    public PlayerAttackState(PlayerStateMachine ctx, PlayerStateFactory factory, AttackData attack)
        : base(ctx, factory)
    {
        data = attack;
    }

    public override void EnterState()
    {
        // Ping-Pong Animation State Name
        string stateName = (ctx.attackAnimIndex == 0) ? "AttackA" : "AttackB";
        
        // Get the placeholder clip for this state
        AnimationClip placeholderClip = (ctx.attackAnimIndex == 0) ? ctx.attackAPlaceholder : ctx.attackBPlaceholder;
        
        // Override it with the actual attack animation
        if (placeholderClip != null && data.animation != null)
        {
            ctx.runtimeOverride[placeholderClip] = data.animation;
        }

        ctx.animator.Play(stateName, 0, 0f); 
        ctx.animator.SetBool("IsAttacking", true);
        
        timer = 0f;
        currentPhase = AttackPhase.Startup;
        hasAppliedDamage = false;

        // Enable Root Motion for attacks
        ctx.animator.applyRootMotion = true;
    }

    public override void UpdateState()
    {
        timer += Time.deltaTime;

        switch (currentPhase)
        {
            case AttackPhase.Startup:
                if (timer >= data.startupTime)
                {
                    currentPhase = AttackPhase.Active;
                }
                break;

            case AttackPhase.Active:
                // Apply Damage ONCE during active frame (or continuous?) usually once per swing
                if (!hasAppliedDamage)
                {
                    ApplyDamage();
                    hasAppliedDamage = true;
                }

                if (timer >= data.startupTime + data.activeTime)
                {
                    currentPhase = AttackPhase.Recovery;
                }
                break;

            case AttackPhase.Recovery:
                // Cancel Window Logic
                float timeSinceRecoveryStart = timer - (data.startupTime + data.activeTime);
                
                if (timeSinceRecoveryStart >= data.cancelStartTime && timeSinceRecoveryStart <= data.cancelEndTime)
                {
                    // Only check if there's actually something buffered
                    if (ctx.inputBuffer.Count > 0)
                    {
                        // Check for buffered input that matches combo routes
                        if (data.comboRoutes != null && data.comboRoutes.Count > 0)
                        {
                            // Apply compensation before switching to next attack in combo
                            if (data.stepOffset != 0)
                            {
                                ctx.ApplyStepOffset(data.stepOffset);
                            }

                            ctx.ConsumeBufferedInputForCombo(data.comboRoutes);
                        }
                    }
                }

                if (timer >= data.startupTime + data.activeTime + data.recoveryTime)
                {
                    currentPhase = AttackPhase.Finished;
                    
                    // Apply animation compensation displacement
                    if (data.stepOffset != 0)
                    {
                        ctx.ApplyStepOffset(data.stepOffset);
                    }

                    // Clear buffer so we don't auto-start a new attack if they weren't comboing
                    ctx.ClearInputBuffer();
                    
                    // Exit to locomotion (Idle or Run)
                    if (ctx.moveInput.magnitude > 0.1f)
                        ctx.SwitchState(factory.Run());
                    else
                        ctx.SwitchState(factory.Idle());
                }
                break;
        }
    }

    public override void ExitState()
    {
        ctx.animator.SetBool("IsAttacking", false);
        
        // Disable Root Motion when leaving attack
        ctx.animator.applyRootMotion = false;
    }

    public void ApplyDamage()
    {
        // ... (Existing damage logic) ...
        if (!ctx.attackOriginMap.TryGetValue(data.AttackOriginName, out var origin))
        {
            // Debug.LogWarning($"No attack origin found for '{data.AttackOriginName}'. Using player transform as fallback.");
            origin = ctx.transform;
        }

        Collider[] hits = Physics.OverlapSphere(origin.position, data.range, ctx.EnemyLayer);
        bool hitSomething = false;
        
        foreach (var hit in hits)
        {
            /*---------------------------------------------------------------------*/

            AttackEvents.Broadcast(ctx.gameObject, hit.gameObject, data);

            if (hit.TryGetComponent<PlayerStateMachine>(out var psm))
            {
                if (psm.wasParried)
                {
                    Debug.Log("Skipped due to parry");
                    continue; // Skips rest of loop body moves to next hit
                }
            }

            /*---------------------------------------------------------------------*/
            
            // Apply damage
            if (hit.TryGetComponent<PlayerHealth>(out var health))
            {
                health.transform.gameObject.GetComponent<PhotonView>().RPC("TakeDamage", RpcTarget.All, data.damage);
                hitSomething = true;
            }
        
            // Apply force if it has Rigidbody
        
            if (hit.attachedRigidbody != null)
            {
                Vector3 pushDir = (hit.transform.position - origin.position).normalized;
                hit.attachedRigidbody.AddForce(pushDir * data.pushForce, ForceMode.Impulse);
                hitSomething = true;
            }
        }
        
        if (hitSomething && data.hitStopDuration > 0)
        {
            ctx.TriggerHitStop(data.hitStopDuration);
        }
    }

    public override bool CanMove() => false;
    public override bool CanRotate() => false; // Usually lock rotation during attack too
}
