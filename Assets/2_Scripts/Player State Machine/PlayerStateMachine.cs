using TMPro;
using UnityEngine;



[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(CharacterController))]
public class PlayerStateMachine : MonoBehaviour
{
    public static  PlayerStateMachine Instance { get; private set; }
    public PlayerBaseState CurrentState { get; private set; }
    public PlayerGroundedState GroundedState { get; private set; }
    public PlayerCrouchingState CrouchingState { get; private set; }
    public PlayerJumpingState JumpingState { get; private set; }
    public PlayerFallingState FallingState { get; private set; }
    public PlayerLandingState LandingState { get; private set; }
    public PlayerInteractingState InteractingState { get; private set; }
    public PlayerInMenuState InMenuState { get; private set; }

    [Header("Movement")]
    [Tooltip("Walking speed when holding the walk button")]
    public float walkSpeed = 4f;
    [Tooltip("Default running speed")]
    public float runSpeed = 8f;
    [Tooltip("Maximum speed when sprinting with sufficient input")]
    public float sprintSpeed = 12f;
    [Tooltip("How quickly the character reaches target speed")]
    public float acceleration = 10f;
    [Tooltip("Base rotation speed when turning on the ground")]
    public float rotationSpeed = 2f;
    [Tooltip("Rotation speed when turning while aiming")]
    public float aimRotationSpeed = 4f;

    [Header("Air Movement")]
    [Tooltip("Maximum horizontal speed while in the air")]
    public float airMoveSpeed = 4f;
    [Tooltip("Base rotation speed when turning in the air")]
    public float airRotationSpeed = 1f;
    [Tooltip("How quickly the character reaches target speed in air")]
    public float airAcceleration = 3f;
    [Tooltip("How quickly the character loses momentum in air")]
    public float airFriction = 2.0f;
    
    [Header("Jump")]
    [Tooltip("Initial upward velocity applied when jumping")]
    public float jumpForce = 8f;

    [Header("Gravity")]
    [Tooltip("Downward acceleration applied while in the air")]
    public float gravity = -15f;
    [Tooltip("Small downward force applied while grounded to stick to slopes")]
    public float groundedGravity = -5f;
    [Tooltip("Maximum downward velocity the character can reach")]
    public float maxVerticalVelocity = -50f;

    [Header("Land")]
    [Tooltip("Minimum time falling before impact animations trigger")]
    public float fallThreshold = 0.1f;
    [Tooltip("Fall time that results in maximum impact effect")]
    public float maxFallTime = 2.0f;
    [Tooltip("Time needed to recover from maximum impact landing")]
    public float recoveryDuration = 1f;
    [Tooltip("Percentage of movement control retained during landing recovery")]
    public float minMovementControl = 0.1f;


    [Header("Collision Check")]
    [Tooltip("Radius of the sphere used to detect environment")]
    [SerializeField] private float environmentCheckRadius = 0.3f;
    [Tooltip("Offset from character position for ground detection")]
    [SerializeField] private Vector3 groundCheckOffset = new Vector3(0, -0.7f, 0);
    [Tooltip("Offset from character position for ceiling detection")]
    [SerializeField] private Vector3 ceilingCheckOffset = new Vector3(0, 0.63f, 0);
    [Tooltip("Layer mask defining what objects count as environment")]
    [SerializeField] private LayerMask environmentLayer = 1;
    [Tooltip("Maximum distance the aim ray will travel")]
    public float aimRayMaxDistance = 20f;
    [Tooltip("Layer mask for objects that can be hit by the aim ray")]
    public LayerMask aimRayHitMask= 1;
    [Tooltip("The start position of the aim ray")]
    public Transform aimRayStartPosition;

