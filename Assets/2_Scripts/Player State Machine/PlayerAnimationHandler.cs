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
    [Tooltip("Maximum fall time used for animation blending")]
    [SerializeField] private float maxFallTime = 2.0f;

    [Header("IK")]
    [SerializeField] private Rig rig;
    [SerializeField] private MultiAimConstraint headIK;
    [SerializeField] private MultiAimConstraint spineIK;
    [SerializeField] private Transform IKTarget;
    
    private readonly int _stateHash = Animator.StringToHash("StateIndex");
    private readonly int _verticalHash = Animator.StringToHash("VerticalValue");
    private readonly int _horizontalHash = Animator.StringToHash("HorizontalValue");
    private readonly int _moveTypeHash = Animator.StringToHash("MoveTypeValue");
    private readonly int _fallTimeHash = Animator.StringToHash("FallTime");
    private readonly int _rotationMismatchHash = Animator.StringToHash("RotationMismatch");
    private readonly int _isRotatingToTargetHash = Animator.StringToHash("IsRotatingToTarget");
    
    private Animator _animator;
    private PlayerStateMachine _stateMachine;
    private CameraManager _cameraManager;
    private float _targetHeadWeight = 1f;
    private float _targetSpineWeight = 1f;
    private Vector3 _currentIKPosition;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _stateMachine = GetComponent<PlayerStateMachine>();
    }

    private void Start()
    {
        if (!_cameraManager) _cameraManager = FindFirstObjectByType<CameraManager>();
    }

    private void Update()
    {
        // Animator
        UpdateStateIndex();
        UpdateMovementAnimation();
        UpdateFallAnimation();
        UpdateRotationAnimation();

        // IK
        UpdateIKTarget();
        UpdateHeadIK();
        UpdateSpineIK();
    }

    
    
    
    #region IK -------------------------------------------------------------------------------------------------------

    private void UpdateIKTarget()
    {
        if (!IKTarget) return;
        
        if (_stateMachine.CurrentState != _stateMachine.InMenuState)
        {
            Vector3 targetPosition;
        
            if (_stateMachine.CurrentInteractable && !_stateMachine.IsAiming)
            {
                targetPosition = _stateMachine.CurrentInteractable.transform.position;
            }
            else if (_cameraManager)
            {
                targetPosition = _cameraManager.targetTransform.position;
            }
            else
            {
                return;
            }
        
            // Lerp the position
            _currentIKPosition = Vector3.Lerp(_currentIKPosition, targetPosition, Time.deltaTime * 5);
        
            // Apply the lerped position
            IKTarget.transform.position = _currentIKPosition;
        }
    }
    
    private void UpdateSpineIK()
    {
        if (!rig || !spineIK) return;

        if (_stateMachine.CurrentState != _stateMachine.InMenuState && _stateMachine.CurrentState != _stateMachine.CrouchingState && _stateMachine.ActiveHorizontalVelocity < _stateMachine.runSpeed)
        {
            if (_stateMachine.CurrentInteractable && !_stateMachine.IsAiming)
            {
            
                // Determine target weight based on conditions
                if (!_stateMachine.IsAiming && Mathf.Abs(_stateMachine.RotationMismatch) > Mathf.Abs(180))
                {
                    _targetSpineWeight = 0f;
                }
                else
                {
                    _targetSpineWeight = 0.5f;
                }

                // Smoothly interpolate the actual weight toward the target weight
                spineIK.weight = Mathf.Lerp(spineIK.weight, _targetSpineWeight, Time.deltaTime * 5);
            }
            else if (_cameraManager)
            {
            
                // Determine target weight based on conditions
                if (!_stateMachine.IsAiming && Mathf.Abs(_stateMachine.RotationMismatch) > Mathf.Abs(140))
                {
                    _targetSpineWeight = 0f;
                }
                else
                {
                    _targetSpineWeight = 0.7f;
                }

                // Smoothly interpolate the actual weight toward the target weight
                spineIK.weight = Mathf.Lerp(spineIK.weight, _targetSpineWeight, Time.deltaTime * 5);
            }
        }
        else
        {
            spineIK.weight = Mathf.Lerp(spineIK.weight, 0f, Time.deltaTime * 5);
        }
    }

    private void UpdateHeadIK()
    {
        if (!rig || !headIK) return;

        if (_stateMachine.CurrentState != _stateMachine.InMenuState)
        {
            if (_stateMachine.CurrentInteractable && !_stateMachine.IsAiming)
            {
            
                // Determine target weight based on conditions
                if (!_stateMachine.IsAiming && Mathf.Abs(_stateMachine.RotationMismatch) > Mathf.Abs(180))
                {
                    _targetHeadWeight = 0f;
                }
                else
                {
                    _targetHeadWeight = 0.7f;
                }

                // Smoothly interpolate the actual weight toward the target weight
                headIK.weight = Mathf.Lerp(headIK.weight, _targetHeadWeight, Time.deltaTime * 5);
            }
            else if (_cameraManager)
            {
            
                // Determine target weight based on conditions
                if (!_stateMachine.IsAiming && Mathf.Abs(_stateMachine.RotationMismatch) > Mathf.Abs(140))
                {
                    _targetHeadWeight = 0f;
                }
                else
                {
                    _targetHeadWeight = 1f;
                }

                // Smoothly interpolate the actual weight toward the target weight
                headIK.weight = Mathf.Lerp(headIK.weight, _targetHeadWeight, Time.deltaTime * 5);
            }
        }
        else
        {
            headIK.weight = Mathf.Lerp(headIK.weight, 0f, Time.deltaTime * 5);
        }
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

    #endregion IK -------------------------------------------------------------------------------------------------------

    

    #region Animator -------------------------------------------------------------------------------------------------------

    private void UpdateMovementAnimation()
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
    
        // Calculate moveType value based on speed (linear)
        float moveType = CalculateMoveTypeValue(activeSpeed);
    
        // Apply values to animator with smoothing
        _animator.SetFloat(_verticalHash, verticalValue, animationSmoothTime, Time.deltaTime);
        _animator.SetFloat(_horizontalHash, horizontalValue, animationSmoothTime, Time.deltaTime);
        _animator.SetFloat(_moveTypeHash, moveType, animationSmoothTime, Time.deltaTime);
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
        
        // Sprinting range: 1.0 to 2.0
        return 1.0f + ((currentSpeed - _stateMachine.runSpeed) / 
                       (_stateMachine.sprintSpeed - _stateMachine.runSpeed)) * 1.0f;
    }

    #endregion Animator -------------------------------------------------------------------------------------------------------
}