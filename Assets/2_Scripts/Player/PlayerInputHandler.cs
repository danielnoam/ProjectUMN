using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VInspector;





public class PlayerInputHandler : MonoBehaviour
{
    [Header("Settings")] 
    [SerializeField, Min(0f)] private float jumpBufferTime = 0.2f;
    [SerializeField, Min(0f)] private float interactBufferTime = 0.15f;
    [SerializeField, Min(0f)] private float commandRobotBufferTime = 0.15f;
    [SerializeField, Min(0f)] private float crouchBufferTime = 0.15f;
    [SerializeField, Min(0f)] private float toggleMenuBufferTime = 0.15f;
    [SerializeField, Min(0f)] private float sprintBufferTime = 0.15f;
    [SerializeField, Min(0f)] private float moveSpeedBufferTime = 0.15f;
    [SerializeField, Min(0f)] private float aimBufferTime = 0.15f;
    
    [Header("References")]
    [SerializeField] private SOInputReader inputReader;

    private PlayerStateMachine _player;
    private float _jumpBufferCounter;
    private float _interactBufferCounter;
    private float _commandRobotBufferCounter;
    private float _crouchBufferCounter;
    private float _toggleMenuBufferCounter;
    private float _sprintBufferCounter;
    private float _moveSpeedBufferCounter;
    private float _aimBufferCounter;

    
    public SOInputReader InputReader => inputReader;
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


    private void Awake()
    {
        _player = GetComponent<PlayerStateMachine>();
    }

    private void Start()
    {
        inputReader.LoadSettings();
    }


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
        if (_player && _player.CurrentState == _player.InMenuState)
        {
            if (controlType == ControlType.Gamepad)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        else if (_player)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
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
        if (inputReader.ActiveSettings.toggleAimInput)
        {
            if (context.started) 
            {
                AimInput = !AimInput;
            }
        } 
        else 
        {
            AimInput = context.performed || context.started;
            
            if (context.started)
            {
                _aimBufferCounter = aimBufferTime;
            }
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
            _commandRobotBufferCounter = commandRobotBufferTime;
        }
    }

    private void OnCrouchInput(InputAction.CallbackContext context)
    {
        if (inputReader.ActiveSettings.toggleCrouch)
        {
            if (context.started) 
            {
                CrouchInput = !CrouchInput;
            }
        } 
        else 
        {
            CrouchInput = context.performed;
            
            if (context.started)
            {
                _crouchBufferCounter = crouchBufferTime;
            }
        }
    }

    private void OnSprintInput(InputAction.CallbackContext context)
    {
        if (inputReader.ActiveSettings.toggleSprint)
        {
            if (context.started) 
            {
                SprintInput = !SprintInput;
            }
        } 
        else 
        {
            SprintInput = context.performed;
            
            if (context.started)
            {
                _sprintBufferCounter = sprintBufferTime;
            }
        }
    }

    private void OnMoveSpeedInput(InputAction.CallbackContext context)
    {
        if (inputReader.ActiveSettings.toggleMoveSpeed)
        {
            if (context.started) 
            {
                MoveSpeedInput = !MoveSpeedInput;
            }
        } 
        else 
        {
            MoveSpeedInput = context.performed;
            
            if (context.started)
            {
                _moveSpeedBufferCounter = moveSpeedBufferTime;
            }
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
        
        // Crouch buffer (for non-toggle mode)
        if (!inputReader.ActiveSettings.toggleCrouch && _crouchBufferCounter > 0)
        {
            _crouchBufferCounter -= Time.deltaTime;
        }
        
        // Sprint buffer (for non-toggle mode)
        if (!inputReader.ActiveSettings.toggleSprint && _sprintBufferCounter > 0)
        {
            _sprintBufferCounter -= Time.deltaTime;
        }
        
        // Move speed buffer (for non-toggle mode)
        if (!inputReader.ActiveSettings.toggleMoveSpeed && _moveSpeedBufferCounter > 0)
        {
            _moveSpeedBufferCounter -= Time.deltaTime;
        }
        
        // Aim buffer (for non-toggle mode)
        if (!inputReader.ActiveSettings.toggleAimInput && _aimBufferCounter > 0)
        {
            _aimBufferCounter -= Time.deltaTime;
        }
    }
    
    // Combined consumption methods
    public void ConsumeJumpInput()
    {
        _jumpBufferCounter = 0;
        JumpInput = false;
    }
    
    public void ConsumeInteractInput()
    {
        _interactBufferCounter = 0;
        InteractInput = false;
    }
    
    public void ConsumeCommandRobotInput()
    {
        _commandRobotBufferCounter = 0;
        CommandRobotInput = false;
    }
    
    public void ConsumeToggleMenuInput()
    {
        _toggleMenuBufferCounter = 0;
        ToggleMenuInput = false;
    }
    
    public void ConsumeCrouchInput()
    {
        _crouchBufferCounter = 0;
        CrouchInput = false;
    }
    
    public void ConsumeSprintInput()
    {
        _sprintBufferCounter = 0;
        SprintInput = false;
    }
    
    public void ConsumeMoveSpeedInput()
    {
        _moveSpeedBufferCounter = 0;
        MoveSpeedInput = false;
    }
    
    public void ConsumeAimInput()
    {
        _aimBufferCounter = 0;
        AimInput = false;
    }

    #endregion Buffers -------------------------------------------------------------------------------------------



}