    [Header("References")] 
    public TextMeshProUGUI debugText;
    public GameObject menu;
    
    
    public float AirTime { get;  set; }
    public float FallTime { get;  set; }
    public float LandingIntensity { get; set; }
    public float ActiveHorizontalVelocity { get; private set; }
    public float ActiveVerticalVelocity { get; set; }
    public Vector3 ActiveMoveDirection { get; private set; } = Vector3.zero;
    public bool IsGrounded { get; private set; }
    public bool CanStand { get; private set; }
    public bool CanInteract { get; private set; }
    public bool IsAiming { get; private set; }
    public PlayerInputHandler InputHandler { get; private set; }
    public IInteractable CurrentInteractable { get; private set; }
    public IInteractable CurrentAimedInteractable { get; private set; }
    private CharacterController _controller;
    private RobotCompanion _robot;
    private CameraManager _cameraManager;
    private LineRenderer _lineRenderer;
    private bool _lockSprinting;
    private Vector3 _lastRotationDirection = Vector3.forward;
    private float _defaultCharacterHeight;
    private Vector3 _defaultCharacterCenter;
    private readonly float _crouchCharacterHeight = 1.2333f;
    private readonly Vector3 _crouchCharacterCenter = new Vector3(0, -0.3f, 0.2f);
    
    
    public struct MovementParams
    {
        public readonly bool IsAirborne;        // Whether to use air or ground movement rules
        public readonly float SpeedMultiplier;  // Multiplier for max speed (1.0f is normal)
        public readonly float AccelMultiplier;  // Multiplier for acceleration (1.0f is normal)
        public readonly float ControlMultiplier; // For movement control during landing (1.0f is full control)
    
        // Constructor with default values
        public MovementParams(
            bool isAirborne = false, 
            float speedMultiplier = 1f, 
            float accelMultiplier = 1f, 
            float controlMultiplier = 1f)
        {
            this.IsAirborne = isAirborne;
            this.SpeedMultiplier = speedMultiplier;
            this.AccelMultiplier = accelMultiplier;
            this.ControlMultiplier = controlMultiplier;
        }
    }


    public struct RotationParams
    {
        public readonly bool IsAirborne;         // Whether to use air or ground rotation rules
        public readonly bool UseAimRotation;     // Whether to use aim-based rotation
        public readonly float RotationMultiplier; // Multiplier for rotation speed (1.0f is normal)
        public readonly bool AllowRotation;       // Whether to allow rotation at all
    
        // Constructor with default values
        public RotationParams(
            bool isAirborne = false, 
            bool useAimRotation = false, 
            float rotationMultiplier = 1f, 
            bool allowRotation = true)
        {
            this.IsAirborne = isAirborne;
            this.UseAimRotation = useAimRotation;
            this.RotationMultiplier = rotationMultiplier;
            this.AllowRotation = allowRotation;
        }
    }


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
        
        _lineRenderer = GetComponent<LineRenderer>();
        _controller = GetComponent<CharacterController>();
        InputHandler = GetComponent<PlayerInputHandler>();
        GroundedState = new PlayerGroundedState(this);
        JumpingState = new PlayerJumpingState(this);
        FallingState = new PlayerFallingState(this);
        LandingState = new PlayerLandingState(this);
        InteractingState = new PlayerInteractingState(this);
        CrouchingState = new PlayerCrouchingState(this);
        InMenuState = new PlayerInMenuState(this);
        _defaultCharacterHeight = _controller.height;
        _defaultCharacterCenter = _controller.center;
        
