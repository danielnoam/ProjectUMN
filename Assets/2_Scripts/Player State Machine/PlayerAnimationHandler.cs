using UnityEngine;
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

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerStateMachine))]
public class PlayerAnimationHandler : MonoBehaviour
{
    private Animator _animator;
    private PlayerStateMachine _stateMachine;
    
    [Header("Animation Smoothing")]
    [SerializeField, Range(0.01f, 1f)] private float animationSmoothTime = 0.1f;
    
    [Header("Animation Blend Ranges")]
    [Tooltip("Value in the blend tree for walk animations")]
    [SerializeField] private float walkBlendMax = 0.5f;
    [Tooltip("Value in the blend tree for run animations")]
    [SerializeField] private float runBlendMax = 1.0f;
    [Tooltip("Value in the blend tree for sprint animations")]
    [SerializeField] private float sprintBlendMax = 2f;
    [Tooltip("Maximum fall time used for animation blending")]
    [SerializeField] private float maxFallTime = 2.0f;


    private readonly int _stateHash = Animator.StringToHash("StateIndex");
    private readonly int _verticalHash = Animator.StringToHash("InputVertical");
    private readonly int _horizontalHash = Animator.StringToHash("InputHorizontal");
    private readonly int _inputMagnitudeHash = Animator.StringToHash("InputMagnitude");
    private readonly int _fallTimeHash = Animator.StringToHash("FallTime");
    private readonly int _rotationMismatchHash = Animator.StringToHash("RotationMismatch");
    private readonly int _isRotatingToTargetHash = Animator.StringToHash("IsRotatingToTarget");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _stateMachine = GetComponent<PlayerStateMachine>();
        
