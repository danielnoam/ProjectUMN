using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem.XR;

/// <summary>
/// Scriptable Object that handles input system events and broadcasts them to listeners
/// create a scriptable object and connect the input asset to it
/// create a reference to the input reader and subscribe to the needed events in the mono behavior
/// </summary>
[CreateAssetMenu(fileName = "InputReader", menuName = "SO Manager/Input Reader")]
public class SOInputReader : ScriptableObject
{
    [SerializeField] private InputActionAsset inputAsset;
    [SerializeField] private bool printDebug;
    
    
    [Header("Keybind Icons")]
    public KeyboardIcons keyboardIcons;
    public GamepadIcons gamepadIcons;
    
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
        //if (printDebug) Debug.Log($"Control Scheme: {CurrentControlScheme}");
    }

    #endregion Control Scheme -----------------------------------------------------------------------------------------------------------------
    
    
    #region Input Actions -----------------------------------------------------------------------------------------------------------------

    private void OnToggleMenu(InputAction.CallbackContext context)
    {
        ToggleMenuEvent?.Invoke(context);
    }
    
    private void OnNavigate(InputAction.CallbackContext context)
    {
        NavigateEvent?.Invoke(context);
    }
    private void OnMove(InputAction.CallbackContext context)
    {
        MoveEvent?.Invoke(context);
        CurrentMoveInput = context.ReadValue<Vector2>();
    }
    
    private void OnLook(InputAction.CallbackContext context)
    {
        LookEvent?.Invoke(context);
        CurrentLookInput = context.ReadValue<Vector2>();
    }
    
    private void OnJump(InputAction.CallbackContext context)
    {
        JumpEvent?.Invoke(context);
    }
    
    private void OnSprint(InputAction.CallbackContext context)
    {
        SprintEvent?.Invoke(context);
    }
    
    private void OnCrouch(InputAction.CallbackContext context)
    {
        CrouchEvent?.Invoke(context);
    }
    
    private void OnToggleMoveSpeed(InputAction.CallbackContext context)
    {
        MoveSpeedEvent?.Invoke(context);
    }
    
    private void OnPlayerInteract(InputAction.CallbackContext context)
    {
        PlayerInteractEvent?.Invoke(context);
    }
    
    private void OnRobotInteract(InputAction.CallbackContext context)
    {
        RobotInteractEvent?.Invoke(context);
    }
    
    private void OnAim(InputAction.CallbackContext context)
    {
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
                        // For keyboard/mouse, check for keyboard path or empty groups
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

            // Add control scheme prefix if we're showing all schemes
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
    


#region Icons  -----------------------------------------------------------------------------------------------------------------


[Serializable] 
public struct GamepadIcons
{
    public Sprite buttonSouth;
    public Sprite buttonNorth;
    public Sprite buttonEast;
    public Sprite buttonWest;
    public Sprite startButton;
    public Sprite selectButton;
    public Sprite leftTrigger;
    public Sprite rightTrigger;
    public Sprite leftShoulder;
    public Sprite rightShoulder;
    public Sprite dpad;
    public Sprite dpadUp;
    public Sprite dpadDown;
    public Sprite dpadLeft;
    public Sprite dpadRight;
    public Sprite leftStick;
    public Sprite rightStick;
    public Sprite leftStickPress;
    public Sprite rightStickPress;

    public Sprite GetSprite(string controlPath)
    {
        // From the input system, we get the path of the control on device. So we can just
        // map from that to the sprites we have for gamepads.
        switch (controlPath)
        {
            case "buttonSouth": return buttonSouth;
            case "buttonNorth": return buttonNorth;
            case "buttonEast": return buttonEast;
            case "buttonWest": return buttonWest;
            case "start": return startButton;
            case "select": return selectButton;
            case "leftTrigger": return leftTrigger;
            case "rightTrigger": return rightTrigger;
            case "leftShoulder": return leftShoulder;
            case "rightShoulder": return rightShoulder;
            case "dpad": return dpad;
            case "dpad/up": return dpadUp;
            case "dpad/down": return dpadDown;
            case "dpad/left": return dpadLeft;
            case "dpad/right": return dpadRight;
            case "leftStick": return leftStick;
            case "rightStick": return rightStick;
            case "leftStickPress": return leftStickPress;
            case "rightStickPress": return rightStickPress;
        }
        return null;
    }
}

[Serializable]
public struct KeyboardIcons
{
    // Letter keys
    public Sprite keyA;
    public Sprite keyB;
    public Sprite keyC;
    public Sprite keyD;
    public Sprite keyE;
    public Sprite keyF;
    public Sprite keyG;
    public Sprite keyH;
    public Sprite keyI;
    public Sprite keyJ;
    public Sprite keyK;
    public Sprite keyL;
    public Sprite keyM;
    public Sprite keyN;
    public Sprite keyO;
    public Sprite keyP;
    public Sprite keyQ;
    public Sprite keyR;
    public Sprite keyS;
    public Sprite keyT;
    public Sprite keyU;
    public Sprite keyV;
    public Sprite keyW;
    public Sprite keyX;
    public Sprite keyY;
    public Sprite keyZ;

    // Number keys
    public Sprite key1;
    public Sprite key2;
    public Sprite key3;
    public Sprite key4;
    public Sprite key5;
    public Sprite key6;
    public Sprite key7;
    public Sprite key8;
    public Sprite key9;
    public Sprite key0;

    // Special keys
    public Sprite keySpace;
    public Sprite keyEnter;
    public Sprite keyEscape;
    public Sprite keyTab;
    public Sprite keyBackspace;
    public Sprite keyDelete;
    public Sprite keyShift;
    public Sprite keyCtrl;
    public Sprite keyAlt;
    
    // Arrow keys
    public Sprite keyArrowUp;
    public Sprite keyArrowDown;
    public Sprite keyArrowLeft;
    public Sprite keyArrowRight;

    // Mouse
    public Sprite mouseLeft;
    public Sprite mouseRight;
    public Sprite mouseMiddle;
    public Sprite mouseWheel;

    public Sprite GetSprite(string controlPath)
    {
        // Remove the device prefix if present
        string key = controlPath.Replace("<Keyboard>/", "").Replace("<Mouse>/", "");
        
        switch (key.ToLower())
        {
            // Letters
            case "a": return keyA;
            case "b": return keyB;
            case "c": return keyC;
            case "d": return keyD;
            case "e": return keyE;
            case "f": return keyF;
            case "g": return keyG;
            case "h": return keyH;
            case "i": return keyI;
            case "j": return keyJ;
            case "k": return keyK;
            case "l": return keyL;
            case "m": return keyM;
            case "n": return keyN;
            case "o": return keyO;
            case "p": return keyP;
            case "q": return keyQ;
            case "r": return keyR;
            case "s": return keyS;
            case "t": return keyT;
            case "u": return keyU;
            case "v": return keyV;
            case "w": return keyW;
            case "x": return keyX;
            case "y": return keyY;
            case "z": return keyZ;

            // Numbers
            case "1": return key1;
            case "2": return key2;
            case "3": return key3;
            case "4": return key4;
            case "5": return key5;
            case "6": return key6;
            case "7": return key7;
            case "8": return key8;
            case "9": return key9;
            case "0": return key0;

            // Special keys
            case "space": return keySpace;
            case "enter": return keyEnter;
            case "escape": return keyEscape;
            case "tab": return keyTab;
            case "backspace": return keyBackspace;
            case "delete": return keyDelete;
            case "leftshift":
            case "rightshift": return keyShift;
            case "leftctrl":
            case "rightctrl": return keyCtrl;
            case "leftalt":
            case "rightalt": return keyAlt;

            // Arrow keys
            case "uparrow": return keyArrowUp;
            case "downarrow": return keyArrowDown;
            case "leftarrow": return keyArrowLeft;
            case "rightarrow": return keyArrowRight;

            // Mouse
            case "leftbutton": return mouseLeft;
            case "rightbutton": return mouseRight;
            case "middlebutton": return mouseMiddle;
            case "scroll": return mouseWheel;
        }
        
        return null;
    }
}

#endregion Icons  -----------------------------------------------------------------------------------------------------------------


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



