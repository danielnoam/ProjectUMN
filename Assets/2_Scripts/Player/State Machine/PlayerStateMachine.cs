
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
    public float walkSpeed = 3f;
    public float runSpeed = 7.5f;
    public bool allowSprint = false;
    [EnableIf("allowSprint")] public float sprintSpeed = 10f; [EndIf]
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
    [Tooltip("Rotation speed when turning on the ground")]
    public float rotationSpeed = 2f;
    [Tooltip("How quickly player rotates to align with camera when idle")]
    public float idleAlignmentSpeed = 2f;
    [Tooltip("Time to complete an idle rotation")]
    public float idleRotationDuration = 0.67f;

    
    [Header("Air")]
    [Tooltip("How quickly the character reaches target speed in air")]
    public float airAcceleration = 5f;
    [Tooltip("Drag force applied to movement in air")]
    public float airDrag = 7f;
    [Tooltip("Rotation speed when turning while aiming")]
    public float aimRotationSpeed = 4f;
    [Tooltip("Initial upward velocity applied when jumping")]
    public float jumpForce = 1.5f;
    [Tooltip("Minimum time falling before impact animations trigger")]
    public float fallThreshold = 0.1f;

    [Header("Gravity")]
    [Tooltip("Downward acceleration applied while in the air")]
    public float gravity = -15f;
    [Tooltip("Maximum downward velocity the character can reach")]
    public float maxVerticalVelocity = -25f;


    [Header("Collision")]
    [Tooltip("Radius of the sphere used to detect environment")]
    [SerializeField] private float environmentCheckRadius = 0.29f;
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
    public float aimRayMaxDistance = 45f;
    [SerializeField] private float lineVisibilityLerpSpeed = 10f;
    [Tooltip("The start position of the aim ray")]
    public Transform aimRayStartPosition;
    

    [Foldout("Events")] 
    public UnityEvent onPlayerDeath = new UnityEvent();
    public UnityEvent onPlayerSpawned = new UnityEvent();
    public UnityEvent onPlayerSpawnedFromCheckpoint = new UnityEvent();
    public UnityEvent onPlayerOpenedMenu = new UnityEvent();
    [EndFoldout]
    
    public InteractorType InteractorType { get; } = InteractorType.Player;
    public CameraMode CurrentCameraMode => cameraMode;
    public float AirTime { get;  set; }
    public float FallTime { get;  set; }
    public float ActiveHorizontalVelocity { get; private set; }
    public float ActiveVerticalVelocity { get; set; }
    public Vector3 ActiveMoveDirection { get; private set; } = Vector3.zero;
    public Vector3 LookAtPosition { get; private set; } = Vector3.zero;
    public bool IsGrounded { get; private set; }
    public bool CanStand { get; private set; }
    public bool IsAiming { get; private set; }
    public PlayerInputHandler InputHandler { get; private set; }
    public Interactable CurrentInteractable { get; private set; }
    public Interactable CurrentAimedInteractable { get; private set; }
    public float RotationMismatch { get; private set; }
    public bool IsRotatingToTarget { get; private set; }
    public RobotCompanion Robot { get; private set; }
    public CameraManager CameraManager { get; private set; }
    public TestManager TestManager { get; private set; }
    
    private TextMeshProUGUI _debugText;
    private CharacterController _controller;
    private LineRenderer _lineRenderer;
    private float _lineRendererDefaultWidth;
    private Vector3 _targetLineEndPosition = Vector3.zero;
    private bool _isLineVisible = false;
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
        
        _lineRendererDefaultWidth = _lineRenderer.startWidth;
        IsAiming = cameraMode == CameraMode.AimOnly;
        SwitchState(GroundedState);
    }

    private void Start()
    {
        if (!Robot) Robot = FindFirstObjectByType<RobotCompanion>();
        CameraManager = CameraManager.Instance;
        CameraManager?.Initialize(this);
    }


    private void OnEnable()
    {
        TestManager = TestManager.Instance;
        if (TestManager)
        {
            TestManager.onTestLoaded.AddListener(OnTestLoaded);
            TestManager.onTestStartLoading.AddListener(OnTestStartLoading);
            TestManager.onTestStartUnloading.AddListener(OnTestStartUnLoading);
            TestManager.onIntroSequenceStart.AddListener(OnIntroSequenceStart);
            TestManager.onIntroSequenceEnd.AddListener(OnIntroSequenceEnd);
            _debugText = TestManager.DebugTextLeft;
        }
    }

    private void OnDisable()
    {
        if (TestManager)
        {
            TestManager.onTestLoaded.RemoveListener(OnTestLoaded);
            TestManager.onTestStartLoading.RemoveListener(OnTestStartLoading);
            TestManager.onTestStartUnloading.RemoveListener(OnTestStartUnLoading);
            TestManager.onIntroSequenceStart.RemoveListener(OnIntroSequenceStart);
            TestManager.onIntroSequenceEnd.RemoveListener(OnIntroSequenceEnd);
            _debugText = null;
        }
    }
    

    private void Update()
    {
        // Intro Sequence
        if (TestManager && TestManager.IsIntroSequenceActive)
        {
            if (TestManager.IntroSequenceState <= 0.1f && CurrentState != InMenuState)
            {
                SwitchState(InMenuState);
                InMenuState.SelectPage(InMenuState.StartPage);
            }
        }


        UpdateFallTime();
        UpdateDebugInformation();
        UpdateLineRenderer();
        CurrentState.UpdateState();
    }

    private void FixedUpdate()
    {
        CurrentState.FixedUpdateState();
        MoveCharacter(); 
        
        
        
        // Ripple effect
        if (CurrentState == FallingState) 
        {
            if (Physics.Raycast(transform.position + groundCheckOffset, Vector3.down, out var hit, 0.5f, environmentLayer))
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
        Robot = TestManager.Robot;
        
    }
    
    private void OnTestStartLoading(SOTest test)
    {
        Robot = null;
        SwitchState(new PlayerTeleportingState(this, TestManager.CurrentSpawnPoint, Quaternion.Euler(0, 0, 0), test.GetTimeToLoad(), TeleportationType.SpawnPoint));
    }
    
    private void OnTestStartUnLoading(SOTest test)
    {
        SwitchState(new PlayerTeleportingState(this, transform.position + Vector3.up, Quaternion.Euler(0, 0, 0), 5f, TeleportationType.EndPoint));
    }
    
    private void OnIntroSequenceStart()
    {
        Robot = null;
        SwitchState(new PlayerTeleportingState(this, Vector3.zero + new Vector3(0, 0.9f, 0), Quaternion.Euler(0, 0, 0), 0.1f, TeleportationType.Checkpoint));
        InputHandler.enabled = false;
    }

    private void OnIntroSequenceEnd()
    {
        InputHandler.enabled = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out LaserGround laserGround))
        {
            if (!laserGround.AffectsPlayer) return;
            
            onPlayerDeath?.Invoke();
            SwitchState(new PlayerTeleportingState(this, TestManager.CurrentCheckpoint, Quaternion.Euler(0, 0, 0), 2f, TeleportationType.Checkpoint));
        }
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


    #endregion State machine ---------------------------------------------------------------
    
    
    #region Calculations ---------------------------------------------------------------

    private Vector3 CalculateMoveDirection()
    {
        if (!CameraManager) return transform.forward;
        
        // Get camera forward and right
        var forward = CameraManager.freeLookCamera.transform.forward;
        var right = CameraManager.freeLookCamera.transform.right;
    
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
        if (!CameraManager) return transform.forward;
        return CameraManager.GetCameraAimDirection(true);
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
        if (!Robot) return;
        
        // Set ray origin 
        Vector3 rayOrigin = aimRayStartPosition.position;

        // Get ray direction from camera
        Vector3 rayDirection = CameraManager.GetCameraAimDirection() + new Vector3(0, 0.2f,0);
        LookAtPosition = rayDirection;

        // Create a layer mask that includes both interactable objects AND environment/walls
        // This ensures we hit walls first if they're in the way
        LayerMask raycastMask = interactableLayer | environmentLayer;

        // Create the actual ray for Physics ray-casting
        Ray aimRay = new Ray(rayOrigin, rayDirection);

        // Perform raycast to see if we hit anything
        if (Physics.Raycast(aimRay, out RaycastHit hitInfo, aimRayMaxDistance, raycastMask))
        {
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
                        CurrentAimedInteractable.MarkForRobotInteraction(Robot);
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
            // No hit, clear current target if we had one
            ClearCurrentAimedInteractable();
        }
    }
    

    private void UpdateLineRenderer()
    {
        if (!Robot || !Robot.IsOn() || !aimRayStartPosition || !_lineRenderer) return;

        // Handle line renderer visibility based on aiming state
        if (IsAiming && !_isLineVisible)
        {
            _lineRenderer.enabled = true;
            _isLineVisible = true;
        }
        else if (!IsAiming && _isLineVisible)
        {
            // Instead of immediately disabling, we'll wait until the line has lerped back
            // to zero length, which happens in the lerping code below
            _isLineVisible = false;
        }
        
        // Set ray origin 
        Vector3 rayOrigin = aimRayStartPosition.position;

        // Get ray direction from camera
        Vector3 rayDirection = CameraManager.GetCameraAimDirection() + new Vector3(0, 0.2f, 0);

        // Set first point of line renderer
        _lineRenderer.SetPosition(0, rayOrigin);

        // Set lerp speed based on whether the line is visible or not
        float lerpSpeed;

        // Calculate the target end position of the line
        if (_isLineVisible)
        {
            // Create the actual ray for Physics ray-casting
            Ray aimRay = new Ray(rayOrigin, rayDirection);

            // Perform raycast to see if we hit anything
            if (Physics.Raycast(aimRay, out RaycastHit hitInfo, aimRayMaxDistance, interactableLayer | environmentLayer))
            {
                // Set target to hit position
                _targetLineEndPosition = hitInfo.point;
            }
            else
            {
                // No hit, set target to max distance
                _targetLineEndPosition = rayOrigin + (rayDirection * aimRayMaxDistance);
            }

            lerpSpeed = lineVisibilityLerpSpeed;
        }
        else
        {
            // When not aiming, target position is the same as ray origin (zero length)
            _targetLineEndPosition = rayOrigin;
            lerpSpeed = lineVisibilityLerpSpeed * 2f;
        }

        // Get current end position
        Vector3 currentEndPosition = _lineRenderer.GetPosition(1);
        
        // Lerp towards target position
        Vector3 newEndPosition = Vector3.Lerp(currentEndPosition, _targetLineEndPosition, lerpSpeed * Time.deltaTime);
        _lineRenderer.SetPosition(1, newEndPosition);
        
        // If line is nearly invisible, and we're not aiming, disable it completely
        if (!_isLineVisible && Vector3.Distance(rayOrigin, newEndPosition) < 0.5f)
        {
            _lineRenderer.enabled = false;
        }
    }
    

    #endregion States methods - Collisions ---------------------------------------------------------------
    

    #region State methods - Movement ---------------------------------------------------------------
    
    
    public void ResetHorizontalVelocity()
    {
        ActiveHorizontalVelocity = 0f;
    }
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
        if (CameraManager)
        {
            Vector3 cameraForward = CameraManager.GetCameraAimDirection(true);
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

    public void ResetVerticalVelocity()
    {
        ActiveVerticalVelocity = 0f;
    }

    public void MoveInDirection(Vector3 direction, Vector3 velocity)
    {
        // Calculate the new velocity based on the direction and speed
        Vector3 newVelocity = direction.normalized * velocity.magnitude;
        
        // Apply the new velocity to the character controller
        _controller.Move(newVelocity * Time.fixedDeltaTime);
        
        // Update the active move direction
        ActiveMoveDirection = newVelocity.normalized;
    }

    public void SetCharacterPosition(Vector3 position, Quaternion rotation)
    {
        _controller.transform.position = position;
        _controller.transform.rotation = rotation;
    }
    
    public void SetCharacterColliderState(bool enabled)
    {
        if (_controller)
        {
            _controller.enabled = enabled;
        }
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
        if (!Robot || !Robot.CanCommend()) return;
        
        
        
        if (InputHandler.CommandRobotInput)
        {
            InputHandler.ConsumeCommandRobotBuffer();
            
            if (CurrentAimedInteractable && !CurrentAimedInteractable.OnlyPlayerCanInteract)
            {
                Robot.CommandInteractWith(CurrentAimedInteractable);
                return;
            }
            
            if (Robot.CurrentState != RobotState.FollowingPlayer)
            {
                Robot.CommandFollowPlayer();
                return;
            }
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
    
    #endregion State methods - Utility ---------------------------------------------------------------
    
    
    #region Debug ---------------------------------------------------------------

    
    private void UpdateDebugInformation()
    {
        if (TestManager && TestManager.DebugMode)
        {
            if (_debugText)
            {
                string robotInfo = Robot ? $"Robot: {Robot}, {Robot.CurrentState}" : "Robot: null";
            
                _debugText.text = $"State: {CurrentState.GetType().Name}\n" +
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
                                 $"InteractInput: {InputHandler.InteractInput}\n" +
                                 
                                 $"\nPlayerVersion: {TestManager.PlayerVersion:F4}\n"
                                 
                                 
                                 
                                 ;
            }
            
            _lineRenderer.startWidth = _lineRendererDefaultWidth * 2;
            _lineRenderer.endWidth = _lineRendererDefaultWidth * 2;

        
        }
        else
        {
            _lineRenderer.startWidth = 0;
            _lineRenderer.endWidth = _lineRendererDefaultWidth;
        
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