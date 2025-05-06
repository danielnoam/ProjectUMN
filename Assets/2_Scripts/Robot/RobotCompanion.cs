
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using VInspector;


public enum RobotState
{
    Idle = 0,
    GoingToTarget = 1,
    FollowingPlayer = 2,
    Sitting = 3,
    Interacting = 4,
    Off = 5,
    Dead = 6,
    LoadingIn = 7,
}

public enum CommandToSend
{
    Nothing,
    Follow,
    Sit,
    Idle,
    Respawn,
}

[SelectionBase]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class RobotCompanion : MonoBehaviour, Iinteractor
{
    [Header("State")]
    [SerializeField, Min(0)] private int fullBattery = 100;
    [SerializeField, Min(0)] private int lowBattery = 20;
    [SerializeField, Min(0)] private int currentBattery = 100;
    [SerializeField] private RobotState currentState;
    

    [Foldout("Horizontal Movement")]
    [Tooltip("Base movement speed of the robot")]
    [SerializeField] private float horizontalMoveSpeed = 25f;
    
    [Tooltip("Minimum distance to maintain from target")]
    [SerializeField] private float minFollowDistance = 2f;
    
    [Tooltip("Maximum distance before reaching max speed")]
    [SerializeField] private float maxFollowDistance = 4f;
    
    [Tooltip("Friction coefficient applied when the robot has no target")]
    [SerializeField] private float friction = 1f;
    
    [Tooltip("How smoothly the robot accelerates and decelerates")]
    [SerializeField] private float followSmoothness = 0.02f;
    
    [Tooltip("Distance at which the robot stops moving towards the target")]
    [SerializeField] private float interactDistance = 0.5f;
    
    [Tooltip("Animation curve controlling how speed changes based on distance")]
    [SerializeField] private AnimationCurve followCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Aim Mode")]
    [Tooltip("Movement speed when player is aiming")]
    [SerializeField] private float aimingMoveSpeed = 50f;

    [Tooltip("Speed multiplier when transitioning to aiming mode")]
    [SerializeField] private float aimingTransitionMultiplier = 1.2f;
    
    [Tooltip("Minimum distance to maintain from target when player is aiming")]
    [SerializeField] private float aimingMinFollowDistance = 0.1f;
    [EndFoldout]
    
    
    
    [Foldout("Vertical Movement")]
    [Tooltip("Vertical movement speed of the robot")]
    [SerializeField] private float verticalMoveSpeed = 5f;
    
    [SerializeField] private float sitDownSpeed = 2f;
    
    [Tooltip("Minimum safe distance the robot must maintain from ground")]
    [SerializeField] private float minEnvironmentClearance = 0.5f;
    
    [Tooltip("Base height maintained above ground")]
    [SerializeField] private float baseHeight = 1.5f;
    
    [Tooltip("Time to wait before adjusting height for small changes")]
    [SerializeField] private float heightAdjustmentDelay = 1f;
    
    [Tooltip("Minimum vertical distance change required before the robot adjusts its height")]
    [SerializeField] private float verticalThreshold = 1f;
    
    [Tooltip("Layers that the robot considers as ground for hover calculations")]
    [SerializeField] private LayerMask environmentLayer;
    
    [Tooltip("Animation curve controlling how vertical speed changes based on height difference")]
    [SerializeField] private AnimationCurve heightAdjustmentCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Hover")]
    [Tooltip("Maximum distance the robot will hover up and down")]
    [SerializeField] private float hoverHeight = 0.3f;
    
    [Tooltip("Speed of the hover movement cycle")]
    [SerializeField] private float hoverSpeed = 1f;
    [EndFoldout]

    
    [Foldout("Rotation stabilization")]
    [Tooltip("How quickly the robot returns to its desired rotation")]
    [SerializeField] private float rotationStability = 4f;
    
    [Tooltip("Maximum angular velocity for rotation")]
    [SerializeField] private float maxAngularVelocity = 3f;
    
    [Tooltip("How quickly the robot rotates when sitting down")]
    [SerializeField] private float sittingRotationStability = 10f;

    [Tooltip("Maximum angular velocity when sitting down")]
    [SerializeField] private float sittingMaxAngularVelocity = 6f;
    
    [Tooltip("Animation curve controlling how rotation speed changes based on angle difference")]
    [SerializeField] private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [EndFoldout]
    
    
    
    [Foldout("Following Player Settings")]
    [Tooltip("Enable automatic teleporting when robot can't reach player")]
    [SerializeField] private bool enableTeleportWhenStuck = true;

    [Tooltip("Time in seconds after which the robot will teleport if it can't reach the player")]
    [SerializeField] private float teleportAfterStuckTime = 5f;

    [Tooltip("Maximum distance to player that will trigger teleport")]
    [SerializeField] private float maxFollowTeleportDistance = 10f;

    [Tooltip("Minimum movement speed to consider robot not stuck")]
    [SerializeField] private float minMovementSpeed = 1f;
    [EndFoldout]
    
    
    
    [Foldout("References")]
    [SerializeField] private GameObject lightDissolver;
    [SerializeField] private SOAudioEvent sfxTurnOn;
    [SerializeField] private SOAudioEvent sfxTurnOff;
    [SerializeField] private SOAudioEvent sfxDeath;
    [SerializeField] private SOAudioEvent sfxReceiveCommand;
    [SerializeField] private SOAudioEvent sfxImpact;
    [EndFoldout]
    
    [Foldout("Events")]
    public UnityEvent onRobotDeath = new UnityEvent();
    public UnityEvent onRobotRespawn = new UnityEvent();
    public UnityEvent onRobotTurnedOn = new UnityEvent();
    public UnityEvent onRobotTurnedOff = new UnityEvent();
    [EndFoldout]
    
    private AudioSource _audioSource;
    private Rigidbody _rigidBody;
    private PlayerStateMachine _player;
    private Transform _playerFollowPosition;
    private Transform _playerAimingFollowPosition;
    private Transform _target;
    private TextMeshProUGUI _debugText;
    private RobotState _previousState;
    private float _lastHeightAdjustmentTime;
    private float _lastTargetHeight;
    private float _currentHoverOffset;
    private float _hoverTime;
    private float _targetSitHeight;
    private Vector3 _leftEarRotation;
    private Vector3 _rightEarRotation;
    private float _sitCommandTime;
    private float _stuckTimer = 0f;
    private bool _isCheckingStuck = false;
    public float MaxAngularVelocity => maxAngularVelocity;
    public bool PlayerIsAiming => _player && _player.IsAiming;
    public RobotState CurrentState => currentState;
    public InteractorType InteractorType { get; private set;} = InteractorType.Robot;
    public Interactable CurrentInteractable { get; private set;}
    public GameObject LightDissolver => lightDissolver;
    public int CurrentButtery => currentBattery;
    public int FullBattery => fullBattery;
    public int LowBattery => lowBattery;

   private void Awake()
   {
       _rigidBody = GetComponent<Rigidbody>();
       _audioSource = GetComponent<AudioSource>();
       currentBattery = fullBattery;
       _previousState = currentState;
   }

   private void Start()
   {
       _player = PlayerStateMachine.Instance;
       _playerFollowPosition = _player.transform.GetChild(1);
       _playerAimingFollowPosition = _player.transform.GetChild(2);
       if (_player)
       {
           _player.onPlayerSpawned.AddListener(OnPlayerSpawned);
           _player.onPlayerSpawnedFromCheckpoint.AddListener(OnPlayerSpawned);
       }
       
       if (TestManager.Instance)
       {
           TestManager.Instance.onTestStartLoading.AddListener(OnTestStartLoading);
           TestManager.Instance.onTestLoaded.AddListener(OnTestLoaded);
           _debugText = TestManager.Instance.DebugTextRight;
       }

   }
   

   private void OnDisable()
   {
       if (_player)
       {
           _player.onPlayerSpawned.RemoveListener(OnPlayerSpawned);
           _player.onPlayerSpawnedFromCheckpoint.RemoveListener(OnPlayerSpawned);
       }
       
       
       if (TestManager.Instance)
       {
           TestManager.Instance.onTestStartLoading.RemoveListener(OnTestStartLoading);
           TestManager.Instance.onTestLoaded.RemoveListener(OnTestLoaded);
           _debugText = null;
       }
       
   }
   
    private void OnTestStartLoading(SOTest test)
    {
        if (IsOn())
        {
            currentState = RobotState.LoadingIn;
        }
    }
   
   private void OnTestLoaded(SOTest test)
   {
       _player = TestManager.Instance.Player;
       if (_player)
       {
           _playerFollowPosition = _player.transform.GetChild(1);
           _playerAimingFollowPosition = _player.transform.GetChild(2);
       }
       
        CommandFollowPlayer();

   }
   
   private void OnPlayerSpawned(ISpawnPoint spawnPoint = null)
   {
       if (!IsOn()) return;
       
       CommandFollowPlayer();
       
        float distanceToPlayer = Vector3.Distance(_player.transform.position, transform.position);
        if (Mathf.Abs(distanceToPlayer) > Mathf.Abs(maxFollowTeleportDistance))
        {
            // Teleport to player position
            Teleport(_player.transform.position, Quaternion.identity);
        }
   }

   private void OnCollisionEnter(Collision other)
   {
       bool allowSfx = CurrentState != RobotState.Dead && 
                       CurrentState != RobotState.Off &&
                       CurrentState != RobotState.Sitting &&
                       (_rigidBody.linearVelocity.x > 4f || _rigidBody.linearVelocity.y > 4f || _rigidBody.linearVelocity.z > 4f);
       if (allowSfx)
       {
           sfxImpact?.Play(_audioSource);
       }


       if (Physics.Raycast(transform.position, Vector3.down, out var hit, 0.5f, environmentLayer))
       {
            
           var ground = hit.transform.GetComponent<GroundRipple>();
        
           if (ground)
           {
               ground.GetHit(hit);
           }
       }
   }
   
   private void OnTriggerEnter(Collider other)
   {
       if (other.TryGetComponent(out LaserGround laserGround))
       {
           if (!laserGround.AffectsRobot) return;

           if (laserGround.DestroyRobot)
           {
               Die();
           }
           else
           {
               onRobotRespawn?.Invoke();
               OnPlayerSpawned();
           }
       }
   }


   private void Update()
   {
       
       if (IsOn())
       {
           CheckBattery();
       }
       
       if (currentState != _previousState)
       {
           if (currentState == RobotState.Sitting)
           {
               _sitCommandTime = Time.time;
           }
           _previousState = currentState;
       }
       
       UpdateDebugInformation();
   }

   private void FixedUpdate()
   {
       ApplyFriction();
    
       if (IsOn())
       {
           HandleRotation();
        
           switch (currentState)
           {
               case RobotState.GoingToTarget:
                   AdjustHeight();
                   MoveToTarget(_target); 
                   break;
               case RobotState.FollowingPlayer:
                   AdjustHeight();
                   FollowPlayer();
                   CheckFollowTeleport();
                   break;
               case RobotState.Idle:
                   AdjustHeight();
                   if (_target) {MoveToPosition(_target); }
                   break;
               case RobotState.Sitting:
                   HandleSitting();
                   break;
               case RobotState.Interacting:

                   break;
               
           }
       }
   }


   
      
   [Button]
   public void TurnOff()
   {
       sfxTurnOff?.Play(_audioSource);
       currentState = RobotState.Off;
       _rigidBody.useGravity = true;
       onRobotTurnedOff?.Invoke();
   }

   [Button]
   public void TurnOn()
   {
       if (currentBattery <= 0) return;
       
       currentState = RobotState.Idle;
       sfxTurnOn?.Play(_audioSource);
       _rigidBody.useGravity = false;
       _rigidBody.isKinematic = false;
       onRobotTurnedOn?.Invoke();
   }

   [Button]
   public void Die()
   {
       if (currentState == RobotState.Dead) return;
         
       currentState = RobotState.Dead;
       sfxDeath?.Play(_audioSource);
       _rigidBody.useGravity = false;
       _rigidBody.isKinematic = true;
       onRobotDeath?.Invoke();
   }

   public void Respawn()
   {
       TurnOn();
       if (_player)
       {
           Teleport(_player.transform.position, Quaternion.identity);
       }

   }


   #region Commends ------------------------------------------------------------------------------



   public void CommandInteractWith(Interactable interactable)
   {
       if (!CanCommend()) return;

       sfxReceiveCommand?.Play(_audioSource);
       CurrentInteractable = interactable;
       _target = interactable.GetInteractPosition(this);
       currentState = RobotState.GoingToTarget;
       _rigidBody.isKinematic = false;
       _rigidBody.useGravity = false;
   }
   
   
   [Button]
   public void CommandFollowPlayer()
   {
       if (!_player || !IsOn() || currentState == RobotState.FollowingPlayer) return;
       
       sfxReceiveCommand?.Play(_audioSource);
       _rigidBody.isKinematic = false;
       _rigidBody.useGravity = false;
       currentState = RobotState.FollowingPlayer;
       
       _stuckTimer = 0f;
       _isCheckingStuck = false;
   }
   
   [Button]
   public void CommandIdle(Transform target = null)
   {
       if (!CanCommend() || currentState == RobotState.Idle) return;
       
       sfxReceiveCommand?.Play(_audioSource);
       _target = target;
       currentState = RobotState.Idle;
       _rigidBody.useGravity = false;
       _rigidBody.isKinematic = false;
   }
   
   [Button]
   public void CommandSitDown()
   {
       if (!CanCommend() || currentState == RobotState.Sitting) return;

       sfxReceiveCommand?.Play(_audioSource);
       currentState = RobotState.Sitting;
       _sitCommandTime = Time.time; 
       _rigidBody.useGravity = false;
       _rigidBody.isKinematic = false;

       // Find the ground position
       if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, Mathf.Infinity, environmentLayer))
       {
           _targetSitHeight = hit.point.y + minEnvironmentClearance/2;
       }
   }

   #endregion Commends ------------------------------------------------------------------------------

   
   #region Interaction ---------------------------------------------------------------
   
   public void InteractWith()
   {
       if (CurrentInteractable)
       {
           CurrentInteractable.Interact(this);
       }
   }

   public void OnInteractionStart(Interactable interactable)
   {
       currentState = RobotState.Interacting;
       _rigidBody.linearVelocity = Vector3.zero;
   }

   public void OnInteractionEnd(Interactable interactable)
   {

       switch (CurrentInteractable.Command)
       {
           case CommandToSend.Idle:
               CommandIdle(CurrentInteractable.RobotInteractPosition);
               break;
           case CommandToSend.Sit:
               CommandSitDown();
               break;
           case  CommandToSend.Follow:
               CommandFollowPlayer();
               break;
           case  CommandToSend.Nothing:
               break;
       }
       CurrentInteractable = null;
   }

   public void CancelInteraction(Interactable interactable)
   {
       interactable.CancelInteraction();
       OnInteractionEnd(interactable);
   }
   
   
   #endregion Interaction ---------------------------------------------------------------


   #region Vertical movement -------------------------------------------------------------------------------
   
   private void HandleSitting()
   {
       // Calculate distance to target height
       float heightDifference = transform.position.y - _targetSitHeight;

       if (heightDifference <= 0.01f)
       {
           // We've reached the sitting position
           transform.position = new Vector3(transform.position.x, _targetSitHeight, transform.position.z);
           _rigidBody.useGravity = false;
           _rigidBody.isKinematic = true; 
           return;
       }

       // Calculate and apply downward velocity
       Vector3 currentVelocity = _rigidBody.linearVelocity;
       float desiredVerticalVelocity = -sitDownSpeed;

       Vector3 newVelocity = new Vector3(
           currentVelocity.x,
           desiredVerticalVelocity,
           currentVelocity.z
       );

       _rigidBody.linearVelocity = newVelocity;
   }
   
   
   
   
   
   
    private void AdjustHeight()
    {
        if (!IsOn()) return;

        // Update hover time
        _hoverTime += Time.fixedDeltaTime * hoverSpeed;

        // Get target height based on current state
        float targetHeight = GetStateBasedTargetHeight();
        (float minHeight, float maxHeight) = GetHeightConstraints();

        // Calculate hover offset (only positive values to hover ABOVE target height)
        float hoverOffset = (Mathf.Sin(_hoverTime) + 1) * hoverHeight * 0.5f;

        // Check if height change requires delay
        float heightDifference = Mathf.Abs(targetHeight - _lastTargetHeight);
        bool needsDelay = heightDifference <= verticalThreshold;

        // Update target if threshold exceeded or delay passed
        if (!needsDelay || Time.time - _lastHeightAdjustmentTime >= heightAdjustmentDelay)
        {
            _lastTargetHeight = targetHeight;
            _lastHeightAdjustmentTime = Time.time;
        }

        // Calculate final target height with hover
        float finalTargetHeight = Mathf.Clamp(_lastTargetHeight + hoverOffset, minHeight, maxHeight);

        // Get current vertical velocity
        float currentVerticalVelocity = _rigidBody.linearVelocity.y;

        // Calculate desired vertical velocity
        float heightError = finalTargetHeight - transform.position.y;
        float normalizedHeightDifference = Mathf.Clamp01(Mathf.Abs(heightError) / verticalThreshold);
        float speedMultiplier = heightAdjustmentCurve.Evaluate(normalizedHeightDifference);
        float desiredVerticalVelocity = heightError * verticalMoveSpeed * speedMultiplier;

        // Calculate damping force
        float dampingForce = -currentVerticalVelocity * followSmoothness;

        // Calculate acceleration needed to reach desired velocity
        float acceleration = (desiredVerticalVelocity - currentVerticalVelocity) * (1f - followSmoothness);

        // Combine forces
        float totalForce = acceleration + dampingForce;

        // Apply forces over time
        float velocityChange = totalForce * Time.fixedDeltaTime;

        // Create new velocity vector, preserving X and Z components
        Vector3 currentVelocity = _rigidBody.linearVelocity;
        Vector3 newVelocity = new Vector3(
            currentVelocity.x,
            currentVerticalVelocity + velocityChange,
            currentVelocity.z
        );

        // Apply final velocity
        _rigidBody.linearVelocity = newVelocity;
    }

    // New method to get the target height based on the robot's current state
    private float GetStateBasedTargetHeight()
    {
        switch (currentState)
        {
            case RobotState.Idle:
                return GetIdleTargetHeight();
                
            case RobotState.FollowingPlayer:
                return GetFollowPlayerTargetHeight();
                
            case RobotState.GoingToTarget:
                return GetGoToTargetHeight();
                
            case RobotState.Interacting:
                return GetInteractingTargetHeight();
                
            default:
                return GetIdleTargetHeight();
        }
    }

    // Height calculation for CommandIdle state
    private float GetIdleTargetHeight()
    {
        // In idle state, just maintain minimum clearance from the ground
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit groundHit, Mathf.Infinity, environmentLayer))
        {
            return groundHit.point.y + baseHeight;
        }
        
        return transform.position.y; // If no ground found, maintain current height
    }

    // Height calculation for FollowingPlayer state
    private float GetFollowPlayerTargetHeight()
    {
        if (!_playerFollowPosition) return GetIdleTargetHeight();
        
        // When following player, match player's height with some adjustments
        float playerHeight = _playerFollowPosition.position.y;
        float terrainHeight = float.MinValue;
        
        // Cast multiple rays to check terrain between robot and player
        for (float t = 0.25f; t <= 0.75f; t += 0.25f)
        {
            Vector3 checkPoint = Vector3.Lerp(transform.position, _playerFollowPosition.position, t);
            if (Physics.Raycast(checkPoint, Vector3.down, out RaycastHit hit, Mathf.Infinity, environmentLayer))
            {
                terrainHeight = Mathf.Max(terrainHeight, hit.point.y);
            }
        }
        
        // If valid terrain found, consider it for height calculation
        if (terrainHeight > float.MinValue)
        {
            // Choose the higher value between player height and terrain height + base clearance
            return Mathf.Max(playerHeight, terrainHeight + baseHeight);
        }
        
        return playerHeight; // Default to player height if no terrain data available
    }

    // Height calculation for GoToInteractable state
    private float GetGoToTargetHeight()
    {
        if (!_target) return GetIdleTargetHeight();
        
        // When going to a target, move toward the target's height
        float targetHeight = _target.position.y;
        
        // Check the ground below target
        if (Physics.Raycast(_target.position, Vector3.down, out RaycastHit targetGroundHit, Mathf.Infinity, environmentLayer))
        {
            // Calculate min safe height above ground at target position
            float minSafeHeight = targetGroundHit.point.y + minEnvironmentClearance;
            
            // If target is below min safe height, use min safe height
            if (targetHeight < minSafeHeight)
            {
                targetHeight = minSafeHeight;
            }
        }
        
        return targetHeight;
    }

    // Height calculation for Interacting state
    private float GetInteractingTargetHeight()
    {
        if (!_target) return GetIdleTargetHeight();
        
        // When interacting, match exactly the target's height
        return _target.position.y;
    }

    // Keep the existing GetHeightConstraints method
    private (float minHeight, float maxHeight) GetHeightConstraints()
    {
        float minHeight = 0;
        float maxHeight = Mathf.Infinity;

        // Check ground clearance
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit groundHit, Mathf.Infinity, environmentLayer))
        {
            minHeight = groundHit.point.y + minEnvironmentClearance;
        }

        // Check ceiling clearance
        if (Physics.Raycast(transform.position, Vector3.up, out RaycastHit ceilingHit, Mathf.Infinity, environmentLayer))
        {
            maxHeight = ceilingHit.point.y - minEnvironmentClearance;
        }

        return (minHeight, maxHeight);
    }




   #endregion Vertical movement -------------------------------------------------------------------------------


   #region Horizontal movement -------------------------------------------------------------------------------
   
   
   private void ApplyFriction()
   {
       if (_rigidBody.isKinematic) return;
       
       // Only apply friction when there's no target
       if (!_target)
       {
           // Get current velocity
           Vector3 currentVelocity = _rigidBody.linearVelocity;
        
           // Calculate friction force for X and Z components
           float frictionX = -currentVelocity.x * friction * Time.fixedDeltaTime;
           float frictionZ = -currentVelocity.z * friction * Time.fixedDeltaTime;
        
           // Create new velocity vector, preserving Y component (handled by hover)
           Vector3 newVelocity = new Vector3(
               currentVelocity.x + frictionX,
               currentVelocity.y,
               currentVelocity.z + frictionZ
           );
        
           // Apply the new velocity
           _rigidBody.linearVelocity = newVelocity;
       }
   }
   
    private void FollowPlayer()
    {
        if (!_playerFollowPosition || !_playerAimingFollowPosition) return;
        
        // Calculate the direction to the target in the horizontal plane only
        bool isPlayerAiming = _player.IsAiming || _player.CurrentState == _player.InMenuState;
        
        Vector3 targetPosition = isPlayerAiming ? new Vector3(_playerAimingFollowPosition.position.x, transform.position.y, _playerAimingFollowPosition.position.z) 
            : new Vector3(_playerFollowPosition.position.x, transform.position.y, _playerFollowPosition.position.z);

        // Calculate distance to target
        Vector3 directionToTarget = (targetPosition - transform.position);
        float distanceToTarget = directionToTarget.magnitude;

        // Get current horizontal velocity
        Vector3 currentHorizontalVelocity = new Vector3(
            _rigidBody.linearVelocity.x,
            0f,
            _rigidBody.linearVelocity.z
        );

        // Calculate desired velocity
        Vector3 desiredVelocity = Vector3.zero;

        // NEW FEATURE: Use different minimum follow distance when aiming
        float effectiveMinFollowDistance = isPlayerAiming ? aimingMinFollowDistance : minFollowDistance;

        if (distanceToTarget > effectiveMinFollowDistance)
        {
            // Normalize the distance between min and max follow distance
            float normalizedDistance = Mathf.Clamp01(
                (distanceToTarget - effectiveMinFollowDistance) / (maxFollowDistance - effectiveMinFollowDistance)
            );
            
            // Apply the curve to get the speed multiplier
            float speedMultiplier = followCurve.Evaluate(normalizedDistance);
            
            // Calculate base desired velocity
            Vector3 moveDirection = directionToTarget.normalized;
            
            // IMPROVEMENT 1: Calculate stopping distance based on current speed
            float currentSpeed = currentHorizontalVelocity.magnitude;
            // Estimate deceleration time (how long it would take to stop at current friction)
            float decelerationTime = currentSpeed / (friction * horizontalMoveSpeed);
            // Estimate stopping distance (simplified)
            float stoppingDistance = currentSpeed * decelerationTime * 0.5f;
            
            // IMPROVEMENT 2: Adjust speed based on stopping distance
            if (distanceToTarget < stoppingDistance * 1.2f) // Add 20% safety margin
            {
                // Reduce speed as we approach stopping distance
                float brakeMultiplier = Mathf.Clamp01(distanceToTarget / (stoppingDistance * 1.2f));
                speedMultiplier *= brakeMultiplier;
            }
            
            // IMPROVEMENT 3: Additional speed reduction when very close
            if (distanceToTarget < effectiveMinFollowDistance * 2f)
            {
                speedMultiplier *= distanceToTarget / (effectiveMinFollowDistance * 2f);
            }
            
            // NEW FEATURE: Select different move speeds based on aiming state
            float selectedMoveSpeed = isPlayerAiming ? aimingMoveSpeed : horizontalMoveSpeed;
            
            // NEW FEATURE: Apply boost when transitioning to aiming mode
            if (isPlayerAiming && currentSpeed < selectedMoveSpeed * 0.9f)
            {
                speedMultiplier *= aimingTransitionMultiplier;
            }
            
            desiredVelocity = moveDirection * (selectedMoveSpeed * speedMultiplier);
        }

        // IMPROVEMENT 4: Check if we're moving away from target instead of towards it
        // Calculate damping force - reduced smoothness for quicker response in aiming mode
        float effectiveSmoothness = isPlayerAiming ? followSmoothness * 0.5f : followSmoothness;
        Vector3 dampingForce = -currentHorizontalVelocity * effectiveSmoothness;

        float dotProduct = Vector3.Dot(currentHorizontalVelocity.normalized, directionToTarget.normalized);
        if (dotProduct < -0.2f && distanceToTarget < maxFollowDistance)
        {
            // We're moving away from target - apply stronger deceleration
            // Increase deceleration even more when aiming
            float decelMultiplier = isPlayerAiming ? 4f : 3f;
            dampingForce = -currentHorizontalVelocity * (effectiveSmoothness * decelMultiplier);
            _rigidBody.AddForce(dampingForce, ForceMode.Acceleration);
        }

        // Calculate acceleration needed to reach desired velocity
        Vector3 acceleration = (desiredVelocity - currentHorizontalVelocity) * (1f - effectiveSmoothness);

        // Combine forces
        Vector3 totalForce = acceleration + dampingForce;

        // Apply forces over time
        Vector3 velocityChange = totalForce * Time.fixedDeltaTime;

        // Create new velocity vector, preserving Y component (handled by hover)
        Vector3 newVelocity = new Vector3(
            currentHorizontalVelocity.x + velocityChange.x,
            _rigidBody.linearVelocity.y,
            currentHorizontalVelocity.z + velocityChange.z
        );

        // IMPROVEMENT 5: Limit maximum horizontal speed based on distance and aim state
        float maxSpeed = isPlayerAiming ? aimingMoveSpeed : horizontalMoveSpeed;
        if (distanceToTarget < maxFollowDistance)
        {
            // Gradually reduce max speed as we get closer, but keep higher minimum speed when aiming
            float minSpeedRatio = isPlayerAiming ? 0.4f : 0.3f;
            maxSpeed = Mathf.Lerp(maxSpeed * minSpeedRatio, maxSpeed, 
                distanceToTarget / maxFollowDistance);
        }

        // Clamp horizontal speed
        float horizontalSpeed = new Vector3(newVelocity.x, 0, newVelocity.z).magnitude;
        if (horizontalSpeed > maxSpeed)
        {
            float scale = maxSpeed / horizontalSpeed;
            newVelocity.x *= scale;
            newVelocity.z *= scale;
        }

        // Apply final velocity
        _rigidBody.linearVelocity = newVelocity;
    }
   
   
   
   
   private void MoveToTarget(Transform target)
   {
       // Calculate distance to target
       Vector3 targetPosition = new Vector3(target.position.x, target.position.y, target.position.z);
       float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
    
       // If we're close enough to the target
       if (distanceToTarget <= interactDistance)
       {
           // If we have an interactable object, interact with it
           if (CurrentInteractable)
           {
               // Perform the interaction
               InteractWith();
           }
       }
       else
       {
           // If we're not close enough, move towards the target
           Vector3 directionToTarget = (targetPosition - transform.position).normalized;
           Vector3 moveDirection = new Vector3(directionToTarget.x, 0f, directionToTarget.z);
           
           Vector3 newVelocity = new Vector3(
               moveDirection.x * (horizontalMoveSpeed * 13 * Time.fixedDeltaTime),
               _rigidBody.linearVelocity.y,
               moveDirection.z * (horizontalMoveSpeed * 13 * Time.fixedDeltaTime)
           );
           
           // Apply the movement
           _rigidBody.linearVelocity = newVelocity;
       }
   }
   
   private void MoveToPosition(Transform target)
   {
       // Calculate distance to target
       Vector3 targetPosition = new Vector3(target.position.x, target.position.y, target.position.z);
       float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
    

       // If we're close enough to the target
       if (distanceToTarget >= interactDistance/2)
       {
           // If we're not close enough, move towards the target
           Vector3 directionToTarget = (targetPosition - transform.position).normalized;
           Vector3 moveDirection = new Vector3(directionToTarget.x, 0f, directionToTarget.z);
           
           Vector3 newVelocity = new Vector3(
               moveDirection.x * (horizontalMoveSpeed * Time.fixedDeltaTime),
               _rigidBody.linearVelocity.y,
               moveDirection.z * (horizontalMoveSpeed * Time.fixedDeltaTime)
           );
           
           // Apply the movement
           _rigidBody.linearVelocity = newVelocity;
       }
       else
       {
            _rigidBody.linearVelocity = new Vector3(0, _rigidBody.linearVelocity.y, 0);
       }
   }

   #endregion Horizontal movement -------------------------------------------------------------------------------
   
   
   #region Rotation -------------------------------------------------------------------
   
   
    private void HandleRotation()
    {
        if (!IsOn()) return;

        Quaternion targetRotation;
        
        // Determine target rotation based on the current state
        if (currentState == RobotState.FollowingPlayer)
        {
            Vector3 directionToTarget = _player.IsAiming ? (_player.LookAtPosition + new Vector3(0, -0.1f, 0)).normalized 
                : (_player.transform.position + new Vector3(0,0.5f, 0) - transform.position).normalized;
            
            targetRotation = Quaternion.LookRotation(directionToTarget);
        }
        else if (currentState == RobotState.GoingToTarget && _target)
        {
            Vector3 directionToTarget = (_target.position - transform.position).normalized;
            targetRotation = Quaternion.LookRotation(directionToTarget);
        }
        else if (currentState == RobotState.Idle && _player)
        {
            Vector3 directionToTarget = ((_player.transform.position + new Vector3(0,0.5f, 0)) - transform.position).normalized;
            targetRotation = Quaternion.LookRotation(directionToTarget);
        }
        else if (currentState == RobotState.Interacting && _target)
        {
            Vector3 directionToTarget = (_target.position - transform.position).normalized;
            targetRotation = Quaternion.LookRotation(directionToTarget);
        }
        else
        {
            // Default to facing forward
            targetRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        }

        // Get current angular velocity
        Vector3 currentAngularVelocity = _rigidBody.angularVelocity;

        // Calculate the angle difference
        float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);
        
        // Normalize the difference to 0-1 range for the curve
        float normalizedDifference = Mathf.Clamp01(angleDifference / 180f);
        
        // Apply the curve to get the stabilization strength
        float curveMultiplier = rotationCurve.Evaluate(normalizedDifference);

        // Determine if we should use sitting rotation parameters
        float effectiveRotationStability = rotationStability;
        float effectiveMaxAngularVelocity = maxAngularVelocity;
        
        if (currentState == RobotState.Sitting)
        {
            // Use enhanced rotation parameters for sitting
            effectiveRotationStability = sittingRotationStability;
            effectiveMaxAngularVelocity = sittingMaxAngularVelocity;
            
            // Apply even faster rotation during the first second of sitting
            float sitTransitionTime = Time.time - _sitCommandTime;
            if (sitTransitionTime < 1.0f)
            {
                // Boost rotation speed during initial sitting transition
                effectiveRotationStability *= 1.5f;
            }
        }

        // Calculate stabilization torque
        Vector3 stabilizationTorque = Vector3.zero;
        
        // Apply torque to counter current angular velocity
        stabilizationTorque -= currentAngularVelocity * effectiveRotationStability;
        
        // Add torque towards target rotation
        (targetRotation * Quaternion.Inverse(transform.rotation)).ToAngleAxis(out var rotationAngle, out var rotationAxis);
        
        if (!float.IsNaN(rotationAngle))
        {
            stabilizationTorque += rotationAxis.normalized * (rotationAngle * effectiveRotationStability * curveMultiplier);
        }

        // Clamp the maximum angular velocity
        _rigidBody.maxAngularVelocity = effectiveMaxAngularVelocity;
        
        // Apply the final torque
        _rigidBody.AddTorque(stabilizationTorque, ForceMode.Acceleration);
    }
    
   #endregion Actions -------------------------------------------------------------------


   #region Utility ------------------------------------------------------------------------
   
   private void Teleport(Vector3 position, Quaternion rotation)
   {
       Vector3 offset = new Vector3(1,1,1);
       _rigidBody.rotation = rotation;
       _rigidBody.position = position + offset;
   }
   
   private void CheckBattery()
   {
       if (currentBattery <= 0)
       {
           TurnOff();
           currentState = RobotState.Dead;
       }
   }

   
   
    private void CheckFollowTeleport()
    {
        // Only run checks if feature is enabled and we're following the player
        if (!enableTeleportWhenStuck || !_player || currentState != RobotState.FollowingPlayer)
        {
            _stuckTimer = 0f;
            _isCheckingStuck = false;
            return;
        }

        // Get current distance to player follow position
        float distanceToPlayer = Vector3.Distance(transform.position, _player.transform.position);
        
        // Get current movement speed
        float currentSpeed = _rigidBody.linearVelocity.magnitude;
        
        // Check if we're too far away and moving too slowly
        bool isTooFar = distanceToPlayer > maxFollowTeleportDistance;
        bool isMovingTooSlow = currentSpeed < minMovementSpeed;
        
        // Start checking for stuck condition if both conditions are true
        if (isTooFar && isMovingTooSlow)
        {
            if (!_isCheckingStuck)
            {
                _isCheckingStuck = true;
                _stuckTimer = 0f;
            }
            
            // Increment stuck timer
            _stuckTimer += Time.deltaTime;
            
            // If we've been stuck for long enough, teleport
            if (_stuckTimer >= teleportAfterStuckTime)
            {
                // Calculate teleport position slightly behind player
                Vector3 playerForward = _player.transform.forward;
                Vector3 teleportOffset = -playerForward * 2f + Vector3.up * 1f; // Position behind player
                
                // Teleport the robot
                Teleport(_player.transform.position + teleportOffset, Quaternion.LookRotation(playerForward));
                
                // Reset stuck timer and flag
                _stuckTimer = 0f;
                _isCheckingStuck = false;
                
                // Optional: Play teleport sound effect
                sfxReceiveCommand?.Play(_audioSource);
            }
        }
        else
        {
            // If either condition is false, we're not stuck
            _stuckTimer = 0f;
            _isCheckingStuck = false;
        }
    }
   
   
   public void SetStuckTeleportState(bool state)
   {
       enableTeleportWhenStuck = state;
   }

   public bool IsOn()
   {
       return  currentState != RobotState.Dead && currentState != RobotState.Off && currentBattery > 0;
   }

   public bool CanCommend()
   {
       return IsOn();
   }

   public Vector3 GetLookDirection()
   {
       // Get the look direction
         Vector3 lookDirection = transform.forward;
         lookDirection.Normalize();
         return lookDirection;
   }
   
   
    private void UpdateDebugInformation()
    {
        if (TestManager.Instance && TestManager.Instance.DebugMode)
        {
            if (_debugText)
            {
            
                string distanceToPlayer = _player ? $"Distance to Player: {Vector3.Distance(transform.position, _player.transform.position):F2} / {maxFollowTeleportDistance}\n": "No Player";
                
                _debugText.text = $"State: {CurrentState}\n" +
                                  $"Battery: {currentBattery}\n" +
                                  $"Velocity: {_rigidBody.linearVelocity}\n" +
                                  $"Target: {_target}\n" +
                                  $"Stuck Timer: {_stuckTimer:F2} / {teleportAfterStuckTime}\n" +
                                  $"Distance to Player: {distanceToPlayer}\n"
                          ;
            }
        }

    }
    

   

   #endregion Utility ------------------------------------------------------------------------
   
   
   #region Gizmos ------------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        if (!IsOn()) return;

        // Draw ground and ceiling raycasts
        Gizmos.color = Color.blue;
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit groundHit, Mathf.Infinity, environmentLayer))
        {
            Gizmos.DrawLine(transform.position, groundHit.point);
            Gizmos.DrawWireSphere(groundHit.point, 0.1f);
        }
        if (Physics.Raycast(transform.position, Vector3.up, out RaycastHit ceilingHit, Mathf.Infinity, environmentLayer))
        {
            Gizmos.DrawLine(transform.position, ceilingHit.point);
            Gizmos.DrawWireSphere(ceilingHit.point, 0.1f);
        }

        // Draw state-based target height
        Gizmos.color = Color.yellow;
        float stateBasedHeight = GetStateBasedTargetHeight();
        Vector3 stateBasedPosition = new Vector3(transform.position.x, stateBasedHeight, transform.position.z);
        Gizmos.DrawWireSphere(stateBasedPosition, 0.15f);
        
        // Draw hover range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(stateBasedPosition + Vector3.up * hoverHeight, 0.1f);

        // Draw visual indicators for specific states
        switch (currentState)
        {
            case RobotState.FollowingPlayer:
                if (_playerFollowPosition)
                {
                    // Draw line to player
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(transform.position, _playerFollowPosition.position);
                    
                    // Draw normal min follow distance
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireSphere(transform.position, minFollowDistance);
                    
                    // Draw aiming min follow distance if applicable
                    if (_player && _player.IsAiming)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireSphere(transform.position, aimingMinFollowDistance);
                    }
                    
                    // Draw max follow distance
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(transform.position, maxFollowDistance);
                    
                    // Draw terrain check points
                    Gizmos.color = Color.magenta;
                    for (float t = 0.25f; t <= 0.75f; t += 0.25f)
                    {
                        Vector3 checkPoint = Vector3.Lerp(transform.position, _playerFollowPosition.position, t);
                        Gizmos.DrawLine(checkPoint, checkPoint + Vector3.down * 10f);
                    }
                }
                break;
                
            case RobotState.GoingToTarget:
                if (_target)
                {
                    // Draw line to target
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(transform.position, _target.position);
                }
                break;
        }
    }

   
   #endregion Gizmos ------------------------------------------------------------------------
}