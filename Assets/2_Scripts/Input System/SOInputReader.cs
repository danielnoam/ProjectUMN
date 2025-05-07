using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.InputSystem.XR;
using UnityEngine.Serialization;
using VInspector;

[Serializable]
public class InputSettings
{
    public bool toggleMoveSpeed;
    public bool toggleCrouch;
    public bool toggleSprint;
    public bool toggleAimInput;
    [Range(0f, 1f)] public float movementInputThreshold = 0.01f;
    [Range(0.1f, 10f)] public float mouseSensitivity = 1f;
    [Range(0.1f, 10f)] public float freeCameraSensitivity = 1f;
    [Range(0.1f, 10f)] public float aimCameraSensitivity = 0.5f;
    
    public InputSettings Clone()
    {
        return new InputSettings
        {
            toggleMoveSpeed = this.toggleMoveSpeed,
            toggleCrouch = this.toggleCrouch,
            toggleSprint = this.toggleSprint,
            toggleAimInput = this.toggleAimInput,
            movementInputThreshold = this.movementInputThreshold,
            mouseSensitivity = this.mouseSensitivity,
            freeCameraSensitivity = this.freeCameraSensitivity,
            aimCameraSensitivity = this.aimCameraSensitivity
        };
    }
}



/// <summary>
/// Scriptable Object that handles input system events and broadcasts them to listeners
/// create a scriptable object and connect the input asset to it,
/// create a reference to the input reader and subscribe to the necessary events in the mono behavior
/// </summary>
[CreateAssetMenu(fileName = "InputReader", menuName = "SO Manager/Input Reader")]
public class SOInputReader : ScriptableObject
{
    [Header("Settings")]
    [SerializeField] private InputActionAsset inputAsset;
    [SerializeField] private bool printDebug;
    
    [Header("Control Schemes")]
    [SerializeField] private InputSettings mouseKeyboardDefaultSettings ;
    [SerializeField] private InputSettings gamepadDefaultSettings;
    [SerializeField,ReadOnly] private InputSettings activeSettings = new InputSettings();
    [SerializeField,ReadOnly] private InputSettings mouseKeyboardSettings = new InputSettings();
    [SerializeField,ReadOnly] private InputSettings gamepadSettings = new InputSettings();
    public UnityEvent onResetInputSettingEvent = new UnityEvent();

    // Actions from the input asset
    private InputAction _toggleMenu;
    private InputAction _navigateAction;
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _aimAction;
    private InputAction _jumpAction;
    private InputAction _sprintAction;
    private InputAction _crouchAction;
    private InputAction _moveSpeedAction;
    private InputAction _playerInteractAction;
    private InputAction _robotInteractAction;

    
    
    // Events that other classes can subscribe to
    public event Action<ControlType> ControlSchemeChangedEvent;
    public event Action<InputAction.CallbackContext> ToggleMenuEvent;
    public event Action<InputAction.CallbackContext> NavigateEvent;
    public event Action<InputAction.CallbackContext> MoveEvent;
    public event Action<InputAction.CallbackContext> LookEvent;
    public event Action<InputAction.CallbackContext> JumpEvent;
    public event Action<InputAction.CallbackContext> SprintEvent;
    public event Action<InputAction.CallbackContext> CrouchEvent;
    public event Action<InputAction.CallbackContext> MoveSpeedEvent;
    public event Action<InputAction.CallbackContext> PlayerInteractEvent;
    public event Action<InputAction.CallbackContext> RobotInteractEvent;
    public event Action<InputAction.CallbackContext> AimEvent;

    
    //  public state properties for other classes for easier checks
    public ControlType CurrentControlScheme { get; private set; }
    public Vector2 CurrentMoveInput { get; private set; }
    public Vector2 CurrentLookInput { get; private set; }
    public InputSettings ActiveSettings => activeSettings;
    public InputSettings MouseKeyboardSettings => mouseKeyboardSettings;
    public InputSettings GamepadSettings => gamepadSettings;


