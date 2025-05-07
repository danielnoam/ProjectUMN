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
    

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerStateMachine player;
    
    private readonly int _stateHash = Animator.StringToHash("StateIndex");
    private readonly int _verticalHash = Animator.StringToHash("VerticalValue");
    private readonly int _horizontalHash = Animator.StringToHash("HorizontalValue");
    private readonly int _gaitTypeHash = Animator.StringToHash("GaitTypeValue");
    private readonly int _fallTimeHash = Animator.StringToHash("FallTime");
    private readonly int _rotationMismatchHash = Animator.StringToHash("RotationMismatch");
    private readonly int _isRotatingToTargetHash = Animator.StringToHash("IsRotatingToTarget");
    

    
    
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
        PlayerAnimationState currentAnimState = player.CurrentState switch
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
        animator.SetInteger(_stateHash, (int)currentAnimState);
    }
    

    private void UpdateRotationAnimation()
    {
        animator.SetFloat(_rotationMismatchHash, player.RotationMismatch);
        animator.SetBool(_isRotatingToTargetHash, player.IsRotatingToTarget);
    }
    

    private void UpdateFallAnimation()
    {
        float fallBlend = Mathf.Clamp01(player.ActiveVerticalVelocity / player.maxVerticalVelocity);
        animator.SetFloat(_fallTimeHash, fallBlend);
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
    Vector3 moveDirection = player.ActiveMoveDirection;
    float activeSpeed = player.ActiveHorizontalVelocity;
    bool isAiming = player.IsAiming;

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
    animator.SetFloat(_verticalHash, verticalValue, animationSmoothTime, Time.deltaTime);
    animator.SetFloat(_horizontalHash, horizontalValue, animationSmoothTime, Time.deltaTime);
}

    private void UpdateGaitTypeAnimation()
    {
        bool isAiming = player.IsAiming;
        bool isCrouching = player.CurrentState is PlayerCrouchingState;
        
        // Get input for gait calculation - Using direct input from PlayerStateMachine
        // rather than the modified speed
        float inputIntensity = Mathf.Clamp01(
            Mathf.Abs(player.inputHandler.MovementInput.x) + 
            Mathf.Abs(player.inputHandler.MovementInput.y)
        );
        
        // Determine base speed based on input and flags
        float speedForAnimation = 0f;
        bool lockSprintGait = player.inputHandler.MoveSpeedInput || 
                              !player.allowSprint || 
                              isAiming || 
                              isCrouching;
        
        if (inputIntensity > player.inputHandler.InputReader.ActiveSettings.movementInputThreshold)
        {
            if (!lockSprintGait)
            {
                // Can sprint, determine speed based on sprint input
                float startSpeed = player.inputHandler.SprintInput ? player.runSpeed : 0;
                float targetSpeed = player.inputHandler.SprintInput ? player.sprintSpeed : player.runSpeed;
                speedForAnimation = Mathf.Lerp(startSpeed, targetSpeed, inputIntensity);
            }
            else
            {
                // Cannot sprint, use walk/run only
                float startSpeed = player.inputHandler.SprintInput ? player.walkSpeed : 0;
                float targetSpeed = player.inputHandler.SprintInput ? player.runSpeed : player.walkSpeed;
                speedForAnimation = Mathf.Lerp(startSpeed, targetSpeed, inputIntensity);
            }
        }
        
        // If crouching, apply the crouch speed multiplier to animation speed
        if (isCrouching)
        {
            speedForAnimation *= player.crouchSpeedMultiplier;
        }
        
        // Calculate gait type value based on this determined speed, IGNORING direction multipliers
        float moveType = CalculateMoveTypeValue(speedForAnimation);
        
        // Apply gait value to animator with smoothing
        animator.SetFloat(_gaitTypeHash, moveType, animationSmoothTime * 5, Time.deltaTime);
    }
    
    #endregion Animator -------------------------------------------------------------------------------------------------------



    #region Calculations -------------------------------------------------------------------------------------------------------

    private float CalculateMoveTypeValue(float currentSpeed)
    {
        // No movement
        if (currentSpeed < 0.01f)
            return 0f;
        
        // Walking range: 0 to 0.5
        if (currentSpeed <= player.walkSpeed)
            return (currentSpeed / player.walkSpeed) * 0.5f;
        
        // Running range: 0.5 to 1.0
        if (currentSpeed <= player.runSpeed)
            return 0.5f + ((currentSpeed - player.walkSpeed) / 
                           (player.runSpeed - player.walkSpeed)) * 0.5f;
        
        // Sprinting: max at 2
        if (currentSpeed > player.sprintSpeed)
            return 2;
        
        // Sprinting range: 1.0 to 2.0
        return 1.0f + ((currentSpeed - player.runSpeed) / 
                       (player.sprintSpeed - player.runSpeed)) * 1.0f;
    }

    #endregion Calculations -------------------------------------------------------------------------------------------------------
}