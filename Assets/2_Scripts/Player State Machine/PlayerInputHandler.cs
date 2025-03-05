using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("Settings")] 
    [SerializeField] private SOInputReader inputReader;
    [SerializeField] private bool toggleMoveSpeed = true;
    [SerializeField] private bool toggleCrouch = true;
    [SerializeField] private bool toggleSprint = false;
    [SerializeField] private bool toggleAimInput = false;
    [SerializeField, Min(0f)] private float jumpBufferTime = 0.2f;
    [SerializeField, Min(0f)] private float interactBufferTime = 0.15f;
    [SerializeField, Min(0f)] private float toggleMenuBufferTime = 0.15f;
    [SerializeField, Range(0.1f, 2f)] private float mouseSensitivity = 1f;
    [Tooltip("Minimum movement input to register sprint")]
    [SerializeField, Range(0f, 1f)] private float sprintInputThreshold = 0.01f;
    [Tooltip("Minimum movement input to register movement")]
    [SerializeField, Range(0f, 1f)] private float movementInputThreshold = 0.01f;
    
    public Vector2 MovementInput { get; private set; }
    public Vector2 MouseDelta { get; private set; }
    public bool JumpInput { get; private set; }
    public bool SprintInput { get; private set; }
    public bool CrouchInput { get; private set; }
    public bool MoveSpeedInput { get; private set; }
    public bool PlayerInteractInput { get; private set; }
    public bool RobotInteractInput { get; private set; }
    public bool AimInput { get; private set; }
    public bool ToggleMenuInput { get; private set; }
    public float MovementInputThreshold => movementInputThreshold;
    public float SprintInputThreshold => sprintInputThreshold;
    public float MouseSensitivity => mouseSensitivity;
    public bool IsCrouchToggle => toggleCrouch;
    
    // Buffer timers
    private float _jumpBufferCounter;
    private float _playerInteractBufferCounter;
    private float _robotInteractBufferCounter;
    private float _toggleMenuBufferCounter; // Added buffer counter for toggle menu

    private void OnEnable()
    {
        inputReader.MoveEvent += OnMovementInput;
        inputReader.LookEvent += OnMouseInput;
        inputReader.JumpEvent += OnJumpInput;
        inputReader.PlayerInteractEvent += OnPlayerInteractInput;
        inputReader.RobotInteractEvent += OnRobotInteractInput;
        inputReader.CrouchEvent += OnCrouchInput;
        inputReader.SprintEvent += OnSprintInput;
        inputReader.MoveSpeedEvent += OnMoveSpeedInput;
        inputReader.AimEvent += OnAimInput;
        inputReader.ToggleMenuEvent += OnToggleMenuInput;
    }
    
    private void OnDisable()
    {
        inputReader.MoveEvent -= OnMovementInput;
        inputReader.LookEvent -= OnMouseInput;
        inputReader.JumpEvent -= OnJumpInput;
        inputReader.PlayerInteractEvent -= OnPlayerInteractInput;
        inputReader.RobotInteractEvent -= OnRobotInteractInput;
        inputReader.CrouchEvent -= OnCrouchInput;
        inputReader.SprintEvent -= OnSprintInput;
        inputReader.MoveSpeedEvent -= OnMoveSpeedInput;
        inputReader.AimEvent -= OnAimInput;
        inputReader.ToggleMenuEvent -= OnToggleMenuInput;
    }

    private void Update()
    {
        ProcessBuffers();
    }


    #region Input events -------------------------------------------------------------------------------------------
    
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
        if (toggleAimInput)
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

    private void OnPlayerInteractInput(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _playerInteractBufferCounter = interactBufferTime;
        }
        
    }
    
    private void OnRobotInteractInput(InputAction.CallbackContext context)
    {
        
        if (context.started)
        {
            _robotInteractBufferCounter = interactBufferTime;
        }
    }

    private void OnCrouchInput(InputAction.CallbackContext context)
    {
        if (toggleCrouch)
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
        if (toggleSprint)
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
        if (toggleMoveSpeed)
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
        
        // Player interact buffer
        PlayerInteractInput = _playerInteractBufferCounter > 0;
        if (_playerInteractBufferCounter > 0)
        {
            _playerInteractBufferCounter -= Time.deltaTime;
        }
        
        // Robot interact buffer
        RobotInteractInput = _robotInteractBufferCounter > 0;
        if (_robotInteractBufferCounter > 0)
        {
            _robotInteractBufferCounter -= Time.deltaTime;
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
    
    public void ConsumePlayerInteractBuffer()
    {
        _playerInteractBufferCounter = 0;
    }
    
    public void ConsumeRobotInteractBuffer()
    {
        _robotInteractBufferCounter = 0;
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