    private void OnEnable()
    {
        if (inputAsset == null)
        {
            // Debug.LogError("Input asset is not assigned in the SOInputReader");
            return;
        }
        
        // Get references to input asset actions
        _toggleMenu = inputAsset.FindAction("ToggleMenu");
        _navigateAction = inputAsset.FindAction("Navigate");
        _moveAction = inputAsset.FindAction("Move");
        _lookAction = inputAsset.FindAction("Look");
        _jumpAction = inputAsset.FindAction("Jump");
        _sprintAction = inputAsset.FindAction("Sprint");
        _crouchAction = inputAsset.FindAction("Crouch");
        _moveSpeedAction = inputAsset.FindAction("MoveSpeed");
        _playerInteractAction = inputAsset.FindAction("PlayerInteract");
        _robotInteractAction = inputAsset.FindAction("RobotInteract");
        _aimAction = inputAsset.FindAction("Aim");

        
        // Enable the action map or actions and subscribe to the event, actions are disabled by default
        _toggleMenu.EnableAndSubscribe(OnToggleMenu);
        _navigateAction.EnableAndSubscribe(OnNavigate);
        _moveAction.EnableAndSubscribe(OnMove);
        _lookAction.EnableAndSubscribe(OnLook);
        _jumpAction.EnableAndSubscribe(OnJump);
        _sprintAction.EnableAndSubscribe(OnSprint);
        _crouchAction.EnableAndSubscribe(OnCrouch);
        _moveSpeedAction.EnableAndSubscribe(OnToggleMoveSpeed);
        _playerInteractAction.EnableAndSubscribe(OnPlayerInteract);
        _robotInteractAction.EnableAndSubscribe(OnRobotInteract);
        _aimAction.EnableAndSubscribe(OnAim);

        
        // Subscribe to control scheme changes
        InputSystem.onDeviceChange += OnDeviceChange;
        
        
        
        CheckControlScheme();
        LoadCustomBindings();
        
    }
    
    private void OnDisable()
    {
        if (inputAsset == null) return;
        
        // Disable the action map or actions, and unsubscribe from the actions
        _toggleMenu.DisableAndUnsubscribe(OnToggleMenu);
        _navigateAction.DisableAndUnsubscribe(OnNavigate);
        _moveAction.DisableAndUnsubscribe(OnMove);
        _jumpAction.DisableAndUnsubscribe(OnJump);
        _lookAction.DisableAndUnsubscribe(OnLook);
        _sprintAction.DisableAndUnsubscribe(OnSprint);
        _crouchAction.DisableAndUnsubscribe(OnCrouch);
        _moveSpeedAction.DisableAndUnsubscribe(OnToggleMoveSpeed);
        _playerInteractAction.DisableAndUnsubscribe(OnPlayerInteract);
        _robotInteractAction.DisableAndUnsubscribe(OnRobotInteract);
        _aimAction.DisableAndUnsubscribe(OnAim);

        
        // Unsubscribe to control scheme changes
        InputSystem.onDeviceChange -= OnDeviceChange;
        
        
        
        SaveCustomBindings();
        
    }
    


