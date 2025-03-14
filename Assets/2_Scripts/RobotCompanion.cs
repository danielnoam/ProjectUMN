

using System;
using UnityEngine;
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
}

[SelectionBase]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
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
    
    [Tooltip("Distance at which the robot stops moving towards the target")]
    [SerializeField] private float interactDistance = 0.5f;
    
    [Tooltip("Minimum distance to maintain from target")]
    [SerializeField] private float minFollowDistance = 2f;
    
    [Tooltip("Maximum distance before reaching max speed")]
    [SerializeField] private float maxFollowDistance = 4f;
    
    [Tooltip("Friction coefficient applied when the robot has no target")]
    [SerializeField] private float friction = 1f;
    
    [Tooltip("How smoothly the robot accelerates and decelerates")]
    [SerializeField] private float followSmoothness = 0.02f;
    
    [Tooltip("Animation curve controlling how speed changes based on distance")]
    [SerializeField] private AnimationCurve followCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [EndFoldout]
    
    
    
    [Foldout("Vertical Movement")]
    
    [Tooltip("Vertical movement speed of the robot")]
    [SerializeField] private float verticalMoveSpeed = 5f;
    
    [SerializeField] private float sitDownSpeed = 3f; // Speed at which the robot sits down
    
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
    
    [Header("Hover effect")]
    [Tooltip("Maximum distance the robot will hover up and down")]
    [SerializeField] private float hoverHeight = 0.3f;
    
    [Tooltip("Speed of the hover movement cycle")]
    [SerializeField] private float hoverSpeed = 1f;
    [EndFoldout]

    
    [Foldout("Rotation")]
    [Tooltip("How quickly the robot returns to its desired rotation")]
    [SerializeField] private float rotationStability = 4f;
    
    [Tooltip("Maximum angular velocity for rotation")]
    [SerializeField] private float maxAngularVelocity = 3f;
    
    [Tooltip("Animation curve controlling how rotation speed changes based on angle difference")]
    [SerializeField] private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [EndFoldout]


    [Foldout("Ears")] 
    [SerializeField] private bool rotateEars = true;
    [Tooltip("Transform reference for the left ear/antenna")]
    [SerializeField] private Transform leftEarPivot;

    [Tooltip("Transform reference for the right ear/antenna")]
    [SerializeField] private Transform rightEarPivot;

    [Tooltip("Maximum bend angle of the ears")]
    [SerializeField] private float maxEarBend = 45f;

    [Tooltip("How smoothly the ears rotate")]
    [SerializeField] private float earRotationSmoothness = 0.2f;

    [Tooltip("Speed at which ears reach their maximum bend")]
    [SerializeField] private float maxSpeedForEarRotation = 4f;
    [EndFoldout]
    
    
    [Foldout("Eye")] 
    [SerializeField] private GameObject eye;
    [SerializeField] private Light eyeLight;
    [EndFoldout]
    
    [Foldout("SFX")] 
    [SerializeField] private SOAudioEvent sfxTurnOn;
    [SerializeField] private SOAudioEvent sfxTurnOff;
    [SerializeField] private SOAudioEvent sfxReceiveCommand;
    [SerializeField] private SOAudioEvent sfxImpact;
    [EndFoldout]

    [Header("References")]
    [SerializeField] private Rigidbody rigidBody;
    [SerializeField] private SphereCollider sphereCollider;
    [SerializeField] private AudioSource audioSource;
    
    
    private PlayerStateMachine _player;
    private Transform _playerFollowPosition;
    private Transform _target;
    private float _lastHeightAdjustmentTime;
    private float _lastTargetHeight;
    private float _currentHoverOffset;
    private float _hoverTime;
    private float _targetSitHeight;
    private Vector3 _leftEarRotation;
    private Vector3 _rightEarRotation;
    private Quaternion _leftEarBaseRotation;
    private Quaternion _rightEarBaseRotation;
    private Color _defaultEyeLightColor;
    private float _fullEyeLightIntensity;
    
    public RobotState CurrentState => currentState;
    public InteractorType InteractorType { get; private set;} = InteractorType.Robot;
    public Interactable CurrentInteractable { get; private set;}

   private void Awake()
   {
       if (!rigidBody) rigidBody = GetComponent<Rigidbody>();
       if (!sphereCollider) sphereCollider = GetComponent<SphereCollider>();
       if (!audioSource) audioSource = GetComponent<AudioSource>();
       if (leftEarPivot) _leftEarBaseRotation = leftEarPivot.localRotation;
       if (rightEarPivot) _rightEarBaseRotation = rightEarPivot.localRotation;
       if (eyeLight)
       {
           _defaultEyeLightColor = eyeLight.color;
           _fullEyeLightIntensity = eyeLight.intensity;
       }
       currentBattery = fullBattery;
   }

   private void OnEnable()
   {
       _player = GameObject.Find("Player").GetComponent<PlayerStateMachine>();
       _playerFollowPosition = _player.transform.GetChild(2);

       if (_player)
       {
           _player.onPlayerSpawned.AddListener(OnPlayerSpawned);
       }
       if (TestManager.Instance)
       {
           TestManager.Instance.onTestLoaded.AddListener(OnTestLoaded);
       }
   }
   

   private void OnDisable()
   {
       if (_player)
       {
           _player.onPlayerSpawned.RemoveListener(OnPlayerSpawned);
       }
       
       if (TestManager.Instance)
       {
           TestManager.Instance.onTestLoaded.RemoveListener(OnTestLoaded);
       }
       
   }

   private void OnCollisionEnter(Collision other)
   {
       if (CurrentState != RobotState.Dead && CurrentState != RobotState.Off && CurrentState != RobotState.Sitting)
       {
           sfxImpact?.Play(audioSource);
       }
       
       
       RaycastHit hit;
       if (Physics.Raycast(transform.position, Vector3.down, out hit, 0.5f, environmentLayer))
       {
            
           var ground = hit.transform.GetComponent<GroundRipple>();
        
           if (ground)
           {
               ground.GetHit(hit);
           }
       }
   }


   private void Update()
   {
       UpdateEye();
       UpdateEarRotation();
       
       if (IsOn())
       {
           CheckBattery();
       }
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
                   break;
               case RobotState.Idle:
                   AdjustHeight();
                   break;
               case RobotState.Sitting:
                   HandleSitting();
                   break;
               case RobotState.Interacting:

                   break;
           }
       }
   }

   private void OnTestLoaded(SOTest test)
   {
       _player = TestManager.Instance.Player;
       _playerFollowPosition = _player.transform.GetChild(2);
       
       if (!test.HasRobot()) { Teleport(test.GetRobotSpawnPoint(), Quaternion.identity); }
   }
   
   private void OnPlayerSpawned()
   {
       if (currentState != RobotState.Off) Teleport(_player.transform.position, Quaternion.identity);
   }
   
      
   [Button]
   public void TurnOff()
   {
       sfxTurnOff?.Play(audioSource);
       currentState = RobotState.Off;
       rigidBody.useGravity = true;
       eye.gameObject.SetActive(false);
   }

   [Button]
   public void TurnOn()
   {
       if (currentBattery <= 0) return;
       
       sfxTurnOn?.Play(audioSource);
       currentState = RobotState.Idle;
       rigidBody.useGravity = false;
       rigidBody.isKinematic = false;
       eye.gameObject.SetActive(true);
   }


   #region Commends ------------------------------------------------------------------------------



   public void CommandInteractWith(Interactable interactable)
   {
       if (!CanCommend()) return;

       sfxReceiveCommand?.Play(audioSource);
       CurrentInteractable = interactable;
       _target = interactable.GetInteractPosition(this);
       currentState = RobotState.GoingToTarget;
   }
   
   
   [Button]
   public void CommandFollowPlayer()
   {
       if (!_player || !IsOn()) return;
       
       sfxReceiveCommand?.Play(audioSource);
       rigidBody.isKinematic = false;
       rigidBody.useGravity = false;
       currentState = RobotState.FollowingPlayer;
   }
   
   [Button]
   public void CommandIdle()
   {
       if (!CanCommend()) return;
       
       sfxReceiveCommand?.Play(audioSource);
       _target = null;
       currentState = RobotState.Idle;
       rigidBody.useGravity = false;
       rigidBody.isKinematic = false;
   }
   
   [Button]
   public void CommandSitDown()
   {
       if (!CanCommend()) return;

       sfxReceiveCommand?.Play(audioSource);
       currentState = RobotState.Sitting;
       rigidBody.useGravity = false;
       rigidBody.isKinematic = false;

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
       rigidBody.linearVelocity = Vector3.zero;
   }

   public void OnInteractionEnd(Interactable interactable)
   {

       switch (CurrentInteractable.Command)
       {
           case CommandToSend.Idle:
               CommandIdle();
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
           rigidBody.useGravity = false;
           rigidBody.isKinematic = true; 
           return;
       }

       // Calculate and apply downward velocity
       Vector3 currentVelocity = rigidBody.linearVelocity;
       float desiredVerticalVelocity = -sitDownSpeed;

       Vector3 newVelocity = new Vector3(
           currentVelocity.x,
           desiredVerticalVelocity,
           currentVelocity.z
       );

       rigidBody.linearVelocity = newVelocity;
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
    float currentVerticalVelocity = rigidBody.linearVelocity.y;

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
    Vector3 currentVelocity = rigidBody.linearVelocity;
    Vector3 newVelocity = new Vector3(
        currentVelocity.x,
        currentVerticalVelocity + velocityChange,
        currentVelocity.z
    );

    // Apply final velocity
    rigidBody.linearVelocity = newVelocity;
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
    
    // Check the terrain between robot and player
    Vector3 toPlayer = _playerFollowPosition.position - transform.position;
    Vector3 midPoint = transform.position + toPlayer * 0.5f;
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

// Update the OnDrawGizmos method to show the different height targets based on state
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
                
                // Draw terrain check points
                Gizmos.color = Color.magenta;
                Vector3 toPlayer = _playerFollowPosition.position - transform.position;
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

   

   #endregion Vertical movement -------------------------------------------------------------------------------


   #region Horizontal movement -------------------------------------------------------------------------------
   
   
   private void ApplyFriction()
   {
       if (rigidBody.isKinematic) return;
       
       // Only apply friction when there's no target
       if (!_target)
       {
           // Get current velocity
           Vector3 currentVelocity = rigidBody.linearVelocity;
        
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
           rigidBody.linearVelocity = newVelocity;
       }
   }
   
   private void FollowPlayer()
   {
       if (!_playerFollowPosition) return;
       
        // Calculate the direction to the target in the horizontal plane only
        Vector3 targetPosition = new Vector3(_playerFollowPosition.position.x, transform.position.y, _playerFollowPosition.position.z);
        Vector3 directionToTarget = (targetPosition - transform.position);

        // Calculate distance to target
        float distanceToTarget = directionToTarget.magnitude;

        // Get current horizontal velocity
        Vector3 currentHorizontalVelocity = new Vector3(
            rigidBody.linearVelocity.x,
            0f,
            rigidBody.linearVelocity.z
        );

        // Calculate desired velocity
        Vector3 desiredVelocity = Vector3.zero;

        if (distanceToTarget > minFollowDistance)
        {
            // Normalize the distance between min and max follow distance
            float normalizedDistance = Mathf.Clamp01(
                (distanceToTarget - minFollowDistance) / (maxFollowDistance - minFollowDistance)
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
            if (distanceToTarget < minFollowDistance * 2f)
            {
                speedMultiplier *= distanceToTarget / (minFollowDistance * 2f);
            }
            
            desiredVelocity = moveDirection * (horizontalMoveSpeed * speedMultiplier);
        }

        // IMPROVEMENT 4: Check if we're moving away from target instead of towards it
        // Calculate damping force
        Vector3 dampingForce = -currentHorizontalVelocity * followSmoothness;

        float dotProduct = Vector3.Dot(currentHorizontalVelocity.normalized, directionToTarget.normalized);
        if (dotProduct < -0.2f && distanceToTarget < maxFollowDistance)
        {
            // We're moving away from target - apply stronger deceleration
            dampingForce = -currentHorizontalVelocity * (followSmoothness * 3f);
            rigidBody.AddForce(dampingForce, ForceMode.Acceleration);
        }



        // Calculate acceleration needed to reach desired velocity
        Vector3 acceleration = (desiredVelocity - currentHorizontalVelocity) * (1f - followSmoothness);

        // Combine forces
        Vector3 totalForce = acceleration + dampingForce;

        // Apply forces over time
        Vector3 velocityChange = totalForce * Time.fixedDeltaTime;

        // Create new velocity vector, preserving Y component (handled by hover)
        Vector3 newVelocity = new Vector3(
            currentHorizontalVelocity.x + velocityChange.x,
            rigidBody.linearVelocity.y,
            currentHorizontalVelocity.z + velocityChange.z
        );

        // IMPROVEMENT 5: Limit maximum horizontal speed based on distance
        float maxSpeed = horizontalMoveSpeed;
        if (distanceToTarget < maxFollowDistance)
        {
            // Gradually reduce max speed as we get closer
            maxSpeed = Mathf.Lerp(horizontalMoveSpeed * 0.3f, horizontalMoveSpeed, 
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
        rigidBody.linearVelocity = newVelocity;
   }

   #endregion Horizontal movement -------------------------------------------------------------------------------
   
   
   #region Rotation -------------------------------------------------------------------
   
   
   private void HandleRotation()
   {
       if (!IsOn()) return;

       Quaternion targetRotation;
    
       if (currentState == RobotState.FollowingPlayer)
       {
           Vector3 directionToTarget = ((_player.transform.position + new Vector3(0,0.5f, 0)) - transform.position).normalized;
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
       Vector3 currentAngularVelocity = rigidBody.angularVelocity;

       // Calculate the angle difference
       float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);
    
       // Normalize the difference to 0-1 range for the curve
       float normalizedDifference = Mathf.Clamp01(angleDifference / 180f);
    
       // Apply the curve to get the stabilization strength
       float curveMultiplier = rotationCurve.Evaluate(normalizedDifference);

       // Calculate stabilization torque
       Vector3 stabilizationTorque = Vector3.zero;
    
       // Apply torque to counter current angular velocity
       stabilizationTorque -= currentAngularVelocity * rotationStability;
    
       // Add torque towards target rotation
       Vector3 rotationAxis;
       float rotationAngle;
       (targetRotation * Quaternion.Inverse(transform.rotation)).ToAngleAxis(out rotationAngle, out rotationAxis);
    
       if (!float.IsNaN(rotationAngle))
       {
           stabilizationTorque += rotationAxis.normalized * (rotationAngle * rotationStability * curveMultiplier);
       }

       // Clamp the maximum angular velocity
       rigidBody.maxAngularVelocity = maxAngularVelocity;
    
       // Apply the final torque
       rigidBody.AddTorque(stabilizationTorque, ForceMode.Acceleration);
   }
   

    
   #endregion Actions -------------------------------------------------------------------


   #region Utility ------------------------------------------------------------------------
   
   
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
               rigidBody.linearVelocity.y,
               moveDirection.z * (horizontalMoveSpeed * 13 * Time.fixedDeltaTime)
           );
           
           // Apply the movement
           rigidBody.linearVelocity = newVelocity;
       }
   }
   
   private void Teleport(Vector3 position, Quaternion rotation)
   {
       Vector3 offset = new Vector3(-1, 0f, -1);
       sphereCollider.enabled = false;
       transform.position = position + offset;
       transform.rotation = rotation;
       sphereCollider.enabled = true;
   }
   
   private void UpdateEarRotation() 
   {
        if (!leftEarPivot || !rightEarPivot) return;
        if (!rotateEars) return;

        // Get the movement direction in local space
        Vector3 localVelocity = transform.InverseTransformDirection(rigidBody.linearVelocity);
        Vector3 localAngularVelocity = transform.InverseTransformDirection(rigidBody.angularVelocity);

        float movementSpeed = rigidBody.linearVelocity.magnitude;
        float rotationSpeed = rigidBody.angularVelocity.magnitude;

        // Calculate bend strength for both movement and rotation
        float movementBendStrength = Mathf.Clamp01(movementSpeed / maxSpeedForEarRotation);
        float rotationBendStrength = Mathf.Clamp01(rotationSpeed / maxAngularVelocity);

        // Calculate movement-based rotation
        Vector3 movementRotation = new Vector3(
            -localVelocity.z, // Forward/back movement causes up/down rotation
            -localVelocity.x, // Left/right movement causes side rotation
            0
        ).normalized * (maxEarBend * movementBendStrength);

        // Calculate rotation-based ear bend
        // For the left ear
        Vector3 leftRotationBend = new Vector3(
            0,
            localAngularVelocity.y, // Yaw rotation causes side bend
            0
        ) * (maxEarBend * rotationBendStrength);

        // For the right ear (opposite of left ear for rotation)
        Vector3 rightRotationBend = new Vector3(
            0,
            localAngularVelocity.y, // Opposite direction for right ear
            0
        ) * (maxEarBend * rotationBendStrength);

        // Combine movement and rotation effects
        Quaternion leftTargetRotation = Quaternion.Euler(movementRotation + leftRotationBend);
        Quaternion rightTargetRotation = Quaternion.Euler(movementRotation + rightRotationBend);

        // Apply rotation with smoothing
        leftEarPivot.localRotation = Quaternion.Slerp(
            leftEarPivot.localRotation,
            leftTargetRotation * _leftEarBaseRotation,
            1f - Mathf.Pow(earRotationSmoothness, Time.deltaTime)
        );

        rightEarPivot.localRotation = Quaternion.Slerp(
            rightEarPivot.localRotation,
            rightTargetRotation * _rightEarBaseRotation,
            1f - Mathf.Pow(earRotationSmoothness, Time.deltaTime)
        );
   }
   
   private void CheckBattery()
   {
       if (currentBattery <= 0)
       {
           TurnOff();
           currentState = RobotState.Dead;
       }
   }

   private void UpdateEye()
   {
       if (currentBattery <= 0)
       {
           eyeLight.intensity = 0;
       }
       else if (currentBattery <= lowBattery)
       {
           eyeLight.color = Color.red;
           // Interpolate intensity between 0 and half of full intensity for low battery
           float normalizedLowBattery = (float)currentBattery / lowBattery;
           eyeLight.intensity = normalizedLowBattery * (_fullEyeLightIntensity * 0.5f);
       }
       else
       {
           eyeLight.color = _defaultEyeLightColor;
           // Interpolate intensity between half and full intensity for normal operation
           float normalizedBattery = (float)(currentBattery - lowBattery) / (fullBattery - lowBattery);
           float minIntensity = _fullEyeLightIntensity * 0.5f;
           eyeLight.intensity = Mathf.Lerp(minIntensity, _fullEyeLightIntensity, normalizedBattery);
       }
   }
   

   public bool IsOn()
   {
       return  currentState != RobotState.Dead && currentState != RobotState.Off && currentBattery > 0;
   }

   public bool CanCommend()
   {
       return IsOn();
   }
   

   #endregion Utility ------------------------------------------------------------------------
   
}