        _lineRenderer.positionCount = 2;
        _lineRenderer.enabled = false;
        SwitchState(GroundedState);
    }

    private void Start()
    {
        if (!_robot) _robot = FindFirstObjectByType<RobotCompanion>();
        if (!_cameraManager) _cameraManager = FindFirstObjectByType<CameraManager>();
        _cameraManager.Initialize(this);
    }

    private void Update()
    {
        if (!IsAiming && CurrentInteractable == null && _robot && _robot.CanCommend() && InputHandler.RobotInteractInput)
        {
            _robot.FollowPlayer();
        }
        CheckCollisions();
        UpdateFallTime();
        UpdateAimRay();
        HandleAimingToggle();
        CurrentState.UpdateState();
        UpdateDebugText();
    }

    private void FixedUpdate()
    {
        CurrentState.FixedUpdateState();
        MoveCharacter(); 
    }
    
    
    #region Collisions ---------------------------------------------------------------

    private void CheckCollisions()
    {
        Vector3 groundSpherePosition = transform.position + groundCheckOffset;
        IsGrounded = Physics.CheckSphere(groundSpherePosition, environmentCheckRadius, environmentLayer);
        
        Vector3 ceilingSpherePosition = transform.position + ceilingCheckOffset;
        CanStand = !Physics.CheckSphere(ceilingSpherePosition, environmentCheckRadius, environmentLayer);
    }
    
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out IInteractable interactable))
        {
            CurrentInteractable = interactable;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent(out IInteractable interactable) && interactable == CurrentInteractable)
        {
            // Allow player interaction
            CanInteract = CurrentInteractable.PlayerCanInteract && (CurrentState == GroundedState || CurrentState == CrouchingState) && (CurrentInteractable != CurrentAimedInteractable);
        
            // Allow robot interaction
            if (_robot && CurrentInteractable.RobotCanInteract && InputHandler.RobotInteractInput)
            {
                InputHandler.ConsumeRobotInteractBuffer();
                _robot.InteractWith(CurrentInteractable);
            }

            if (CanInteract && !CurrentInteractable.IsHighlighted())
            {
                CurrentInteractable.SetHighlight(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out IInteractable interactable) && interactable == CurrentInteractable)
        {
            CurrentInteractable.SetHighlight(false);
            CurrentInteractable = null;
            CanInteract = false;
        }
    }
    
    private void OnAimEnter(IInteractable interactable)
    {
        if (_robot && CurrentAimedInteractable is { RobotCanInteract: true })
        {
            interactable.OnAimEnter(this);
        }
    }

    private void OnAimStay(IInteractable interactable)
    {
        interactable.OnAimStay(this);
        
        if (_robot && CurrentAimedInteractable is { RobotCanInteract: true } && InputHandler.RobotInteractInput)
        {
            InputHandler.ConsumeRobotInteractBuffer();
            _robot.InteractWith(CurrentAimedInteractable);
        }
    }

    private void OnAimExit(IInteractable interactable)
    {
        interactable.OnAimExit(this);
    }

    #endregion Collisions ---------------------------------------------------------------
    
    
    #region State Control ---------------------------------------------------------------

    public void SwitchState(PlayerBaseState newState)
    {
        CurrentState?.ExitState();
        CurrentState = newState;
        CurrentState.EnterState();
    }
    
    public void OnInteractionComplete(IInteractable interactable)
    {
        InteractingState.OnInteractionComplete(interactable);
    }


    #endregion State Control ---------------------------------------------------------------

    
    #region Aiming ---------------------------------------------------------------
    

    
    private void UpdateAimRay()
    {
        if (!IsAiming)
        {
            // Hide line renderer when not aiming
            if (_lineRenderer.enabled)
            {
                _lineRenderer.enabled = false;
                if (CurrentAimedInteractable != null)
                {
                    OnAimExit(CurrentAimedInteractable);
                    CurrentAimedInteractable = null;
                }
            }
            return;
        }

        // Show line renderer when aiming
        if (!_lineRenderer.enabled)
        {
            _lineRenderer.enabled = true;
        }

        // Set ray origin (player position, slightly adjusted to match camera view)
        Vector3 rayOrigin = transform.position;
        if (aimRayStartPosition)
        {
            rayOrigin = aimRayStartPosition.position;
        }
        
        // Get ray direction from camera
        Vector3 rayDirection = _cameraManager.GetCameraAimDirection();
        
        // Set first point of line renderer
        _lineRenderer.SetPosition(0, rayOrigin);
        
        // Create the actual ray for Physics ray-casting
        Ray aimRay = new Ray(rayOrigin, rayDirection);
        
        // Perform raycast to see if we hit anything
        if (Physics.Raycast(aimRay, out RaycastHit hitInfo, aimRayMaxDistance, aimRayHitMask))
        {
            // Set second point of line renderer to hit position
            _lineRenderer.SetPosition(1, hitInfo.point);
            
            // Check if the hit object implements IInteractable
            if (hitInfo.collider.TryGetComponent(out IInteractable hitInteractable))
            {
                if (CurrentAimedInteractable != hitInteractable)
                {
                    // Exit previous target if there was one
                    if (CurrentAimedInteractable != null)
                    {
                        OnAimExit(CurrentAimedInteractable);
                    }
                    
                    // Set new target and enter it
                    CurrentAimedInteractable = hitInteractable;
                    OnAimEnter(CurrentAimedInteractable);
                }
                
                // Update aim on current target
                OnAimStay(CurrentAimedInteractable);
            }
            else if (CurrentAimedInteractable != null)
            {
                // We're no longer aiming at an interactable
                OnAimExit(CurrentAimedInteractable);
                CurrentAimedInteractable = null;
            }
        }
        else
        {
            // No hit, set line end point to max distance
            _lineRenderer.SetPosition(1, rayOrigin + (rayDirection * aimRayMaxDistance));
            
            // Clear current target if we had one
            if (CurrentAimedInteractable != null)
            {
                OnAimExit(CurrentAimedInteractable);
                CurrentAimedInteractable = null;
            }
        }
    }
    
    private void HandleAimingToggle()
    {
        // Toggle aim mode based on input
        if (InputHandler.AimInput)
        {
            if (!IsAiming)
            {
                IsAiming = true;
            
                // When first entering aiming mode, immediately align character with camera
                Vector3 initialAimDirection = GetCameraAimDirection();
                // Flatten the direction to prevent tilting
                Vector3 flattenedAimDirection = new Vector3(initialAimDirection.x, 0, initialAimDirection.z).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(flattenedAimDirection);
                transform.rotation = targetRotation;
                _lastRotationDirection = flattenedAimDirection;
        
                // Switch to aim camera
                _cameraManager.SwitchToAimCamera();
            }
        }
        else if (IsAiming)
        {
            IsAiming = false;
        
            // Switch to free look camera
            _cameraManager.SwitchToFreeLookCamera();
        }
    }
    
    
    #endregion Aiming ---------------------------------------------------------------
    
    
    #region State modules ---------------------------------------------------------------
    
    public void ApplyMovement(MovementParams parameters)
    {
        // Calculate movement intensity (0-1)
        float movementIntensity = Mathf.Clamp01(
            Mathf.Abs(InputHandler.MovementInput.x) + 
            Mathf.Abs(InputHandler.MovementInput.y)
        );

        // Determine movement direction
        Vector3 inputDirection;
        Quaternion targetRotation = transform.rotation;
        
        if (IsAiming)
        {
            // When aiming, maintain player orientation toward camera
            // and calculate movement direction relative to player orientation
            Vector3 aimDirection = GetCameraAimDirection();
            targetRotation = Quaternion.LookRotation(aimDirection);
            
            // Calculate movement relative to player orientation when aiming
            // This creates proper strafing movement
            Vector3 forward = aimDirection;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            
            inputDirection = (forward * InputHandler.MovementInput.y + 
                             right * InputHandler.MovementInput.x).normalized;
        }
        else
        {
            // Standard camera-relative movement for non-aiming
            inputDirection = CalculateMoveDirection();
        }
        
        float inputMagnitude = inputDirection.magnitude;
        float targetSpeed;
        
        // Calculate target speed based on whether we're airborne or grounded
        if (parameters.IsAirborne)
        {
            // Airborne movement has fixed speed
            targetSpeed = inputMagnitude > PlayerInputHandler.MovementInputThreshold ? 
                airMoveSpeed * parameters.SpeedMultiplier : 0f;
        }
        else
        {
            // Grounded movement uses CalculateTargetSpeed with intensity
            targetSpeed = CalculateTargetSpeed(movementIntensity) * parameters.SpeedMultiplier;
        }

        // Only update direction if we have meaningful input
        if (inputMagnitude > PlayerInputHandler.MovementInputThreshold)
        {
            ActiveMoveDirection = inputDirection;
        }
        else
        {
            // No input - keep last direction but set target speed to 0
            targetSpeed = 0f;
        }

        // Determine acceleration/deceleration rate
        float speedChange;
        
        if (parameters.IsAirborne)
        {
            // Airborne uses different acceleration/deceleration rates
            speedChange = ActiveHorizontalVelocity > targetSpeed ? 
                airFriction * parameters.AccelMultiplier : 
                airAcceleration * parameters.AccelMultiplier;
        }
        else
        {
            // Grounded uses standard acceleration
            speedChange = acceleration * parameters.AccelMultiplier;
        }

        // Apply movement control multiplier (for landing recovery, etc.)
        targetSpeed *= parameters.ControlMultiplier;
        speedChange *= parameters.ControlMultiplier;

        // Update current speed with acceleration
        float newSpeed = Mathf.MoveTowards(
            ActiveHorizontalVelocity, 
            targetSpeed, 
            speedChange * Time.fixedDeltaTime
        );

        // Update state machine's speed
        ActiveHorizontalVelocity = newSpeed;
        
        // Only reset move direction when completely stopped
        if (newSpeed <= 0.01f)
        {
            ActiveMoveDirection = Vector3.zero;
        }
    }
    
    public void ApplyRotation(RotationParams parameters)
    {
        // Skip rotation if not allowed
        if (!parameters.AllowRotation)
            return;
                
        // If aiming and using aim rotation
        if (IsAiming && parameters.UseAimRotation)
        {
            // Get aim direction from camera
            Vector3 aimDirection = GetCameraAimDirection();
            
            // Flatten the aim direction to prevent tilting up or down
            Vector3 flattenedAimDirection = new Vector3(aimDirection.x, 0, aimDirection.z).normalized;
            
            // Calculate angle difference between current aim direction and last rotation direction
            float angleChange = Vector3.Angle(_lastRotationDirection, flattenedAimDirection);
            
            // Check if mouse movement exceeds our threshold
            if (InputHandler.MouseDelta.magnitude > _cameraManager.AimRotationThreshold || angleChange > 1.0f)
            {
                // There's significant mouse movement, so update the character rotation
                Quaternion targetRotation = Quaternion.LookRotation(flattenedAimDirection);
                
                // Rotate player to face aim direction
                float rotationSpeed = aimRotationSpeed * parameters.RotationMultiplier;
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.fixedDeltaTime * 100f
                );
                
                // Store this as the last direction we rotated to
                _lastRotationDirection = flattenedAimDirection;
            }
        }
        // Standard non-aiming rotation
        else if (!IsAiming && ActiveMoveDirection.sqrMagnitude > PlayerInputHandler.RotationInputThreshold)
        {
            // Calculate target rotation based on movement direction
            Quaternion targetRotation = Quaternion.LookRotation(ActiveMoveDirection);
            
            // Calculate rotation speed based on parameters and movement
            float baseSpeed = parameters.IsAirborne ? airRotationSpeed : rotationSpeed;
            float speedMultiplier = ActiveHorizontalVelocity > 0.1f ? 1.5f : 1f;
            
            // Apply rotation
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                baseSpeed * parameters.RotationMultiplier * speedMultiplier * Time.fixedDeltaTime * 100f
            );
        }
    }
    
    private void MoveCharacter()
    {
        // Create movement vector using the active properties
        Vector3 movement = Vector3.zero;
    
        // Only apply horizontal movement if we have both direction and speed
        if (ActiveMoveDirection.sqrMagnitude > 0.001f && ActiveHorizontalVelocity > 0.01f)
        {
            movement = ActiveMoveDirection * ActiveHorizontalVelocity;
        }
    
        // Always apply vertical movement
        movement.y = ActiveVerticalVelocity;
    
        // Apply movement
        _controller.Move(movement * Time.fixedDeltaTime);
    }
    
    
    public void ApplyGravity(bool isGrounded)
    {
        if (isGrounded)
        {
            // Apply constant grounded gravity
            ActiveVerticalVelocity = groundedGravity;
        }
        else
        {
            // Calculate new vertical velocity with gravity applied
            ActiveVerticalVelocity += gravity * Time.fixedDeltaTime;
        
            // Limit to terminal velocity
            ActiveVerticalVelocity = Mathf.Max(ActiveVerticalVelocity, maxVerticalVelocity);
        }
    }
    
    
    public void SetCharacterHeight(bool crouching)
    {
        if (crouching)
        {
            _controller.height = _crouchCharacterHeight;
            _controller.center = _crouchCharacterCenter;
        }
        else
        {
            _controller.height = _defaultCharacterHeight;
            _controller.center = _defaultCharacterCenter;
        }
    }
    
    #endregion State modules ---------------------------------------------------------------

    
    #region Calculations ---------------------------------------------------------------

    private Vector3 CalculateMoveDirection()
    {
        if (!_cameraManager) return transform.forward;
        
        // Get camera forward and right
        var forward = _cameraManager.freeLookCamera.transform.forward;
        var right = _cameraManager.freeLookCamera.transform.right;
    
        // Project onto horizontal plane
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // Calculate movement direction relative to camera
        return (forward * InputHandler.MovementInput.y + 
                right * InputHandler.MovementInput.x).normalized;
    }
    
    private float CalculateTargetSpeed(float movementIntensity)
    {
        _lockSprinting = InputHandler.MoveSpeedInput || IsAiming || CurrentState == CrouchingState;
    
        if (movementIntensity < PlayerInputHandler.MovementInputThreshold)
            return 0f;

        if (!_lockSprinting)
        {
            if (InputHandler.SprintInput && movementIntensity > PlayerInputHandler.SprintInputThreshold)
                return sprintSpeed;
            else
                return runSpeed;
        }
        else
        {

            if (InputHandler.SprintInput && movementIntensity > PlayerInputHandler.SprintInputThreshold)
                return runSpeed;
            else
                return walkSpeed;
        }
    }
    
    
    private Vector3 GetCameraAimDirection()
    {
        if (!_cameraManager) return transform.forward;
        return _cameraManager.GetCameraAimDirection(true);
    }
    

    #endregion Calculations ---------------------------------------------------------------
    
    
    #region Utility ---------------------------------------------------------------

    private void UpdateFallTime()
    {
        // Only increment fall time when moving downward
        if (!IsGrounded && CurrentState != JumpingState)
        {
            FallTime += Time.deltaTime;
        }
    }

    
    private void UpdateDebugText()
    {
        if (!debugText) return;

        debugText.text = $"State: {CurrentState.GetType().Name}\n" +
                         $"IsGrounded: {IsGrounded}\n" +
                         $"CanStand: {CanStand}\n" +
                         $"IsAiming: {IsAiming}\n" +
                         $"AirTime: {AirTime}\n" +
                         $"FallTime: {FallTime}\n" +
                         $"LandingIntensity: {LandingIntensity}\n" +
                         $"MoveDirection: {ActiveMoveDirection}\n" +
                         $"Interactable: {CurrentInteractable}\n" +
                         $"AimedInteractable: {CurrentAimedInteractable}\n" +
                         $"ActiveHorizontalSpeed: {ActiveHorizontalVelocity}\n" +
                         $"ActiveVerticalVelocity: {ActiveVerticalVelocity}\n";
    }
    
    private void OnDrawGizmos()
    {
        // Ground sphere
        if (IsGrounded)
        {
            Gizmos.color = Color.green;
        } 
        else if (FallTime < fallThreshold)
        {
            Gizmos.color = Color.yellow;
        }
        else
        {
            Gizmos.color = Color.red;
        }
        Vector3 groundSpherePosition = transform.position + groundCheckOffset;
        Gizmos.DrawWireSphere(groundSpherePosition, environmentCheckRadius);
        
        // Ceiling sphere
        Gizmos.color = CanStand ? Color.green : Color.red;
        Vector3 ceilingSpherePosition = transform.position + ceilingCheckOffset;
        Gizmos.DrawWireSphere(ceilingSpherePosition, environmentCheckRadius);
    }

    #endregion Utility ---------------------------------------------------------------
}