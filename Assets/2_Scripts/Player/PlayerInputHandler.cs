using System;
using UnityEngine;
using UnityEngine.InputSystem;


[Serializable]
public class InputSettings
{
    public bool toggleMoveSpeed = false;
    public bool toggleCrouch = false;
    public bool toggleSprint = false;
    public bool toggleAimInput = false;
    [Range(0f, 1f)] public float movementInputThreshold = 0.01f;
    [Range(0.1f, 2f)] public float mouseSensitivity = 1f;
    [Range(0.1f, 2f)] public float freeCameraSensitivity = 1f;
    [Range(0.1f, 2f)] public float aimCameraSensitivity = 0.5f;
}


public class PlayerInputHandler : MonoBehaviour
{
    [Header("Settings")] 
    [SerializeField] private SOInputReader inputReader;
    [SerializeField, Min(0f)] private float jumpBufferTime = 0.2f;
    [SerializeField, Min(0f)] private float interactBufferTime = 0.15f;
    [SerializeField, Min(0f)] private float toggleMenuBufferTime = 0.15f;
    [SerializeField] private InputSettings mouseKeyboardSettings = new InputSettings();
    [SerializeField] private InputSettings gamepadSettings = new InputSettings();
    
    private InputSettings _activeSettings;
    private bool _toggleMoveSpeed;
    private bool _toggleCrouch;
    private bool _toggleSprint;
    private bool _toggleAimInput;
    private float _mouseSensitivity;
    private float _movementInputThreshold;
    private float _freeCameraSensitivity;
    private float _aimCameraSensitivity;
    private float _jumpBufferCounter;
    private float _interactBufferCounter;
    private float _commandRobotBufferCounter;
    private float _toggleMenuBufferCounter;

    
    
    public Vector2 MovementInput { get; private set; }
    public Vector2 MouseDelta { get; private set; }
    public bool JumpInput { get; private set; }
    public bool SprintInput { get; private set; }
    public bool CrouchInput { get; private set; }
    public bool MoveSpeedInput { get; private set; }
    public bool InteractInput { get; private set; }
    public bool CommandRobotInput { get; private set; }
    public bool AimInput { get; private set; }
    public bool ToggleMenuInput { get; private set; }
    public float MovementInputThreshold => _movementInputThreshold;
    public float MouseSensitivity => _mouseSensitivity;
    public float FreeCameraSensitivity => _freeCameraSensitivity;
    public float AimCameraSensitivity => _aimCameraSensitivity;
    public bool IsCrouchToggle => _toggleCrouch;
    


    private void OnEnable()
    {
        inputReader.MoveEvent += OnMovementInput;
        inputReader.LookEvent += OnMouseInput;
        inputReader.JumpEvent += OnJumpInput;
        inputReader.PlayerInteractEvent += OnInteractInput;
        inputReader.RobotInteractEvent += OnCommandRobotInput;
        inputReader.CrouchEvent += OnCrouchInput;
        inputReader.SprintEvent += OnSprintInput;
        inputReader.MoveSpeedEvent += OnMoveSpeedInput;
        inputReader.AimEvent += OnAimInput;
        inputReader.ToggleMenuEvent += OnToggleMenuInput;
        inputReader.ControlSchemeChangedEvent += OnControlSchemeChanged;
        
        OnControlSchemeChanged(inputReader.CurrentControlScheme);
    }
    
    private void OnDisable()
    {
        inputReader.MoveEvent -= OnMovementInput;
        inputReader.LookEvent -= OnMouseInput;
        inputReader.JumpEvent -= OnJumpInput;
        inputReader.PlayerInteractEvent -= OnInteractInput;
        inputReader.RobotInteractEvent -= OnCommandRobotInput;
        inputReader.CrouchEvent -= OnCrouchInput;
        inputReader.SprintEvent -= OnSprintInput;
        inputReader.MoveSpeedEvent -= OnMoveSpeedInput;
        inputReader.AimEvent -= OnAimInput;
        inputReader.ToggleMenuEvent -= OnToggleMenuInput;
        inputReader.ControlSchemeChangedEvent -= OnControlSchemeChanged;
    }

    private void Update()
    {
        ProcessBuffers();
    }


    #region Input events -------------------------------------------------------------------------------------------
    
