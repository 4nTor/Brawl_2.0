using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStateMachine : MonoBehaviourPun
{
    private PlayerControls _playerControls;
    
    private float turnVelocity;

    [Header("Player Name")]
    public string PlayerName;
    public TextMeshPro PlayerNameText;

    [Header("Character Controller")]
    public CharacterController characterController;

    [Header("Camera")]
    public Camera cam;

    [Header("Player Animator")]
    public Animator animator;
    public AnimatorOverrideController baseOverrideController;
    [HideInInspector] public AnimatorOverrideController runtimeOverride;

    [Header("Parameters")]
    public float Speed = 5f;
    public float TurnVelocity = 0.1f;
    public float gravity = -9.81f;
    public float jumpHeight = 3f;
    public float groundDistance = 0.1f;
    public Transform groundCheck;
    public LayerMask gLayer;

    [Header("Enemy and Player Layer")]
    public LayerMask EnemyLayer;
    public LayerMask PlayerLayer;

    [Header("Attack Data")]
    public List<AttackData> attacks;

    [Header("Attack Origins")]
    public List<AttackOriginEntry> attackOrigins = new();
    public Dictionary<string, Transform> attackOriginMap = new();

    private Dictionary<string, AttackData> attackMap;
    private Dictionary<string, float> cooldowns;

    [Header("Grab Parameters")]
    public float grabRadius = 1.2f;
    public float grabDuration = 2.5f;
    public float grabCooldown = 10f;
    public LayerMask pushableLayer;
    public Transform grabPoint;
    [HideInInspector] public float grabCooldownTimer = 0f;
    private bool isGrabbing = false;

    [Header("Physics & Movement")]
    public float acceleration = 25f;
    public float deceleration = 30f;
    public float airAcceleration = 10f; // Lower than ground
    public float airDeceleration = 0f;  // Usually 0 or low friction in air
    public float maxFallSpeed = 40f;
    
    [Header("Jump Physics")]
    public float fallGravityMult = 5f;     // Falling = High gravity
    public float jumpCutGravityMult = 5f;    // Release jump = Very high gravity? Or just higher
    // User requested: "Rising = Low, Falling = High, Fast fall = Very high" -> handled by logic
    
    [Header("Jump Assist")]
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.2f;
    [HideInInspector] public float coyoteTimeCounter;
    [HideInInspector] public float jumpBufferCounter;
    
    private Vector3 horizontalVelocity; // XZ velocity

    [Header("Power Ups")]
    public GameObject bubbleShieldEffect;
    public bool isShieldActive = false;
    public float bubbleShieldDuration = 10f;
    public float airPullDistance = 10f;
    public float airPullDuration = 0.4f;
    [HideInInspector] public bool isDashing = false;

    [Header("Inputs")]
    // public variables for classes
    public Vector2 moveInput;
    public Vector3 velocity;
    public bool isJumpPressed;
    public bool isGrounded;
    public bool isAttacking;

    /*----------------------------------------------------------------*/
    public bool isBlockHeld;
    public bool isBlockJustPressed;
    public bool isParryWindowOpen = false;
    public bool wasParried = false;
    private float parryWindowStartTime;
    [SerializeField]private float parryWindowDuration = 0.5f;
    /*----------------------------------------------------------------*/

    [Header("Combo System")]
    public Queue<string> inputBuffer = new Queue<string>();
    private float bufferTimer;
    public float bufferWindow = 0.25f;
    
    [HideInInspector] public int attackAnimIndex = 0; // 0 or 1 for Ping Pong
    
    [Header("Attack Animation Placeholders")]
    [Tooltip("Assign the placeholder clips used in AttackA and AttackB states (e.g., fist_1 and fist_2)")]
    public AnimationClip attackAPlaceholder;
    public AnimationClip attackBPlaceholder;

    private PlayerBaseState currentState;
    private PlayerStateFactory stateFactory;


    // ------------------------ Awake method ---------------------------
    void Awake()
    {
        InitializeControls();
        cam = Camera.main;
        //animator.applyRootMotion = true;
        runtimeOverride = new AnimatorOverrideController(baseOverrideController);
        animator.runtimeAnimatorController = runtimeOverride;

        // Set up attack maps
        SetUpAttackMaps();

        stateFactory = new PlayerStateFactory(this);
        currentState = stateFactory.Idle();
        currentState.EnterState();

        // Photon Setup
        if (photonView.IsMine)
        {
            ThirdPersonCamera tpc = FindObjectOfType<ThirdPersonCamera>(true);
            if (tpc != null)
            {
                cam = tpc.GetComponentInChildren<Camera>(true);
            }
            enabled = true;
        }
        else 
        {
            enabled = false;
        
        }
        int targetLayer = photonView.IsMine ? LayerMask.NameToLayer("Player") : LayerMask.NameToLayer("Enemy");

        SetLayerRecursively(gameObject, targetLayer);
    }
    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    [PunRPC]
    public void SetPlayerName(string _name)
    {
        PlayerName = _name;
        PlayerNameText.text = PlayerName;
    }

    // ----------------- Setting up attack Maps -----------------------------
    void SetUpAttackMaps()
    {
        attackMap = new Dictionary<string, AttackData>();
        cooldowns = new Dictionary<string, float>();

        if (attacks == null)
        {
            Debug.LogError("Attacks list is null in PlayerStateMachine!");
            return;
        }

        foreach (var atk in attacks)
        {
            if (atk == null)
            {
                Debug.LogWarning("Null attack entry found in attacks list!");
                continue;
            }

            if (string.IsNullOrEmpty(atk.inputActionName))
            {
                Debug.LogWarning($"Attack '{atk.attackName}' has no inputActionName assigned!");
                continue;
            }

            attackMap[atk.inputActionName] = atk;
            cooldowns[atk.inputActionName] = 0f;

            if (_playerControls == null)
            {
                 Debug.LogError("_playerControls is null! InitializeControls must be called before SetUpAttackMaps.");
                 continue;
            }

            var attackMapObj = _playerControls.Attack.Get();
            if (attackMapObj == null)
            {
                Debug.LogError("Attack action map not found in PlayerControls!");
                continue;
            }

            var inputAction = attackMapObj.FindAction(atk.inputActionName);
            if (inputAction != null)
            {
                inputAction.performed += ctx => BufferAttackInput(atk.inputActionName);
            }
            else
            {
                Debug.LogWarning($"InputAction '{atk.inputActionName}' not found for attack '{atk.attackName}'.");
            }
        }

        // Set up Atk Origins as well
        if (attackOrigins != null)
        {
            foreach (var entry in attackOrigins)
            {
                if (entry.originTransform != null && !string.IsNullOrEmpty(entry.originName))
                {
                    if (!attackOriginMap.ContainsKey(entry.originName))
                        attackOriginMap[entry.originName] = entry.originTransform;
                }
            }
        }
    }
    // -------------------------- Attack Input manager -------------------------------
    // OLD METHOD - Replaced by BufferAttackInput
    /*
    private void OnAttackInput(AttackData atk)
    {
        //Debug.Log("Hello");
        if (Time.time >= cooldowns[atk.inputActionName])
        {
            cooldowns[atk.inputActionName] = Time.time + atk.cooldown;
            SwitchState(stateFactory.Attack(atk));
        }
    }
    */

    public void BufferAttackInput(string inputName)
    {
        // Clear old buffer if needed or just enqueue
        if (inputBuffer.Count >= 2) inputBuffer.Dequeue();
        
        inputBuffer.Enqueue(inputName);
        bufferTimer = bufferWindow;
        
        // If Idle or Run, immediately consume to start the first attack
        if (currentState is PlayerIdleState || currentState is PlayerRunState || currentState is PlayerWalkState)
        {
            ConsumeBufferedInput();
        }
        // If already attacking, the input stays buffered for combo checking during recovery
    }

    public void ConsumeBufferedInput(List<string> allowedInputs = null)
    {
        if (inputBuffer.Count == 0) return;

        string input = inputBuffer.Peek();
        
        // If specific inputs required (combo chaining)
        if (allowedInputs != null && !allowedInputs.Contains(input))
        {
            return;
        }

        // Valid input found
        inputBuffer.Dequeue();
        if (attackMap.TryGetValue(input, out AttackData atk))
        {
             // Ping Pong Index
             attackAnimIndex = (attackAnimIndex + 1) % 2;
             RotateToCameraDirection();
             SwitchState(stateFactory.Attack(atk));
        }
    }

    public void ConsumeBufferedInputForCombo(List<ComboRoute> comboRoutes)
    {
        if (inputBuffer.Count == 0) return;

        string input = inputBuffer.Peek();
        
        // Find the matching combo route
        ComboRoute matchingRoute = comboRoutes.Find(route => route.inputAction == input);
        
        if (matchingRoute.targetAttack == null)
        {
            return;
        }

        // Valid route found - use the TARGET ATTACK from the route
        inputBuffer.Dequeue();
        
         // Ping Pong Index
         attackAnimIndex = (attackAnimIndex + 1) % 2;
         RotateToCameraDirection();
         SwitchState(stateFactory.Attack(matchingRoute.targetAttack));
    }

    public void ClearInputBuffer()
    {
        if (inputBuffer.Count > 0)
        {
            inputBuffer.Clear();
            bufferTimer = 0;
        }
    }

    public void UpdateInputBuffer()
    {
        if (inputBuffer.Count > 0)
        {
            bufferTimer -= Time.deltaTime;
            if (bufferTimer <= 0)
            {
                inputBuffer.Clear();
            }
        }
    }

    //----------------------------------- Initialize controls -------------------------------------------
    void InitializeControls()
    {
        _playerControls = new PlayerControls();
        _playerControls.Movement.Jump.performed += ctx => {
            isJumpPressed = true;
            jumpBufferCounter = jumpBufferTime;
        };
        _playerControls.Grab.GrabMouse.performed += OnGrabPressed;
        _playerControls.PowerUps.Shield.performed += ActivateShield;
        _playerControls.PowerUps.PullThroughAir.performed += ActivatePullThroughAir;
        /*-------------------------------------------------------------------*/
        _playerControls.Parry.Parry.performed += ctx =>
        {
            isBlockHeld = true;
            isBlockJustPressed = true;
            if (!(currentState is PlayerDefenseState))
            {
                SwitchState(stateFactory.Defense());
            }
        };
        _playerControls.Parry.Parry.canceled += ctx =>
        {
            isBlockHeld = false;
        };
        /*-------------------------------------------------------------------*/
    }
    /*------------------------------------------------------------------*/

    private void HandleIncomingAttacks(GameObject attacker, GameObject target, AttackData data) 
    {
        if (target != gameObject) return;

        wasParried = false;

        TriggerParryWindow();

        if (isParryWindowOpen && isBlockJustPressed && !isBlockHeld) 
        {
            wasParried = true;
            Debug.Log("Attack Parried");
        }

    }
    public void TriggerParryWindow()
    {
        parryWindowStartTime = Time.time;
        isParryWindowOpen = true;
        

        Debug.Log("Parry window triggered.");
    }

    private void OnEnable() 
    {
        AttackEvents.OnIncomingAttack += HandleIncomingAttacks;
        _playerControls.Enable();
    }

    private void OnDisable() 
    {
        AttackEvents.OnIncomingAttack -= HandleIncomingAttacks;
        _playerControls.Disable();
    }

    /*-------------------------------------------------------------------*/
    void OnGrabPressed(InputAction.CallbackContext ctx)
    {
        isGrabbing = ctx.ReadValueAsButton();
        if (isGrabbing)
        {
            SwitchState(stateFactory.Grab());
        }

    }
    void ActivateShield(InputAction.CallbackContext ctx)
    {
        SwitchState(stateFactory.PowerUp(PowerUpType.BubbleShield,bubbleShieldDuration));
    }
    void ActivatePullThroughAir(InputAction.CallbackContext ctx)
    {
        SwitchState(stateFactory.PowerUp(PowerUpType.PullThroughAir, airPullDuration));
    }
    
    /*----------------------hh-----------------------------*/
    //void OnEnable() => _playerControls.Enable();
    //void OnDisable() => _playerControls.Disable();
    /*----------------------hh-----------------------------*/

    
    void Update()
    {
        UpdateInputBuffer();
        UpdateGroundStatus();
        HandleJumpTimers();
        HandleMovement();
        HandleGravity();
        
        // Final centralized move
        if (!animator.applyRootMotion)
        {
            Vector3 finalVelocity = horizontalVelocity;
            finalVelocity.y = velocity.y;
            characterController.Move(finalVelocity * Time.deltaTime);
        }

        HandleRotation();
        currentState.UpdateState();
        
        // Input reading
        moveInput = _playerControls.Movement.Keyboard.ReadValue<Vector2>();

        // Jump processing handled via timers in State logic or here
        // We will delegate to State's HandleJumpInput which checks timers now
        if (jumpBufferCounter > 0)
        {
            currentState.HandleJumpInput();
            // isJumpPressed = false; // logic handled by timers now
        }
        /*---------------------------------------------------------------------------*/
        if (isParryWindowOpen)
        {
            float elapsed = Time.time - parryWindowStartTime;

            if (elapsed > parryWindowDuration)
            {
                isParryWindowOpen = false;
            }
            else if (isBlockJustPressed)
            {
                
                Debug.Log("Perfect parry!");
                SwitchState(stateFactory.ParrySub());
                isParryWindowOpen = false;
            }
            else if (isBlockHeld)
            {
                
                Debug.Log("Blocking  parry not triggered");
            }
        }

        isBlockJustPressed = false;
        /*----------------------------------------------------------------------------*/
       
    }

    private void OnAnimatorMove()
    {
        if (animator != null && animator.applyRootMotion)
        {
            // Get root motion delta
            Vector3 delta = animator.deltaPosition;
            
            // Add manual gravity
            delta.y = velocity.y * Time.deltaTime;
            
            // Move character
            characterController.Move(delta);
            
            // Apply root rotation if available
            transform.rotation *= animator.deltaRotation;
        }
    }

    void HandleJumpTimers()
    {
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        jumpBufferCounter -= Time.deltaTime;
    }

    void HandleMovement()
    {
        if (isDashing) 
        {
            // Dashing overwrites physics momentarily
             horizontalVelocity = Vector3.zero;
             return; 
        }

        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        if (currentState != null && !currentState.CanMove())
        {
            direction = Vector3.zero;
        }
        
        // Orient input to camera
        Vector3 targetDir = Vector3.zero;
        if (direction.magnitude >= 0.1f) {
            targetDir = GetMoveDirection(direction);
        }

        Vector3 targetVelocity = targetDir * Speed;

        // Acceleration logic (Ground vs Air)
        float accelRate = isGrounded ? acceleration : airAcceleration;
        float decelRate = isGrounded ? deceleration : airDeceleration;

        // Separate handling for acceleration and deceleration can feel better
        // Simple approach: MoveTowards
        
        if (horizontalVelocity.magnitude < targetVelocity.magnitude && targetVelocity.magnitude > 0.1f)
        {
            // Accelerating
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, accelRate * Time.deltaTime);
        }
        else
        {
            // Decelerating or Changing Direction
             horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, decelRate * Time.deltaTime);
        }
    }

    public void SwitchState(PlayerBaseState newState)
    {
        if (currentState != null)
        {
            currentState.ExitState();
        }
        currentState = newState;
        newState.EnterState();
    }
    
    // ------------------ Basic physics and camera -------------------------------
    public void HandleRotation()
    {
        if (currentState != null && !currentState.CanRotate()) return;

        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.transform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnVelocity, TurnVelocity);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }
    }

    public Vector3 GetMoveDirection(Vector3 direction)
    {
        float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.transform.eulerAngles.y;
        Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        return moveDir.normalized;
    }

    public void RotateToCameraDirection()
    {
        if (cam == null) return;

        Vector3 camForward = cam.transform.forward;
        camForward.y = 0;
        if (camForward.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(camForward);
        }
    }
    void UpdateGroundStatus()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, gLayer);
        // animator.SetBool("IsGrounded", isGrounded); // Parameter doesn't exist in Animator
        if (isGrounded)
        {
            animator.SetBool("falling", false);
            Debug.Log("Grounded");
        }
    }

    void HandleGravity() // Renamed from ApplyGravity to avoid confusion, though logic is changed
    {
        // Grounded reset
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Variable Gravity Logic
        float currentGravityMultiplier = 1f;

        if (velocity.y < 0)
        {
            // Falling
            currentGravityMultiplier = fallGravityMult;
        }
        
        velocity.y += gravity * currentGravityMultiplier * Time.deltaTime;
        velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);

        // Movement moved to Update()
        // characterController.Move(velocity * Time.deltaTime);

        if (!isGrounded && velocity.y < -0.1f) 
        {
             animator.SetBool("falling", true);
        }
    }


    public void ApplyStepOffset(float distance)
    {
        if (distance == 0) return;
        characterController.Move(transform.forward * distance);
    }
    
    public void TriggerHitStop(float duration)
    {
        StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        if (duration <= 0) yield break;

        float originalAnimatorSpeed = animator.speed;
        animator.speed = 0.05f; // Almost freeze, but keeps micro-movement
        
        // Optionally freeze other things if needed
        yield return new WaitForSecondsRealtime(duration);
        
        animator.speed = originalAnimatorSpeed;
    }
    
    //--------------------------- Animation Events Refrences ------------------------------------
    //--------------------------- Animation Events Refrences ------------------------------------
    public void ApplyJumpVelocity()
    {
        // Only jump if allowed
        if (coyoteTimeCounter > 0f || isGrounded) // State machine handles logic, this just applies force
        {
             velocity.y = Mathf.Sqrt(-2f * gravity * jumpHeight);
             coyoteTimeCounter = 0f; // Consume coyote time
             jumpBufferCounter = 0f; // Consume buffer
        }
    }

    public void ApplyAttackDamage()
    {
        if (currentState is PlayerAttackState atk)
            atk.ApplyDamage();
    }

    public void EndAttack()
    {
        if (currentState is PlayerAttackState atk)
        {
            // SwitchState(stateFactory.Idle()); // Disabled for combo system
        }
    }


    // --------------------------------------- Power Ups -------------------------------------
    public void EnableShield(bool active)
    {
        isShieldActive = active;
        if (bubbleShieldEffect != null)
        {
            bubbleShieldEffect.SetActive(active);
        }
    }

    public void PullPlayerThroughAir()
    {
        if (isDashing) return; // Prevent overlapping

        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        if (inputDirection == Vector3.zero)
        {
            inputDirection = Vector3.forward;
            
        }
            
        
        Vector3 moveDir = GetMoveDirection(inputDirection);
        transform.rotation = Quaternion.LookRotation(moveDir);
        StartCoroutine(PerformAirPull(moveDir));
    }
    private IEnumerator PerformAirPull(Vector3 direction)
    {
        isDashing = true;

        float elapsed = 0f;
        Vector3 start = transform.position;
        Vector3 target = start + direction * airPullDistance;

        // Disable gravity while pulling (optional)
        bool wasGrounded = isGrounded;
        velocity.y = 0f;
        

        while (elapsed < airPullDuration)
        {
            
            float t = elapsed / airPullDuration;
            Vector3 newPosition = Vector3.Lerp(start, target, t);
            Vector3 delta = newPosition - transform.position;
            characterController.Move(delta);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Final move in case of small gap
        Vector3 finalDelta = target - transform.position;
        characterController.Move(finalDelta);

        isDashing = false;

        // Restore downward velocity
        if (!wasGrounded)
            velocity.y = 0f;
    }


}
