using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Serialization;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }
    
    [Header("Camera Settings")]
    [SerializeField] private int freeLookCameraPriority = 10;
    [SerializeField] private int aimCameraPriority = 15;
    [SerializeField] private int menuCameraPriority = 20;
    [SerializeField, Range(0.1f, 2f)] private float freeCameraSensitivity = 1f;
    [SerializeField, Range(0.1f, 2f)] private float aimCameraSensitivity = 1f;
    [SerializeField] private float aimRotationThreshold = 0.1f;
    [SerializeField] private float aimMaxPitch = 80f;
    
    
    [Header("References")]
    public CinemachineCamera freeLookCamera;
    public CinemachineCamera aimCamera;
    public CinemachineCamera menuCamera;
    public GameObject aimCore;
    
    public float AimRotationThreshold => aimRotationThreshold;
    private PlayerStateMachine _player;
    private PlayerInputHandler _playerInputHandler;
    private Vector3 _lastAimDirection = Vector3.forward;
    private float _pitchAccumulation = 0f;
    private float _yawAccumulation = 0f;
    private bool IsMenuActive => _player != null && _player.CurrentState == _player.InMenuState;
    private bool IsPlayerAiming => _player != null && _player.IsAiming;
    

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
        
        // Validate references
        if (freeLookCamera == null || aimCamera == null || aimCore == null || menuCamera == null)
        {
            Debug.LogError("Camera references not assigned to CameraManager!");
        }

        // Hide cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Set initial camera priorities
        freeLookCamera.Priority = freeLookCameraPriority + 5; // Start with free look as active camera
        aimCamera.Priority = freeLookCameraPriority;
        menuCamera.Priority = freeLookCameraPriority;
    }
    
    
    private void Update()
    {
        UpdateAimCore();
        HandleCameraSwitching();
    }
    
    
    

    #region Public methods ----------------------------------------------------------------------------

    public void Initialize(PlayerStateMachine player)
    {
        _player = player;
        _playerInputHandler = _player.InputHandler;
        freeLookCamera.Follow = _player.transform;
        menuCamera.Follow = _player.transform;
    }
    
    public bool IsAimCameraActive()
    {
        return aimCamera.Priority > freeLookCamera.Priority && aimCamera.Priority > menuCamera.Priority;
    }

    public bool IsMenuCameraActive()
    {
        return menuCamera.Priority > freeLookCamera.Priority && menuCamera.Priority > aimCamera.Priority;
    }
    
    public bool IsFreeLookCameraActive()
    {
        return freeLookCamera.Priority > aimCamera.Priority && freeLookCamera.Priority > menuCamera.Priority;
    }
    
    public Vector3 GetCameraAimDirection(bool flatenY = false)
    {
        // Use the active camera to determine the aim direction
        Transform activeCameraTransform;
        
        if (IsMenuCameraActive())
        {
            activeCameraTransform = menuCamera.transform;
        }
        else if (IsAimCameraActive()) 
        {
            activeCameraTransform = aimCamera.transform;
        }
        else
        {
            activeCameraTransform = freeLookCamera.transform;
        }

        if (!activeCameraTransform) return _lastAimDirection; // Fallback if camera missing

        // Get forward direction
        Vector3 cameraForward = activeCameraTransform.forward;
        if (flatenY) cameraForward.y = 0; // Remove vertical tilt

        // Validate direction
        if (cameraForward.sqrMagnitude < 0.001f)
            return _lastAimDirection;

        // Normalize and store
        cameraForward.Normalize();
        _lastAimDirection = cameraForward;
        return cameraForward;
    }
    

    #endregion Public methods ----------------------------------------------------------------------------

    

    #region Private methods ----------------------------------------------------------------------------
    
    private void UpdateAimCore()
    {
        // Update aim core position to follow the player
        if (!aimCore || !_playerInputHandler || _player.CurrentState == _player.InMenuState) return;
    
        aimCore.transform.position = _playerInputHandler.transform.position;
    
        // When not aiming, sync aim core rotation with free look camera
        if (!IsPlayerAiming && freeLookCamera != null)
        {
            // Extract pitch and yaw from free look camera
            Vector3 forward = freeLookCamera.transform.forward;
            float pitch = -Mathf.Asin(forward.y) * Mathf.Rad2Deg;
            float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        
            // Update our accumulated values to match current free look camera
            _pitchAccumulation = pitch;
            _yawAccumulation = yaw;
        
            // Apply rotation to aim core
            aimCore.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            return;
        }
    
        // Only handle manual rotation updates when actually aiming or in free look mode
        if (IsMenuActive) return;

        // Get the appropriate sensitivity based on current camera state
        float cameraSensitivity = IsAimCameraActive() 
            ? aimCameraSensitivity 
            : freeCameraSensitivity;

        // Accumulate rotation values from mouse input
        _yawAccumulation += _playerInputHandler.MouseDelta.x * _playerInputHandler.MouseSensitivity * cameraSensitivity;
        _pitchAccumulation -= _playerInputHandler.MouseDelta.y * _playerInputHandler.MouseSensitivity * cameraSensitivity;

        // Clamp pitch to prevent camera flipping
        _pitchAccumulation = Mathf.Clamp(_pitchAccumulation, -aimMaxPitch, aimMaxPitch);

        // Apply the accumulated rotation
        aimCore.transform.rotation = Quaternion.Euler(_pitchAccumulation, _yawAccumulation, 0f);
    }
    

    private void HandleCameraSwitching()
    {
        if (IsMenuActive)
        {
            if (!IsMenuCameraActive())
            {
                SwitchToMenuCamera();
            }
        } 
        else if (IsPlayerAiming && !IsAimCameraActive())
        {
            SwitchToAimCamera();
        }
        else if (!IsPlayerAiming &&!IsFreeLookCameraActive())
        {
            SwitchToFreeLookCamera();
        }
    }
    
    private void SwitchToAimCamera()
    {
        // Set camera priorities to switch to aim camera
        aimCamera.Priority = aimCameraPriority;
        freeLookCamera.Priority = freeLookCameraPriority;
        menuCamera.Priority = freeLookCameraPriority;
        
        // When switching to aim camera, align it with the free look camera
        if (aimCamera && freeLookCamera)
        {
            aimCamera.transform.rotation = freeLookCamera.transform.rotation;
        }
        
        // Hide cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void SwitchToFreeLookCamera()
    {
        if (aimCamera && freeLookCamera)
        {
            // Extract rotation from aim camera/aim core
            Quaternion aimRotation = aimCamera.transform.rotation;
            
            // Apply this rotation to the free look camera before switching
            // This ensures rotation continuity when transitioning back
            freeLookCamera.transform.rotation = aimRotation;
        }
        
        // Reset camera priorities
        freeLookCamera.Priority = aimCameraPriority;
        aimCamera.Priority = freeLookCameraPriority;
        menuCamera.Priority = freeLookCameraPriority;
        
        // Hide cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void SwitchToMenuCamera()
    {
        // Set menu camera as highest priority
        menuCamera.Priority = menuCameraPriority;
        freeLookCamera.Priority = freeLookCameraPriority;
        aimCamera.Priority = freeLookCameraPriority;
        
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    
    
    #endregion Private methods ----------------------------------------------------------------------------






    
}