    #region Control Scheme -----------------------------------------------------------------------------------------------------------------
    
    
    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Added || change == InputDeviceChange.Removed)
        {
            CheckControlScheme();
        }
    }
    
    
    private void CheckControlScheme()
    {
        ControlType newScheme = ControlType.KeyboardMouse; // Default scheme
        
        if (InputSystem.GetDevice<Gamepad>() != null)
        {
            newScheme = ControlType.Gamepad;
        }
        else if (InputSystem.GetDevice<Touchscreen>() != null)
        {
            newScheme = ControlType.Touch;
        }
        else if (InputSystem.GetDevice<XRController>() != null)
        {
            newScheme = ControlType.XR;
        }
        
        SetControlScheme(newScheme);
    }

    
    private void SetControlScheme(ControlType newScheme)
    {
        if (CurrentControlScheme == newScheme) return;
        
        CurrentControlScheme = newScheme;
        ControlSchemeChangedEvent?.Invoke(CurrentControlScheme);
        activeSettings = newScheme == ControlType.KeyboardMouse ? mouseKeyboardSettings : gamepadSettings;
        if (printDebug) Debug.Log($"Control Scheme changed to: {CurrentControlScheme}");
    }
    
    private void CheckInputControlScheme(InputAction.CallbackContext context)
    {
        // Only check for device changes on the 'started' phase
        if (!context.started)
            return;
            
        var device = context.control?.device;
        if (device == null)
            return;
            
        ControlType detectedScheme = CurrentControlScheme; // Default to current
        
        // Determine the control scheme based on the device used
        if (device is Keyboard || device is Mouse)
        {
            detectedScheme = ControlType.KeyboardMouse;
        }
        else if (device is Gamepad)
        {
            detectedScheme = ControlType.Gamepad;
        }
        else if (device is Touchscreen)
        {
            detectedScheme = ControlType.Touch;
        }
        else if (device is XRController)
        {
            detectedScheme = ControlType.XR;
        }
        
        // Update the control scheme if it changed
        if (detectedScheme != CurrentControlScheme)
        {
            SetControlScheme(detectedScheme);
        }
    }

    #endregion Control Scheme -----------------------------------------------------------------------------------------------------------------
    
    
    #region Input Actions -----------------------------------------------------------------------------------------------------------------

    private void OnToggleMenu(InputAction.CallbackContext context)
    {
        CheckInputControlScheme(context);
        ToggleMenuEvent?.Invoke(context);
    }
    
    private void OnNavigate(InputAction.CallbackContext context)
    {
        CheckInputControlScheme(context);
        NavigateEvent?.Invoke(context);
    }
    
    private void OnMove(InputAction.CallbackContext context)
    {
        CheckInputControlScheme(context);
        MoveEvent?.Invoke(context);
        CurrentMoveInput = context.ReadValue<Vector2>();
    }
    
    private void OnLook(InputAction.CallbackContext context)
    {
        CheckInputControlScheme(context);
        LookEvent?.Invoke(context);
        CurrentLookInput = context.ReadValue<Vector2>();
    }
    
    private void OnJump(InputAction.CallbackContext context)
    {
        CheckInputControlScheme(context);
        JumpEvent?.Invoke(context);
    }
    
    private void OnSprint(InputAction.CallbackContext context)
    {
        CheckInputControlScheme(context);
        SprintEvent?.Invoke(context);
    }
    
    private void OnCrouch(InputAction.CallbackContext context)
    {
        CheckInputControlScheme(context);
        CrouchEvent?.Invoke(context);
    }
    
    private void OnToggleMoveSpeed(InputAction.CallbackContext context)
    {
        CheckInputControlScheme(context);
        MoveSpeedEvent?.Invoke(context);
    }
    
    private void OnPlayerInteract(InputAction.CallbackContext context)
    {
        CheckInputControlScheme(context);
        PlayerInteractEvent?.Invoke(context);
    }
    
    private void OnRobotInteract(InputAction.CallbackContext context)
    {
        CheckInputControlScheme(context);
        RobotInteractEvent?.Invoke(context);
    }
    
    private void OnAim(InputAction.CallbackContext context)
    {
        CheckInputControlScheme(context);
        AimEvent?.Invoke(context);
    }
    

    #endregion Actions -----------------------------------------------------------------------------------------------------------------

    

    #region Custom bindings -----------------------------------------------------------------------------------------------------------------

    public void ResetAllCustomBindings()
    {
        foreach (InputActionMap actionMap in inputAsset.actionMaps)
        {
            actionMap.RemoveAllBindingOverrides();

        }
    }
    
    
    private void ResetControlSchemeBindings(string targetControlScheme)
    {
        foreach (InputActionMap actionMap in inputAsset.actionMaps)
        {
            foreach (InputAction action in actionMap.actions)
            {
                action.RemoveBindingOverride(InputBinding.MaskByGroup(targetControlScheme));
                // if (printDebug) { Debug.Log($"Reset bindings for {action.name} in scheme {targetControlScheme}"); }
            }
        }
    }
    
    
    public void ResetKeyboardBindings()
    {
        ResetControlSchemeBindings("Keyboard&Mouse");
    }

    public void ResetGamepadBindings()
    {
        ResetControlSchemeBindings("Gamepad");
    }


    private void SaveCustomBindings()
    {
        PlayerPrefs.SetString("CustomBindings", inputAsset.SaveBindingOverridesAsJson());
        PlayerPrefs.Save();
    }
    
    private void LoadCustomBindings()
    {
        if (PlayerPrefs.HasKey("CustomBindings"))
        {
            inputAsset.LoadBindingOverridesFromJson(PlayerPrefs.GetString("CustomBindings"));
        }
    }
    
    

    #endregion Custom bindings -----------------------------------------------------------------------------------------------------------------
    
    
    
    #region Controls Information -----------------------------------------------------------------------------------------------------------------
    
    public string GetBindingsText(string actionName, ControlType? controlType = null)
    {
        // Find the action
        InputAction action = inputAsset.FindAction(actionName);
        if (action == null)
        {
            Debug.LogError($"Action '{actionName}' not found in input asset");
            return string.Empty;
        }

        List<string> bindingTexts = new List<string>();

        // Go through all bindings for this action
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var binding = action.bindings[i];
            
            // Skip parts of composites (like individual WASD keys)
            if (binding.isPartOfComposite) continue;

            // Check if this binding matches the requested control scheme
            if (controlType.HasValue)
            {
                bool matchesScheme = false;
                
                switch (controlType.Value)
                {
                    case ControlType.KeyboardMouse:
                        // For keyboard/mouse, check for a keyboard path or empty groups
                        matchesScheme = binding.path.StartsWith("<Keyboard>") || 
                                      binding.path.StartsWith("<Mouse>") ||
                                      string.IsNullOrEmpty(binding.groups) ||
                                      binding.groups.Contains("KeyboardMouse");
                        break;
                    case ControlType.Gamepad:
                        matchesScheme = binding.groups.Contains("Gamepad");
                        break;
                    case ControlType.Touch:
                        matchesScheme = binding.groups.Contains("Touch");
                        break;
                    case ControlType.XR:
                        matchesScheme = binding.groups.Contains("XR");
                        break;
                }

                if (!matchesScheme) continue;
            }

            // Get the display string for this binding
            string displayString = action.GetBindingDisplayString(i, InputBinding.DisplayStringOptions.DontUseShortDisplayNames);
            if (string.IsNullOrEmpty(displayString)) continue;

            // Add a control scheme prefix if we're showing all schemes
            if (!controlType.HasValue)
            {
                string schemePrefix;
                // Check for keyboard bindings first
                if (binding.path.StartsWith("<Keyboard>") || string.IsNullOrEmpty(binding.groups))
                    schemePrefix = "[Keyboard] ";
                else if (binding.groups.Contains("Gamepad"))
                    schemePrefix = "[Gamepad] ";
                else if (binding.groups.Contains("Touch"))
                    schemePrefix = "[Touch] ";
                else if (binding.groups.Contains("XR"))
                    schemePrefix = "[VR] ";
                else
                    schemePrefix = "[Other] ";

                displayString = schemePrefix + displayString;
            }

            bindingTexts.Add(displayString);
        }

        return string.Join(" or ", bindingTexts);
    }

    public string GetCurrentSchemeBindings(string actionName)
    {
        return GetBindingsText(actionName, CurrentControlScheme);
    }

    

    
    #endregion Controls Information -----------------------------------------------------------------------------------------------------------------
    
    
    #region Player Settings --------------------------------------------------------------------------------------------
    
    [Button]
    private void ResetInputSettings()
    {
        mouseKeyboardSettings = mouseKeyboardDefaultSettings.Clone();
        gamepadSettings = gamepadDefaultSettings.Clone();
        activeSettings = CurrentControlScheme == ControlType.KeyboardMouse ? mouseKeyboardSettings : gamepadSettings;
    
        SaveSettings();
        onResetInputSettingEvent?.Invoke();
    }
    
    public void LoadSettings()
    {
        // Check if the settings files exist
        if (System.IO.File.Exists(Application.persistentDataPath + "/mouseKeyboardSettings.json"))
        {
            // Load the settings from JSON
            string mouseKeyboardSettingsJson = System.IO.File.ReadAllText(Application.persistentDataPath + "/mouseKeyboardSettings.json");
            mouseKeyboardSettings = JsonUtility.FromJson<InputSettings>(mouseKeyboardSettingsJson);
        }
        else
        {
            mouseKeyboardSettings = mouseKeyboardDefaultSettings.Clone();
        }
        
        if (System.IO.File.Exists(Application.persistentDataPath + "/gamepadSettings.json"))
        {
            // Load the settings from JSON
            string gamepadSettingsJson = System.IO.File.ReadAllText(Application.persistentDataPath + "/gamepadSettings.json");
            gamepadSettings = JsonUtility.FromJson<InputSettings>(gamepadSettingsJson);
        }
        else
        {
            gamepadSettings = gamepadDefaultSettings.Clone();
        }
        
        // Set the active settings based on the current control scheme
        if (CurrentControlScheme == ControlType.KeyboardMouse)
        {
            activeSettings = mouseKeyboardSettings;
        }
        else
        {
            activeSettings = gamepadSettings;
        }
    }
    
    public void SaveSettings()
    {
        
        // Save each setting to JSON
        string mouseKeyboardSettingsJson = JsonUtility.ToJson(mouseKeyboardSettings);
        string gamepadSettingsJson = JsonUtility.ToJson(gamepadSettings);
        // Save to file
        System.IO.File.WriteAllText(Application.persistentDataPath + "/mouseKeyboardSettings.json", mouseKeyboardSettingsJson);
        System.IO.File.WriteAllText(Application.persistentDataPath + "/gamepadSettings.json", gamepadSettingsJson);
    }
    
    public void SetAimCameraSensitivity(ControlType type, float value)
    {
        
        if (value < 0.1f || value > 10f)
        {
            Debug.LogError("Aim camera sensitivity must be between 0.1 and 10");
            return;
        }

        activeSettings.aimCameraSensitivity = value;
        if (type == ControlType.Gamepad)
        {
            gamepadSettings.aimCameraSensitivity = value;
        }
        else
        {
            mouseKeyboardSettings.aimCameraSensitivity = value;
        }
        
        SaveSettings();
    }
    
    public void SetFreeCameraSensitivity(ControlType type, float value)
    {
        if (value < 0.1f || value > 10f)
        {
            Debug.LogError("Free camera sensitivity must be between 0.1 and 10");
            return;
        }

        activeSettings.freeCameraSensitivity = value;
        if (type == ControlType.Gamepad)
        {
            gamepadSettings.freeCameraSensitivity = value;
        }
        else
        {
            mouseKeyboardSettings.freeCameraSensitivity = value;
        }
        
        SaveSettings();
    }
    
    public void SetToggleSprint(ControlType type, bool value)
    {
        activeSettings.toggleSprint = value;
        if (type == ControlType.Gamepad)
        {
            gamepadSettings.toggleSprint = value;
        }
        else
        {
            mouseKeyboardSettings.toggleSprint = value;
        }
        
        SaveSettings();
    }
    
    public void SetToggleCrouch(ControlType type, bool value)
    {
        activeSettings.toggleCrouch = value;
        if (type == ControlType.Gamepad)
        {
            gamepadSettings.toggleCrouch = value;
        }
        else
        {
            mouseKeyboardSettings.toggleCrouch = value;
        }
        
        SaveSettings();
    }
    
    public void SetToggleAimInput(ControlType type, bool value)
    {
        activeSettings.toggleAimInput = value;
        if (type == ControlType.Gamepad)
        {
            gamepadSettings.toggleAimInput = value;
        }
        else
        {
            mouseKeyboardSettings.toggleAimInput = value;
        }
        
        SaveSettings();
    }
    

    #endregion Player Settings --------------------------------------------------------------------------------------------



}


#region Extension  -----------------------------------------------------------------------------------------------------------------

public static class InputActionExtensions
{

    public static void EnableAndSubscribe(this InputAction action, Action<InputAction.CallbackContext> callback)
    {
        if (action == null) return;
        
        if (!action.enabled) action.Enable();
        action.started += callback;
        action.performed += callback;
        action.canceled += callback;
    }

    public static void DisableAndUnsubscribe(this InputAction action, Action<InputAction.CallbackContext> callback)
    {
        if (action == null) return;
        
        if (action.enabled) action.Disable();
        action.started -= callback;
        action.performed -= callback;
        action.canceled -= callback;
    }
}


public enum ControlType
{
    KeyboardMouse,
    Gamepad,
    Touch,
    XR  
}

#endregion Extension  -----------------------------------------------------------------------------------------------------------------