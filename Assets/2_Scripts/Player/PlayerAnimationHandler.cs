using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;


public enum PlayerAnimationState
{
    Grounded = 0,
    Jump = 1,
    Fall = 2,
    Landing = 3,
    Interact = 4,
    Crouch = 5,
    Teleporting = 6,
}


[RequireComponent(typeof(PlayerStateMachine))]
public class PlayerAnimationHandler : MonoBehaviour
{

    
    [Header("Animation Smoothing")]
    [SerializeField, Range(0.01f, 1f)] private float animationSmoothTime = 0.1f;
    
    
    private readonly int _stateHash = Animator.StringToHash("StateIndex");
    private readonly int _verticalHash = Animator.StringToHash("VerticalValue");
    private readonly int _horizontalHash = Animator.StringToHash("HorizontalValue");
    private readonly int _gaitTypeHash = Animator.StringToHash("GaitTypeValue");
    private readonly int _fallTimeHash = Animator.StringToHash("FallTime");
    private readonly int _rotationMismatchHash = Animator.StringToHash("RotationMismatch");
    private readonly int _isRotatingToTargetHash = Animator.StringToHash("IsRotatingToTarget");
    
    private Animator _animator;
    private PlayerStateMachine _stateMachine;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _stateMachine = GetComponent<PlayerStateMachine>();
    }
    
    private void Update()
    {
        UpdateStateIndex();
        UpdateMovementAnimation();
        UpdateFallAnimation();
        UpdateRotationAnimation();
    }

    
    #region Animator -------------------------------------------------------------------------------------------------------

    private void UpdateStateIndex()
    {
        // Convert current state to animation state enum
        PlayerAnimationState currentAnimState = _stateMachine.CurrentState switch
        {
            PlayerGroundedState => PlayerAnimationState.Grounded,
            PlayerJumpingState => PlayerAnimationState.Jump,
            PlayerFallingState => PlayerAnimationState.Fall,
            PlayerLandingState => PlayerAnimationState.Landing,
            PlayerInteractingState => PlayerAnimationState.Interact,
            PlayerCrouchingState => PlayerAnimationState.Crouch,
            PlayerTeleportingState => PlayerAnimationState.Teleporting,
            _ => PlayerAnimationState.Grounded
        };

        // Set the animation state parameter
        _animator.SetInteger(_stateHash, (int)currentAnimState);
    }
    

    private void UpdateRotationAnimation()
    {
        _animator.SetFloat(_rotationMismatchHash, _stateMachine.RotationMismatch);
        _animator.SetBool(_isRotatingToTargetHash, _stateMachine.IsRotatingToTarget);
    }
    

    private void UpdateFallAnimation()
    {
        float fallBlend = Mathf.Clamp01(_stateMachine.ActiveVerticalVelocity / _stateMachine.maxVerticalVelocity);
        _animator.SetFloat(_fallTimeHash, fallBlend);
    }
    
private void UpdateMovementAnimation()
{
    // Update direction values
    UpdateMovementDirectionAnimation();
    
    // Update gait type separately
    UpdateGaitTypeAnimation();
}

private void UpdateMovementDirectionAnimation()
{
    // Get current movement data
    Vector3 moveDirection = _stateMachine.ActiveMoveDirection;
    float activeSpeed = _stateMachine.ActiveHorizontalVelocity;
    bool isAiming = _stateMachine.IsAiming;

    // Initialize animation values
    float horizontalValue = 0f;
    float verticalValue = 0f;

    // Handle movement animation if we're actually moving
    if (moveDirection.sqrMagnitude > 0.01f && activeSpeed > 0.01f)
    {
        // Get local movement direction relative to player's facing direction
        Vector3 localMoveDir = transform.InverseTransformDirection(moveDirection);
        if (localMoveDir.sqrMagnitude > 0.01f)
        {
            localMoveDir.Normalize();
            
            // Set base horizontal/vertical values
            horizontalValue = localMoveDir.x;
            verticalValue = localMoveDir.z;
            
            // When aiming, we might want to enhance diagonal movement
            if (isAiming && Mathf.Abs(horizontalValue) > 0.1f && Mathf.Abs(verticalValue) > 0.1f)
            {
                float correctionFactor = 1.414f; // sqrt(2) for diagonal correction
                horizontalValue *= correctionFactor;
                verticalValue *= correctionFactor;
                
                // Clamp values to avoid exceeding range
                horizontalValue = Mathf.Clamp(horizontalValue, -1f, 1f);
                verticalValue = Mathf.Clamp(verticalValue, -1f, 1f);
            }
        }
    }
    
    // Apply values to animator with smoothing
    _animator.SetFloat(_verticalHash, verticalValue, animationSmoothTime, Time.deltaTime);
    _animator.SetFloat(_horizontalHash, horizontalValue, animationSmoothTime, Time.deltaTime);
}

