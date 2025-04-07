using System;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;
using VInspector;

[SelectionBase]
public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }
    
    [Header("Postion/Rotation Settings")]
    [SerializeField] private float aimMaxPitch = 80f;
    [SerializeField] private float crouchVerticalOffset = -0.3f;
    
    [Header("Fov Settings")]
    [SerializeField] private float minFov = 40f;
    [SerializeField] private float maxFov = 65f;
    
    [Header("Noise Settings")]
    [SerializeField] private float maxAmplitude = 0.2f;
    [SerializeField] private float maxFrequency = 4f;
    
    [Header("References")]
    public CinemachineCamera freeLookCamera;
    public CinemachineCamera aimCamera;
    public CinemachineCamera menuCamera;
    public CinemachineCamera startMenuCamera;
    public CinemachineCamera introCamera;
    public GameObject aimCore;
    public Transform targetTransform;



    private CinemachineCamera _currentCamera;
    private CinemachineThirdPersonFollow _menuCameraFollow;
    private CinemachineBasicMultiChannelPerlin _aimCameraNoise;
    private CinemachineBasicMultiChannelPerlin _freeLookCameraNoise;
    private CinemachineInputAxisController _freeLookCameraInput;
    private CinemachineSplineDolly _introCameraDolly;
    private PlayerStateMachine _player;
    private PlayerInputHandler _playerInputHandler;
    private Vector3 _lastAimDirection = Vector3.forward;
    private Vector3 _currentOffset = Vector3.zero;
    private float _pitchAccumulation = 0f;
    private float _yawAccumulation = 0f;
    private float _freeLookInitialFOV;
    private float _aimInitialFOV;
    private bool IsMenuActive => _player && _player.CurrentState == _player.InMenuState;
    private bool IsPlayerAiming => _player && _player.IsAiming;
    private bool IsAimOnlyMode => _player && _player.CurrentCameraMode == CameraMode.AimOnly;
    private int _freeLookCameraPriority;
    private int _aimCameraPriority;
    private int _menuCameraPriority;
    private int _startMenuCameraPriority;
    private int _introCameraPriority;
    

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
        

        if (!freeLookCamera|| !aimCamera || !aimCore || !menuCamera)
        {
            Debug.LogError("Camera references not assigned to CameraManager!");
        }



        _freeLookInitialFOV = freeLookCamera.Lens.FieldOfView;
        _aimInitialFOV = aimCamera.Lens.FieldOfView;
        _freeLookCameraPriority = freeLookCamera.Priority;
        _aimCameraPriority = aimCamera.Priority;
        _menuCameraPriority = menuCamera.Priority;
        _startMenuCameraPriority = startMenuCamera.Priority;
        _introCameraPriority = introCamera.Priority;
        _aimCameraNoise = aimCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
        _freeLookCameraNoise = freeLookCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
        _freeLookCameraInput = freeLookCamera.GetComponent<CinemachineInputAxisController>();
        _menuCameraFollow = menuCamera.GetComponent<CinemachineThirdPersonFollow>();
        _introCameraDolly = introCamera.GetComponent<CinemachineSplineDolly>();
    }


    private void Start()
    {
        TestManager.Instance?.onIntroSequenceStart.AddListener(StartIntroSequenceCamera);
    }

    private void OnDestroy()
    {
        TestManager.Instance?.onIntroSequenceStart.RemoveListener(StartIntroSequenceCamera);
    }

    private void Update()
    {
        if (IsIntroCameraActive())
        {
            switch (_introCameraDolly.CameraPosition)
            {
                case >= 1f:
                    SwitchToCamera(startMenuCamera, true);
                    break;
                case >= 0.9f when _player.CurrentState != _player.InMenuState:
                    _player.SwitchState(_player.InMenuState);
                    _player.InMenuState.SelectPage(_player.InMenuState.StartPage);
                    break;            
        
                default:
                    float startSpeed = 0.2f;
                    float endSpeed = 0.03f;
                    float t = Mathf.Clamp01(_introCameraDolly.CameraPosition / 1f);
                    float speed = Mathf.Lerp(startSpeed, endSpeed, t);
                    
                    _introCameraDolly.CameraPosition += speed * Time.deltaTime;
                    break;
            }

            return;
        }
        
        HandleCameraSwitching();
        UpdateCameraFOV();
        UpdateCameraNoise();
    }
    

    private void LateUpdate()
    {
        
        UpdateAimCore();
    }


    #region Public methods ----------------------------------------------------------------------------

    public void Initialize(PlayerStateMachine player)
    {
        _player = player;
        _playerInputHandler = _player.InputHandler;
        menuCamera.Follow = _player.transform;
        startMenuCamera.Follow = _player.transform;
        freeLookCamera.Follow = aimCore.transform;
        aimCamera.Follow = aimCore.transform;
        
    }
    
    public bool IsAimCameraActive()
    {
        return aimCamera.Priority == 10;
    }
    
    public bool IsFreeLookCameraActive()
    {
        return freeLookCamera.Priority == 10;
    }

    public bool IsMenuCameraActive()
    {
        return menuCamera.Priority == 10;
    }
    
    public bool IsStartMenuCameraActive()
    {
        return startMenuCamera.Priority == 10;
    }
    
    public bool IsIntroCameraActive()
    {
        return introCamera.Priority == 10;
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
        if (!aimCore || !_player || IsMenuActive) return;
        

        // Set aim core position
        // Determine target offset based on player state
        Vector3 targetOffset = _player.CurrentState == _player.CrouchingState ? new Vector3(0,crouchVerticalOffset,0) : Vector3.zero;
    
        // Smoothly interpolate between current and target offset
        _currentOffset = Vector3.Lerp(_currentOffset, targetOffset, Time.deltaTime * 10f);
    
        // Apply position with smooth offset
        aimCore.transform.position = _player.transform.position + _currentOffset;
        
    
        // When not aiming, sync aim core rotation with free look camera
        if (!IsPlayerAiming && freeLookCamera)
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
        

        // Get the appropriate sensitivity based on current camera state
        float cameraSensitivity = IsAimCameraActive() 
            ? _playerInputHandler.AimCameraSensitivity 
            : _playerInputHandler.FreeCameraSensitivity;

        // Accumulate rotation values from mouse input
        _yawAccumulation += _playerInputHandler.MouseDelta.x * _playerInputHandler.MouseSensitivity * cameraSensitivity;
        _pitchAccumulation -= _playerInputHandler.MouseDelta.y * _playerInputHandler.MouseSensitivity * cameraSensitivity;

        // Clamp pitch to prevent camera flipping
        _pitchAccumulation = Mathf.Clamp(_pitchAccumulation, -aimMaxPitch, aimMaxPitch);

        // Apply the accumulated rotation
        aimCore.transform.rotation = Quaternion.Euler(_pitchAccumulation, _yawAccumulation, 0f);
    }

    private void UpdateCameraNoise()
    {
        // Skip if player reference is missing or menu camera is active
        if (!_player || IsMenuCameraActive()) return;

        bool notAllowedStates = _player.CurrentState == _player.TeleportingState || _player.CurrentState == _player.FallingState || _player.CurrentState == _player.JumpingState;

        if (!notAllowedStates)
        {
            // Get the player's current speed
            float currentSpeed = _player.ActiveHorizontalVelocity;
            
        
            // Calculate a noise factor (0 to 1) based on current speed
            // This will be 0 below minNoiseSpeed and 1 at or above maxNoiseSpeed
            float noiseFactor = Mathf.Clamp01((currentSpeed - 0) / (_player.sprintSpeed - 0));
        
            // Calculate target amplitude and frequency based on the noise factor
            float targetAmplitude = noiseFactor * maxAmplitude;
            float targetFrequency = noiseFactor * maxFrequency;
        
            // Apply noise to the active camera
            CinemachineBasicMultiChannelPerlin activeNoise = IsAimCameraActive() ? _aimCameraNoise : _freeLookCameraNoise;
        
            if (activeNoise)
            {
                // Smoothly interpolate current values to target values
                activeNoise.AmplitudeGain = Mathf.Lerp(activeNoise.AmplitudeGain, targetAmplitude, Time.deltaTime * 5f);
                activeNoise.FrequencyGain = Mathf.Lerp(activeNoise.FrequencyGain, targetFrequency, Time.deltaTime * 5f);
            }
        }
        else
        {
            // Apply noise to the active camera
            CinemachineBasicMultiChannelPerlin activeNoise = IsAimCameraActive() ? _aimCameraNoise : _freeLookCameraNoise;
        
            if (activeNoise)
            {
                // Smoothly interpolate current values to target values
                activeNoise.AmplitudeGain = Mathf.Lerp(activeNoise.AmplitudeGain, 0, Time.deltaTime * 5f);
                activeNoise.FrequencyGain = Mathf.Lerp(activeNoise.FrequencyGain, 0, Time.deltaTime * 5f);
            }
        }
    }
    
    private void UpdateCameraFOV()
    {
        // Skip if player reference is missing or menu camera is active
        if (!_player || IsMenuCameraActive()|| IsStartMenuCameraActive()) return;

        // Determine which camera is active and its initial FOV
        var initialFOV = IsAimCameraActive() ? _aimInitialFOV : _freeLookInitialFOV;

        // Calculate velocity factor (0 to 1)
        float velocityFactor = Mathf.Clamp01(_player.ActiveHorizontalVelocity / _player.sprintSpeed);
    
        // Calculate pitch factor (0 to 1) based on how close we are to maximum pitch
        // Take the absolute value of pitch to handle looking up or down equally
        float pitchFactor = Mathf.Abs(_pitchAccumulation) / aimMaxPitch;
        pitchFactor = Mathf.Clamp01(pitchFactor); // Ensure value is between 0-1
    
        // Combine factors - give each factor a weight
        // You can adjust these weights based on how much you want each to influence the FOV
        float velocityWeight = 0.5f;
        float pitchWeight = 0.5f;
        float combinedFactor = (velocityFactor * velocityWeight) + (pitchFactor * pitchWeight);
    
        // Calculate fov offset and apply it to initial FOV
        float fovOffset = maxFov - minFov;
        float targetFOV = Mathf.Min(initialFOV + (fovOffset * combinedFactor), maxFov);

        // Smoothly interpolate to target FOV
        float currentFOV = _currentCamera.Lens.FieldOfView;
        float newFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * 5f);

        // Apply new FOV
        _currentCamera.Lens.FieldOfView = newFOV;
    }
    

    private void HandleCameraSwitching()
    {
        if (IsMenuActive)
        {
            if (!IsMenuCameraActive() && _player.InMenuState.CurrentPage != _player.InMenuState.StartPage)
            {
                SwitchToCamera(menuCamera, true);
            } else if (!IsStartMenuCameraActive() && _player.InMenuState.CurrentPage == _player.InMenuState.StartPage)
            {
                SwitchToCamera(startMenuCamera, true);
            }

            if (_player.InMenuState.CurrentPage == _player.InMenuState.DebugPage && _menuCameraFollow.CameraSide != 0)
            {
                _menuCameraFollow.CameraSide = Mathf.Lerp(_menuCameraFollow.CameraSide, 0f, Time.deltaTime * 5f);
            }
            else if (_player.InMenuState.CurrentPage == _player.InMenuState.PausePage && !Mathf.Approximately(_menuCameraFollow.CameraSide, 1))
            {
                _menuCameraFollow.CameraSide = Mathf.Lerp(_menuCameraFollow.CameraSide, 1f, Time.deltaTime * 5f);
            }
            else if (_player.InMenuState.CurrentPage == _player.InMenuState.StartPage)
            {
                
            }
        } 
        else if (IsPlayerAiming && !IsAimCameraActive())
        {
            SwitchToCamera(aimCamera, false);
        }
        else if (!IsAimOnlyMode && !IsPlayerAiming &&!IsFreeLookCameraActive())
        {
            SwitchToCamera(freeLookCamera, false);
        }
    }
    
    
    
   
    private void SwitchToCamera(CinemachineCamera cam, bool enableCursor)
    {
        menuCamera.Priority = _menuCameraPriority;
        freeLookCamera.Priority = _freeLookCameraPriority;
        aimCamera.Priority = _aimCameraPriority;
        startMenuCamera.Priority = _startMenuCameraPriority;
        introCamera.Priority = _introCameraPriority;

        _currentCamera = cam;
        _freeLookCameraInput.enabled = cam == freeLookCamera;
        cam.Priority = 10;
        Cursor.lockState = enableCursor ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = enableCursor;
        
    }
    
    private void StartIntroSequenceCamera()
    {
        _introCameraDolly.CameraPosition = 0;
        SwitchToCamera(introCamera, false);
    }
    
    
    #endregion Private methods ----------------------------------------------------------------------------
    

    
}