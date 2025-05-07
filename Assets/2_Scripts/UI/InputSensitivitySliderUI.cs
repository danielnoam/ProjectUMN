using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class InputSensitivitySliderUI : MonoBehaviour
{
    public enum SensitivityType
    {
        FreeCameraSensitivity,
        AimCameraSensitivity
    }
    
    [SerializeField] private ControlType controlType;
    [SerializeField] private SensitivityType sensitivityType;
    [SerializeField] private SOInputReader inputReader;
    [SerializeField] private TextMeshProUGUI sliderValueText;

    private Slider _slider;

    private void Awake()
    {
        _slider = GetComponent<Slider>();
    }

    private void OnEnable()
    {
        inputReader?.onResetInputSettingEvent?.AddListener(UpdateSliderValue);
    }
    
    private void OnDisable()
    {
        inputReader?.onResetInputSettingEvent?.RemoveListener(UpdateSliderValue);
    }

    private void Start()
    {
        SetupSlider();
    }

    private void SetupSlider()
    {
        if (_slider == null || inputReader == null) return;
        
        // Set the min and max value from the input reader's settings
        // Using the Range attributes defined in the InputSettings class
        SetSliderMinMaxValues();
        
        // Set initial slider value based on the specified control type
        UpdateSliderValue();

        // Add listeners
        _slider.onValueChanged.AddListener(OnSliderValueChanged);
    }
    
    private void SetSliderMinMaxValues()
    {
        // Looking at your InputSettings class, there are Range attributes for sensitivities
        // The AimCameraSensitivity has [Range(0.1f, 10f)]
        // So we'll use those values for the slider
        switch (sensitivityType)
        {
            case SensitivityType.FreeCameraSensitivity:
                _slider.minValue = 0.1f; // Based on the Range attribute in InputSettings
                _slider.maxValue = 10f;  // Based on the Range attribute in InputSettings
                break;
                
            case SensitivityType.AimCameraSensitivity:
                _slider.minValue = 0.1f; // Based on the Range attribute in InputSettings
                _slider.maxValue = 10f;  // Based on the Range attribute in InputSettings
                break;
        }
    }

    private void UpdateSliderValue()
    {
        float value = 1f;
        
        // Get value from the appropriate settings based on control type
        InputSettings settings = (controlType == ControlType.KeyboardMouse) 
            ? inputReader.MouseKeyboardSettings 
            : inputReader.GamepadSettings;
        
        switch (sensitivityType)
        {
            case SensitivityType.FreeCameraSensitivity:
                value = settings.freeCameraSensitivity;
                break;
                
            case SensitivityType.AimCameraSensitivity:
                value = settings.aimCameraSensitivity;
                break;
        }
        
        _slider.value = value;
        UpdateSliderText(value);
    }

    private void OnSliderValueChanged(float value)
    {
        // Update the setting in the input reader using our specific control type
        switch (sensitivityType)
        {
            case SensitivityType.FreeCameraSensitivity:
                inputReader.SetFreeCameraSensitivity(controlType, value);
                break;
                
            case SensitivityType.AimCameraSensitivity:
                inputReader.SetAimCameraSensitivity(controlType, value);
                break;
        }

        // Update the display text
        UpdateSliderText(value);
    }

    private void UpdateSliderText(float value)
    {
        if (sliderValueText != null)
        {
            sliderValueText.text = $"{value:F1}x";
        }
    }
}