        // Validate animation blend ranges
        ValidateBlendRanges();
    }
    
    private void ValidateBlendRanges()
    {
        // Ensure blend ranges are properly ordered
        if (walkBlendMax > runBlendMax)
        {
            Debug.LogWarning("Walk blend max should not exceed run blend max. Adjusting to maintain proper order.");
            walkBlendMax = runBlendMax;
        }
        
        if (runBlendMax > sprintBlendMax)
        {
            Debug.LogWarning("Run blend max should not exceed sprint blend max. Adjusting to maintain proper order.");
            runBlendMax = sprintBlendMax;
        }
    }

    private void Update()
    {
        UpdateStateIndex();
        UpdateMovementAnimation();
        UpdateFallAnimation();
        UpdateRotationAnimation();
    }

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

    private void UpdateMovementAnimation()
    {
        // Default animation values
        float verticalValue = 0f;
        float horizontalValue = 0f;
        
        // Get current movement speed and direction from state machine
        bool aiming = _stateMachine.IsAiming;
        float activeSpeed = _stateMachine.ActiveHorizontalVelocity;
        float speedBlendValue = CalculateSpeedBlend(activeSpeed);
        Vector3 moveDirection = _stateMachine.ActiveMoveDirection;

        // Calculate the rotation mismatch between movement and facing direction
        float movementRotationMismatch = 0f;
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            // Calculate angle between movement direction and forward direction
            movementRotationMismatch = Vector3.SignedAngle(transform.forward, moveDirection, Vector3.up);
        }

        // Handle animation based on whether the player is aiming or not
        if (aiming && moveDirection.sqrMagnitude > 0.01f && activeSpeed > 0.01f)
        {
            // When aiming, map movement direction to the animation blend tree
            Vector3 localMoveDir = transform.InverseTransformDirection(moveDirection);
            
            if (localMoveDir.sqrMagnitude > 0.01f)
            {
                // Normalize to get pure direction
                localMoveDir.Normalize();
                
                // Use direction components for strafe animations
                horizontalValue = localMoveDir.x;
                verticalValue = localMoveDir.z;
                
                // For diagonal movement, make sure we reach the same magnitude as non-aiming movement
                // by adjusting the scale factor calculation
                float directionMagnitude = Mathf.Sqrt(horizontalValue * horizontalValue + verticalValue * verticalValue);
                if (directionMagnitude > 0.01f)
                {
                    // When moving diagonally, we need to apply a correction factor to reach the same blend values
                    // as non-aiming movement. This ensures diagonal movement has the proper animation intensity.
                    float scaleFactor = speedBlendValue / directionMagnitude;
                    
                    // For diagonal movement, we need to boost the scale factor to match non-aiming intensity
                    if (Mathf.Abs(horizontalValue) > 0.1f && Mathf.Abs(verticalValue) > 0.1f)
                    {
                        // This correction ensures diagonal movement reaches the same intensity as cardinal directions
                        scaleFactor *= 1.414f; // Approximately sqrt(2) to compensate for diagonal normalization
                    }
                    
                    horizontalValue *= scaleFactor;
                    verticalValue *= scaleFactor;
                }
            }
        }
        else if (activeSpeed > 0.01f)
        {
            // For non-aiming movement, check if we're moving in a direction different from where we're facing
            bool isMovingPrimarilyForward = _stateMachine.InputHandler.MovementInput.y > 0.7f && 
                                            Mathf.Abs(_stateMachine.InputHandler.MovementInput.x) < 0.3f;
                                            
            // If we're moving forward but following camera (not directly forward relative to character)
            if (isMovingPrimarilyForward && Mathf.Abs(movementRotationMismatch) > 10f)
            {
                // Convert movement rotation mismatch to horizontal value for the animation blend tree
                // This creates a slight strafe animation effect when following camera while "moving forward"
                horizontalValue = Mathf.Clamp(movementRotationMismatch / 90f, -1f, 1f) * 0.5f; // Scale down for subtle effect
                verticalValue = speedBlendValue * 0.85f; // Slightly reduce forward component for natural look
            }
            else if (!isMovingPrimarilyForward && Mathf.Abs(movementRotationMismatch) > 30f)
            {
                // For more significant directional changes, calculate better blend values
                float absAngle = Mathf.Abs(movementRotationMismatch);
                
                // As angle approaches 90 degrees, increase horizontal component
                if (absAngle > 80f)
                {
                    // Close to perpendicular movement - strong sideways component
                    horizontalValue = Mathf.Sign(movementRotationMismatch) * speedBlendValue * 0.8f;
                    verticalValue = speedBlendValue * 0.3f; // Still some forward component
                }
                else if (absAngle > 45f)
                {
                    // Diagonal movement
                    horizontalValue = Mathf.Sign(movementRotationMismatch) * speedBlendValue * 0.5f;
                    verticalValue = speedBlendValue * 0.7f;
                }
                else
                {
                    // Slight angle difference
                    horizontalValue = Mathf.Sign(movementRotationMismatch) * speedBlendValue * 0.3f;
                    verticalValue = speedBlendValue * 0.9f;
                }
            }
            else
            {
                // Standard forward movement
                verticalValue = speedBlendValue;
                horizontalValue = 0f;
            }
        }

        // Apply with smoothing
        _animator.SetFloat(_verticalHash, verticalValue, animationSmoothTime, Time.deltaTime);
        _animator.SetFloat(_horizontalHash, horizontalValue, animationSmoothTime, Time.deltaTime);
        _animator.SetFloat(_inputMagnitudeHash, speedBlendValue, animationSmoothTime, Time.deltaTime);
    }

    private void UpdateRotationAnimation()
    {
        _animator.SetFloat(_rotationMismatchHash, _stateMachine.RotationMismatch);
        _animator.SetBool(_isRotatingToTargetHash, _stateMachine.IsRotatingToTarget);
    }
    

    private void UpdateFallAnimation()
    {
        float fallBlend = Mathf.Clamp01(_stateMachine.FallTime / maxFallTime);
        _animator.SetFloat(_fallTimeHash, fallBlend);
    }

    private float CalculateSpeedBlend(float currentSpeed)
    {
        // Map the current speed to animation blend values using the customizable ranges
        if (currentSpeed <= _stateMachine.walkSpeed)
        {
            // Walk range: 0 to walkBlendMax
            return (currentSpeed / _stateMachine.walkSpeed) * walkBlendMax;
        }
        
        if (currentSpeed <= _stateMachine.runSpeed)
        {
            // Run range: walkBlendMax to runBlendMax
            return walkBlendMax + ((currentSpeed - _stateMachine.walkSpeed) / 
                (_stateMachine.runSpeed - _stateMachine.walkSpeed)) * (runBlendMax - walkBlendMax);
        }
        
        if (currentSpeed <= _stateMachine.sprintSpeed)
        {
            // Sprint range: runBlendMax to sprintBlendMax
            return runBlendMax + ((currentSpeed - _stateMachine.runSpeed) / 
                (_stateMachine.sprintSpeed - _stateMachine.runSpeed)) * (sprintBlendMax - runBlendMax);
        }

        return 0f;
    }
}