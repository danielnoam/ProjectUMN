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
    [Tooltip("Minimum mouse movement required to rotate character when aiming")]
    [SerializeField] private float aimRotationThreshold = 0.1f;
    
    
    [Header("References")]
    public CinemachineCamera freeLookCamera;
    public CinemachineCamera aimCamera;
    public CinemachineCamera menuCamera;
    public GameObject aimCore;
    
    private PlayerStateMachine _player;
    private PlayerInputHandler _playerInputHandler;
    private Vector3 _lastAimDirection = Vector3.forward;
    private float _pitchAccumulation = 0f;
    private float _yawAccumulation = 0f;
    private bool IsMenuActive => _player != null && _player.CurrentState == _player.InMenuState;
    public float AimRotationThreshold => aimRotationThreshold;

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
        CheckMenuToggle();
    }

    public void Initialize(PlayerStateMachine player)
    {
        _player = player;
        _playerInputHandler = _player.InputHandler;
        freeLookCamera.Follow = _player.transform;
        menuCamera.Follow = _player.transform;
    }

    private void UpdateAimCore()
    {
        // Update aim core position to follow the player
        if (!aimCore || !_playerInputHandler) return;
        
        aimCore.transform.position = _playerInputHandler.transform.position;
        
        
        // Update aim core rotation
        if (IsMenuActive) return;

        // Get the appropriate sensitivity based on current camera state
        float cameraSensitivity = IsAimCameraActive() 
            ? _playerInputHandler.AimCameraSensitivity 
            : _playerInputHandler.FreeCameraSensitivity;
    
        // Accumulate rotation values
        _yawAccumulation += _playerInputHandler.MouseDelta.x * _playerInputHandler.MouseSensitivity * cameraSensitivity;
        _pitchAccumulation -= _playerInputHandler.MouseDelta.y * _playerInputHandler.MouseSensitivity * cameraSensitivity;
    
        // Clamp pitch to prevent camera flipping
        _pitchAccumulation = Mathf.Clamp(_pitchAccumulation, -89f, 89f);
    
        // Apply the accumulated rotation
        aimCore.transform.rotation = Quaternion.Euler(_pitchAccumulation, _yawAccumulation, 0f);
    }
    

    private void CheckMenuToggle()
    {
        // If player is in menu state, ensure menu camera is active
        if (IsMenuActive)
        {
            if (!IsMenuCameraActive())
            {
                SwitchToMenuCamera();
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }
        // If player is not in menu state, ensure appropriate gameplay camera is active
        else if (IsMenuCameraActive())
        {
            // Switch back to previous camera (either free look or aim)
            if (IsAimCameraActive())
            {
                SwitchToAimCamera();
            }
            else
            {
                SwitchToFreeLookCamera();
            }
            
            // Hide cursor
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void SwitchToAimCamera()
    {
        // Set camera priorities to switch to aim camera
        aimCamera.Priority = aimCameraPriority;
        freeLookCamera.Priority = freeLookCameraPriority;
        menuCamera.Priority = freeLookCameraPriority;
        
        // When switching to aim camera, align it with the freelook camera
        if (aimCamera && freeLookCamera)
        {
            aimCamera.transform.rotation = freeLookCamera.transform.rotation;
        }
    }

    public void SwitchToFreeLookCamera()
    {
        // Reset camera priorities
        freeLookCamera.Priority = aimCameraPriority;
        aimCamera.Priority = freeLookCameraPriority;
        menuCamera.Priority = freeLookCameraPriority;
    }

    private void SwitchToMenuCamera()
    {
        // Set menu camera as highest priority
        menuCamera.Priority = menuCameraPriority;
        freeLookCamera.Priority = freeLookCameraPriority;
        aimCamera.Priority = freeLookCameraPriority;
    }

    public bool IsAimCameraActive()
    {
        return aimCamera.Priority > freeLookCamera.Priority && aimCamera.Priority > menuCamera.Priority;
    }

    public bool IsMenuCameraActive()
    {
        return menuCamera.Priority > freeLookCamera.Priority && menuCamera.Priority > aimCamera.Priority;
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
    
}