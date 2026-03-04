using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Combat/Attack")]
public class AttackData : ScriptableObject
{
    public string attackName;         // e.g., "Headbutt"
    public string inputActionName;    // e.g., "Attack1", "HeavyAttack"
    public int damage;
    public float range;
    public float cooldown;
    public AnimationClip animation;
    public float pushForce = 5f; // set in your AttackData asset or prefab

    [Header("Timings (Seconds)")]
    public float startupTime;
    public float activeTime;
    public float recoveryTime;
    
    [Header("Cancel Window (Relative to Recovery Start)")]
    public float cancelStartTime; 
    public float cancelEndTime;

    [Header("Combos")]
    public List<ComboRoute> comboRoutes; // List of allowed follow-ups

    [Header("Feel")]
    public float hitStopDuration = 0.1f;

    [Header("Animation Compensation")]
    [Tooltip("Moves the character forward by this amount at the end of the attack to match a forward-leaning pose.")]
    public float stepOffset;

    public string AttackOriginName;
    //public GameObject attackOrigin;
}

[System.Serializable]
public struct ComboRoute
{
    public string inputAction; // e.g., "Attack1", "Attack2"
    public AttackData targetAttack;
}