private void UpdateGaitTypeAnimation()
{
    bool isAiming = _stateMachine.IsAiming;
    
    // Get input for gait calculation - Using direct input from PlayerStateMachine
    // rather than the modified speed
    float inputIntensity = Mathf.Clamp01(
        Mathf.Abs(_stateMachine.InputHandler.MovementInput.x) + 
        Mathf.Abs(_stateMachine.InputHandler.MovementInput.y)
    );
    
    // Determine base speed based on input and flags
    float speedForAnimation = 0f;
    bool lockSprintGait = _stateMachine.InputHandler.MoveSpeedInput || 
                          !_stateMachine.allowSprint || 
                          isAiming || 
                          _stateMachine.CurrentState is PlayerCrouchingState;
    
    if (inputIntensity > _stateMachine.InputHandler.MovementInputThreshold)
    {
        if (!lockSprintGait)
        {
            // Can sprint, determine speed based on sprint input
            float startSpeed = _stateMachine.InputHandler.SprintInput ? _stateMachine.runSpeed : 0;
            float targetSpeed = _stateMachine.InputHandler.SprintInput ? _stateMachine.sprintSpeed : _stateMachine.runSpeed;
            speedForAnimation = Mathf.Lerp(startSpeed, targetSpeed, inputIntensity);
        }
        else
        {
            // Cannot sprint, use walk/run only
            float startSpeed = _stateMachine.InputHandler.SprintInput ? _stateMachine.walkSpeed : 0;
            float targetSpeed = _stateMachine.InputHandler.SprintInput ? _stateMachine.runSpeed : _stateMachine.walkSpeed;
            speedForAnimation = Mathf.Lerp(startSpeed, targetSpeed, inputIntensity);
        }
    }
    
    // Calculate gait type value based on this determined speed, IGNORING direction multipliers
    float moveType = CalculateMoveTypeValue(speedForAnimation);
    
    // Apply gait value to animator with smoothing
    _animator.SetFloat(_gaitTypeHash, moveType, animationSmoothTime * 5, Time.deltaTime);
}
    
    #endregion Animator -------------------------------------------------------------------------------------------------------



    #region Calculations -------------------------------------------------------------------------------------------------------

    private float CalculateMoveTypeValue(float currentSpeed)
    {
        // No movement
        if (currentSpeed < 0.01f)
            return 0f;
        
        // Walking range: 0 to 0.5
        if (currentSpeed <= _stateMachine.walkSpeed)
            return (currentSpeed / _stateMachine.walkSpeed) * 0.5f;
        
        // Running range: 0.5 to 1.0
        if (currentSpeed <= _stateMachine.runSpeed)
            return 0.5f + ((currentSpeed - _stateMachine.walkSpeed) / 
                           (_stateMachine.runSpeed - _stateMachine.walkSpeed)) * 0.5f;
        
        // Sprinting: max at 2
        if (currentSpeed > _stateMachine.sprintSpeed)
            return 2;
        
        // Sprinting range: 1.0 to 2.0
        return 1.0f + ((currentSpeed - _stateMachine.runSpeed) / 
                       (_stateMachine.sprintSpeed - _stateMachine.runSpeed)) * 1.0f;
    }

    #endregion Calculations -------------------------------------------------------------------------------------------------------
}