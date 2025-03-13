
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using VInspector;


public enum CameraMode
{
    AimOnly,            
    ExplorationAndAim   
}

[SelectionBase]
[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(CharacterController))]
public class PlayerStateMachine : MonoBehaviour, Iinteractor
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
    public PlayerTeleportingState TeleportingState { get;  set; }
    
    [Header("Movement")]
    [Tooltip("Controls whether the player can only aim or can toggle between aim and non-aim modes")]
    [SerializeField] private CameraMode cameraMode = CameraMode.ExplorationAndAim;
    [Tooltip("Walking speed when holding the walk button")]
    public float walkSpeed = 3f;
    [Tooltip("Default running speed")]
    public float runSpeed = 7.5f;
    [Tooltip("If sprint gait is allowed")]
    public bool allowSprint = true;
    [EnableIf("allowSprint"), Tooltip("Maximum speed when sprinting with sufficient input")]
    public float sprintSpeed = 10f; [EndIf]
    [Tooltip("Speed multiplier when strafing (moving sideways)")]
    public float strafeSpeedMultiplier = 0.7f;
    [Tooltip("Speed multiplier when moving backward")]
    public float backwardSpeedMultiplier = 0.6f;
    [Tooltip("How quickly the character reaches target speed")]
    public float acceleration = 10f;
    [Tooltip("Drag force applied to movement on ground")]
    public float groundDrag = 10f;
    [Tooltip("Anti-bump force to prevent sticking to slopes")]
    public float antiBumpForce = 4f;
    [Tooltip("Base rotation speed when turning on the ground")]
    public float rotationSpeed = 2f;
    [Tooltip("How quickly player rotates to align with camera when idle")]
    public float idleAlignmentSpeed = 2f;
    [Tooltip("Time to complete an idle rotation")]
    public float idleRotationDuration = 0.67f;

    
    [Header("Air")]
    [Tooltip("How quickly the character reaches target speed in air")]
    public float airAcceleration = 5f;
    [Tooltip("Drag force applied to movement in air")]
    public float airDrag = 5f;
    [Tooltip("Rotation speed when turning while aiming")]
    public float aimRotationSpeed = 4f;
    [Tooltip("Initial upward velocity applied when jumping")]
    public float jumpForce = 2f;
    [Tooltip("Minimum time falling before impact animations trigger")]
    public float fallThreshold = 0.1f;

    [Header("Gravity")]
    [Tooltip("Downward acceleration applied while in the air")]
    public float gravity = -15f;
    [Tooltip("Maximum downward velocity the character can reach")]
    public float maxVerticalVelocity = -25f;




    [Header("Collision Check")]
    [Tooltip("Radius of the sphere used to detect environment")]
    [SerializeField] private float environmentCheckRadius = 0.3f;
    [Tooltip("Offset from character position for ground detection")]
    [SerializeField] private Vector3 groundCheckOffset = new Vector3(0, -0.7f, 0);
    [Tooltip("Offset from character position for ceiling detection")]
    [SerializeField] private Vector3 ceilingCheckOffset = new Vector3(0, 0.63f, 0);
    [Tooltip("Layer mask defining what objects count as environment")]
    [SerializeField] private LayerMask environmentLayer = 1;
    [Tooltip("Radius of the sphere used to detect interactable")]
    [SerializeField] private float interactableCheckRadius = 0.5f;
    [Tooltip("Offset from character position for interactable detection")]
    [SerializeField] private Transform interactableCheckPosition;
    [Tooltip("Layer mask defining what objects count as interactable")]
    [SerializeField] private LayerMask interactableLayer = 1;
    [Tooltip("Maximum distance the aim ray will travel")]
    public float aimRayMaxDistance = 25f;
    [Tooltip("The start position of the aim ray")]
    public Transform aimRayStartPosition;

    [Header("References")] 
    public TextMeshProUGUI debugText;
    public GameObject menu;

    [Header("Events")] 
    public UnityEvent onPlayerSpawned = new UnityEvent();
    public UnityEvent onPlayerOpenedMenu = new UnityEvent();
    
    
    public InteractorType InteractorType { get; } = InteractorType.Player;
    public CameraMode CurrentCameraMode => cameraMode;
    public float AirTime { get;  set; }
    public float FallTime { get;  set; }
    public float ActiveHorizontalVelocity { get; private set; }
    public float ActiveVerticalVelocity { get; set; }
    public Vector3 ActiveMoveDirection { get; private set; } = Vector3.zero;
    public bool IsGrounded { get; private set; }
    public bool CanStand { get; private set; }
    public bool IsAiming { get; private set; }
    public PlayerInputHandler InputHandler { get; private set; }
    public Interactable CurrentInteractable { get; private set; }
    public Interactable CurrentAimedInteractable { get; private set; }
    public float RotationMismatch { get; private set; }
    public bool IsRotatingToTarget { get; private set; }
    public RobotCompanion robot { get; private set; }
    public CameraManager cameraManager { get; private set; }
    

    private CharacterController _controller;
    private LineRenderer _lineRenderer;
    private float _defaultCharacterHeight;
    private Vector3 _defaultCharacterCenter;
    private readonly float _crouchCharacterHeight = 1.2333f;
    private readonly Vector3 _crouchCharacterCenter = new Vector3(0, -0.3f, 0f);
    private float _rotatingToTargetTimer = 0f;
    private Vector3 _lastCameraForward;
    private bool _isMovingLaterally = false;
    private bool _lastGroundedState = false;
    private Vector3 _lastMoveDirection = Vector3.zero;
    private bool _wasMovingLastFrame = false;
    private bool _isRotatingFromIdle = false;
    private Quaternion _targetIdleRotation = Quaternion.identity;
    private float _rotationProgress = 1.0f; 
    

    
    


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
        IsAiming = cameraMode == CameraMode.AimOnly;
        SwitchState(GroundedState);
    }

    private void Start()
    {
        if (!robot) robot = FindFirstObjectByType<RobotCompanion>();
        if (!cameraManager) cameraManager = FindFirstObjectByType<CameraManager>();
        cameraManager.Initialize(this);

        if (TestManager.Instance)
        {
            TestManager.Instance.onTestLoaded.AddListener(OnTestLoaded);
        }
        
    }
    
    
    private void OnDisable()
    {
        if (TestManager.Instance)
        {
            TestManager.Instance.onTestLoaded.RemoveListener(OnTestLoaded);
        }
    }
    

    private void Update()
    {
        UpdateFallTime();
        UpdateDebugInformation();
        CurrentState.UpdateState();
    }

    private void FixedUpdate()
    {
        CurrentState.FixedUpdateState();
        MoveCharacter(); 
        
        
        
        if (CurrentState == FallingState) // Ripple effect
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position + groundCheckOffset, Vector3.down, out hit, 0.5f, environmentLayer))
            {
            
                var ground = hit.transform.GetComponent<GroundRipple>();

                if (ground)
                {
                    ground.GetHit(hit);
                }
            }
        }
    }
    
    private void OnTestLoaded(SOTest test)
    {
        robot = TestManager.Instance.Robot;
        SwitchState(new PlayerTeleportingState(this, TestManager.Instance.GetSpawnPoint(), Quaternion.Euler(0, 0, 0), 2f));
    }
    


    
    #region State machine ---------------------------------------------------------------

    private void MoveCharacter()
    {
        if (!_controller || _controller.enabled == false) return;
        
        // Get horizontal velocity from direction and speed
        Vector3 horizontalMovement = ActiveMoveDirection * ActiveHorizontalVelocity;
    
        // Create full movement vector with vertical component
        Vector3 movement = new Vector3(
            horizontalMovement.x,
            ActiveVerticalVelocity,
            horizontalMovement.z
        );
    
        // Apply movement
        _controller.Move(movement * Time.fixedDeltaTime);
    }
    
    private void UpdateFallTime()
    {
        // Only increment fall time when moving downward
        if (!IsGrounded && CurrentState != JumpingState)
        {
            FallTime += Time.deltaTime;
        }
    }
    
    public void SwitchState(PlayerBaseState newState)
    {
        CurrentState?.ExitState();
        CurrentState = newState;
        CurrentState.EnterState();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out LaserGround laserGround))
        {
            SwitchState(new PlayerTeleportingState(this, TestManager.Instance.GetCheckPoint(), Quaternion.Euler(0, 0, 0), 2f));
        }
    }

    #endregion State machine ---------------------------------------------------------------
    
    
    #region Calculations ---------------------------------------------------------------

    private Vector3 CalculateMoveDirection()
    {
        if (!cameraManager) return transform.forward;
        
        // Get camera forward and right
        var forward = cameraManager.freeLookCamera.transform.forward;
        var right = cameraManager.freeLookCamera.transform.right;
    
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
        bool lockSprintGait = InputHandler.MoveSpeedInput || !allowSprint || IsAiming || CurrentState == CrouchingState;

        if (movementIntensity < InputHandler.MovementInputThreshold)
            return 0f;

        // Determine base speed based on input and state
        float baseSpeed;
        float startSpeed;
        float targetSpeed;
        
        if (!lockSprintGait)
        {
            startSpeed = InputHandler.SprintInput ? runSpeed : 0;
            targetSpeed = InputHandler.SprintInput ? sprintSpeed : runSpeed;
                
        }
        else
        {
            startSpeed = InputHandler.SprintInput ? walkSpeed : 0;
            targetSpeed = InputHandler.SprintInput ? runSpeed : walkSpeed;
        }
        
        baseSpeed = Mathf.Lerp(startSpeed, targetSpeed, movementIntensity);
    
        // Apply direction multipliers based on movement input
        float directionMultiplier = 1.0f;

        if (IsAiming)
        {
            // Check for backward movement (negative Y input)
            if (InputHandler.MovementInput.y < -0.3f)
            {
                // More negative Y = more backward movement effect
                float backwardFactor = Mathf.Abs(InputHandler.MovementInput.y);
                directionMultiplier *= Mathf.Lerp(1.0f, backwardSpeedMultiplier, backwardFactor);
            }
    
            // Check for strafing movement (X input)
            if (Mathf.Abs(InputHandler.MovementInput.x) > 0.3f)
            {
                // Stronger X input = more strafe effect
                float strafeFactor = Mathf.Abs(InputHandler.MovementInput.x);
                directionMultiplier *= Mathf.Lerp(1.0f, strafeSpeedMultiplier, strafeFactor);
            }
        }

    
        // Return the modified speed
        return baseSpeed * directionMultiplier;
    }
    
    
    private Vector3 GetCameraAimDirection()
    {
        if (!cameraManager) return transform.forward;
        return cameraManager.GetCameraAimDirection(true);
    }
    
        
    private Vector3 HandleSteepSurfaces(Vector3 velocity)
    {
        // Don't apply when moving upward
        if (ActiveVerticalVelocity >= 0) return velocity;
        
        // Cast a sphere to detect surface normal
        Vector3 origin = transform.position + Vector3.up * _controller.radius;
        float distance = _controller.height * 0.5f + 0.1f;
        
        if (Physics.SphereCast(origin, _controller.radius, Vector3.down, 
                out RaycastHit hitInfo, distance, environmentLayer))
        {
            float angle = Vector3.Angle(hitInfo.normal, Vector3.up);
            
            // Only apply for steep slopes beyond character controller's slope limit
            if (angle > _controller.slopeLimit)
            {
                // Project movement onto the surface to slide
                return Vector3.ProjectOnPlane(velocity, hitInfo.normal);
            }
        }
        
        return velocity;
    }
    

    #endregion Calculations ---------------------------------------------------------------
    
    
    #region Interaction ---------------------------------------------------------------
    
    private void SelectInteractable(Collider collider3d)
    {
        CurrentInteractable = collider3d.GetComponent<Interactable>();
        if (CurrentInteractable)
        {
            if (CurrentInteractable.OnlyRobotCanInteract) return;
            
            CurrentInteractable.MarkForPlayerInteraction(this);

            if (CurrentInteractable == CurrentAimedInteractable)
            {
                CurrentAimedInteractable = null;
            }
        }
    }
    
    public void ClearCurrentInteractable()
    {
        if (!CurrentInteractable) return;

        CurrentInteractable.UnmarkForInteraction();
        CurrentInteractable = null;
    }

    public void ClearCurrentAimedInteractable()
    {
        if (!CurrentAimedInteractable) return;
        
        CurrentAimedInteractable.UnmarkForInteraction();
        CurrentAimedInteractable = null;
    }
    
    public void OnInteractionStart(Interactable interactable)
    {
        SwitchState(InteractingState);
    }

    public void OnInteractionEnd(Interactable interactable)
    {
        
        
        InteractingState.OnInteractionComplete();
        if (CurrentInteractable == interactable)
        {
            CurrentInteractable = null;
        }
        
    }

    public void CancelInteraction(Interactable interactable)
    {
        if (CurrentInteractable != interactable) return;
        
        interactable.CancelInteraction();
        InteractingState.OnInteractionComplete();
        CurrentInteractable = null;
    }
    


    #endregion Interaction ---------------------------------------------------------------
    
    
    #region States methods - Collisions ---------------------------------------------------------------

    public void CheckEnvironmentCollisions()
    {
        Vector3 groundSpherePosition = transform.position + groundCheckOffset;
        IsGrounded = Physics.CheckSphere(groundSpherePosition, environmentCheckRadius, environmentLayer);
        
        Vector3 ceilingSpherePosition = transform.position + ceilingCheckOffset;
        CanStand = !Physics.CheckSphere(ceilingSpherePosition, environmentCheckRadius, environmentLayer);
    }

    public void CheckForInteractable()
    {
        Collider[] interactableColliders = Physics.OverlapSphere(interactableCheckPosition.position, interactableCheckRadius, interactableLayer);
        if (interactableColliders == null || interactableColliders.Length == 0)
        {
            ClearCurrentInteractable();
        }
        else
        {
            Collider firstOverlap = interactableColliders[0];
            SelectInteractable(firstOverlap);
        }
    }
    
    private void CheckForAimInteractable()
    {
        if (!robot) return;
        
        // Set ray origin 
        Vector3 rayOrigin = aimRayStartPosition.position;

        // Get ray direction from camera
        Vector3 rayDirection = cameraManager.GetCameraAimDirection() + new Vector3(0, 0.2f,0);

        // Set first point of line renderer
        _lineRenderer.SetPosition(0, rayOrigin);

        // Create a layer mask that includes both interactable objects AND environment/walls
        // This ensures we hit walls first if they're in the way
        LayerMask raycastMask = interactableLayer | environmentLayer;

        // Create the actual ray for Physics ray-casting
        Ray aimRay = new Ray(rayOrigin, rayDirection);

        // Perform raycast to see if we hit anything
        if (Physics.Raycast(aimRay, out RaycastHit hitInfo, aimRayMaxDistance, raycastMask))
        {
            // Set second point of line renderer to hit position
            _lineRenderer.SetPosition(1, hitInfo.point);
            
            // Check if the hit object is on the interactable layer
            if (((1 << hitInfo.collider.gameObject.layer) & interactableLayer) != 0)
            {
                // Check if the hit object implements IInteractable
                if (hitInfo.collider.TryGetComponent(out Interactable hitInteractable))
                {
                    // Check if this interactable is different from CurrentInteractable
                    // Only set it as CurrentAimedInteractable if it's not already the CurrentInteractable
                    if (CurrentAimedInteractable != hitInteractable && CurrentInteractable != hitInteractable)
                    {
                        // Exit previous target if there was one
                        ClearCurrentAimedInteractable();
        
                        // Set new target and enter it
                        CurrentAimedInteractable = hitInteractable;
                        CurrentAimedInteractable.MarkForRobotInteraction(robot);
                    }
                }
                else if (CurrentAimedInteractable)
                {
                    // We're no longer aiming at an interactable
                    ClearCurrentAimedInteractable();
                }
            }
            else
            {
                // We hit something that's not an interactable (like a wall)
                ClearCurrentAimedInteractable();
            }
        }
        else
        {
            // No hit, set line end point to max distance
            _lineRenderer.SetPosition(1, rayOrigin + (rayDirection * aimRayMaxDistance));
            
            // Clear current target if we had one
            ClearCurrentAimedInteractable();
        }
    }
    
    

    #endregion States methods - Collisions ---------------------------------------------------------------
    

    #region State methods - Movement ---------------------------------------------------------------
    
    
    public void HandleMovement(bool allowMovement, bool isAirborne)
    {
        // Current velocity excluding vertical component
        Vector3 currentHorizontalVelocity = new Vector3(
            _controller.velocity.x, 
            0, 
            _controller.velocity.z
        );
        
        // Initialize the new velocity to current (will be modified below)
        Vector3 newHorizontalVelocity = currentHorizontalVelocity;
        
        // Calculate movement input magnitude
        float movementIntensity = Mathf.Clamp01(
            Mathf.Abs(InputHandler.MovementInput.x) + 
            Mathf.Abs(InputHandler.MovementInput.y)
        );
        
        // Check if input is above threshold
        bool hasMovementInput = movementIntensity > InputHandler.MovementInputThreshold;
        
        // Process movement if allowed
        if (allowMovement && hasMovementInput)
        {
            // Determine movement direction
            Vector3 inputDirection;
            if (IsAiming)
            {
                // When aiming, calculate strafe movement
                Vector3 aimDirection = GetCameraAimDirection();
                Vector3 forward = new Vector3(aimDirection.x, 0, aimDirection.z).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                
                inputDirection = (forward * InputHandler.MovementInput.y + 
                               right * InputHandler.MovementInput.x).normalized;
            }
            else
            {
                // Standard camera-relative movement
                inputDirection = CalculateMoveDirection();
            }
            
            // Calculate target speed
            float targetSpeed = CalculateTargetSpeed(movementIntensity);
            Vector3 targetVelocity = inputDirection * targetSpeed;
            
            // Apply appropriate acceleration
            float accelRate = isAirborne ? airAcceleration : acceleration;
            Vector3 velocityChange = targetVelocity - currentHorizontalVelocity;
            velocityChange = Vector3.ClampMagnitude(velocityChange, accelRate * Time.fixedDeltaTime);
            
            newHorizontalVelocity = currentHorizontalVelocity + velocityChange;
        }
        else
        {
            // No movement input or movement not allowed - apply greater drag to slow down
            float dragMultiplier = allowMovement ? 1.0f : 2.0f; // Extra drag when movement disabled
            
            // Apply drag to slow down if there's velocity
            if (newHorizontalVelocity.magnitude > 0.01f)
            {
                float dragToUse = isAirborne ? airDrag : groundDrag;
                Vector3 dragForce = newHorizontalVelocity.normalized * (dragToUse * dragMultiplier * Time.fixedDeltaTime);
                
                // Only apply drag up to current speed
                if (dragForce.magnitude > newHorizontalVelocity.magnitude)
                {
                    newHorizontalVelocity = Vector3.zero;
                }
                else
                {
                    newHorizontalVelocity -= dragForce;
                }
            }
        }
        
        // Handle steep surfaces in air
        if (isAirborne)
        {
            newHorizontalVelocity = HandleSteepSurfaces(newHorizontalVelocity);
        }
        
        // Update state machine values
        ActiveHorizontalVelocity = newHorizontalVelocity.magnitude;
        if (newHorizontalVelocity.magnitude > 0.01f)
        {
            ActiveMoveDirection = newHorizontalVelocity.normalized;
        }
        
        // Track if we're moving laterally for animation/rotation purposes
        _isMovingLaterally = ActiveHorizontalVelocity > InputHandler.MovementInputThreshold;
    }


    public void HandleRotation(bool allowRotation, bool alignWithCameraWhenIdle)
    {
        // Skip rotation if not allowed
        if (!allowRotation)
            return;
        
        // Calculate camera-to-player rotation mismatch
        if (cameraManager)
        {
            Vector3 cameraForward = cameraManager.GetCameraAimDirection(true);
            Vector3 cameraForwardFlat = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
            Vector3 playerForward = transform.forward;
            
            // Calculate cross product to determine direction
            Vector3 cross = Vector3.Cross(playerForward, cameraForwardFlat);
            float sign = Mathf.Sign(Vector3.Dot(cross, Vector3.up));
            
            // Calculate and store rotation mismatch
            RotationMismatch = sign * Vector3.Angle(playerForward, cameraForwardFlat);
            _lastCameraForward = cameraForwardFlat;
        }
        
        // AIMING MODE ROTATION 
        if (IsAiming)
        {
            // When aiming while idle, align with camera
            if (!_isMovingLaterally)
            {
                // Start timer if needed
                if (_rotatingToTargetTimer <= 0)
                {
                    _rotatingToTargetTimer = idleRotationDuration;
                    IsRotatingToTarget = true;
                }
                
                // Update timer
                if (_rotatingToTargetTimer > 0)
                {
                    _rotatingToTargetTimer -= Time.fixedDeltaTime;
                    
                    // Calculate target rotation based on camera
                    Quaternion targetRotation = Quaternion.LookRotation(_lastCameraForward);
                    
                    // Apply rotation with Lerp for smoother transitions
                    transform.rotation = Quaternion.Lerp(
                        transform.rotation,
                        targetRotation,
                        idleAlignmentSpeed * Time.fixedDeltaTime * 3f
                    );
                    
                    if (_rotatingToTargetTimer <= 0)
                    {
                        IsRotatingToTarget = false;
                    }
                }
            }
            // When aiming while moving, use smoother rotation to face aim direction
            else if (_isMovingLaterally)
            {
                // Get aim direction from camera
                Vector3 aimDirection = GetCameraAimDirection();
                
                // Flatten the aim direction to prevent tilting
                Vector3 flattenedAimDirection = new Vector3(aimDirection.x, 0, aimDirection.z).normalized;
                
                // Calculate target rotation and apply it using Lerp for smoothness
                Quaternion targetRotation = Quaternion.LookRotation(flattenedAimDirection);
                
                // Use Lerp for smoother transitions
                transform.rotation = Quaternion.Lerp(
                    transform.rotation,
                    targetRotation,
                    aimRotationSpeed * Time.fixedDeltaTime * 3f
                );
                
                IsRotatingToTarget = true;
            }
            else
            {
                IsRotatingToTarget = false;
            }
        }
        // NON-AIMING MODE ROTATION
        else
        {
            // Check if we just started moving from idle
            if (!_wasMovingLastFrame && _isMovingLaterally)
            {
                // Check if we're moving primarily forward
                bool isMovingPrimarilyForward = InputHandler.MovementInput.y > 0.7f && 
                                                Mathf.Abs(InputHandler.MovementInput.x) < 0.3f;

                // Calculate target rotation based on movement direction
                var targetDirection = isMovingPrimarilyForward ? _lastCameraForward : ActiveMoveDirection;

                // Start the idle-to-movement rotation
                _isRotatingFromIdle = true;
                _targetIdleRotation = Quaternion.LookRotation(targetDirection);
                
                // Calculate initial rotation progress
                float angleDifference = Quaternion.Angle(transform.rotation, _targetIdleRotation);
                _rotationProgress = Mathf.Clamp01(1f - (angleDifference / 180f));
                
                IsRotatingToTarget = true;
            }
            // If we're already moving, handle normal movement rotation
            else if (_isMovingLaterally)
            {
                // Check if we're changing direction significantly while moving
                bool isChangingDirection = false;
                if (_lastMoveDirection.sqrMagnitude > 0.01f && ActiveMoveDirection.sqrMagnitude > 0.01f)
                {
                    float directionChangeAngle = Vector3.Angle(_lastMoveDirection, ActiveMoveDirection);
                    isChangingDirection = directionChangeAngle > 30f;
                }
                
                // Check if we're moving primarily forward
                bool isMovingPrimarilyForward = InputHandler.MovementInput.y > 0.7f && 
                                               Mathf.Abs(InputHandler.MovementInput.x) < 0.3f;

                // Calculate new target rotation
                var targetRotation = Quaternion.LookRotation(
                    isMovingPrimarilyForward ? _lastCameraForward : ActiveMoveDirection
                );

                // If we're still in the idle-to-movement rotation, continue that rotation
                if (_isRotatingFromIdle)
                {
                    // Apply rotation with enhanced speed for the initial rotation
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation,
                        _targetIdleRotation,
                        rotationSpeed * Time.fixedDeltaTime * 100f
                    );
                    
                    // Update rotation progress
                    float angleDifference = Quaternion.Angle(transform.rotation, _targetIdleRotation);
                    _rotationProgress = Mathf.Clamp01(1f - (angleDifference / 180f));
                    
                    // If rotation is nearly complete or target has changed, end the idle rotation state
                    if (_rotationProgress > 0.95f || isChangingDirection)
                    {
                        _isRotatingFromIdle = false;
                        IsRotatingToTarget = false;
                    }
                }
                // Otherwise, handle normal movement rotation
                else
                {
                    // Increase rotation speed when changing direction significantly
                    float rotationFactor = isChangingDirection ? 1.5f : 1.0f;
                    IsRotatingToTarget = isChangingDirection;
                    
                    // Apply rotation
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation,
                        targetRotation,
                        rotationSpeed * rotationFactor * Time.fixedDeltaTime * 100f
                    );
                }
            }
            else if (alignWithCameraWhenIdle && !_isMovingLaterally)
            {
                // Gradually align with camera when idle if the parameter is set
                Quaternion targetRotation = Quaternion.LookRotation(_lastCameraForward);
                transform.rotation = Quaternion.Lerp(
                    transform.rotation,
                    targetRotation,
                    idleAlignmentSpeed * Time.fixedDeltaTime
                );
            }
            else
            {
                // Reset idle rotation state when stopping
                _isRotatingFromIdle = false;
                IsRotatingToTarget = false;
            }
        }
        
        // Store current movement state for next frame
        _wasMovingLastFrame = _isMovingLaterally;
        _lastMoveDirection = ActiveMoveDirection;
    }
    
    public void ApplyGravity(bool isGrounded)
    {
        if (isGrounded)
        {
            // Apply anti-bump when grounded to prevent sticking to slopes
            if (ActiveVerticalVelocity < 0)
            {
                ActiveVerticalVelocity = -antiBumpForce;
            }
        }
        else
        {
            // Calculate new vertical velocity with gravity applied
            ActiveVerticalVelocity += gravity * Time.fixedDeltaTime;
    
            // Limit to terminal velocity
            ActiveVerticalVelocity = Mathf.Max(ActiveVerticalVelocity, maxVerticalVelocity);
        }
    
        // Special case for going from grounded to airborne
        if (_lastGroundedState && !isGrounded && ActiveVerticalVelocity < 0)
        {
            // Apply small upward push to prevent immediately falling
            ActiveVerticalVelocity += antiBumpForce;
        }
    
        _lastGroundedState = isGrounded;
    }

    public void ResetGravity()
    {
        ActiveVerticalVelocity = 0f;
    }
    
    
    #endregion State methods - Movement ---------------------------------------------------------------

    
    #region State methods - Actions ---------------------------------------------------------------

    public void HandleAiming(bool allowAiming)
    {
        // If aiming isn't allowed, disable it regardless of the aim mode setting
        if (!allowAiming)
        {
            if (IsAiming)
            {
                IsAiming = false;
            }
            return;
        }

        // Handle aim mode based on the selected CameraMode
        switch (cameraMode)
        {
            case CameraMode.AimOnly:
                // In AimOnly mode, always enable aiming when it's allowed
                if (!IsAiming)
                {
                    IsAiming = true;
                }
                break;
            
            case CameraMode.ExplorationAndAim:
                // Toggle aim mode based on input (original behavior)
                if (InputHandler.AimInput)
                {
                    if (!IsAiming)
                    {
                        IsAiming = true;
                    }
                }
                else if (IsAiming)
                {
                    IsAiming = false;
                }
                break;
        }

        CheckForAimInteractable();
    }
    
    public void InteractWith()
    {
        if (CurrentInteractable)
        {
            CurrentInteractable.Interact(this);
        }
    }
    

    public void CommandRobot()
    {
        if (!robot || !robot.CanCommend()) return;
        
        
        
        if (InputHandler.CommandRobotInput)
        {
            InputHandler.ConsumeCommandRobotBuffer();
            
            if (CurrentAimedInteractable)
            {
                robot.CommandInteractWith(CurrentAimedInteractable);
                return;
            }
            
            if (robot.CurrentState != RobotState.FollowingPlayer)
            {
                robot.CommandFollowPlayer();
                return;
            }
            
            robot.CommandIdle();
        }
    }
    

    #endregion State methods - Actions ---------------------------------------------------------------

    
    #region State methods - Utility ---------------------------------------------------------------

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
    
    public void SetCharacterCollider(bool state)
    {
        _controller.enabled = state;
    }

    #endregion State methods - Utility ---------------------------------------------------------------

    
    
    #region Debug ---------------------------------------------------------------

    
    private void UpdateDebugInformation()
    {
        if (TestManager.Instance && TestManager.Instance.DebugMode)
        {
            if (debugText)
            {
                string robotInfo = robot ? $"Robot: {robot}, {robot.CurrentState}" : "Robot: null";
            
                debugText.text = $"State: {CurrentState.GetType().Name}\n" +
                                 $"IsGrounded: {IsGrounded}\n" +
                                 $"CanStand: {CanStand}\n" +
                                 $"IsAiming: {IsAiming}\n" +
                                 $"AirTime: {AirTime}\n" +
                                 $"FallTime: {FallTime}\n" +
                                 $"{robotInfo}\n" +
                                 $"MoveDirection: {ActiveMoveDirection}\n" +
                                 $"Interactable: {CurrentInteractable}\n" +
                                 $"AimedInteractable: {CurrentAimedInteractable}\n" +
                                 $"ActiveHorizontalSpeed: {ActiveHorizontalVelocity}\n" +
                                 $"ActiveVerticalVelocity: {ActiveVerticalVelocity}\n" + 
                                 
                                 // Get input
                                 $"\nMovementInput: {InputHandler.MovementInput}\n" +
                                 $"AimInput: {InputHandler.AimInput}\n" +
                                 $"CommandRobotInput: {InputHandler.CommandRobotInput}\n" +
                                 $"InteractInput: {InputHandler.InteractInput}\n"
                                 
                                 
                                 
                                 
                                 ;
            }
    
    
            if (!_lineRenderer.enabled)
            {
                _lineRenderer.enabled = true;
            }
        
        }
        else
        {
            if (debugText)
            {
                debugText.text = "";
            }

            if (_lineRenderer.enabled)
            {
                _lineRenderer.enabled = false;
            }
        
        }
    }

#if UNITY_EDITOR
    
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
        
        // Interactable sphere
        Gizmos.color = Color.blue;
        Vector3 interactableSpherePosition = interactableCheckPosition.position;
        Gizmos.DrawWireSphere(interactableSpherePosition, interactableCheckRadius);
    }
#endif

    
    #endregion Debug ---------------------------------------------------------------

}