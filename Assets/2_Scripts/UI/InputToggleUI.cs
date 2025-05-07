using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Toggle))]
public class InputToggleUI : MonoBehaviour
{
    public enum ToggleSettingType
    {
        ToggleSprint,
        ToggleCrouch,
        ToggleAimInput
    }
    
    [SerializeField] private ControlType controlType;
    [SerializeField] private ToggleSettingType toggleType;
    [SerializeField] private SOInputReader inputReader;
    [SerializeField] private TextMeshProUGUI toggleLabel;
    [SerializeField] private string onText = "On";
    [SerializeField] private string offText = "Off";

    private Toggle _toggle;

    private void Awake()
    {
        _toggle = GetComponent<Toggle>();
    }
    
    private void OnEnable()
    {
        inputReader?.onResetInputSettingEvent?.AddListener(UpdateToggleValue);
    }
    
    private void OnDisable()
    {
        inputReader?.onResetInputSettingEvent?.RemoveListener(UpdateToggleValue);
    }

    private void Start()
    {
        SetupToggle();
    }

    private void SetupToggle()
    {
        if (_toggle == null || inputReader == null) return;

        // Set initial toggle value based on the specified control type
        UpdateToggleValue();

        // Add listeners
        _toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    private void UpdateToggleValue()
    {
        // Get the appropriate settings based on control type
        InputSettings settings = (controlType == ControlType.KeyboardMouse) 
            ? inputReader.MouseKeyboardSettings 
            : inputReader.GamepadSettings;
            
        bool isOn = false;
        
        switch (toggleType)
        {
            case ToggleSettingType.ToggleSprint:
                isOn = settings.toggleSprint;
                break;
                
            case ToggleSettingType.ToggleCrouch:
                isOn = settings.toggleCrouch;
                break;
            
            case ToggleSettingType.ToggleAimInput:
                isOn = settings.toggleAimInput;
                break;
        }
        
        _toggle.isOn = isOn;
        UpdateToggleText(isOn);
    }

    private void OnToggleValueChanged(bool isOn)
    {
        // Update the setting in the input reader using our specific control type
        switch (toggleType)
        {
            case ToggleSettingType.ToggleSprint:
                inputReader.SetToggleSprint(controlType, isOn);
                break;
                
            case ToggleSettingType.ToggleCrouch:
                inputReader.SetToggleCrouch(controlType, isOn);
                break;
                
            case ToggleSettingType.ToggleAimInput:
                inputReader.SetToggleAimInput(controlType, isOn);
                break;
        }

        // Update the display text
        UpdateToggleText(isOn);
    }

    private void UpdateToggleText(bool isOn)
    {
        if (toggleLabel != null)
        {
            toggleLabel.text = isOn ? onText : offText;
        }
    }
}