    private void OnControlSchemeChanged(ControlType controlType)
    {
        // Set the active settings based on the control type
        _activeSettings = controlType == ControlType.KeyboardMouse ? 
            mouseKeyboardSettings : gamepadSettings;
    
        // Update the local variables to match the active settings
        _toggleMoveSpeed = _activeSettings.toggleMoveSpeed;
        _toggleCrouch = _activeSettings.toggleCrouch;
        _toggleSprint = _activeSettings.toggleSprint;
        _toggleAimInput = _activeSettings.toggleAimInput;
        _mouseSensitivity = _activeSettings.mouseSensitivity;
        _movementInputThreshold = _activeSettings.movementInputThreshold;
        _freeCameraSensitivity = _activeSettings.freeCameraSensitivity;
        _aimCameraSensitivity = _activeSettings.aimCameraSensitivity;
    }
    
    private void OnToggleMenuInput(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _toggleMenuBufferCounter = toggleMenuBufferTime;
        }
    }

    private void OnMovementInput(InputAction.CallbackContext context)
    {
        MovementInput = context.ReadValue<Vector2>();

    }

    private void OnMouseInput(InputAction.CallbackContext context)
    {
        MouseDelta = context.ReadValue<Vector2>() /3;
    }

    private void OnAimInput(InputAction.CallbackContext context)
    {
        if (_toggleAimInput)
        {
            if (context.started) 
            {
                AimInput = !AimInput;
            }
        } 
        else 
        {
            AimInput = context.performed || context.started;
        }
    }

    private void OnJumpInput(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _jumpBufferCounter = jumpBufferTime;
        }
    }

    private void OnInteractInput(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _interactBufferCounter = interactBufferTime;
        }
        
    }
    
    private void OnCommandRobotInput(InputAction.CallbackContext context)
    {
        
        if (context.started)
        {
            _commandRobotBufferCounter = interactBufferTime;
        }
    }

    private void OnCrouchInput(InputAction.CallbackContext context)
    {
        if (_toggleCrouch)
        {
            if (context.started) 
            {
                CrouchInput = !CrouchInput;
            }
        } 
        else 
        {
            CrouchInput = context.performed;
        }
    }

    private void OnSprintInput(InputAction.CallbackContext context)
    {
        if (_toggleSprint)
        {
            if (context.started) 
            {
                SprintInput = !SprintInput;
            }
        } 
        else 
        {
            SprintInput = context.performed;
        }
    }

    private void OnMoveSpeedInput(InputAction.CallbackContext context)
    {
        if (_toggleMoveSpeed)
        {
            if (context.started) 
            {
                MoveSpeedInput = !MoveSpeedInput;
            }
        } 
        else 
        {
            MoveSpeedInput = context.performed;
        }
    }

    #endregion Input events -------------------------------------------------------------------------------------------


    #region Buffers -------------------------------------------------------------------------------------------

    private void ProcessBuffers()
    {
        // Jump buffer
        JumpInput = _jumpBufferCounter > 0;
        if (_jumpBufferCounter > 0)
        {
            _jumpBufferCounter -= Time.deltaTime;
        }
        
        // Interact buffer
        InteractInput = _interactBufferCounter > 0;
        if (_interactBufferCounter > 0)
        {
            _interactBufferCounter -= Time.deltaTime;
        }
        
        // Robot command buffer
        CommandRobotInput = _commandRobotBufferCounter > 0;
        if (_commandRobotBufferCounter > 0)
        {
            _commandRobotBufferCounter -= Time.deltaTime;
        }
        
        // Toggle menu buffer
        ToggleMenuInput = _toggleMenuBufferCounter > 0;
        if (_toggleMenuBufferCounter > 0)
        {
            _toggleMenuBufferCounter -= Time.deltaTime;
        }
    }
    
    public void ConsumeJumpBuffer()
    {
        _jumpBufferCounter = 0;
    }
    
    public void ConsumeInteractBuffer()
    {
        _interactBufferCounter = 0;
    }
    
    public void ConsumeCommandRobotBuffer()
    {
        _commandRobotBufferCounter = 0;
    }
    
    public void ConsumeToggleMenuBuffer()
    {
        _toggleMenuBufferCounter = 0;
    }
    
    public void ConsumeCrouchInput()
    {
        CrouchInput = false;
    }

    #endregion Buffers -------------------------------------------------------------------------------------------


}